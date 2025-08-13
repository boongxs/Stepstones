using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using stepstones.Services;

namespace stepstones.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IFolderDialogService _folderDialogService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IFileService _fileService;

        public MainViewModel(ILogger<MainViewModel> logger,
                             ISettingsService settingsService,
                             IFolderDialogService folderDialogService,
                             IFileDialogService fileDialogService,
                             IMessageBoxService messageBoxService,
                             IFileService fileService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _folderDialogService = folderDialogService;
            _fileDialogService = fileDialogService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;

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

        [RelayCommand]
        private async Task UploadFiles()
        {
            _logger.LogInformation("'Upload' button clicked.");

            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Upload aborted: Media folder path has not been set.");
                _messageBoxService.Show("No media folder path has been set, please set it first before attempting to upload file(s).");
                return;
            }

            var selectedFiles = _fileDialogService.ShowDialog();
            if (selectedFiles == null || !selectedFiles.Any())
            {
                _logger.LogInformation("File selection was cancelled or no files were selected.");
                return;
            }

            var fileList = selectedFiles.ToList();
            _logger.LogInformation("{FileCount} file(s) have been selected for upload.", fileList.Count);

            await _fileService.CopyFilesAsync(fileList, mediaFolderPath);
        }
    }
}
