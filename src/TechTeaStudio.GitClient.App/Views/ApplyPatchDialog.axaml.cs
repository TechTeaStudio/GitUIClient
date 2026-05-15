namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

public sealed partial class ApplyPatchDialog : Window
{
    public ApplyPatchDialog() => InitializeComponent();

    public string InitialPath
    {
        get => PathBox.Text ?? string.Empty;
        set => PathBox.Text = value;
    }

    public bool IndexOnly
    {
        get => IndexOnlyBox.IsChecked ?? false;
        set => IndexOnlyBox.IsChecked = value;
    }

    private async void PickFile_Click(object? sender, RoutedEventArgs e)
    {
        var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select patch file",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Patch files")
                {
                    Patterns = ["*.patch", "*.diff"],
                },
                new FilePickerFileType("All files") { Patterns = ["*"] },
            ],
        });

        if (picked.Count > 0)
            PathBox.Text = picked[0].Path.LocalPath;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        var path = PathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(path))
            return;
        Close(new ApplyPatchRequest(path, IndexOnlyBox.IsChecked ?? false));
    }
}

/// <summary>Result returned from the apply-patch dialog on success.</summary>
public sealed record ApplyPatchRequest(string PatchPath, bool IndexOnly);
