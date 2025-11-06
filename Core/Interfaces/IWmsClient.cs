namespace Core.Interfaces
{
    public interface IWmsClient
    {
        Task SendPurchaseOrderAsync(object message, CancellationToken ct = default);
        Task SendSalesOrderAsync(object message, CancellationToken ct = default);
    }
}
