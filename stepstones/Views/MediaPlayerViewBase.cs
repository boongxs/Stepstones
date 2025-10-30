using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;
using static stepstones.Resources.AppConstants;

namespace stepstones.Views
{
    public abstract class MediaPlayerViewBase : UserControl, INotifyPropertyChanged
    {
        private bool _isPlaying = false;
        private readonly DispatcherTimer _indicatorTimer;
        private readonly DispatcherTimer _inactivityTimer;
        private readonly DispatcherTimer _volumePopupTimer;
        private bool _isOverlayVisible = true;
        private bool _isMuted = false;

        protected MediaElement? MediaPlayer;
        protected FrameworkElement? PlayIndicator;
        protected FrameworkElement? PauseIndicator;
        protected FrameworkElement? ControlsOverlay;
        protected Popup? VolumePopup;
        protected FrameworkElement? VolumeControlContainer;

        protected bool _isVideoEnded = false;

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

        public int MinSize => MinimumDisplaySize;

        public MediaPlayerViewBase()
        {
            this.Loaded += EnlargeView_Loaded;
            this.Unloaded += EnlargeView_Unloaded;
            this.MouseLeftButtonDown += EnlargeView_MouseLeftButtonDown;
            this.MouseMove += EnlargeView_MouseMove;

            _indicatorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(IndicatorTimerInterval)
            };
            _indicatorTimer.Tick += IndicatorTimer_Tick;

            _inactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(InactivityTimerInterval)
            };
            _inactivityTimer.Tick += InactivityTimer_Tick;

            _volumePopupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(VolumePopupTimerInterval)
            };
            _volumePopupTimer.Tick += VolumePopupTimer_Tick;
        }

        protected void EnlargeView_Loaded(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer == null)
            {
                return;
            }

            MediaPlayer.Play();
            IsPlaying = true;
            _inactivityTimer.Start();
        }

        protected void EnlargeView_Unloaded(object sender, RoutedEventArgs e)
        {
            _indicatorTimer.Stop();
            _inactivityTimer.Stop();

            if (MediaPlayer == null)
            {
                return;
            }

            MediaPlayer.Stop();
            MediaPlayer.Close();
        }

        protected void EnlargeView_MouseLeftButtonDown(object sender, MouseButtonEventArgs? e)
        {
            if (MediaPlayer == null || PlayIndicator == null || PauseIndicator == null)
            {
                return;
            }

            if (IsPlaying)
            {
                StopAnimationAndHide(PlayIndicator);
                PauseIndicator.Visibility = Visibility.Visible;

                if (!_isVideoEnded)
                {
                    AnimateElement(PauseIndicator, "FadeInAnimation");
                }

                MediaPlayer.Pause();
                IsPlaying = false;
            }
            else
            {
                StopAnimationAndHide(PauseIndicator);
                PlayIndicator.Visibility = Visibility.Visible;

                if (!_isVideoEnded)
                {
                    AnimateElement(PlayIndicator, "FadeInAnimation");
                }

                MediaPlayer.Play();
                IsPlaying = true;
            }

            if (!_isVideoEnded)
            {
                _indicatorTimer.Stop();
                _indicatorTimer.Start();
            }
            else
            {
                StopAnimationAndHide(PlayIndicator);
                StopAnimationAndHide(PauseIndicator);
            }

            _inactivityTimer.Stop();
            _inactivityTimer.Start();

            if (e != null)
            {
                e.Handled = true;
            }
        }

        protected void EnlargeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isOverlayVisible)
            {
                if (ControlsOverlay == null)
                {
                    return;
                }

                AnimateElement(ControlsOverlay, "FadeInAnimation");
                _isOverlayVisible = true;
            }

            _inactivityTimer.Stop();
            _inactivityTimer.Start();
        }

        protected void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            EnlargeView_MouseLeftButtonDown(this, null);
        }

        protected void ControlsOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        protected void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer == null)
            {
                return;
            }

            MediaPlayer.IsMuted = !MediaPlayer.IsMuted;
            IsMuted = MediaPlayer.IsMuted;
        }

        protected void IndicatorTimer_Tick(object sender, EventArgs e)
        {
            if (!_isVideoEnded)
            {
                if (PlayIndicator != null && PlayIndicator.Opacity > 0)
                {
                    AnimateElement(PlayIndicator, "FadeOutAnimation");
                }
                if (PauseIndicator != null && PauseIndicator.Opacity > 0)
                {
                    AnimateElement(PauseIndicator, "FadeOutAnimation");
                }
            }

            _indicatorTimer.Stop();
        }

        protected void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            _inactivityTimer.Stop();

            if (_isOverlayVisible
                && ControlsOverlay != null
                && !ControlsOverlay.IsMouseOver
                && VolumePopup != null
                && !VolumePopup.IsMouseOver)
            {
                AnimateElement(ControlsOverlay, "FadeOutAnimation");
                _isOverlayVisible = false;
            }
        }

        protected void VolumePopupTimer_Tick(object? sender, EventArgs e)
        {
            _volumePopupTimer.Stop();

            if (VolumeControlContainer != null
                && !VolumeControlContainer.IsMouseOver
                && VolumePopup != null
                && !VolumePopup.IsMouseOver)
            {
                VolumePopup.IsOpen = false;
            }
        }

        protected void AnimateElement(FrameworkElement element, string storyboardName)
        {
            var storyboard = (Storyboard)this.FindResource(storyboardName);

            storyboard.Begin(element);
        }

        protected void StopAnimationAndHide(FrameworkElement icon)
        {
            icon.BeginAnimation(UIElement.OpacityProperty, null);
            icon.Opacity = 0;
            icon.Visibility = Visibility.Collapsed;
        }

        protected void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaPlayer == null)
            {
                return;
            }

            MediaPlayer.IsMuted = e.NewValue == 0;
            IsMuted = MediaPlayer.IsMuted;
        }

        protected void VolumeControlContainer_MouseEnter(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Stop();
            if (VolumePopup != null)
            {
                VolumePopup.IsOpen = true;
            }
        }

        protected void VolumeControlContainer_MouseLeave(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Start();
        }

        protected void VolumePopup_MouseEnter(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Stop();
        }

        protected void VolumePopup_MouseLeave(object sender, MouseEventArgs e)
        {
            _volumePopupTimer.Start();
        }

        protected void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Thumb && sender is Slider slider)
            {
                UpdateSliderValueFromMousePosition(slider, e);
                slider.CaptureMouse();
            }
        }

        protected void VolumeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider
                && slider.IsMouseCaptured
                && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSliderValueFromMousePosition(slider, e);
            }
        }

        protected void VolumeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.ReleaseMouseCapture();
            }
        }

        protected void UpdateSliderValueFromMousePosition(Slider slider, MouseEventArgs e)
        {
            if (slider.Template.FindName("PART_Track", slider) is Track track)
            {
                Point point = e.GetPosition(track);
                double newValue = track.Maximum - ((point.Y / track.ActualHeight) * (track.Maximum - track.Minimum));
                newValue = Math.Clamp(newValue, slider.Minimum, slider.Maximum);
                slider.Value = newValue;
            }
        }
    }
}
