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

        public bool ShowConfirmation(string title, string message)
        {
            var result = MessageBox.Show(message, 
                                         title, 
                                         MessageBoxButton.YesNo, 
                                         MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }
    }
}
