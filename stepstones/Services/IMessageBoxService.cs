namespace stepstones.Services
{
    public interface IMessageBoxService
    {
        void Show(string message);
        bool ShowConfirmation(string title, string message);
    }
}
