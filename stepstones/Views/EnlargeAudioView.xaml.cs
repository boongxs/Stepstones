using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using static stepstones.Resources.AppConstants;

namespace stepstones.Views
{
    public partial class EnlargeAudioView : UserControl
    {
        private bool _isPlaying = false;
        private readonly DispatcherTimer _indicatorTimer;

        public EnlargeAudioView()
        {
            InitializeComponent();

            this.Loaded += EnlargeAudioView_Loaded;
            this.Unloaded += EnlargeAudioView_Unloaded;
            this.MouseLeftButtonDown += EnlargeAudioView_MouseLeftButtonDown;

            _indicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(IndicatorTimerInterval)
            };
            _indicatorTimer.Tick += IndicatorTimer_Tick;
        }

        private void EnlargeAudioView_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Play();
            _isPlaying = true;
        }

        private void EnlargeAudioView_Unloaded(object sender, RoutedEventArgs e)
        {
            _indicatorTimer.Stop();
            MediaPlayer.Stop();
            MediaPlayer.Close();
        }

        private void EnlargeAudioView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPlaying)
            {
                StopAnimationAndHide(PlayIndicator);
                AnimateIcon(PauseIndicator, "FadeInAnimation");

                MediaPlayer.Pause();
                _isPlaying = false;
            }
            else
            {
                StopAnimationAndHide(PauseIndicator);
                AnimateIcon(PlayIndicator, "FadeInAnimation");

                MediaPlayer.Play();
                _isPlaying = true;
            }

            _indicatorTimer.Stop();
            _indicatorTimer.Start();

            e.Handled = true;
        }

        private void IndicatorTimer_Tick(object sender, EventArgs e)
        {
            if (PlayIndicator.Opacity > 0)
            {
                AnimateIcon(PlayIndicator, "FadeOutAnimation");
            }
            if (PauseIndicator.Opacity > 0)
            {
                AnimateIcon(PauseIndicator, "FadeOutAnimation");
            }

            _indicatorTimer.Stop();
        }

        private void AnimateIcon(FrameworkElement icon, string storyboardName)
        {
            var storyboard = (Storyboard)this.FindResource(storyboardName);
            storyboard.Begin(icon);
        }

        private void StopAnimationAndHide(FrameworkElement icon)
        {
            icon.BeginAnimation(UIElement.OpacityProperty, null);
            icon.Opacity = 0;
        }
    }
}
