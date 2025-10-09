namespace stepstones.Services.Core
{
    public interface ITranscodingService
    {
        Task<string> EnsurePlayableFileAsync(string filePath, CancellationToken cancellationToken);
        void ClearCache();
        Task<bool> IsTranscodingRequiredAsync(string filePath);
        string GetCachePathForFile(string sourceFilePath);
    }
}
