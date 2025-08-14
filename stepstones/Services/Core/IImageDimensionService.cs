using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IImageDimensionService
    {
        Task<(int Width, int Height)> GetDimensionsAsync(string filePath, MediaType mediaType);
    }
}
