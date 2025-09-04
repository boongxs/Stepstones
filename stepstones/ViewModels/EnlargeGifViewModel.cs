using stepstones.Models;

namespace stepstones.ViewModels
{
    public class EnlargeGifViewModel : EnlargeMediaViewModelBase
    {
        public EnlargeGifViewModel(string filePath, MediaType fileType, int width, int height)
            : base(filePath, fileType, width, height)
        {

        }
    }
}
