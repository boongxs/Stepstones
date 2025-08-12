using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using stepstones.Services;

namespace stepstones.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IFolderDialogService _folderDialogService;

        public MainViewModel(ILogger<MainViewModel> logger,
                             ISettingsService settingsService,
                             IFolderDialogService folderDialogService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _folderDialogService = folderDialogService;

            var savedPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder path: {Path}", savedPath);
            }

            logger.LogInformation("MainViewModel has been created.");
        }

        [RelayCommand]
        private void SelectFolder()
        {
            _logger.LogInformation("'Select Folder' button clicked, opening folder dialog.");

            var selectedPath = _folderDialogService.ShowDialog();

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                _logger.LogWarning("Folder selection was cancelled by the user.");
            }
            else
            {
                _logger.LogInformation("User selected folder: {Path}", selectedPath);
                _settingsService.SaveMediaFolderPath(selectedPath);
            }
        }
    }
}
