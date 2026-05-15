namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Result returned from <see cref="CherryPickDialog"/> on success.</summary>
public sealed record CherryPickRequest(string Sha, bool CommitOnSuccess);

public sealed partial class CherryPickDialog : Window
{
    private readonly string _sha;

    public CherryPickDialog()
        : this(string.Empty, string.Empty)
    {
    }

    public CherryPickDialog(string sha, string summary)
    {
        InitializeComponent();

        _sha = sha ?? string.Empty;
        ShaBlock.Text = _sha;
        SummaryBlock.Text = string.IsNullOrWhiteSpace(summary) ? "(no summary)" : summary;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void CherryPick_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sha))
            return;
        Close(new CherryPickRequest(_sha, CommitOnSuccessBox.IsChecked == true));
    }
}
