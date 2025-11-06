using Core.Entities.SalesOrderAggregate;
using SharedKernal;

namespace Core.Events.ApplicationEvents
{
    public class  SalesOrderCreateEvent:BaseIntegrationEvent
    {
        public const string EventName = nameof(SalesOrderCreateEvent);
        public SalesOrder SalesOrder { get; init; }
        public IEnumerable<SalesOrderLine> SalesOrderLines { get; init; }

        public SalesOrderCreateEvent(SalesOrder salesOrder) : base(EventName)
        {
           SalesOrder = salesOrder;
           SalesOrderLines = salesOrder.SalesOrderLines;
        }

    public SalesOrderCreateEvent()
    { } // Json Deserialization need this
}

}
