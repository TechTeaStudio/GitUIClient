namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Interactive conflict-resolution dialog. Each row shows the conflicted
/// path with "Use Ours" / "Use Theirs" buttons; clicking one calls the
/// matching <see cref="IConflictsService"/> method and refreshes the list.
/// The dialog auto-closes when the conflict list goes empty.
/// </summary>
public sealed partial class ConflictsDialog : Window
{
    private readonly IConflictsService? _conflicts;
    private readonly IRepoHandle? _handle;
    private readonly ObservableCollection<ConflictInfo> _items = [];

    /// <summary>Parameterless ctor for XAML designer support.</summary>
    public ConflictsDialog()
    {
        InitializeComponent();
        ConflictsList.ItemsSource = _items;
    }

    public ConflictsDialog(IConflictsService conflicts, IRepoHandle handle) : this()
    {
        _conflicts = conflicts ?? throw new ArgumentNullException(nameof(conflicts));
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));

        Opened += async (_, _) => await RefreshAsync().ConfigureAwait(true);
    }

    private async Task RefreshAsync()
    {
        if (_conflicts is null || _handle is null) return;

        IReadOnlyList<ConflictInfo> list;
        try
        {
            list = await _conflicts.ListConflictsAsync(_handle).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusLabel.Text = ex.Message);
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _items.Clear();
            foreach (var c in list) _items.Add(c);
            CountLabel.Text = list.Count == 0
                ? "(none — you can close this dialog)"
                : $"({list.Count} conflict{(list.Count == 1 ? "" : "s")})";
            StatusLabel.Text = string.Empty;

            if (list.Count == 0)
                Close();
        });
    }

    private async void UseOurs_Click(object? sender, RoutedEventArgs e)
    {
        if (_conflicts is null || _handle is null) return;
        if (sender is not Button { Tag: string path }) return;
        try
        {
            await _conflicts.ResolveByOursAsync(_handle, path).ConfigureAwait(true);
            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private async void UseTheirs_Click(object? sender, RoutedEventArgs e)
    {
        if (_conflicts is null || _handle is null) return;
        if (sender is not Button { Tag: string path }) return;
        try
        {
            await _conflicts.ResolveByTheirsAsync(_handle, path).ConfigureAwait(true);
            await RefreshAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = ex.Message;
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();
}
