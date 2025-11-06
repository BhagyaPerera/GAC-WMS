using Core.Dtos;
using Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _service;

        public ProductsController(ProductService service)
        {
            _service = service;
        }

        // GET api/products
        [HttpGet]
        public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken ct)
        {
            var products = await _service.GetAllAsync(ct);
            return Ok(products);
        }

        // GET api/products/{productCode}
        [HttpGet("{productCode}")]
        public async Task<ActionResult<ProductDto>> GetByCode(string productCode, CancellationToken ct)
        {
            var product = await _service.GetByCodeAsync(productCode, ct);

            if (product is null)
                return NotFound();

            return Ok(product);
        }

        // POST api/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductDto dto, CancellationToken ct)
        {
            await _service.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetByCode),
                new { productCode = dto.ProductCode },
                null
            );
        }

        // PUT api/products/{productCode}
        [HttpPut("{productCode}")]
        public async Task<IActionResult> Update(string productCode, [FromBody] ProductDto dto, CancellationToken ct)
        {
            await _service.UpdateAsync(productCode, dto, ct);
            return NoContent();
        }

        // DELETE api/products/{productCode} → deactivate
        [HttpDelete("{productCode}")]
        public async Task<IActionResult> Deactivate(string productCode, CancellationToken ct)
        {
            await _service.DeactivateAsync(productCode, ct);
            return NoContent();
        }
    }
}
