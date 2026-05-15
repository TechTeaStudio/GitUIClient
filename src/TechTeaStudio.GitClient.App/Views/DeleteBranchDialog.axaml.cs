namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>
/// Delete-confirmation dialog. The dialog result is:
/// <list type="bullet">
///   <item><c>null</c> — user cancelled.</item>
///   <item><c>false</c> — normal delete (force checkbox left off).</item>
///   <item><c>true</c> — force delete (checkbox ticked).</item>
/// </list>
/// </summary>
public sealed partial class DeleteBranchDialog : Window
{
    public DeleteBranchDialog() => InitializeComponent();

    public string BranchName
    {
        get => NameLabel.Text ?? string.Empty;
        set => NameLabel.Text = value;
    }

    public bool InitialForce
    {
        get => ForceBox.IsChecked == true;
        set => ForceBox.IsChecked = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Close((bool?)(ForceBox.IsChecked == true));
    }
}
