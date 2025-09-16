using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;

namespace stepstones.Services.Data
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly ILogger<SynchronizationService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;
        private readonly IFolderWatcherService _folderWatcherService;
        private readonly IMediaItemProcessorService _mediaItemProcessorService;

        public SynchronizationService(ILogger<SynchronizationService> logger,  
                                      IDatabaseService databaseService, 
                                      IFileService fileService,
                                      IThumbnailService thumbnailService,
                                      IFileTypeIdentifierService fileTypeIdentifierService,
                                      IFolderWatcherService folderWatcherService,
                                      IMediaItemProcessorService mediaItemProcessorService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _fileService = fileService;
            _thumbnailService = thumbnailService;
            _fileTypeIdentifierService = fileTypeIdentifierService;
            _folderWatcherService = folderWatcherService;
            _mediaItemProcessorService = mediaItemProcessorService;
        }

        public async Task SynchronizeDataAsync(string folderPath, Action<MediaItem> onItemProcessed)
        {
            var filesInFolder = _fileService.GetAllFiles(folderPath).ToList();
            var filePathsInDatabase = await _databaseService.GetFilePathsForFolderAsync(folderPath);

            // find and delete ghosts (in DB, not in folder)
            var ghosts = filePathsInDatabase.Except(filesInFolder).ToList();
            if (ghosts.Any())
            {
                _logger.LogInformation("Found {Count} ghost records to delete from the database.", ghosts.Count);
                await _databaseService.DeleteItemsByPathsAsync(ghosts);
            }

            // find and import orphans (in folder, not in DB)
            var orphans = filesInFolder.Except(filePathsInDatabase).ToList();
            if (orphans.Any())
            {
                _logger.LogInformation("Found {Count} orphan files to import into the database.", orphans.Count);

                _folderWatcherService.StopWatching();

                try
                {
                    foreach (var orphanPath in orphans)
                    {
                        try
                        {
                            // rename to unique file name so that we won't have files overwriting each other
                            var uniqueFileName = FileNameGenerator.GenerateUniqueFileName(orphanPath);
                            var newPath = Path.Combine(Path.GetDirectoryName(orphanPath), uniqueFileName);

                            File.Move(orphanPath, newPath);
                            _logger.LogInformation("Renamed orphan file from '{OldPath}' to '{NewPath}'", orphanPath, newPath);

                            var processedItem = await _mediaItemProcessorService.ProcessNewFileAsync(orphanPath, newPath);

                            if (processedItem != null)
                            {
                                onItemProcessed(processedItem);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process orphan file '{Path}'.", orphanPath);
                        }
                    }
                }
                finally
                {
                    _folderWatcherService.StartWatching(folderPath);
                }
            }
        }
    }
}
