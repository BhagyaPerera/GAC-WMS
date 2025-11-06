using Core.Entities.PurchaseOrderAggregate;
using Core.Entities.SalesOrderAggregate;
using Core.Events.ApplicationEvents;
using Core.Interfaces;
using Core.Services;
using SharedKernal.Interfaces;

public class SalesOrderCreatedEventHandler : IIntegrationEventHandler<SalesOrderCreateEvent>
{
    private readonly SalesOrdersService _service;
    private readonly IRepository<SalesOrder> _soRepo;



    public SalesOrderCreatedEventHandler(SalesOrdersService service, IRepository<SalesOrder> soRepo)
    {
        _service = service;
        _soRepo = soRepo;
    }

    public async Task HandleAsync(SalesOrderCreateEvent evt, CancellationToken ct)
    {
        await _soRepo.AddAsync(evt.SalesOrder);
        await _soRepo.SaveChangesAsync();
    }
}
