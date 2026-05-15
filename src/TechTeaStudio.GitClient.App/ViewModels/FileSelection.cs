namespace TechTeaStudio.GitClient.App.ViewModels;

using TechTeaStudio.GitClient.Models;

/// <summary><see cref="FileChange"/> plus a checkbox state for "Commit selected files".</summary>
public sealed class FileSelection : ObservableObject
{
    private bool _isSelected;

    public FileSelection(FileChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Change = change;
    }

    public FileChange Change { get; }
    public string Path => Change.Path;
    public FileChangeKind Kind => Change.Kind;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }
}
