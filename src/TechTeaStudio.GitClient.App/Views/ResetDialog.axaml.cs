namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

using TechTeaStudio.GitClient.Models;

/// <summary>Result returned from <see cref="ResetDialog"/> on success.</summary>
public sealed record ResetRequest(string Sha, ResetKind Mode);

public sealed partial class ResetDialog : Window
{
    private readonly string _sha;

    public ResetDialog()
        : this(string.Empty)
    {
    }

    public ResetDialog(string sha)
    {
        InitializeComponent();

        _sha = sha ?? string.Empty;
        ShaBlock.Text = _sha;
        UpdateHardWarning();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_sha))
            return;

        var mode = HardRadio.IsChecked == true ? ResetKind.Hard
                 : MixedRadio.IsChecked == true ? ResetKind.Mixed
                 : ResetKind.Soft;

        Close(new ResetRequest(_sha, mode));
    }

    private void ModeChanged(object? sender, RoutedEventArgs e) => UpdateHardWarning();

    private void UpdateHardWarning()
    {
        if (HardWarning is null) return;
        HardWarning.IsVisible = HardRadio?.IsChecked == true;
    }
}
