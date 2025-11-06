namespace Core.Interfaces;

public interface IFilePollingService
{
    Task<IEnumerable<string>> GetPendingFilesAsync();
    Task ArchiveFileAsync(string filePath);
}
