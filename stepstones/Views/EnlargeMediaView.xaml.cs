using System.Windows;
using System.Windows.Controls;

namespace stepstones.Views
{
    public partial class EnlargeMediaView : UserControl
    {
        private bool _isVideoPlaying = false;

        public EnlargeMediaView()
        {
            InitializeComponent();
            this.Loaded += EnlargeMediaViewModel_Loaded;
        }

        private void EnlargeMediaViewModel_Loaded(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Source != null)
            {
                VideoPlayer.Play();
                _isVideoPlaying = true;
            }

            if (GifPlayer.Source != null)
            {
                GifPlayer.Play();
            }
        }

        private void VideoPlayer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isVideoPlaying)
            {
                VideoPlayer.Pause();
                _isVideoPlaying = false;
            }
            else
            {
                VideoPlayer.Play();
                _isVideoPlaying = true;
            }

            e.Handled = true;
        }

        private void GifPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            GifPlayer.Position = TimeSpan.FromSeconds(0);
            GifPlayer.Play();
        }

        private void OnMediaViewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
