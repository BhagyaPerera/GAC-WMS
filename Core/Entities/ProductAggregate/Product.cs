using SharedKernal.Interfaces;

namespace Core.Entities.ProductAggregate
{
    public class Product : BaseEntity, IAggregateRoot
    {

        public Guid Id { get; set; }
        public string ProductCode { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string? Description { get; set; }

        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public decimal? Length { get; set; }
        public decimal? Weight { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
