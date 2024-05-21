using System.Security.Claims;
using System.Threading.Tasks;
using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly WishlistBLL _wishlistBLL;

        public WishlistController(IDbContextFactory dbContextFactory)
        {
            _wishlistBLL = new WishlistBLL(dbContextFactory);
        }

        [Authorize]
        [HttpPost("AddToWishList")]
        public async Task<ActionResult<WishlistModel>> AddToWishList(AddToWishlistRequest request, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var productId = request.ProductId;

            if (string.IsNullOrWhiteSpace(userId) || productId == null)
            {
                return BadRequest();
            }
            if (!Guid.TryParse(productId, out var parsedProductId))
            {
                return BadRequest("Invalid productId format");
            }
            return await _wishlistBLL.AddProductToWishlist(parsedProductId, userId, connectionString);
        }

        [Authorize]
        [HttpGet("GetWishlistProducts")]
        public async Task<ActionResult<List<productModelS>>> GetWishlistProducts([FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("Invalid userId");
            }

            List<productModelS> products = await _wishlistBLL.GetWishlistProducts(userId, connectionString);

            return Ok(products);
        }

        [Authorize]
        [HttpDelete("DeleteFromWishlist/{productId}")]
        public async Task<ActionResult<bool>> DeleteWishListProductByID(Guid productId, [FromQuery] string connectionString)
        {
            ClaimsPrincipal currentUser = this.User;
            string userId = currentUser?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId) || productId == Guid.Empty)
            {
                return BadRequest("Invalid input");
            }

            var result = await _wishlistBLL.DeleteProductFromWishlist(productId, userId, connectionString);
            if (result)
            {
                return result;
            }
            else
            {
                return NotFound("Product not found in wishlist.");
            }
        }


    }
}
