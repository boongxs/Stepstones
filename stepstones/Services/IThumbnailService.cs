using stepstones.Models;

namespace stepstones.Services
{
    public interface IThumbnailService
    {
        Task<string?> CreateThumbnailAsync(string sourceFilePath, MediaType mediaType);
    }
}
