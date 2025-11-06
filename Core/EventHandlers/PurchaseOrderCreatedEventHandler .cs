using Core.Entities.PurchaseOrderAggregate;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Services;
using SharedKernal.Interfaces;

namespace Core.EventHandlers
{
    public class PurchaseOrderCreatedEventHandler : IIntegrationEventHandler<PurchaseOrderCreateEvent>
    {
        private readonly PurchaseOrdersService _service;
        private readonly IRepository<PurchaseOrder> _poRepo;

        public PurchaseOrderCreatedEventHandler(PurchaseOrdersService service, IRepository<PurchaseOrder> poRepo)
        {
            _service = service;
            _poRepo = poRepo;
        }

        public async Task HandleAsync(PurchaseOrderCreateEvent evt, CancellationToken ct)
        {
            await _poRepo.AddAsync(evt.PurchaseOrder);
            await _poRepo.SaveChangesAsync();
        }
    }
}

