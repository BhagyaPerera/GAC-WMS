using Core.Entities.CustomerAggregate;
using Core.Entities.ProductAggregate;
using Core.Entities.SalesOrderAggregate;
using SharedKernal.Interfaces;
using System;
using System.Text.Json.Serialization;

namespace Core.Entities.PurchaseOrderAggregate
{
        public class PurchaseOrder : BaseEntity, IAggregateRoot
        {

            public Guid Id { get; set; }
            public string OrderNo { get; set; } = default!;
            public DateTime ProcessingDate { get; set; }

            public Customer Customer { get; set; } = default!;

            public string Status { get; set; } = "Created"; // or enum

            private readonly List<PurchaseOrderLine> _purchaseOrderLines = new();
            public IEnumerable<PurchaseOrderLine> PurchaseOrderLines => _purchaseOrderLines.Where(a => !a.IsDelete);


            public PurchaseOrder() { }


        public void AddPurchaseOrderLine(PurchaseOrderLine orderLine)
        {
            _purchaseOrderLines.Add(orderLine);
        }

        public void RemovePurchaseOrderLine(PurchaseOrderLine orderLine)
        {
            _purchaseOrderLines.Remove(orderLine);
        }
    }

        
}
