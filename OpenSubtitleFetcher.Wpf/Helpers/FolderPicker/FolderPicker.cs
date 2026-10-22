using Microsoft.Win32;

namespace OpenSubtitleFetcher.Wpf.Helpers.FolderPicker;

public interface IFolderPicker
{
    string[] SelectFolders(bool multiselect = true);
}

public class FolderPicker : IFolderPicker
{
    public string[] SelectFolders(bool multiselect = true)
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Multiselect = multiselect,
        };

        return openFolderDialog.ShowDialog() != true ? [] : openFolderDialog.FolderNames;
    }
}