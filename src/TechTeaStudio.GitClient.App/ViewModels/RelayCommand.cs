namespace TechTeaStudio.GitClient.App.ViewModels;

using System.Windows.Input;

/// <summary>Minimal <see cref="ICommand"/> that defers execution to a delegate.</summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = () => { execute(); return Task.CompletedTask; };
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute is null || _canExecute();

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        await _execute().ConfigureAwait(false);
    }

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
