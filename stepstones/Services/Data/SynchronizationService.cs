using Microsoft.Extensions.Logging;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;

namespace stepstones.Services.Data
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly ILogger<SynchronizationService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;
        private readonly IMediaItemProcessorService _mediaItemProcessorService;

        public SynchronizationService(ILogger<SynchronizationService> logger,  
                                      IDatabaseService databaseService, 
                                      IFileService fileService,
                                      IMediaItemProcessorService mediaItemProcessorService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _fileService = fileService;
            _mediaItemProcessorService = mediaItemProcessorService;
        }

        public async Task DeleteGhostRecordsAsync(string folderPath)
        {
            var filesInFolder = _fileService.GetAllFiles(folderPath).ToList();
            var filePathsInDatabase = await _databaseService.GetFilePathsForFolderAsync(folderPath);

            var ghosts = filePathsInDatabase.Except(filesInFolder).ToList();
            if (ghosts.Count != 0)
            {
                _logger.LogInformation("Found {Count} ghost records to delete from the database.", ghosts.Count);
                await _databaseService.DeleteItemsByPathsAsync(ghosts);
            }
        }

        public async Task SynchronizeOrphanFilesAsync(string folderPath, IProgress<(string Main, string Detail)> progress)
        {
            var filesInFolder = _fileService.GetAllFiles(folderPath).ToList();
            var filePathsInDatabase = await _databaseService.GetFilePathsForFolderAsync(folderPath);
            var orphans = filesInFolder.Except(filePathsInDatabase).ToList();

            if (orphans.Count == 0)
            {
                return;
            }

            await _mediaItemProcessorService.ProcessOrphanFilesAsync(orphans, progress);
        }
    }
}
