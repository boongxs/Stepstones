using stepstones.Models;

namespace stepstones.Services.Data
{
    public interface ISynchronizationService
    {
        Task SynchronizeDataAsync(string folderPath, Action<MediaItem> onItemProcessed);
    }
}
