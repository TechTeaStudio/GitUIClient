namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>
/// Modal dialog for renaming a remote. The current name is read-only; user
/// types the new name. Returns the new name on success, <c>null</c> on cancel.
/// </summary>
public sealed partial class RenameRemoteDialog : Window
{
    public RenameRemoteDialog() => InitializeComponent();

    public string CurrentName
    {
        get => CurrentNameBox.Text ?? string.Empty;
        set
        {
            CurrentNameBox.Text = value;
            NewNameBox.Text ??= value;
        }
    }

    public string InitialNewName
    {
        get => NewNameBox.Text ?? string.Empty;
        set => NewNameBox.Text = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Rename_Click(object? sender, RoutedEventArgs e)
    {
        var newName = NewNameBox.Text?.Trim();
        var currentName = CurrentNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newName) || string.Equals(newName, currentName, System.StringComparison.Ordinal))
        {
            return;
        }
        Close(newName);
    }
}
