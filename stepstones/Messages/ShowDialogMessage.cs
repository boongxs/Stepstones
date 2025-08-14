namespace stepstones.Messages
{
    public class ShowDialogMessage
    {
        public object ViewModel { get; }

        public ShowDialogMessage(object viewModel)
        {
            ViewModel = viewModel;
        }
    }
}
