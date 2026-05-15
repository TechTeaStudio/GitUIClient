namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class CreateTagDialog : Window
{
    public CreateTagDialog() => InitializeComponent();

    public string InitialName
    {
        get => NameBox.Text ?? string.Empty;
        set => NameBox.Text = value;
    }

    public string InitialTargetSha
    {
        get => TargetShaBox.Text ?? string.Empty;
        set => TargetShaBox.Text = value;
    }

    public bool InitialAnnotated
    {
        get => AnnotatedBox.IsChecked == true;
        set
        {
            AnnotatedBox.IsChecked = value;
            MessageBox.IsVisible = value;
        }
    }

    private void AnnotatedBox_Changed(object? sender, RoutedEventArgs e)
    {
        MessageBox.IsVisible = AnnotatedBox.IsChecked == true;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Create_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        var sha = TargetShaBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(sha))
        {
            // Leave the dialog open so the user can fix the missing field.
            return;
        }

        var annotated = AnnotatedBox.IsChecked == true;
        string? message = null;
        if (annotated)
        {
            var raw = MessageBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                // Annotated tags require a message — leave open so user can supply one.
                return;
            }
            message = raw;
        }

        Close(new CreateTagRequest(name, sha, annotated, message));
    }
}

/// <summary>Result returned from <see cref="CreateTagDialog"/> on success.</summary>
public sealed record CreateTagRequest(string Name, string TargetSha, bool Annotated, string? Message);
