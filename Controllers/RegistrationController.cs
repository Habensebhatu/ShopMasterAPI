using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;


namespace API.Controllers_
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private readonly UserRegistrationBLL _userRegistrationBLL;
        private readonly IConfiguration _configuration;
        private readonly ISendRegistration _sendRegistration;

        public RegistrationController(IDbContextFactory dbContextFactory, IConfiguration configuration, ISendRegistration sendRegistration)
        {
            _configuration = configuration;
            _userRegistrationBLL = new UserRegistrationBLL(dbContextFactory, _configuration);
            _sendRegistration = sendRegistration;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserRegistrationModel userModel, [FromQuery] string connectionString)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string token = await _userRegistrationBLL.RegisterUser(userModel, connectionString);
            if (connectionString == "Aitmaten")
            {
                if (token != null)
                {
                    var registrationInfo = new UserRegistrationModel
                    {
                        FirstName = $"{userModel.FirstName} {userModel.LastName}",
                        Email = userModel.Email,
                        BedrijfsNaam = userModel.BedrijfsNaam,
                        KvkNummer = userModel.KvkNummer,


                    };
                    await _sendRegistration.SendRegistrationPendingEmail(registrationInfo);
                }
            }
            
            return Ok(new { token = token });
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(Login loginModel, string connectionString)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string token = await _userRegistrationBLL.LoginUser(loginModel, connectionString);

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            return Ok(new { token = token });
        }

        [HttpGet("get-all-users")]
        public async Task<ActionResult<List<UserRegistrationModel>>> GetAllUsers([FromQuery] string connectionString)
        {
            var users = await _userRegistrationBLL.GetAllUsers(connectionString);
            return Ok(users);
        }

        [HttpGet("get-user-by-id/{userId}")]
        public async Task<ActionResult<UserRegistrationModel>> GetUserById(Guid userId, [FromQuery] string connectionString)
        {
            var user = await _userRegistrationBLL.GetUserById(userId, connectionString);
            if (user == null)
            {
                return NotFound("User not found.");
            }
            return Ok(user);
        }



        [HttpPost("approve-user/{userId}")]
        public async Task<IActionResult> ApproveUser(Guid userId, [FromQuery] string connectionString)
        {

            var user = await _userRegistrationBLL.ApproveUser(userId, connectionString);

            await _sendRegistration.SendAccountActivatedEmail(new UserRegistrationModel
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BedrijfsNaam = user.BedrijfsNaam
            });
            if (user != null)
                return Ok();
            else
                return NotFound("User not found.");
        }

        [HttpPost("reject-user/{userId}")]
        public async Task<IActionResult> RejectUser(Guid userId, [FromQuery] string connectionString)
        {
            bool result = await _userRegistrationBLL.RejectUser(userId, connectionString);
            if (result)
                return Ok();
            else
                return NotFound("User not found.");
        }

    }
}
