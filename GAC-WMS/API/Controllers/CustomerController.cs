using Core.Dtos;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _service;

        public CustomersController(CustomerService service)
        {
            _service = service;
        }

        // GET api/customers
        [HttpGet]
        public async Task<ActionResult<List<CustomerDto>>> GetAll(CancellationToken ct)
        {
            var customers = await _service.GetAllAsync(ct);
            return Ok(customers);
        }

        // GET api/customers/{customerId}
        [HttpGet("{customerId}")]
        public async Task<ActionResult<CustomerDto>> GetById(string customerId, CancellationToken ct)
        {
            var customer = await _service.GetByCustomerIdAsync(customerId, ct);

            if (customer is null)
                return NotFound();

            return Ok(customer);
        }

        // POST api/customers
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CustomerDto dto, CancellationToken ct)
        {
            await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { customerId = dto.CustomerNo},
                null
            );
        }

        // PUT api/customers/{customerId}
        [HttpPut("{customerId}")]
        public async Task<IActionResult> Update(string customerId, [FromBody] CustomerDto dto, CancellationToken ct)
        {
            await _service.UpdateAsync(customerId, dto, ct);
            return NoContent();
        }

        // DELETE api/customers/{customerId} → deactivate (soft delete)
        [HttpDelete("{customerId}")]
        public async Task<IActionResult> Deactivate(string customerId, CancellationToken ct)
        {
            await _service.DeactivateAsync(customerId, ct);
            return NoContent();
        }
    }
}
