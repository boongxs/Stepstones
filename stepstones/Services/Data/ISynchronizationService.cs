namespace stepstones.Services.Data
{
    public interface ISynchronizationService
    {
        Task SynchronizeDataAsync(string folderPath);
    }
}
