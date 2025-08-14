using stepstones.Models;

namespace stepstones.Services.Data
{
    public interface IDatabaseService
    {
        Task AddMediaItemAsync(MediaItem mediaItem);
        Task DeleteItemsByPathsAsync(IEnumerable<string> paths);
        Task<List<MediaItem>> GetAllItemsForFolderAsync(string folderPath, string? filterText = null);
        Task DeleteMediaItemAsync(MediaItem item);
        Task UpdateMediaItemAsync(MediaItem item);
    }
}
