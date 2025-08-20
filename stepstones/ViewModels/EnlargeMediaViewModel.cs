using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;
using stepstones.Models;

namespace stepstones.ViewModels
{
    public partial class EnlargeMediaViewModel : ObservableObject
    {
        public BitmapImage? ImageSource { get; }
        public string FilePath { get; }
        public MediaType FileType { get; }
        public int OriginalWidth { get; }
        public int OriginalHeight { get; }

        public bool IsImage { get; }
        public bool IsVideo { get; }
        public bool IsGif { get; }

        public EnlargeMediaViewModel(string filePath, MediaType fileType, int width, int height, BitmapImage? imageSource = null)
        {
            FilePath = filePath;
            FileType = fileType;
            OriginalWidth = width;
            OriginalHeight = height;
            ImageSource = imageSource;

            IsImage = fileType == MediaType.Image;
            IsVideo = fileType == MediaType.Video;
            IsGif = fileType == MediaType.Gif;
        }
    }
}
