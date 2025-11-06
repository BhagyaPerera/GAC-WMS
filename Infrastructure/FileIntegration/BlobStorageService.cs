using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.FileIntegration
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(BlobServiceClient serviceClient, ILogger<BlobStorageService> logger)
        {
            _serviceClient = serviceClient;
            _logger = logger;
        }

        public async Task<string?> GetBlobContentAsync(string containerName, string prefix, string fileName)
        {
            try
            {
                BlobContainerClient blobContainerClient = _serviceClient.GetBlobContainerClient(containerName);
                await foreach (var blobItem in blobContainerClient.GetBlobsAsync(prefix: prefix))
                {
                    if (blobItem.Name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Found Blob: {blobItem.Name}");
                        var blobClient = blobContainerClient.GetBlobClient(blobItem.Name);
                        var downloadInfo = await blobClient.DownloadAsync();

                        using (var reader = new StreamReader(downloadInfo.Value.Content))
                        {
                            return await reader.ReadToEndAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching {fileName} from Blob Storage.");
                throw;
            }

            _logger.LogError($"{fileName} not found in Blob Storage under {prefix}.");
            return null;
        }
    }
}
