using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryBLL _categoryBLL;

        public CategoryController(IDbContextFactory dbContextFactory)
        {
            _categoryBLL = new CategoryBLL(dbContextFactory);
        }

        [HttpPost("AddCategory")]
        public async Task<ActionResult<CategoryModel>> AddCategory([FromBody] CategoryModel category, [FromQuery] string connectionString)
        {
            if (category == null) return BadRequest();

            var result = await _categoryBLL.AddCategory(category, connectionString);
            return result != null ? Ok(result) : BadRequest();
        }

        [HttpGet("GetAllCategories")]
        public async Task<ActionResult<IEnumerable<CategoryModel>>> GetCategories([FromQuery] string connectionString)
        {
            return Ok(await _categoryBLL.GetCategories(connectionString));
        }

        [HttpGet("GetCategoryById/{id}")]
        public async Task<ActionResult<CategoryModel>> GetCategoryById(Guid id, [FromQuery] string connectionString)
        {
            var result = await _categoryBLL.GetCategoryById(id, connectionString);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpGet("GetCategoryByName/{name}")]
        public async Task<ActionResult<CategoryModel>> GetCategoryByName(string name, [FromQuery] string connectionString)
        {
            var result = await _categoryBLL.GetCategoryByName(name, connectionString);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpDelete("DeleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id, [FromQuery] string connectionString)
        {
            var result = await _categoryBLL.RemoveCategory(id, connectionString);
            return result ? NoContent() : NotFound();
        }

        [HttpPut("UpdateCategory/{id}")]
        public async Task<ActionResult<CategoryModel>> UpdateCategory(Guid id, [FromBody] CategoryModel category, [FromQuery] string connectionString)
        {
            var updatedCategory = await _categoryBLL.UpdateCategory(id, category, connectionString);
            return updatedCategory != null ? Ok(updatedCategory) : NotFound();
        }
    }
}
