using Core.Dtos;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/purchaseOrders")]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly PurchaseOrdersService _service;

        public PurchaseOrdersController(PurchaseOrdersService service)
        {
            _service = service;
        }

        // GET api/purchaseorders
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var orders = await _service.GetAllAsync(ct);
            return Ok(orders);
        }

        // GET api/purchaseorders/{orderId}
        // externalOrderId = ERP/WMS OrderId (dto.OrderId)
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetByOrderId(string orderId, CancellationToken ct)
        {
            var order = await _service.GetByOrderIdAsync(orderId, ct);

            if (order is null)
                return NotFound();

            return Ok(order);
        }

        // POST api/purchaseorders
        // Creates a single PO and publishes event to RabbitMQ
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct)
        {
            var id = await _service.CreateAsync(dto, ct);

            // We look up by externalOrderId (dto.OrderId), not DB Id
            return CreatedAtAction(
                nameof(GetByOrderId),
                new { orderId = dto.OrderId },
                null
            );
        }

        // POST api/purchaseorders/bulk
        // Bulk create POs (JSON list) – you can call this from XML parser later
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreate([FromBody] List<CreatePurchaseOrderDto> orders, CancellationToken ct)
        {
            if (orders is null || orders.Count == 0)
                return BadRequest("No orders provided.");

            await _service.BulkCreateAsync(orders, ct);
            return Accepted(); // or NoContent() if you prefer
        }
    }
}
