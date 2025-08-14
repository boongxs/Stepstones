namespace stepstones.Services
{
    public interface IThumbnailService
    {
        Task<string> CreateThumbnailAsync(string sourceFilePath);
    }
}
