namespace TechTeaStudio.GitClient.App;

using Avalonia;

internal static class Program
{
    // Avalonia entry point. Worker C wires real DI and ViewModels.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
