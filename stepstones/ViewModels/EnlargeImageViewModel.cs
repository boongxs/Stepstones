using stepstones.Models;

namespace stepstones.ViewModels
{
    public class EnlargeImageViewModel : EnlargeMediaViewModelBase
    {
        public EnlargeImageViewModel(string filePath, MediaType fileType, int width, int height)
            : base(filePath, fileType, width, height)
        {

        }
    }
}
