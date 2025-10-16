namespace stepstones.Services.Data
{
    public interface ISynchronizationService
    {
        Task DeleteGhostRecordsAsync(string folderPath);
        Task SynchronizeOrphanFilesAsync(string folderPath, IProgress<string> progress);
    }
}
