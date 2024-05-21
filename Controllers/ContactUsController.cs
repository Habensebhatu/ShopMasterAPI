using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactUsController : ControllerBase
    {
        private readonly IEmailContactUs emailService;

        public ContactUsController(IEmailContactUs service)
        {
            this.emailService = service;
        }

        [HttpPost("SubmitContactRequest")]
        public async Task<IActionResult> SubmitContactRequest([FromBody] MailContactUS contactUsRequest, [FromQuery] string connectionString)
        {
            MailContactUS mailrequest = new MailContactUS
            {
                Email = contactUsRequest.Email,
                Name = contactUsRequest.Name,
                Body = contactUsRequest.Body,
                Telephone = contactUsRequest.Telephone
            };
            await emailService.SendEmailAsync(mailrequest, connectionString);

            return Ok();
        }
    }
}
