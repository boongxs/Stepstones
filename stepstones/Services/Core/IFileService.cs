using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IFileService
    {
        Task CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath);
        IEnumerable<string> GetAllFiles(string folderPath);
        void DeleteMediaFile(MediaItem item);
    }
}
