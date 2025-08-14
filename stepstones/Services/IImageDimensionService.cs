using stepstones.Models;

namespace stepstones.Services
{
    public interface IImageDimensionService
    {
        Task<(int Width, int Height)> GetDimensionsAsync(string filePath, MediaType mediaType);
    }
}
