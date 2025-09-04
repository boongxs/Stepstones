using System.Windows.Media.Imaging;
using stepstones.Models;

namespace stepstones.ViewModels
{
    public class EnlargeImageViewModel : EnlargeMediaViewModelBase
    {
        public EnlargeImageViewModel(string filePath, MediaType fileType, int width, int height, BitmapImage? imageSource)
            : base(filePath, fileType, width, height, imageSource)
        {

        }
    }
}
