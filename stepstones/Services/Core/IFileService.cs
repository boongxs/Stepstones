using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IFileService
    {
        IEnumerable<string> GetAllFiles(string folderPath);
        void DeleteMediaFile(MediaItem item);
        Task<string?> CopyFileAsync(string sourcePath, string destinationFolderPath);
    }
}
