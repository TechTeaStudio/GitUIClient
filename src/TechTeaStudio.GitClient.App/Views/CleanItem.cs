namespace TechTeaStudio.GitClient.App.Views;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Bindable row used by <see cref="CleanDialog"/>'s file list.</summary>
public sealed class CleanItem : INotifyPropertyChanged
{
    private bool _isSelected;

    public required string Path { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? property = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
