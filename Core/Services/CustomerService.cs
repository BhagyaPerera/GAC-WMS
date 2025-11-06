using Core.Dtos;
using Core.Entities.CustomerAggregate;
using Core.Specification;
using Microsoft.EntityFrameworkCore;
using SharedKernal.Interfaces;

namespace Core.Services
{
    public class CustomerService
    {
        private readonly IRepository<Customer> _customerRepo;

        public CustomerService(IRepository<Customer> customerRepo)
        {
            _customerRepo = customerRepo;
        }

        // GET all customers
        public async Task<List<CustomerDto>> GetAllAsync(CancellationToken ct = default)
        {
            var customers = await _customerRepo.ListAsync();

            return customers
                .Select(c => new CustomerDto(
                    CustomerNo:c.CustomerNo,
                    Name: c.Name,
                    AddressLine1: c.AddressLine1,
                    AddressLine2: c.AddressLine2,
                    City: c.City,
                    Country: c.Country,
                    PostalCode: c.PostalCode,
                    PhoneNumber: c.PhoneNumber,
                    Email: c.Email
                ))
                .ToList();
        }

        // GET single customer by  CustomerId
        public async Task<CustomerDto?> GetByCustomerIdAsync(string customerId, CancellationToken ct = default)
        {
            var customer = await _customerRepo.FirstOrDefaultAsync(new GetCustomerByCustomerNoSpec(customerId));


            if (customer is null)
                return null;

            return new CustomerDto(
                CustomerNo: customer.CustomerNo,
                Name: customer.Name,
                AddressLine1: customer.AddressLine1,
                AddressLine2: customer.AddressLine2,
                City: customer.City,
                Country: customer.Country,
                PostalCode: customer.PostalCode,
                PhoneNumber: customer.PhoneNumber,
                Email: customer.Email
            );
        }

        // CREATE customer
        public async Task CreateAsync(CustomerDto dto, CancellationToken ct = default)
        {
            var existing = await _customerRepo.FirstOrDefaultAsync(new GetCustomerByCustomerNoSpec(dto.CustomerNo));

            if (existing is not null)
                throw new InvalidOperationException("Customer already exists");

            var customer = new Customer
            {
                CustomerNo = dto.CustomerNo,
                Name = dto.Name,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2 ?? string.Empty,
                City = dto.City,
                Country = dto.Country,
                PostalCode = dto.PostalCode,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();
        }

        // UPDATE customer (by CustomerId)
        public async Task UpdateAsync(string customerId, CustomerDto dto, CancellationToken ct = default)
        {
            var customer = await _customerRepo.FirstOrDefaultAsync(new GetCustomerByCustomerNoSpec(customerId));

            if (customer is null)
                throw new KeyNotFoundException("Customer not found");

            // Usually we DON'T change CustomerId itself; it's the business key.
            customer.Name = dto.Name?? customer.Name;
            customer.AddressLine1 = dto.AddressLine1?? customer.AddressLine1;
            customer.AddressLine2 = dto.AddressLine2 ?? customer.AddressLine2?? string.Empty;
            customer.City = dto.City ?? customer.City;
            customer.Country = dto.Country ?? customer.Country;
            customer.PostalCode = dto.PostalCode ?? customer.PostalCode;
            customer.PhoneNumber = dto.PhoneNumber ?? customer.PhoneNumber;
            customer.Email = dto.Email ?? customer.Email;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _customerRepo.UpdateAsync(customer);
            await _customerRepo.SaveChangesAsync();
        }

        // DEACTIVATE (soft delete) customer
        public async Task DeactivateAsync(string customerId, CancellationToken ct = default)
        {
            var customer = await _customerRepo.FirstOrDefaultAsync(new GetCustomerByCustomerNoSpec(customerId));

            if (customer is null)
                throw new KeyNotFoundException("Customer not found");

            customer.IsActive = false;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            await _customerRepo.UpdateAsync(customer);
            await _customerRepo.SaveChangesAsync();
        }
    }
}
