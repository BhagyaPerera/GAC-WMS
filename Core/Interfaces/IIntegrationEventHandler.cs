namespace Core.Interfaces
{

    public interface IIntegrationEventHandler<TEvent>
    {
        Task HandleAsync(TEvent @event, CancellationToken ct);
    }
}
