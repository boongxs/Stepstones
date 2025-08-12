using Ookii.Dialogs.Wpf;

namespace stepstones.Services
{
    public class FolderDialogService : IFolderDialogService
    {
        public string? ShowDialog()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select a folder";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog().GetValueOrDefault())
            {
                return dialog.SelectedPath;
            }

            return null;
        }
    }
}
