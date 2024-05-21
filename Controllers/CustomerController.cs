using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly customerBLL _customer;

        public CustomerController(IDbContextFactory dbContextFactory)
        {
            _customer = new customerBLL(dbContextFactory);
        }

        [HttpPost("AddCustomer")]
        public async Task<ActionResult<CustomerModel>> AddCustomer([FromBody] CustomerModel customer, [FromQuery] string connectionString)
        {
            if (customer == null)
            {
                return BadRequest();
            }

            CustomerModel result = await _customer.AddCustomer(customer, connectionString);
            return result;
        }

        [HttpGet("GetCustomerByEmail/{email}")]
        public async Task<ActionResult<CustomerModel>> GetCustomerByEmail(string email, [FromQuery] string connectionString)
        {
            return await _customer.GetCustomerByEmail(email, connectionString);
        }

        [HttpGet("GetCustomerByEmail")]
        public async Task<ActionResult<IEnumerable<CustomerModel>>> GetCustomers([FromQuery] string connectionString)
        {
            return await _customer.GetCustomers(connectionString);
        }
    }
}
