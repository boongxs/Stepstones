using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace stepstones.Views
{
    public partial class EnlargeVideoView : UserControl, INotifyPropertyChanged
    {
        private bool _isPlaying = false;
        private readonly DispatcherTimer _indicatorTimer;
        private readonly DispatcherTimer _inactivityTimer;
        private readonly DispatcherTimer _volumePopupTimer;
        private bool _isOverlayVisible = true;
        private bool _isMuted = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMuted
        {
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    OnPropertyChanged();
                }
            }
        }

        private const int MinimumDisplaySize = 400;
        public int MinSize => MinimumDisplaySize;

        public EnlargeVideoView()
        {
            InitializeComponent();

            this.Loaded += EnlargeVideoView_Loaded;
            this.Unloaded += EnlargeVideoView_Unloaded;
            this.MouseLeftButtonDown += EnlargeVideoView_MouseLeftButtonDown;
            this.MouseMove += EnlargeVideoView_MouseMove;

            _indicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _indicatorTimer.Tick += IndicatorTimer_Tick;

            _inactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _inactivityTimer.Tick += InactivityTimer_Tick;

            _volumePopupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _volumePopupTimer.Tick += VolumePopupTimer_Tick;
        }

        private void EnlargeVideoView_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Play();
            IsPlaying = true;
            _inactivityTimer.Start();
        }

        private void EnlargeVideoView_Unloaded(object sender, RoutedEventArgs e)
        {
            _indicatorTimer.Stop();
            _inactivityTimer.Stop();
            MediaPlayer.Stop();
            MediaPlayer.Close();
        }

        private void EnlargeVideoView_MouseLeftButtonDown(object sender, MouseButtonEventArgs? e)
        {
            if (IsPlaying)
            {
                StopAnimationAndHide(PlayIndicator);
                PauseIndicator.Visibility = Visibility.Visible;
                AnimateElement(PauseIndicator, "FadeInAnimation");

                MediaPlayer.Pause();
                IsPlaying = false;
            }
            else
            {
                StopAnimationAndHide(PauseIndicator);
                PlayIndicator.Visibility = Visibility.Visible;
                AnimateElement(PlayIndicator, "FadeInAnimation");

                MediaPlayer.Play();
                IsPlaying = true;
            }

            _indicatorTimer.Stop();
            _indicatorTimer.Start();

            if (e != null)
            {
                e.Handled = true;
            }
        }

        private void EnlargeVideoView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isOverlayVisible)
            {
                AnimateElement(ControlsOverlay, "FadeInAnimation");
                _isOverlayVisible = true;
            }

            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            EnlargeVideoView_MouseLeftButtonDown(this, null);
        }

        private void ControlsOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // to stop a click on controls overlay to pause/play media file
            e.Handled = true;
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
            IsMuted = MediaPlayer.IsMuted;
        }

        private void IndicatorTimer_Tick(object sender, EventArgs e)
        {
            if (PlayIndicator.Opacity > 0)
            {
                AnimateElement(PlayIndicator, "FadeOutAnimation");
            }
            if (PauseIndicator.Opacity > 0)
            {
                AnimateElement(PauseIndicator, "FadeOutAnimation");
            }

            _indicatorTimer.Stop();
        }

        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            _inactivityTimer.Stop();

            if (_isOverlayVisible && !ControlsOverlay.IsMouseOver && !VolumePopup.IsMouseOver)
            {
                AnimateElement(ControlsOverlay, "FadeOutAnimation");
                _isOverlayVisible = false;
            }
        }

        private void VolumePopupTimer_Tick(object? sender, EventArgs e)
        {
            _volumePopupTimer.Stop();
            if (!VolumeControlContainer.IsMouseOver && !VolumePopup.IsMouseOver)
            {
                VolumePopup.IsOpen = false;
            }
        }

        private void AnimateElement(FrameworkElement element, string storyboardName)
        {
            var storyboard = (Storyboard)this.FindResource(storyboardName);
            storyboard.Begin(element);
        }

        private void StopAnimationAndHide(FrameworkElement icon)
        {
            icon.BeginAnimation(UIElement.OpacityProperty, null);
            icon.Opacity = 0;
            icon.Visibility = Visibility.Collapsed;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.IsMuted = e.NewValue == 0;
            IsMuted = MediaPlayer.IsMuted;
        }

        private void VolumeControlContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Stop();
            VolumePopup.IsOpen = true;
        }

        private void VolumeControlContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Start();
        }

        private void VolumePopup_MouseEnter(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Stop();
        }

        private void VolumePopup_MouseLeave(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Start();
        }
    }
}