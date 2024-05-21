using business_logic_layer;
using business_logic_layer.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly OrderBLL _orderBLL;

        public OrderController(IDbContextFactory dbContextFactory)
        {
            _orderBLL = new OrderBLL(dbContextFactory);
        }

        [HttpPost("AddOrder")]
        public async Task<ActionResult<OrderModel>> AddOrder([FromBody] OrderModel orderModel, [FromQuery] string connectionString)
        {
            if (orderModel == null)
            {
                return BadRequest();
            }

            OrderModel result = await _orderBLL.AddOrder(orderModel, connectionString);
            return result;
        }

        [HttpGet("AllOrders")]
        public async Task<ActionResult<List<OrderModel>>> GetOrders([FromQuery] string connectionString)
        {
            List<OrderModel> orderModels = await _orderBLL.GetOrders(connectionString);

            if (orderModels == null || !orderModels.Any())
                return NotFound();

            return orderModels;
        }

        [HttpGet("GetOrderById/{id}")]
        public async Task<ActionResult<GetOrderModel>> GetOrderById(Guid id, [FromQuery] string connectionString)
        {
            GetOrderModel orderModel = await _orderBLL.GetOrderById(id, connectionString);

            if (orderModel == null)
                return NotFound();

            return orderModel;
        }
    }
}
