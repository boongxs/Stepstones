using System.IO;
using System.Windows;
using System.Windows.Controls;
using stepstones.ViewModels;

namespace stepstones.Views
{
    public partial class EnlargeVideoView : UserControl
    {
        public EnlargeVideoView()
        {
            InitializeComponent();

            this.DataContextChanged += EnlargeVideoView_DataContextChanged;
            this.Unloaded += EnlargeVideoView_Unloaded;

            var currentAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (currentAssembly == null)
            {
                return;
            }

            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory == null)
            {
                return;
            }

            var vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            MediaPlayer.SourceProvider.CreatePlayer(vlcLibDirectory);
        }

        private void EnlargeVideoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is EnlargeVideoViewModel vm)
            {
                MediaPlayer.SourceProvider.MediaPlayer.Play(new FileInfo(vm.FilePath));
                MediaPlayer.MouseLeftButtonDown += MediaPlayer_MouseLeftButtonDown;
            }
        }

        private async void EnlargeVideoView_Unloaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.MouseLeftButtonDown -= MediaPlayer_MouseLeftButtonDown;
            await Task.Delay(100);
            MediaPlayer.Dispose();
        }

        private void MediaPlayer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MediaPlayer.SourceProvider.MediaPlayer.Pause();

            // prevent click event from bubbling up and potentially closing the dialog
            e.Handled = true;
        }
    }
}
