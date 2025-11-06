using Core.Entities.PurchaseOrderAggregate;
using Core.Entities.SalesOrderAggregate;
using SharedKernal;

namespace Core.Events.ApplicationEvents
{
    public class PurchaseOrderCreateEvent : BaseIntegrationEvent
    {
        public const string EventName = nameof(PurchaseOrderCreateEvent);
        public PurchaseOrder PurchaseOrder { get; init; }
        public IEnumerable<PurchaseOrderLine> PurchaseOrderLines { get; init; }

        public PurchaseOrderCreateEvent(PurchaseOrder purchaseOrder) : base(EventName)
        {
            PurchaseOrder = purchaseOrder;
            PurchaseOrderLines = purchaseOrder.PurchaseOrderLines;
        }

        public PurchaseOrderCreateEvent()
        { } // Json Deserialization need this
    }
}
