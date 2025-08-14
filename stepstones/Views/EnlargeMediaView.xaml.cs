using stepstones.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace stepstones.Views
{
    public partial class EnlargeMediaView : UserControl
    {
        private bool _isPlaying = false;

        public EnlargeMediaView()
        {
            InitializeComponent();
            this.Loaded += EnlargeMediaViewModel_Loaded;
        }

        private void EnlargeMediaViewModel_Loaded(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.Opacity > 0)
            {
                VideoPlayer.Play();
                _isPlaying = true;
            }
        }

        private void VideoPlayer_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPlaying)
            {
                VideoPlayer.Pause();
                _isPlaying = false;
            }
            else
            {
                VideoPlayer.Play();
                _isPlaying = true;
            }

            e.Handled = true;
        }

        private void OnMediaViewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
