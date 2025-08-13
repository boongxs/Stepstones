using stepstones.Models;

namespace stepstones.Services
{
    public interface IDatabaseService
    {
        Task AddMediaItemAsync(MediaItem mediaItem);
        Task<List<string>> GetAllFilePathsAsync();
        Task DeleteItemsByPathsAsync(IEnumerable<string> paths);
    }
}
