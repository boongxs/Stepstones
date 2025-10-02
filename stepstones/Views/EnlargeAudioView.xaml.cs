using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using static stepstones.Resources.AppConstants;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

namespace stepstones.Views
{
    public partial class EnlargeAudioView : UserControl, INotifyPropertyChanged
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

        public EnlargeAudioView()
        {
            InitializeComponent();

            this.Loaded += EnlargeAudioView_Loaded;
            this.Unloaded += EnlargeAudioView_Unloaded;
            this.MouseLeftButtonDown += EnlargeAudioView_MouseLeftButtonDown;
            this.MouseMove += EnlargeAudioView_MouseMove;

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

        private void EnlargeAudioView_Loaded(object sender, RoutedEventArgs e)
        {
            MediaPlayer.Play();
            IsPlaying = true;
            _inactivityTimer.Start();
        }

        private void EnlargeAudioView_Unloaded(object sender, RoutedEventArgs e)
        {
            _indicatorTimer.Stop();
            _inactivityTimer.Stop();
            MediaPlayer.Stop();
            MediaPlayer.Close();
        }

        private void EnlargeAudioView_MouseLeftButtonDown(object sender, MouseButtonEventArgs? e)
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

        private void EnlargeAudioView_MouseMove(object sender, MouseEventArgs e)
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
            EnlargeAudioView_MouseLeftButtonDown(this, null);
        }

        private void ControlsOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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

        private void VolumeSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Thumb && sender is Slider slider)
            {
                UpdateSliderValueFromMousePosition(slider, e);
                slider.CaptureMouse();
            }
        }

        private void VolumeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Slider slider && slider.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
            {
                UpdateSliderValueFromMousePosition(slider, e);
            }
        }

        private void VolumeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                slider.ReleaseMouseCapture();
            }
        }

        private void UpdateSliderValueFromMousePosition(Slider slider, MouseEventArgs e)
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
