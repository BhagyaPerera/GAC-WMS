using Core.Dtos;
using Core.Entities.CustomerAggregate;
using Core.Entities.ProductAggregate;
using Core.Entities.PurchaseOrderAggregate;
using Core.Entities.SalesOrderAggregate;
using Core.Events;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Specification;
using Microsoft.EntityFrameworkCore;
using SharedKernal.Interfaces;
namespace Core.Services
{
    public class SalesOrdersService
    {
        private readonly IRepository<SalesOrder> _salesOrderRepo;
        private readonly IMessagePublisher _publisher;
        private readonly IRepository<Customer> _customerRepo;
        private readonly IRepository<Product> _productRepo;

        public SalesOrdersService(IRepository<SalesOrder> salesOrderRepo, IMessagePublisher publisher, IRepository<Customer> customerRepo, IRepository<Product> productRepo)
        {
            _salesOrderRepo=salesOrderRepo;
            _publisher = publisher;
            _customerRepo = customerRepo;
            _productRepo=productRepo;
        }

        /// <summary>
        /// Creates a new Sales Order and publishes an integration event to RabbitMQ.
        /// </summary>
        /// <returns>Internal Sales Order Id</returns>
        public async Task<string> CreateAsync(CreateSalesOrderDto dto, CancellationToken ct = default)
        {

            SalesOrder salesOrder = await mapPartnerOrder(dto);

            if (salesOrder is null)
                throw new InvalidOperationException("Invalid Customer or Products in Purchase Order");

            // 4️ Save to Database
            await _salesOrderRepo.AddAsync(salesOrder);
            await _salesOrderRepo.SaveChangesAsync();

            return salesOrder.OrderNo;

        }

        /// <summary>
        /// Bulk create Sales Orders (e.g. from XML or batch process).
        /// </summary>
        public async Task BulkCreateAsync(List<CreateSalesOrderDto> orders, CancellationToken ct = default)
        {
            const int maxDegreeOfParallelism = 10; //no more than 10 publish tasks will run concurrently

            if (orders == null || orders.Count == 0)
                return;

            var throttler = new SemaphoreSlim(maxDegreeOfParallelism); //acts like gatekeeper to limit concurrency
            var tasks = new List<Task>();

            foreach (var dto in orders)
            {
                SalesOrder? salesOrder =await  mapPartnerOrder(dto);

                if (salesOrder is null)
                    continue;

                await throttler.WaitAsync(ct);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var evt = new SalesOrderCreateEvent(salesOrder);
                        _publisher.Publish(evt);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get a Sales Order by external OrderId (ERP/WMS order id).
        /// </summary>
        public async Task<SalesOrder?> GetByOrderIdAsync(string orderId, CancellationToken ct = default)
        {

            var salesOrder = await _salesOrderRepo.FirstOrDefaultAsync(new GetPartnerOrderByOrderNoSpec(orderId));
            return salesOrder;
        }

        /// <summary>
        /// Get all Sales Orders (for monitoring / dashboards).
        /// </summary>
        public async Task<List<SalesOrder>> GetAllAsync(CancellationToken ct = default)
        {
            var salesOrders = await _salesOrderRepo.ListAsync();
            return salesOrders;
        }

        /// <summary>
        /// Cancel a Sales Order by external OrderId.
        /// Sets status and publishes a cancellation event.
        /// </summary>
        public async Task CancelAsync(string orderId, CancellationToken ct = default)
        {
            var so = await _salesOrderRepo.FirstOrDefaultAsync(new GetPartnerOrderByOrderNoSpec(orderId));

            if (so is null)
                throw new KeyNotFoundException($"Sales order '{orderId}' not found.");

            if (so.Status == "Cancelled")
                return; // already cancelled, nothing to do

            so.Status = "Cancelled";
            so.UpdatedAtUtc = DateTime.UtcNow;

            await _salesOrderRepo.UpdateAsync(so);
            await _salesOrderRepo.SaveChangesAsync();
        }

        private async Task<SalesOrder?> mapPartnerOrder(CreateSalesOrderDto dto)
        {
            // 1️ Validate Customer
            var customer = await _customerRepo.FirstOrDefaultAsync(new GetCustomerByCustomerNoSpec(dto.CustomerId));
            if (customer is null)
                return null;

            // 2️ Validate Products
            var requestedProductCodes = dto.Lines.Select(l => l.ProductCode).ToList();

            var products = await _productRepo.ListAsync();

            if (products.Count != dto.Lines.Count)
            {
                var missing = requestedProductCodes
                    .Except(products.Select(p => p.ProductCode))
                    .ToList();

                return null;
            }

            // 3️ Map DTO → entity
            SalesOrder so = new SalesOrder
            {
                OrderNo = dto.OrderId,
                ProcessingDate = dto.ProcessingDate,
                Customer = customer,
                ShipmentAddress = dto.ShipmentAddress,
                Status = "Created",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            int lineNo = 1;
            foreach (var line in dto.Lines)
            {
                SalesOrderLine sline = new SalesOrderLine
                {
                    LineNo = lineNo,
                    Product = products.First(p => p.ProductCode == line.ProductCode),
                    Quantity = line.Quantity

                };
                so.AddSalesOrderLine(sline);
                lineNo++;
            }

            return so;
        }
    }
}
