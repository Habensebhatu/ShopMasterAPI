using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using business_logic_layer;
using IdGen;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StripeController : ControllerBase
    {
        private readonly OrderBLL _orderBLL;
        private readonly ProductBLL _productBLL;
        private readonly customerBLL _customer;
        private readonly IEmailService _emailService;
        private readonly string? _endpointSecret;
        public StripeController(IEmailService emailService, IDbContextFactory dbContextFactory, IConfiguration configuration)
        {

            StripeConfiguration.ApiKey = configuration["StripeSettings:ApiKey"];
            _orderBLL = new OrderBLL(dbContextFactory);
            _productBLL = new ProductBLL(dbContextFactory);
            _customer = new customerBLL(dbContextFactory);
            _emailService = emailService;
            _endpointSecret = configuration["StripeSettings:EndpointSecret"];
        }

        [HttpPost("checkout")]
        public ActionResult Create([FromBody] CheckoutRequestModel request)
        {

            var lineItems = new List<SessionLineItemOptions>();
            decimal totalWeight = 0;

            foreach (var item in request.Items)
            {

                if (item.Kilo != null)
                {
                    totalWeight += (item.Kilo ?? 0) * item.Quantity;
                }
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long?)(item.Price * 100),
                        Currency = "eur",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Title,
                            Images = new List<string> { item.ImageUrl }
                        },

                    },
                    Quantity = item.Quantity,


                });
            }


            decimal shippingCost;


            if (totalWeight <= 10)
            {
                shippingCost = 0;
            }
            else if (totalWeight <= 23)
            {
                shippingCost = 13.90M;
            }
            else if (totalWeight <= 33)
            {
                shippingCost = 21.55M;
            }
            else
            {
                shippingCost = 27.80M;

            }



            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long?)(shippingCost * 100),

                    Currency = "eur",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Shipping",
                    },
                },
                Quantity = 1,
            });



            var options = new SessionCreateOptions
            {
                BillingAddressCollection = "required",

                PaymentMethodTypes = new List<string>

                {
                     "card",
                     "ideal",
                },
                PhoneNumberCollection = new SessionPhoneNumberCollectionOptions
                {
                    Enabled = true,
                },

                LineItems = lineItems,
                Mode = "payment",

                ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                {
                    AllowedCountries = new List<string> { "NL" },
                },

                CustomText = new SessionCustomTextOptions
                {
                    ShippingAddress = new SessionCustomTextShippingAddressOptions
                    {
                        Message = "Please note that we can't guarantee 2-day delivery for PO boxes at this time.",
                    },
                    Submit = new SessionCustomTextSubmitOptions
                    {
                        Message = "We'll email you instructions on how to get started.",
                    },
                },
                SuccessUrl = "https://sofanimarket.com/payment-success",
                CancelUrl = "https://sofanimarket.com/home",

            };


            var service = new SessionService();
            Session session;
            try
            {
                session = service.Create(options);
            }
            catch (StripeException e)
            {
                return BadRequest(e.StripeError.Message);
            }

            return Ok(new { id = session.Id });
        }


        //const string endpointSecret = _configuration["StripeSettings:EndpointSecret"];


        [HttpPost("webhook")]
        public async Task<IActionResult> Index()
        {

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignatureHeader = Request.Headers["Stripe-Signature"];

            if (string.IsNullOrEmpty(stripeSignatureHeader))
            {
                return BadRequest("Stripe-Signature header is missing.");
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignatureHeader, _endpointSecret);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session == null)
                    {
                        return BadRequest("Unexpected event type.");
                    }


                    var service = new Stripe.Checkout.SessionService();
                    var sessionWithLineItems = service.Get(session.Id, new Stripe.Checkout.SessionGetOptions
                    {
                        Expand = new List<string> { "line_items" }
                    });

                    var customerPhone = session.CustomerDetails.Phone;
                    string customerEmail = session.CustomerDetails?.Email;
                    string paymentMethodType = null;

                    
                    if (string.IsNullOrEmpty(customerEmail) && !string.IsNullOrEmpty(session.PaymentIntentId))
                    {
                        var paymentIntentService = new PaymentIntentService();
                        var paymentIntent = paymentIntentService.Get(session.PaymentIntentId);
                        customerEmail = paymentIntent.ReceiptEmail;

                        var paymentMethodService = new PaymentMethodService();
                        var paymentMethod = paymentMethodService.Get(paymentIntent.PaymentMethodId);

                        // Get the Type
                        paymentMethodType = paymentMethod.Type;
                        Console.WriteLine($"Payment method type used: {paymentMethodType}");

                    }

                    if (string.IsNullOrEmpty(customerEmail) && session.CustomerId != null)
                    {
                        var customerService = new CustomerService();
                        var customer = customerService.Get(session.CustomerId);
                        customerEmail = customer.Email;
                    }

              

                    var shippingDetails = session.ShippingDetails;
                    var shippingAddress = shippingDetails.Address;
                    var existingCustomer = await _customer.GetCustomerByEmail(customerEmail, "SofaniMarket");
                    if (existingCustomer == null && shippingDetails != null)
                    {

                        
                        var newCustomer = new CustomerModel
                        {
                            CustomerId = Guid.NewGuid(),
                            CustomerEmail = customerEmail,
                            recipientName = shippingDetails.Name,
                            city = shippingAddress.City,
                            phoneNumber = customerPhone,
                            line1 = shippingAddress.Line1,
                            postalCode = shippingAddress.PostalCode

                        };
                        existingCustomer = await _customer.AddCustomer(newCustomer, "SofaniMarket");
                    }

                    if (existingCustomer == null)
                    {
                        
                        return BadRequest("Unable to create or fetch the customer.");
                    }
                    var generator = new IdGenerator(0);
                    long uniqueId = generator.CreateId() % 100000000;

                    OrderModel orderModel = new OrderModel
                    {
                        CustomerId = existingCustomer.CustomerId,
                        OrderNumber = uniqueId,
                        OrderDetails = new List<OrderDetailModel>()
                    };

                   
                    mailRequestModel mailRequest = new mailRequestModel();
                    mailRequest.CustomerName = customerEmail;
                    mailRequest.recipientName = shippingDetails.Name;
                    mailRequest.city = shippingAddress.City;
                    mailRequest.line1 = shippingAddress.Line1;
                    mailRequest.postalCode = shippingAddress.PostalCode;
                    mailRequest.OrderDate = DateTime.Now;
                    mailRequest.OrderNummer = uniqueId;
                    mailRequest.paymentMethodType = paymentMethodType;
                    foreach (var lineItem in sessionWithLineItems.LineItems)
                    {

                        if (lineItem.Description == "Shipping") 
                        {
                
                            continue; 
                        }

                        StripeImage product = await _productBLL.GetProductsByProductName(lineItem.Description, "SofaniMarket");


                        orderModel.OrderDetails.Add(new OrderDetailModel
                        {
                            ProductId = product.productId,
                            Quantity = (int)lineItem.Quantity,
                            AmountTotal = (decimal)lineItem.AmountTotal

                        });
                        Console.WriteLine($"lineItem.AmountTotal: {lineItem.AmountTotal}");
                        mailRequest.OrderItems.Add(new OrderItemModel
                        {
                            ProductName = lineItem.Description,
                            Quantity = (int)lineItem.Quantity,
                            Price = (decimal)lineItem.AmountTotal / (decimal)lineItem.Quantity / 100,
                            Total = (decimal)lineItem.AmountTotal / 100,
                            ImageUrl = product.ImageUrls

                        });
                    }

                    await _orderBLL.AddOrder(orderModel, "SofaniMarket");
                    await _emailService.SendEmailAsync(mailRequest);
                }
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(e.Message);
            }
        }


    }
}

