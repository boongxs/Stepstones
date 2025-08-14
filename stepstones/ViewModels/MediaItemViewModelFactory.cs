using CommunityToolkit.Mvvm.Messaging;
using stepstones.Models;
using stepstones.Services;

namespace stepstones.ViewModels
{
    public class MediaItemViewModelFactory : IMediaItemViewModelFactory
    {
        private readonly IClipboardService _clipboardService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IFileService _fileService;
        private readonly IDatabaseService _databaseService;
        private readonly IMessenger _messenger;
        private readonly IImageDimensionService _imageDimensionService;
        private readonly IDialogCoordinatorService _dialogCoordinatorService;

        public MediaItemViewModelFactory(IClipboardService clipboardService,
                                         IMessageBoxService messageBoxService,
                                         IFileService fileService,
                                         IDatabaseService databaseService,
                                         IMessenger messsenger,
                                         IImageDimensionService imageDimensionService,
                                         IDialogCoordinatorService dialogCoordinatorService)
        {
            _clipboardService = clipboardService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _messenger = messsenger;
            _imageDimensionService = imageDimensionService;
            _dialogCoordinatorService = dialogCoordinatorService;
        }

        public MediaItemViewModel Create(MediaItem mediaItem)
        {
            return new MediaItemViewModel(mediaItem,
                                          _clipboardService,
                                          _messageBoxService,
                                          _fileService,
                                          _databaseService,
                                          _messenger,
                                          _imageDimensionService,
                                          _dialogCoordinatorService);
        }
    }
}
