using CommunityToolkit.Mvvm.Messaging.Messages;
using stepstones.ViewModels;

namespace stepstones.Messages
{
    public class MediaItemDeletedMessage : ValueChangedMessage<MediaItemViewModel>
    {
        public MediaItemDeletedMessage(MediaItemViewModel value) : base(value)
        {

        }
    }
}
