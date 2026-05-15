namespace TechTeaStudio.GitClient.App.Views;

using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Material.Icons;
using Material.Icons.Avalonia;
using TechTeaStudio.GitClient.App.ViewModels;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeToggleIcon();
    }

    private async void Open_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var picked = await PickFolderAsync(vm.Path, "Select repository folder");
        if (picked is null) return;

        vm.Path = picked;
        await vm.OpenAsync();
    }

    private async void Clone_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var dialog = new CloneDialog
        {
            InitialUrl = vm.CloneUrl,
            InitialPath = vm.Path,
        };

        var result = await dialog.ShowDialog<CloneRequest?>(this);
        if (result is null) return;

        await vm.CloneAsync(result.Url, result.DestinationPath);
    }

    private async Task<string?> PickFolderAsync(string? seedPath, string title)
    {
        var storage = StorageProvider;

        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(seedPath))
        {
            try { start = await storage.TryGetFolderFromPathAsync(seedPath); }
            catch { /* unreachable path or platform-denied — start unscoped */ }
        }

        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });

        return picked.Count > 0 ? picked[0].Path.LocalPath : null;
    }

    private void ThemeToggle_Click(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is null) return;

        var current = app.ActualThemeVariant;
        app.RequestedThemeVariant = current == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        UpdateThemeToggleIcon();
    }

    private void UpdateThemeToggleIcon()
    {
        if (this.FindControl<MaterialIcon>("ThemeToggleIcon") is { } icon)
        {
            icon.Kind = Application.Current?.ActualThemeVariant == ThemeVariant.Dark
                ? MaterialIconKind.WeatherSunny
                : MaterialIconKind.WeatherNight;
        }
    }
}
