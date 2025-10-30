namespace stepstones.Views
{
    public partial class EnlargeVideoView : MediaPlayerViewBase
    {
        public EnlargeVideoView()
        {
            InitializeComponent();
            base.MediaPlayer = this.MediaPlayer;
            base.PlayIndicator = this.PlayIndicator;
            base.PauseIndicator = this.PauseIndicator;
            base.ControlsOverlay = this.ControlsOverlay;
            base.VolumePopup = this.VolumePopup;
            base.VolumeControlContainer = this.VolumeControlContainer;
        }

        private void MediaPlayer_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            base.IsPlaying = false;
            base._isVideoEnded = true;
        }
    }
}