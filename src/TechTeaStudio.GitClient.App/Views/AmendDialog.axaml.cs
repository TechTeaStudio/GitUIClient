namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Result returned from the amend dialog on success.</summary>
public sealed record AmendRequest(string Message, bool IncludeStaged);

public sealed partial class AmendDialog : Window
{
    public AmendDialog() => InitializeComponent();

    /// <summary>
    /// Pre-fill the message box with the previous commit's message — the user
    /// can then edit or replace it.
    /// </summary>
    public string PreviousMessage
    {
        get => MessageBox.Text ?? string.Empty;
        set => MessageBox.Text = value;
    }

    public bool IncludeStaged
    {
        get => IncludeStagedBox.IsChecked ?? false;
        set => IncludeStagedBox.IsChecked = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Amend_Click(object? sender, RoutedEventArgs e)
    {
        var msg = MessageBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(msg))
        {
            // Empty commit message is not valid for an amend.
            return;
        }
        Close(new AmendRequest(msg, IncludeStagedBox.IsChecked ?? false));
    }
}
