namespace TechTeaStudio.GitClient.App;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TechTeaStudio.GitClient.App.Views;
using TechTeaStudio.GitClient.App.ViewModels;

public sealed class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = CompositionRoot.BuildMainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
