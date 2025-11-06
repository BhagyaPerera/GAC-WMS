using Core.Dtos;
using Core.Entities.ProductAggregate;
using Core.Specification;
using Microsoft.EntityFrameworkCore;
using SharedKernal.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Services
{
    public class ProductService
    {
        private readonly IRepository<Product> _productRepo;

        public ProductService(IRepository<Product> productRepo)
        {
            _productRepo = productRepo;
        }

        // GET all products
        public async Task<List<ProductDto>> GetAllAsync(CancellationToken ct = default)
        {
            var products = await _productRepo.ListAsync();


            return products
                .Select(p => new ProductDto(
                    ProductCode: p.ProductCode,
                    Title: p.Title,
                    Description: p.Description,
                    Width: p.Width,
                    Height: p.Height,
                    Length: p.Length,
                    Weight: p.Weight
                ))
                .ToList();
        }

        // GET single product by ProductCode (external SKU)
        public async Task<ProductDto?> GetByCodeAsync(string productCode, CancellationToken ct = default)
        {
            var product = await _productRepo.FirstOrDefaultAsync(new GetProductByProductCodeSpec(productCode));

            if (product is null)
                return null;

            return new ProductDto(
                ProductCode: product.ProductCode,
                Title: product.Title,
                Description: product.Description,
                Width: product.Width,
                Height: product.Height,
                Length: product.Length,
                Weight: product.Weight
            );
        }

        // CREATE product
        public async Task CreateAsync(ProductDto dto, CancellationToken ct = default)
        {
            // prevent duplicates by ProductCode
            var existing = await _productRepo.FirstOrDefaultAsync(new GetProductByProductCodeSpec(dto.ProductCode));


            if (existing is not null)
                throw new InvalidOperationException("Product already exists");

            var product = new Product
            {
                ProductCode = dto.ProductCode,
                Title = dto.Title,
                Description = dto.Description,
                Width = dto.Width??0.00m,
                Height = dto.Height??0.00m,
                Length = dto.Length ?? 0.00m,
                Weight = dto.Weight ?? 0.00m,

                // assuming BaseEntity provides these properties
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            await _productRepo.AddAsync(product);
            await _productRepo.SaveChangesAsync();
        }

        // (Optional) UPDATE product
        public async Task UpdateAsync(string productCode, ProductDto dto, CancellationToken ct = default)
        {
            // prevent duplicates by ProductCode
            var product = await _productRepo.FirstOrDefaultAsync(new GetProductByProductCodeSpec(dto.ProductCode));

            if (product is null)
                throw new KeyNotFoundException("Product not found");

            product.Title = dto.Title?? product.Title;
            product.Description = dto.Description?? product.Description;
            product.Width = dto.Width > 0.00m ? dto.Width : product.Width;
            product.Height = dto.Height > 0.00m ? dto.Height : product.Height;
            product.Length = dto.Length > 0.00m ? dto.Length : product.Length;
            product.Weight = dto.Weight > 0.00m ? dto.Weight : product.Weight;

            product.UpdatedAtUtc = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);
            await _productRepo.SaveChangesAsync();
        }

        // (Optional) soft delete / deactivate
        public async Task DeactivateAsync(string productCode, CancellationToken ct = default)
        {
            var product = await _productRepo.FirstOrDefaultAsync(new GetProductByProductCodeSpec(productCode));

            if (product is null)
                throw new KeyNotFoundException("Product not found");

            product.IsActive = false;
            product.UpdatedAtUtc = DateTime.UtcNow;

            await _productRepo.UpdateAsync(product);
            await _productRepo.SaveChangesAsync();
        }
    }
}
