namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class UpdateSubmoduleDialog : Window
{
    private readonly string _name;

    public UpdateSubmoduleDialog() : this(string.Empty) { }

    public UpdateSubmoduleDialog(string name)
    {
        _name = name ?? string.Empty;
        InitializeComponent();
        NameLabel.Text = _name;
    }

    public bool InitChecked
    {
        get => InitBox.IsChecked ?? false;
        set => InitBox.IsChecked = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_name))
            return;
        Close(new UpdateSubmoduleRequest(_name, InitBox.IsChecked ?? false));
    }
}

/// <summary>Result returned from the update-submodule dialog on success.</summary>
public sealed record UpdateSubmoduleRequest(string Name, bool Init);
