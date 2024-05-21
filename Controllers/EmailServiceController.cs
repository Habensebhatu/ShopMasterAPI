using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailServiceController : ControllerBase
    {
        private readonly IEmailServiceAit emailService;


        public EmailServiceController(IEmailServiceAit service)
        {
            emailService = service;
        }

        [HttpPost("ConfirmationReceive")]
        public async Task<IActionResult> ConfirmationReceive([FromBody] mailRequestModelAit contactUsRequest)
        {
            contactUsRequest.OrderDate = DateTime.Now;

            await emailService.SendEmailAsyncAit(contactUsRequest);

            return Ok();
        }
    }
}
