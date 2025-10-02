using Ookii.Dialogs.Wpf;
using static stepstones.Resources.AppConstants;

namespace stepstones.Services.Interaction
{
    public class FolderDialogService : IFolderDialogService
    {
        public string? ShowDialog()
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = SelectFolderDialogDescription;
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog().GetValueOrDefault())
            {
                return dialog.SelectedPath;
            }

            return null;
        }
    }
}
