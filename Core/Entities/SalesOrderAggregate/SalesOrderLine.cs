using Core.Entities.ProductAggregate;
using SharedKernal.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Core.Entities.SalesOrderAggregate
{
    public class SalesOrderLine : BaseEntity,IAggregateRoot
    {
        public Guid Id { get; set; }

        public int LineNo { get; set; }
        public Guid SalesOrderId { get; set; }

        [JsonIgnore]
        public SalesOrder SalesOrder { get; set; } = default!;
        public Product Product { get; set; } = default!;

        public decimal Quantity { get; set; }

        public bool IsDelete { get; set; } = false;

        public SalesOrderLine() { }

        public SalesOrderLine(int lineNo, Product product, int quantity)
        {
            LineNo = lineNo;
            Product = product;
            Quantity = quantity;
        }
    }
}
