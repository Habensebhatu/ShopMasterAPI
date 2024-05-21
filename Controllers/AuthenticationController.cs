using Microsoft.AspNetCore.Mvc;
using business_logic_layer;
using business_logic_layer.ViewModel;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly LoginBLL _loginBLL;

        public AuthenticationController(IDbContextFactory dbContextFactory)
        {
            _loginBLL = new LoginBLL(dbContextFactory);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginModel>> Login([FromBody] LoginModel model)
        {
            LoginModel user = await _loginBLL.Authenticate(model.username, model.password);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid email or password" });
            }

            return user;
        }
    }
}
