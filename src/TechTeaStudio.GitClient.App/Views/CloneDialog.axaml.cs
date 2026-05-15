namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

public sealed partial class CloneDialog : Window
{
    public CloneDialog() => InitializeComponent();

    public string InitialUrl
    {
        get => UrlBox.Text ?? string.Empty;
        set => UrlBox.Text = value;
    }

    public string InitialPath
    {
        get => PathBox.Text ?? string.Empty;
        set => PathBox.Text = value;
    }

    private async void PickFolder_Click(object? sender, RoutedEventArgs e)
    {
        var storage = StorageProvider;

        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(PathBox.Text))
        {
            try { start = await storage.TryGetFolderFromPathAsync(PathBox.Text!); }
            catch { /* unreachable path; pick unscoped */ }
        }

        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select destination folder",
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });

        if (picked.Count > 0)
        {
            PathBox.Text = picked[0].Path.LocalPath;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Clone_Click(object? sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        var path = PathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(path))
        {
            // Leave the dialog open so the user can fix the missing field.
            return;
        }
        Close(new CloneRequest(url, path));
    }
}
