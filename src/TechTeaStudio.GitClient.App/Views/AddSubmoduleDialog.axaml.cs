namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class AddSubmoduleDialog : Window
{
    public AddSubmoduleDialog() => InitializeComponent();

    public string InitialUrl
    {
        get => UrlBox.Text ?? string.Empty;
        set => UrlBox.Text = value;
    }

    public string InitialPath
    {
        get => PathBox.Text ?? string.Empty;
        set => PathBox.Text = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        var path = PathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(path))
            return; // leave open so the user can fix the missing field
        Close(new AddSubmoduleRequest(url, path));
    }
}

/// <summary>Result returned from the add-submodule dialog on success.</summary>
public sealed record AddSubmoduleRequest(string Url, string Path);
