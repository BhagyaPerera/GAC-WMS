using SharedKernal.Interfaces;

namespace Core.Entities.CustomerAggregate
{
    public class Customer : BaseEntity, IAggregateRoot
    {  
        public Guid Id { get; set; }
        public string CustomerNo { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string AddressLine1 { get; set; } = default!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string PostalCode { get; set; } = default!;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
