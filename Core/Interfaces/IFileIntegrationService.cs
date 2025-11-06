namespace Core.Interfaces
{
   public interface IFileIntegrationService
    {
    Task PullSalesOrderProcess(bool useLocalFile = false, string localFilePath = null);

   }
}
