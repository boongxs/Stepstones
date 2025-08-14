using stepstones.Models;

namespace stepstones.Messages
{
    public class DialogClosedMessage
    {
        public object? Result { get; }

        public DialogClosedMessage(object? result)
        {
            Result = result;
        }
    }
}
