using Core.Entities.CustomerAggregate;
using Core.Entities.ProductAggregate;
using SharedKernal.Interfaces;
using System.Text.Json.Serialization;

namespace Core.Entities.SalesOrderAggregate
{
    public class SalesOrder : BaseEntity, IAggregateRoot
    {
        public Guid Id { get; set; }
        public string OrderNo { get; set; } = default!;
        public DateTime ProcessingDate { get; set; }

        public Customer Customer { get; set; } = default!;

        public string ShipmentAddress { get; set; } = default!;
        public string Status { get; set; } = "Created";

        private readonly List<SalesOrderLine> _salesOrderLines = new();
        public IEnumerable<SalesOrderLine> SalesOrderLines => _salesOrderLines.Where(a => !a.IsDelete);


        public SalesOrder() { }


        public void AddSalesOrderLine(SalesOrderLine orderLine)
        {
            _salesOrderLines.Add(orderLine);
        }

        public void RemoveSalesOrderLine(SalesOrderLine orderLine)
        {
            _salesOrderLines.Remove(orderLine);
        }


    }
}
