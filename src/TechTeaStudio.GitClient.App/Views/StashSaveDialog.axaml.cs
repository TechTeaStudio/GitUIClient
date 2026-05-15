namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Result returned from the stash-save dialog on success.</summary>
public sealed record StashSaveRequest(string? Message, bool IncludeUntracked);

public sealed partial class StashSaveDialog : Window
{
    public StashSaveDialog() => InitializeComponent();

    public string InitialMessage
    {
        get => MessageBox.Text ?? string.Empty;
        set => MessageBox.Text = value;
    }

    public bool InitialIncludeUntracked
    {
        get => IncludeUntrackedBox.IsChecked ?? false;
        set => IncludeUntrackedBox.IsChecked = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var raw = MessageBox.Text?.Trim();
        var message = string.IsNullOrWhiteSpace(raw) ? null : raw;
        Close(new StashSaveRequest(message, IncludeUntrackedBox.IsChecked ?? false));
    }
}
