using Core.Entities.ProductAggregate;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Entities.PurchaseOrderAggregate
{
    public class PurchaseOrderLine : BaseEntity,IAggregateRoot
    {
        public Guid Id { get; set; }

        public int LineNo { get; set; }
        public Guid? PurchaseOrderId { get; set; }

        [JsonIgnore]
        public PurchaseOrder PurchaseOrder { get; set; } = default!;

        public Product Product { get; set; } = default!;

        public decimal Quantity { get; set; }

        public bool IsDelete { get; set; } = false;
        public PurchaseOrderLine() { }

        public PurchaseOrderLine(int lineNo,Product product,int quantity)
        {
            LineNo = lineNo;
            Product = product;
            Quantity = quantity;
        }
    }
}
