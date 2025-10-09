namespace stepstones.Resources
{
    public static class AppConstants
    {
        #region File System

        public const string AppDataFolderName = "stepstones";
        public const string DatabaseFileName = "stepstones.db";
        public const string LogsFolderName = "logs";
        public const string LogFileNameFormat = "stepstones-.txt";
        public const string ThumbnailCacheFolderName = "thumbnails";
        public const string TranscodedCacheFolderName = "transcode-cache";
        public const string MediaFolderPathFileName = "media_folder.path";

        #endregion

        #region File Extensions

        public const string GifExtension = ".gif";
        public const string PngExtension = ".png";
        public const string JpgExtension = ".jpg";
        public const string Mp4Extension = ".mp4";

        #endregion

        #region UI Text
        public const string PageInfoFormat = "Page {0} of {1}";

        #endregion

        #region Empty View Messages

        public const string NoMediaFolderTitle = "No media folder selected";
        public const string NoMediaFolderSubtitle = "Use the Folder button to select a media folder";
        public const string EmptyFolderTitle = "This media folder is empty";
        public const string EmptyFolderSubtitle = "Use the Upload button to import some media files (e.g. pictures, videos, GIFs...";

        #endregion

        #region Dialog and Toast Messages

        public const string EditTagsDialogTitle = "Tags";
        public const string TranscodingProgressText = "Preparing video for playback, please wait...";
        public const string SelectFilesDialogTitle = "Select file(s) to upload";
        public const string SelectFolderDialogDescription = "Select a folder";
        public const string NoMediaFolderSetTitle = "No media folder set";
        public const string NoMediaFolderSetMessage = "No media folder path has been set, please set it first before uploading file(s).";
        public const string DeleteFileConfirmationTitle = "Delete File";
        public const string DeleteFileConfirmationMessage = "Are you sure you want to permanently delete '{0}'?";
        public const string FileCopiedSuccessMessage = "File copied to clipboard.";
        public const string FileCopyErrorMessage = "Failed to copy file.";
        public const string TagsUpdateSuccessMessage = "Tags updated successfully.";
        public const string TagsUpdateErrorMessage = "Failed to save tags.";
        public const string FileDeleteSuccessMessage = "'{0}' was deleted.";
        public const string FileDeleteErrorMessage = "Failed to delete '{0}'.";
        public const string FolderLoadSuccessMessage = "Folder '{0}' loaded.";
        public const string FolderLoadErrorMessage = "Failed to load folder.";
        public const string DuplicateFileSkippedMessage = "'{0}' already in media folder. Skipped.";

        #endregion

        #region Numeric Constants

        // Sizing
        public const int MinimumDisplaySize = 400;
        public const double DesiredThumbnailWidth = 270;
        public const double HorizontalItemMargin = 14;
        public const int DefaultPageSize = 24;
        public const int ThumbnailSize = 250;

        // Timers and delays (in milliseconds)
        public const int FolderWatcherDebounceTime = 2000;
        public const int ToastNotificationDuration = 3100;
        public const int FilterTriggerDelay = 300;
        public const int IndicatorTimerInterval = 500;
        public const int InactivityTimerInterval = 2000;
        public const int VolumePopupTimerInterval = 200;

        #endregion

        #region Configuration

        public const string CompatibleVideoCodec = "h264";
        public const string TranscodeVideoCodec = "libx264";
        public const string TranscodeAudioCodec = "aac";

        #endregion

        #region Command Parameters

        public const string SaveCommandParameter = "Save";
        public const string CancelCommandParameter = "Cancel";

        #endregion
    }
}
