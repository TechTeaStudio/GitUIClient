namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Modal dialog that collects a remote name + URL. Returns a populated
/// <see cref="RemoteInfo"/> on Add (with <c>PushUrl = null</c> — push URL is
/// the same as fetch by default), or <c>null</c> on Cancel.
/// </summary>
public sealed partial class AddRemoteDialog : Window
{
    public AddRemoteDialog() => InitializeComponent();

    public string InitialName
    {
        get => NameBox.Text ?? string.Empty;
        set => NameBox.Text = value;
    }

    public string InitialUrl
    {
        get => UrlBox.Text ?? string.Empty;
        set => UrlBox.Text = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        var url = UrlBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
        {
            // Leave the dialog open so the user can fix the missing field.
            return;
        }
        Close(new RemoteInfo { Name = name, Url = url });
    }
}
