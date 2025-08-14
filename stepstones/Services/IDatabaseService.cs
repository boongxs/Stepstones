using stepstones.Models;

namespace stepstones.Services
{
    public interface IDatabaseService
    {
        Task AddMediaItemAsync(MediaItem mediaItem);
        Task DeleteItemsByPathsAsync(IEnumerable<string> paths);
        Task<List<MediaItem>> GetAllItemsForFolderAsync(string folderPath);
        Task DeleteMediaItemAsync(MediaItem item);  
    }
}
