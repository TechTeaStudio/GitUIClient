namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class AddNoteDialog : Window
{
    private readonly string _sha;

    public AddNoteDialog() : this(string.Empty) { }

    public AddNoteDialog(string sha)
    {
        _sha = sha ?? string.Empty;
        InitializeComponent();
        // Show a shortened SHA in the header but keep the full one for the result.
        ShaLabel.Text = _sha.Length >= 12 ? _sha[..12] : _sha;
        ShaLabel.SetValue(ToolTip.TipProperty, _sha);
    }

    public string InitialMessage
    {
        get => MessageBox.Text ?? string.Empty;
        set => MessageBox.Text = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        var msg = MessageBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(_sha) || string.IsNullOrWhiteSpace(msg))
            return;
        Close(new AddNoteRequest(_sha, msg));
    }
}

/// <summary>Result returned from the add-note dialog on success.</summary>
public sealed record AddNoteRequest(string Sha, string Message);
