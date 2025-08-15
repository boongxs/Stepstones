using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;
using stepstones.Services.Core;

namespace stepstones.Services.Data
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly ILogger<SynchronizationService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;

        public SynchronizationService(ILogger<SynchronizationService> logger,  
                                      IDatabaseService databaseService, 
                                      IFileService fileService,
                                      IThumbnailService thumbnailService,
                                      IFileTypeIdentifierService fileTypeIdentifierService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _fileService = fileService;
            _thumbnailService = thumbnailService;
            _fileTypeIdentifierService = fileTypeIdentifierService;
        }

        public async Task SynchronizeDataAsync(string folderPath)
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
                foreach (var orphanPath in orphans)
                {
                    var mediaType = await _fileTypeIdentifierService.IdentifyAsync(orphanPath);
                    if (mediaType == MediaType.Unknown)
                    {
                        continue;
                    }

                    var thumbnailPath = await _thumbnailService.CreateThumbnailAsync(orphanPath, mediaType);

                    var newItem = new MediaItem
                    {
                        FileName = Path.GetFileName(orphanPath),
                        FilePath = orphanPath,
                        FileType = mediaType,
                        ThumbnailPath = thumbnailPath
                    };
                    await _databaseService.AddMediaItemAsync(newItem);
                }
            }
        }
    }
}
