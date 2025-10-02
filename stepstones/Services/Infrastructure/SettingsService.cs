using Microsoft.Extensions.Logging;
using System.IO;
using static stepstones.Resources.AppConstants;

namespace stepstones.Services.Infrastructure
{
    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _appDataFolder;
        private readonly string _settingsFilePath;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
            _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolderName);
            _settingsFilePath = Path.Combine(_appDataFolder, MediaFolderPathFileName);
        }

        public string? LoadMediaFolderPath()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var path = File.ReadAllText(_settingsFilePath);
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        _logger.LogInformation("Media folder path loaded from settings file: '{Path}'", path);
                        return path;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load media folder path from settings file.");
            }

            _logger.LogInformation("No saved media folder path found.");
            return null;
        }

        public void SaveMediaFolderPath(string path)
        {
            try
            {
                Directory.CreateDirectory(_appDataFolder);
                File.WriteAllText(_settingsFilePath, path);
                _logger.LogInformation("Successfully saved media folder path: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save media folder path to settings file.");
            }
        }
    }
}
