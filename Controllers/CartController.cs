using System.Security.Claims;
using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class CartController : ControllerBase
    {
        private readonly CartBLL _cartBLL;
       
        public CartController(IDbContextFactory dbContextFactory)
        {
            _cartBLL = new CartBLL(dbContextFactory);
        }

        [HttpPost("AddCartItem")]
        public async Task<ActionResult<CartModel>> AddToCart([FromBody] CartModel cart, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = _cartBLL.GetDefaultUserId(connectionString);  
            }

            if (cart == null)
            {
                return BadRequest();
            }
            CartModel result = await _cartBLL.AddCart(cart, cart.sessionId, userId, connectionString);
            return result;
        }



        [HttpGet("GetCartItems")]
        public async Task<ActionResult<List<CartModel>>> GetCart(string sessionId, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = null;
            }
            var cart = await _cartBLL.GetCart(sessionId, userId, connectionString);
            if (cart == null)
            {
                return NotFound();
            }
            return cart;
        }

        [HttpDelete("ClearAllCartItems")]
        public async Task<IActionResult> ClearCart(string sessionId, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _cartBLL.ClearCart(sessionId, userId, connectionString);
            return Ok(new { message = "Cart Cleared" });
        }


        [HttpDelete("RemoveFromCart/{productId}")]
        public async Task<IActionResult> RemoveFromCart(Guid productId, [FromQuery] string sessionId, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId) && string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "Either User Id or Session Id is required." });

            await _cartBLL.RemoveCart(productId, userId, sessionId, connectionString);
            return Ok(new { message = "Product removed from cart" });
        }


        [HttpPut("UpdateProductQuantity/{productId}")]
        public async Task<ActionResult<CartModel>> UpdateProductQuantity(Guid productId, [FromQuery] string sessionId, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var updatedProduct = await _cartBLL.UpdateCartQuantity(productId, userId, sessionId, connectionString);
            if (updatedProduct == null)
            {
                return NotFound();
            }
            return Ok(updatedProduct);
        }

    }
}
