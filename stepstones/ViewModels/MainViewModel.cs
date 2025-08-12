using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace stepstones.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public MainViewModel(ILogger<MainViewModel> logger)
        {
            logger.LogInformation("MainViewModel has been created.");
        }
    }
}
