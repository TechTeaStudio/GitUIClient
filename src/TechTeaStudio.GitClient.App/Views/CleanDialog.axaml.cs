namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;

/// <summary>Result returned from the clean dialog on success.</summary>
public sealed record CleanRequest(IReadOnlyList<string> SelectedPaths);

public sealed partial class CleanDialog : Window
{
    private readonly ObservableCollection<CleanItem> _items = new();

    public CleanDialog()
    {
        InitializeComponent();
        FilesList.ItemsSource = _items;
    }

    /// <summary>Set the list of untracked files to choose from.</summary>
    public void SetFiles(IReadOnlyList<string> files)
    {
        _items.Clear();
        foreach (var f in files)
            _items.Add(new CleanItem { Path = f, IsSelected = false });
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var item in _items)
            item.IsSelected = true;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Clean_Click(object? sender, RoutedEventArgs e)
    {
        var selected = _items.Where(i => i.IsSelected).Select(i => i.Path).ToList();
        if (selected.Count == 0)
        {
            // Nothing selected — leave open.
            return;
        }
        Close(new CleanRequest(selected));
    }
}
