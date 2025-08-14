namespace stepstones.Services
{
    public interface ISynchronizationService
    {
        Task SynchronizeDataAsync(string folderPath);
    }
}
