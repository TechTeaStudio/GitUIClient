namespace TechTeaStudio.GitClient.App.Views;

using Avalonia.Controls;
using Avalonia.Interactivity;

public sealed partial class CreateBranchDialog : Window
{
    public CreateBranchDialog() => InitializeComponent();

    public string InitialName
    {
        get => NameBox.Text ?? string.Empty;
        set => NameBox.Text = value;
    }

    public string InitialStartSha
    {
        get => StartPointBox.Text ?? string.Empty;
        set => StartPointBox.Text = value;
    }

    public bool InitialCheckout
    {
        get => CheckoutBox.IsChecked == true;
        set => CheckoutBox.IsChecked = value;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Create_Click(object? sender, RoutedEventArgs e)
    {
        var name = NameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            // Leave the dialog open so the user can fix the missing branch name.
            return;
        }

        var startRaw = StartPointBox.Text?.Trim();
        var start = string.IsNullOrWhiteSpace(startRaw) ? null : startRaw;
        var checkout = CheckoutBox.IsChecked == true;

        Close(new CreateBranchRequest(name, start, checkout));
    }
}

/// <summary>Result returned from <see cref="CreateBranchDialog"/> on success.</summary>
public sealed record CreateBranchRequest(string Name, string? StartSha, bool Checkout);
