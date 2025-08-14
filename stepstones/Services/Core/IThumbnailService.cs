using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IThumbnailService
    {
        Task<string?> CreateThumbnailAsync(string sourceFilePath, MediaType mediaType);
    }
}
