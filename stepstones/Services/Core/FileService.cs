using Microsoft.Extensions.Logging;
using stepstones.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace stepstones.Services.Core
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath)
        {
            var pathMappings = new Dictionary<string, string>();

            await Task.Run(() =>
            {
                foreach (var sourcePath in sourceFilePaths)
                {
                    try
                    {
                        var uniqueFileName = GenerateUniqueFileName(sourcePath);
                        var destinationPath = Path.Combine(destinationFolderPath, uniqueFileName);

                        if (!File.Exists(destinationPath))
                        {
                            File.Copy(sourcePath, destinationPath);
                            _logger.LogInformation("Successfully copied '{SourceFile}' to '{DestinationFile}'", sourcePath, destinationPath);
                        }
                        else
                        {
                            _logger.LogInformation("File '{SourceFile}' already exists as '{DestinationFile}'. Skipping.", sourcePath, destinationPath);
                        }

                        pathMappings[sourcePath] = destinationPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to copy '{SourceFile}'", sourcePath);
                    }
                }
            });

            return pathMappings;
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

        private string GenerateUniqueFileName(string sourceFilePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(sourceFilePath);
            var hashBytes = md5.ComputeHash(stream);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            var extension = Path.GetExtension(sourceFilePath);
            return $"{hashString}{extension}";
        }
    }
}
