using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;

namespace stepstones.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly ILogger<SynchronizationService> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;

        public SynchronizationService(ILogger<SynchronizationService> logger,
                                      ISettingsService settingsService,
                                      IDatabaseService databaseService,
                                      IFileService fileService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _databaseService = databaseService;
            _fileService = fileService;
        }

        public async Task SynchronizeDataAsync()
        {
            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Synchronization skipped: Media folder is not selected.");
                return;
            }

            var filesInFolder = _fileService.GetAllFiles(mediaFolderPath).ToList();
            var filesInDatabase = await _databaseService.GetAllFilePathsAsync();

            var ghosts = filesInDatabase.Except(filesInFolder).ToList();
            if (ghosts.Any())
            {
                _logger.LogInformation("Found {Count} ghost records to delete from the database.", ghosts.Count);
                await _databaseService.DeleteItemsByPathsAsync(ghosts);
            }

            var orphans = filesInFolder.Except(filesInDatabase).ToList();
            if (orphans.Any())
            {
                _logger.LogInformation("Found {Count} orphan files to import into the database.", orphans.Count);
                foreach (var orphanPath in orphans)
                {
                    var newItem = new MediaItem
                    {
                        FileName = Path.GetFileName(orphanPath),
                        FilePath = orphanPath,
                        FileType = Path.GetExtension(orphanPath).ToLowerInvariant()
                    };
                    await _databaseService.AddMediaItemAsync(newItem);
                }
            }
        }
    }
}
