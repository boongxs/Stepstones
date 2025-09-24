namespace stepstones.Messages
{
    public class TranscodingStartedMessage
    {
        public CancellationTokenSource CancellationTokenSource { get; }

        public TranscodingStartedMessage(CancellationTokenSource cancellationTokenSource)
        {
            CancellationTokenSource = cancellationTokenSource;
        }
    }
}
