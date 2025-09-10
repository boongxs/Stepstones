using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using stepstones.ViewModels;
using Vlc.DotNet.Core;

namespace stepstones.Views
{
    public partial class EnlargeVideoView : UserControl
    {
        private readonly DispatcherTimer _indicatorTimer;
        private bool _videoHasEnded = false;
        private const int MinimumDisplaySize = 400;
        public int MinSize => MinimumDisplaySize;

        public EnlargeVideoView()
        {
            InitializeComponent();

            this.DataContextChanged += EnlargeVideoView_DataContextChanged;
            this.Unloaded += EnlargeVideoView_Unloaded;

            _indicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(750)
            };
            _indicatorTimer.Tick += IndicatorTimer_Tick;

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

            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "stepstones", "vlc-cache");
            Directory.CreateDirectory(appDataFolder);

            var options = new string[]
            {
                $"--config={appDataFolder}\\vlcrc",
                "--no-video-title-show",
                "--no-sub-autodetect-file"
            };

            MediaPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, options);

            MediaPlayer.SourceProvider.MediaPlayer.EndReached += MediaPlayer_EndReached;
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

        private void MediaPlayer_EndReached(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _videoHasEnded = true;

                StopAnimationAndHide(PlayIcon);
                StopAnimationAndHide(PauseIcon);

                EndOverlay.Opacity = 1;
            });
        }

        private void MediaPlayer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_videoHasEnded)
            {
                e.Handled = true;
                return;
            }

            if (MediaPlayer.SourceProvider.MediaPlayer.IsPlaying())
            {
                StopAnimationAndHide(PlayIcon);
                AnimateIcon(PauseIcon, "FadeInAnimation");
            }
            else
            {
                StopAnimationAndHide(PauseIcon);
                AnimateIcon(PlayIcon, "FadeInAnimation");
            }

            _indicatorTimer.Stop();
            _indicatorTimer.Start();

            MediaPlayer.SourceProvider.MediaPlayer.Pause();

            // prevent click event from bubbling up and potentially closing the dialog
            e.Handled = true;
        }

        private void IndicatorTimer_Tick(object sender, EventArgs e)
        {
            if (PlayIcon.Opacity > 0)
            {
                AnimateIcon(PlayIcon, "FadeOutAnimation");
            }
            if (PauseIcon.Opacity > 0)
            {
                AnimateIcon(PauseIcon, "FadeOutAnimation");
            }

            _indicatorTimer.Stop();
        }

        private void AnimateIcon(FrameworkElement icon, string storyboardName)
        {
            var storyboard = (Storyboard)this.Resources[storyboardName];
            storyboard.Begin(icon);
        }

        private void StopAnimationAndHide(FrameworkElement icon)
        {
            icon.BeginAnimation(UIElement.OpacityProperty, null);
            icon.Opacity = 0;
        }
    }
}
