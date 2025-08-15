using Microsoft.Extensions.Logging;
using stepstones.Models;
using System.IO;

namespace stepstones.Services.Core
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }

        public async Task CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath)
        {
            await Task.Run(() =>
            {
                foreach (var sourcePath in sourceFilePaths)
                {
                    try
                    {
                        var fileName = Path.GetFileName(sourcePath);
                        var destinationPath = Path.Combine(destinationFolderPath, fileName);

                        File.Copy(sourcePath, destinationPath, true);
                        _logger.LogInformation("Successfully copied '{SourceFile}' to '{DestinationFile}'", sourcePath, destinationPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to copy '{SourceFile}'", sourcePath);
                    }
                }
            });
        }

        public IEnumerable<string> GetAllFiles(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    return Directory.EnumerateFiles(folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate files in folder '{FolderPath}'", folderPath);
            }
            return Enumerable.Empty<string>();
        }

        public void DeleteMediaFile(MediaItem item)
        {
            // delete the media file
            try
            {
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                    _logger.LogInformation("Successfully deleted media file '{Path}'", item.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media file '{Path}'", item.FilePath);
            }

            // delete media file's thumbnail
            try
            {
                if (!string.IsNullOrWhiteSpace(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
                {
                    File.Delete(item.ThumbnailPath);
                    _logger.LogInformation("Successfully deleted thumbnail file '{Path}'", item.ThumbnailPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete thumbnail file '{Path}'", item.ThumbnailPath);
            }
        }
    }
}
