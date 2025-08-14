using stepstones.Models;

namespace stepstones.ViewModels
{
    public interface IMediaItemViewModelFactory
    {
        MediaItemViewModel Create(MediaItem mediaItem);
    }
}
