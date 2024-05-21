using System.Threading.Tasks;
using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Stripe;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductBLL _productBLL;

        public ProductController(IDbContextFactory dbContextFactory)
        {
            _productBLL = new ProductBLL(dbContextFactory);
        }

        [HttpPost("AddProduct")]
        public async Task<ActionResult<productModel>> AddProduct([FromForm] List<IFormFile> files, [FromForm] string product, [FromQuery] string connectionString)
        {
            productModel productData = JsonConvert.DeserializeObject<productModel>(product);

            if (productData == null)
            {
                return BadRequest();
            }
            Console.WriteLine($"Number of received files: {files.Count}");
            productData.NewImages = files;
            productModel result = await _productBLL.AddProduct(productData, connectionString);

            return result;
        }

        [HttpGet("ByCategory/{category}")]
        public async Task<ActionResult<List<productModelS>>> GetProductsByName(string category, [FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string connectionString)
        {
            var product = await _productBLL.GetProductsByName(category, pageNumber, pageSize, connectionString);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        [HttpGet("AllProducts")]
        public async Task<ActionResult<List<productModelS>>> GetProducts([FromQuery] string connectionString)
        {
            var products = await _productBLL.GetProducts(connectionString);
            return products;
        }

        [HttpGet("PageNumber")]
        public async Task<ActionResult<List<productModelS>>> GetProductsPageNumber([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string connectionString)
        {
            var product = await _productBLL.GetProductsPageNumber(pageNumber, pageSize, connectionString);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }


        [HttpGet("ByCategory/{category}/ByPriceRange")]
        public async Task<ActionResult<List<productModelS>>> GetProductsByNameAndPrice(
     string category,
     [FromQuery] decimal minPrice,
     [FromQuery] decimal? maxPrice,
     [FromQuery] int pageNumber,
     [FromQuery] int pageSize, [FromQuery] string connectionString)
        {
            var product = await _productBLL.GetProductsByNameAndPrice(category, minPrice, maxPrice, pageNumber, pageSize, connectionString);
            if (product == null || !product.Any())
            {
                return NotFound();
            }
            return product;
        }


        [HttpGet("ById/{id:guid}")]
        public async Task<ActionResult<productModelS>> GetProductById(Guid id, [FromQuery] string connectionString)
        {
            return await _productBLL.GetProductById(id, connectionString);
        }

        [HttpGet("ByName/{product}")]
        public async Task<StripeImage> GetProductsByProductName(string product, [FromQuery] string connectionString)
        {
            return await _productBLL.GetProductsByProductName(product, connectionString);
        }


        [HttpGet("ByPriceRange/{min}/{max}")]
        public async Task<ActionResult<List<StripeImage>>> fillterPrice([FromRoute] Decimal min, [FromRoute] Decimal max, [FromQuery] string connectionString)
        {
            var product = await _productBLL.fillterPrice(min, max, connectionString);
            if (product == null)
            {
                return NotFound();
            }
            return product;
        }

        [HttpGet("SearchByName/{productName}")]
        public async Task<ActionResult<List<productModelS>>> SearchProductsByProductName(string productName, [FromQuery] string connectionString)
        {
            var product = await _productBLL.SearchProductsByProductName(productName, connectionString);
            return product;
        }



        [HttpDelete("RemoveProduct/{id}")]
        public async Task<IActionResult> RemoveProduct(Guid id, [FromQuery] string connectionString)
        {
            await _productBLL.RemoveProduct(id, connectionString);
            return NoContent();
        }

        [HttpPut("UpdateProduct")]
        public async Task<ActionResult<productModel>> UpdateProduct([FromForm] List<IFormFile> newImages, [FromForm] List<int> newImageIndices, [FromForm] string product, [FromForm] string existingImages, [FromQuery] string connectionString)
        {
            productModel productData = JsonConvert.DeserializeObject<productModel>(product);
            List<ExistingImageUrlModel> existingImageUrls = JsonConvert.DeserializeObject<List<ExistingImageUrlModel>>(existingImages);

            if (productData == null || productData.ProductId == Guid.Empty)
            {
                return BadRequest();
            }
            productData.NewImages = newImages;
            productData.NewImageIndices = newImageIndices;
            productData.ExistingImageUrls = existingImageUrls;

            productModel result = await _productBLL.UpdateProduct(productData, connectionString);

            return result;
        }


        [HttpGet("PopularProducts")]
        public async Task<ActionResult<List<productModelS>>> GetPopularProducts([FromQuery] string connectionString)
        {
            var popularProducts = await _productBLL.GetPopularProducts(connectionString);
            return Ok(popularProducts);
        }

        [HttpGet("GetProductsByCategory/{category}")]
        public async Task<ActionResult<List<productModelS>>> GetProductsByCategory(string category, [FromQuery] string connectionString)
        {
            var popularProducts = await _productBLL.GetProductsByCategory(category, connectionString);
            return Ok(popularProducts);
        }

        public class StockUpdateModel
        {
            public Guid ProductId { get; set; }
            public int NewStock { get; set; }
            public decimal price { get; set; }
        }

        [HttpPost("UpdateProductStock")]
        public async Task<IActionResult> UpdateProductStock([FromBody] StockUpdateModel model, [FromQuery] string connectionString)
        {
            try
            {
                await _productBLL.UpdateProductStock(model.ProductId, model.NewStock, model.price, connectionString);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating product stock.");
            }
        }

    }
}

