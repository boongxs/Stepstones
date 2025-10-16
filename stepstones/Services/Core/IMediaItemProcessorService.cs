using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IMediaItemProcessorService
    {
        Task<MediaItem?> ProcessNewFileAsync(string originalPath, string finalPath);
        Task ProcessUploadedFilesAsync(IEnumerable<string> sourceFilePaths, string destinationPath, IProgress<string> progress);
        Task ProcessOrphanFilesAsync(IEnumerable<string> orphanPaths, IProgress<string> progress);
    }
}
