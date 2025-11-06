using Core.Dtos;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/salesOrders")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly SalesOrdersService _service;

        public SalesOrdersController(SalesOrdersService service)
        {
            _service = service;
        }

        // GET api/salesorders
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var orders = await _service.GetAllAsync(ct);
            return Ok(orders);
        }

        // GET api/salesorders/{orderId}
        // externalOrderId = ERP/WMS OrderId (dto.OrderId)
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetByOrderId(string orderId, CancellationToken ct)
        {
            var order = await _service.GetByOrderIdAsync(orderId, ct);

            if (order is null)
                return NotFound();

            return Ok(order);
        }

        // POST api/salesorders
        // Creates a single Sales Order and publishes event to RabbitMQ
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSalesOrderDto dto, CancellationToken ct)
        {
            var id = await _service.CreateAsync(dto, ct);

            // We look up by externalOrderId (dto.OrderId), not DB Id
            return CreatedAtAction(
                nameof(GetByOrderId),
                new { orderId = dto.OrderId },
                null
            );
        }

        // POST api/salesorders/bulk
        // Bulk create SOs (JSON list) – can be used by importers or integrations
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] List<CreateSalesOrderDto> orders, CancellationToken ct)
        {
            if (orders is null || orders.Count == 0)
                return BadRequest("No orders provided.");

            await _service.BulkCreateAsync(orders, ct);
            return Accepted(); // or NoContent() if you prefer
        }
    }
}
