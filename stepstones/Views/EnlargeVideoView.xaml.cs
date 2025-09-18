using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace stepstones.Views
{
    public partial class EnlargeVideoView : UserControl
    {
        private bool _isPlaying = false;
        private readonly DispatcherTimer _indicatorTimer;

        private const int MinimumDisplaySize = 400;
        public int MinSize => MinimumDisplaySize;

        public EnlargeVideoView()
        {
            InitializeComponent();

            this.Loaded += EnlargeVideoView_Loaded;
            this.Unloaded += EnlargeVideoView_Unloaded;
            this.MouseLeftButtonDown += EnlargeVideoView_MouseLeftButtonDown;

            _indicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _indicatorTimer.Tick += IndicatorTimer_Tick;
        }

        private void EnlargeVideoView_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Play();
            _isPlaying = true;
        }

        private void EnlargeVideoView_Unloaded(object sender, RoutedEventArgs e)
        {
            _indicatorTimer.Stop();
            MediaPlayer.Stop();
            MediaPlayer.Close();
        }

        private void EnlargeVideoView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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