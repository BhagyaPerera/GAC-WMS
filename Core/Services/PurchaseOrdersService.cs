using Core.Dtos;
using Core.Entities.CustomerAggregate;
using Core.Entities.ProductAggregate;
using Core.Entities.PurchaseOrderAggregate;
using Core.Entities.SalesOrderAggregate;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Specification;
using Microsoft.EntityFrameworkCore;
using SharedKernal.Interfaces;
using System.Threading.Tasks;

namespace Core.Services
{
    public class PurchaseOrdersService
    {
        private readonly IRepository<PurchaseOrder> _purchaseOrderRepo;
        private readonly IRepository<Customer> _customerRepo;
        private readonly IRepository<Product> _productRepo;
        private readonly IMessagePublisher _messagePublisher;

        public PurchaseOrdersService(IRepository<PurchaseOrder> purchaseOrderRepo, IMessagePublisher messagePublisher, IRepository<Customer> customerRepo,IRepository<Product> productRepo)
        {
            _purchaseOrderRepo = purchaseOrderRepo;
            _messagePublisher = messagePublisher;
            _customerRepo = customerRepo;
            _productRepo = productRepo;
        }

        /// <summary>
        /// Creates a new Purchase Order and publishes an integration event to RabbitMQ.
        public async Task<string> CreateAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default)
        {
            PurchaseOrder? purchaseOrder = await mapPurchaseOrder(dto);

            if(purchaseOrder is null)
                throw new InvalidOperationException("Invalid Customer or Products in Purchase Order");

            // 4️ Save to Database
            await _purchaseOrderRepo.AddAsync(purchaseOrder);
            await _purchaseOrderRepo.SaveChangesAsync();

            return purchaseOrder.OrderNo;
            
        }

        /// <summary>
        /// Bulk create Purchase Orders from a list (used for XML uploads or batch imports).
        /// </summary>
        public async Task BulkCreateAsync(List<CreatePurchaseOrderDto> orders, CancellationToken ct = default)
        {
            const int maxDegreeOfParallelism = 10; //no more than 10 publish tasks will run concurrently

            if (orders == null || orders.Count == 0)
                return;

            var throttler = new SemaphoreSlim(maxDegreeOfParallelism); //acts like gatekeeper to limit concurrency
            var tasks = new List<Task>();

            foreach (var dto in orders)
            {
                PurchaseOrder? purchaseOrder = await mapPurchaseOrder(dto);

                if (purchaseOrder is null)
                    continue;

                await throttler.WaitAsync(ct);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        PurchaseOrderCreateEvent evt = new PurchaseOrderCreateEvent(purchaseOrder);
                        _messagePublisher.Publish(evt);
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
        /// Retrieves a Purchase Order by external OrderId.
        /// </summary>
        public async Task<PurchaseOrder?> GetByOrderIdAsync(string orderId, CancellationToken ct = default)
        {
            PurchaseOrder po= await _purchaseOrderRepo.FirstOrDefaultAsync(new GetPurchaseOrderbyOrderNoSpec(orderId));
            return po;
        }

        /// <summary>
        /// Gets all Purchase Orders (for monitoring or dashboards).
        /// </summary>
        public async Task<List<PurchaseOrder>> GetAllAsync(CancellationToken ct = default)
        {
            return await _purchaseOrderRepo.ListAsync();
        }


        private async Task<PurchaseOrder?> mapPurchaseOrder(CreatePurchaseOrderDto dto)
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

            int lineNo = 1;
            List<PurchaseOrderLine> orderLines = new List<PurchaseOrderLine>();
           
            // 3️ Map DTO → Entity
            PurchaseOrder purchaseOrder = new PurchaseOrder
            {
                OrderNo = dto.OrderId,
                ProcessingDate = dto.ProcessingDate,
                Customer = customer,
                Status = "Created",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,

            };

            foreach (var line in dto.Lines)
            {
                PurchaseOrderLine pline = new PurchaseOrderLine
                {
                    LineNo = lineNo,
                    Product = products.First(p => p.ProductCode == line.ProductCode),
                    Quantity = line.Quantity

                };
                purchaseOrder.AddPurchaseOrderLine(pline);
                lineNo++;
            }

            return purchaseOrder;
        }
    }
}
