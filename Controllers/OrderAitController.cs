using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderAitController : ControllerBase
    {
        private readonly OrderBLLAit _orderBLL;

        public OrderAitController(IDbContextFactory dbContextFactory)
        {
            _orderBLL = new OrderBLLAit(dbContextFactory);
        }

        [HttpPost("AddOrder")]
        public async Task<ActionResult<OrderModelAdd>> addProduct([FromBody] OrderModelAdd orderModel, [FromQuery] string connectionString)
        {
           
            if (orderModel == null)
            {
                return BadRequest();
            }

            OrderModelAdd result = await _orderBLL.AddOrder(orderModel, connectionString);


            return result;
        }

        [HttpGet("AllOrders")]
        public async Task<ActionResult<List<OrderModelAit>>> GetOrders([FromQuery] string connectionString)
        {
            List<OrderModelAit> orderModels = await _orderBLL.GetOrders(connectionString);

            if (orderModels == null || !orderModels.Any())
                return NotFound();

            return orderModels;
        }

        [HttpGet("GetOrderById/{id}")]
        public async Task<ActionResult<GetOrderModelAit>> GetOrderById(Guid id, [FromQuery] string connectionString)
        {
            GetOrderModelAit orderModel = await _orderBLL.GetOrderById(id, connectionString);

            if (orderModel == null)
                return NotFound();

            return orderModel;
        }
    }
}
