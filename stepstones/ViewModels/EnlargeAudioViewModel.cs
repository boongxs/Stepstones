using stepstones.Models;

namespace stepstones.ViewModels
{
    public class EnlargeAudioViewModel : EnlargeMediaViewModelBase
    {
        public EnlargeAudioViewModel(string filePath, MediaType fileType, int width, int height)
            : base(filePath, fileType, width, height)
        {

        }
    }
}
