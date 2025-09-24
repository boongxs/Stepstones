using stepstones.Models;

namespace stepstones.ViewModels
{
    public class EnlargeVideoViewModel : EnlargeMediaViewModelBase
    {
        public EnlargeVideoViewModel(string filePath, MediaType fileType, int width, int height)
            : base(filePath, fileType, width, height)
        {
            
        }
    }
}
