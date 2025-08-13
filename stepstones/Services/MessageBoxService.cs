using System.Windows;

namespace stepstones.Services
{
    public class MessageBoxService : IMessageBoxService
    {
        public void Show(string message)
        {
            MessageBox.Show(message,
                            "Stepstones",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
        }
    }
}
