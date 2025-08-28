using System.Windows;
using System.Windows.Controls;
using stepstones.ViewModels;
using Serilog;
using System.IO;
using Vlc.DotNet.Core;
using Vlc.DotNet.Core.Interops.Signatures;
using System.Threading.Tasks;

namespace stepstones.Views
{
    public partial class EnlargeMediaView : UserControl
    {
        public EnlargeMediaView()
        {
            InitializeComponent();

            this.DataContextChanged += EnlargeMediaView_DataContextChanged;
            this.Unloaded += EnlargeMediaView_Unloaded;

            var currentAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory != null)
            {
                var vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

                var options = new string[]
                {
                    "--no-video-title-show",
                    "--no-sub-autodetect-file",
                    "--no-stats",
                    "--no-osd",
                    "--avcodec-hw=auto"
                };

                MediaPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, options);
            }

            MediaPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
            MediaPlayer.SourceProvider.MediaPlayer.Log += MediaPlayer_Log;
        }

        private async void EnlargeMediaView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is EnlargeMediaViewModel vm && vm.IsPlayableMedia)
            {
                MediaPlayer.SourceProvider.MediaPlayer.Play(new FileInfo(vm.FilePath));
                MediaPlayer.Opacity = 1;
                MediaPlayer.MouseLeftButtonDown += MediaPlayer_MouseLeftButtonDown;
            }
        }

        private async void EnlargeMediaView_Unloaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.MouseLeftButtonDown -= MediaPlayer_MouseLeftButtonDown;
            await Task.Delay(100);
            MediaPlayer.Dispose();
            ImageViewer.Source = null;
        }

        private void MediaPlayer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MediaPlayer.SourceProvider.MediaPlayer.Pause();

            e.Handled = true;
        }

        private void MediaPlayer_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (DataContext is EnlargeMediaViewModel vm && vm.IsGif)
                {
                    MediaPlayer.SourceProvider.MediaPlayer.Play();
                }
            });
        }

        private void MediaPlayer_Log(object sender, VlcMediaPlayerLogEventArgs e)
        {
            if (e.Level == VlcLogLevel.Error)
            {
                Log.Error("VLC Engine error: {Message} (Module: {Module})", e.Message, e.Module);
            }
        }

        private void OnMediaViewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
