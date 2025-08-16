using CommunityToolkit.Mvvm.ComponentModel;
using stepstones.Models;

namespace stepstones.ViewModels
{
    public partial class EnlargeMediaViewModel : ObservableObject
    {
        public string FilePath { get; }
        public MediaType FileType { get; }
        public int OriginalWidth { get; }
        public int OriginalHeight { get; }

        public bool IsImage { get; }
        public bool IsVideo { get; }
        public bool IsGif { get; }

        public EnlargeMediaViewModel(string filePath, MediaType fileType, int width, int height)
        {
            FilePath = filePath;
            FileType = fileType;
            OriginalWidth = width;
            OriginalHeight = height;

            IsImage = fileType == MediaType.Image;
            IsVideo = fileType == MediaType.Video;
            IsGif = fileType == MediaType.Gif;
        }
    }
}
