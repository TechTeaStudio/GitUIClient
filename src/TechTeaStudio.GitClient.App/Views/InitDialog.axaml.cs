namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

/// <summary>Result returned from the init-repo dialog on success.</summary>
public sealed record InitRequest(string Path, bool Bare);

public sealed partial class InitDialog : Window
{
    public InitDialog() => InitializeComponent();

    public string InitialPath
    {
        get => PathBox.Text ?? string.Empty;
        set => PathBox.Text = value;
    }

    public bool InitialBare
    {
        get => BareBox.IsChecked ?? false;
        set => BareBox.IsChecked = value;
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
            Title = "Select repository folder",
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });

        if (picked.Count > 0)
        {
            PathBox.Text = picked[0].Path.LocalPath;
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Init_Click(object? sender, RoutedEventArgs e)
    {
        var path = PathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            // Leave the dialog open so the user can fix it.
            return;
        }
        Close(new InitRequest(path, BareBox.IsChecked ?? false));
    }
}
