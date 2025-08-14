using CommunityToolkit.Mvvm.Messaging.Messages;
using stepstones.Models;

namespace stepstones.Messages
{
    public class RequestEditTagsMessage : RequestMessage<EditTagsResult>
    {
        public string? InitialTags { get; }

        public RequestEditTagsMessage(string? initialTags)
        {
            InitialTags = initialTags; 
        }
    }
}
