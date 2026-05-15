namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Result returned from <see cref="RevertDialog"/> on success.</summary>
public sealed record RevertRequest(string Sha, bool CommitOnSuccess);

public sealed partial class RevertDialog : Window
{
    private readonly string _sha;

    public RevertDialog()
        : this(string.Empty, string.Empty)
    {
    }

    public RevertDialog(string sha, string summary)
    {
        InitializeComponent();

        _sha = sha ?? string.Empty;
        ShaBlock.Text = _sha;
        SummaryBlock.Text = string.IsNullOrWhiteSpace(summary) ? "(no summary)" : summary;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Revert_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sha))
            return;
        Close(new RevertRequest(_sha, CommitOnSuccessBox.IsChecked == true));
    }
}
