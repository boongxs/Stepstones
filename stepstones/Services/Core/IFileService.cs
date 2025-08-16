using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IFileService
    {
        Task<Dictionary<string, string>> CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath);
        IEnumerable<string> GetAllFiles(string folderPath);
        void DeleteMediaFile(MediaItem item);
    }
}
