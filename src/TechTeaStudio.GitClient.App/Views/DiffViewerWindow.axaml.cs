namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Media;

using TechTeaStudio.GitClient.Models;

/// <summary>Lightweight viewer for a structured <see cref="FileDiff"/> set.
/// The window is constructed empty and populated via <see cref="SetDiffs"/> so
/// callers don't need a constructor flavour for each call site.</summary>
public sealed partial class DiffViewerWindow : Window
{
    private static readonly IBrush AddedBg = Brush.Parse("#1B3A1B");
    private static readonly IBrush RemovedBg = Brush.Parse("#3A1B1B");
    private static readonly IBrush AddedFg = Brush.Parse("#9DD49D");
    private static readonly IBrush RemovedFg = Brush.Parse("#E8A9A9");
    private static readonly IBrush ContextFg = Brushes.Gainsboro;
    private static readonly IBrush HunkBg = Brush.Parse("#1E2A3A");
    private static readonly IBrush HunkFg = Brush.Parse("#7BAFE0");

    private IReadOnlyList<FileDiff> _diffs = [];

    public DiffViewerWindow() => InitializeComponent();

    public void SetDiffs(IReadOnlyList<FileDiff> diffs, string? title = null)
    {
        _diffs = diffs;
        HeaderSubtitle.Text = title ?? string.Empty;

        FileList.ItemsSource = diffs.Select(BuildFileItem).ToList();
        if (FileList.ItemCount > 0)
            FileList.SelectedIndex = 0;
        else
            DiffLines.ItemsSource = System.Array.Empty<DiffRowVm>();
    }

    private static FileDiffItem BuildFileItem(FileDiff diff)
    {
        var (glyph, brush) = diff.Kind switch
        {
            FileDiffKind.Added => ("A", (IBrush)Brush.Parse("#2E7D32")),
            FileDiffKind.Deleted => ("D", (IBrush)Brush.Parse("#C62828")),
            FileDiffKind.Renamed => ("R", (IBrush)Brush.Parse("#1976D2")),
            _ => ("M", (IBrush)Brush.Parse("#EF6C00")),
        };
        return new FileDiffItem(
            diff,
            diff.Path,
            glyph,
            brush,
            diff.AddedLines > 0 ? "+" + diff.AddedLines : string.Empty,
            diff.DeletedLines > 0 ? "-" + diff.DeletedLines : string.Empty);
    }

    private void FileList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FileList.SelectedItem is not FileDiffItem item)
        {
            DiffLines.ItemsSource = System.Array.Empty<DiffRowVm>();
            FileHeader.Text = string.Empty;
            return;
        }

        FileHeader.Text = item.Diff.OldPath is { } old && old != item.Diff.Path
            ? $"{old}  →  {item.Diff.Path}"
            : item.Diff.Path;

        var rows = new List<DiffRowVm>(capacity: 64);
        foreach (var hunk in item.Diff.Hunks)
        {
            rows.Add(new DiffRowVm
            {
                OldNumberText = string.Empty,
                NewNumberText = string.Empty,
                DisplayContent = $"@@ -{hunk.OldStartLine},{hunk.OldLineCount} +{hunk.NewStartLine},{hunk.NewLineCount} @@",
                RowBackground = HunkBg,
                Foreground = HunkFg,
            });
            foreach (var line in hunk.Lines)
            {
                var (bg, fg, prefix) = line.Kind switch
                {
                    DiffLineKind.Added => (AddedBg, AddedFg, "+"),
                    DiffLineKind.Removed => (RemovedBg, RemovedFg, "-"),
                    DiffLineKind.NoNewline => (HunkBg, HunkFg, "\\"),
                    _ => ((IBrush)Brushes.Transparent, ContextFg, " "),
                };
                rows.Add(new DiffRowVm
                {
                    OldNumberText = line.OldLineNumber?.ToString() ?? string.Empty,
                    NewNumberText = line.NewLineNumber?.ToString() ?? string.Empty,
                    DisplayContent = prefix + line.Content,
                    RowBackground = bg,
                    Foreground = fg,
                });
            }
        }
        DiffLines.ItemsSource = rows;
    }

    public sealed record FileDiffItem(
        FileDiff Diff,
        string Path,
        string KindGlyph,
        IBrush KindBrush,
        string AddedText,
        string DeletedText);

    public sealed class DiffRowVm
    {
        public required string OldNumberText { get; init; }
        public required string NewNumberText { get; init; }
        public required string DisplayContent { get; init; }
        public required IBrush RowBackground { get; init; }
        public required IBrush Foreground { get; init; }
    }
}
