namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class RenameBranchDialog : Window
{
    public RenameBranchDialog() => InitializeComponent();

    public string CurrentName
    {
        get => CurrentNameBox.Text ?? string.Empty;
        set => CurrentNameBox.Text = value;
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
        if (string.IsNullOrWhiteSpace(newName))
        {
            // Leave open so user can fix the missing new name.
            return;
        }

        Close(newName);
    }
}
