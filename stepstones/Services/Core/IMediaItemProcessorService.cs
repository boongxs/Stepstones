using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IMediaItemProcessorService
    {
        Task<MediaItem?> ProcessNewFileAsync(string originalPath, string finalPath);
    }
}
