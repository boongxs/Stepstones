namespace stepstones.Services.Interaction
{
    public interface IMessageBoxService
    {
        void Show(string message);
        bool ShowConfirmation(string title, string message);
    }
}
