using stepstones.Enums;

namespace stepstones.Messages
{
    public class ShowToastMessage
    {
        public string Message { get; }
        public ToastNotificationType Type { get; }

        public ShowToastMessage(string message, ToastNotificationType type)
        {
            Message = message; 
            Type = type;
        }
    }
}
