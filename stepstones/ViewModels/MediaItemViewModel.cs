using CommunityToolkit.Mvvm.ComponentModel;
using stepstones.Models;

namespace stepstones.ViewModels
{
    public partial class MediaItemViewModel : ObservableObject
    {
        private readonly MediaItem _mediaItem;

        public MediaItemViewModel(MediaItem mediaItem)
        {
            _mediaItem = mediaItem;
        }

        public string FileName => _mediaItem.FileName;
        public string FilePath => _mediaItem.FilePath;
        public string FileType => _mediaItem.FileType;
        public string? ThumbnailPath => _mediaItem.ThumbnailPath;
    }
}
