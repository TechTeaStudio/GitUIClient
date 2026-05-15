namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media;

using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Per-line blame viewer. Short-sha cells are clickable: clicking copies
/// the full SHA to the clipboard so the user can paste it into <c>git log</c>.</summary>
public sealed partial class BlameWindow : Window
{
    private static readonly IBrush[] InitialsPalette =
    [
        Brush.Parse("#9D2499"),
        Brush.Parse("#5EC6C6"),
        Brush.Parse("#4CAF50"),
        Brush.Parse("#FF9800"),
        Brush.Parse("#29B6F6"),
        Brush.Parse("#EF5350"),
        Brush.Parse("#BA68C8"),
        Brush.Parse("#FFB74D"),
    ];

    private static readonly IBrush ZebraEven = Brushes.Transparent;
    private static readonly IBrush ZebraOdd = Brush.Parse("#22FFFFFF");

    public BlameWindow() => InitializeComponent();

    public async Task LoadAsync(
        IBlameService blameService,
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default)
    {
        FileLabel.Text = path;
        CommitLabel.Text = commitSha.Length > 12 ? commitSha[..12] : commitSha;

        var lines = await blameService.BlameFileAsync(handle, commitSha, path, ct).ConfigureAwait(true);
        var rows = new List<BlameRowVm>(lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            var l = lines[i];
            rows.Add(BlameRowVm.From(l, Clipboard, (i & 1) == 0 ? ZebraEven : ZebraOdd));
        }
        BlameLines.ItemsSource = rows;
    }

    internal static IBrush PickInitialsBrush(string author)
    {
        int hash = 17;
        foreach (var ch in author)
            hash = unchecked(hash * 31 + ch);
        return InitialsPalette[(uint)hash % InitialsPalette.Length];
    }

    internal static string PickInitials(string author)
    {
        if (string.IsNullOrWhiteSpace(author)) return "?";
        var parts = author.Split([' ', '.', '-', '_'], System.StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => string.Concat(parts[0][..1], parts[^1][..1]).ToUpperInvariant(),
        };
    }

    public sealed class BlameRowVm
    {
        public required int LineNumber { get; init; }
        public required string Content { get; init; }
        public required string ShortSha { get; init; }
        public required string FullSha { get; init; }
        public required string Author { get; init; }
        public required string DateText { get; init; }
        public required string Initials { get; init; }
        public required IBrush InitialsBrush { get; init; }
        public required IBrush RowBackground { get; init; }
        public required ICommand CopyShaCommand { get; init; }

        public static BlameRowVm From(BlameLine line, IClipboard? clipboard, IBrush rowBg) => new()
        {
            LineNumber = line.LineNumber,
            Content = line.Content,
            ShortSha = line.CommitSha.Length > 7 ? line.CommitSha[..7] : line.CommitSha,
            FullSha = line.CommitSha,
            Author = line.Author,
            DateText = line.When.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Initials = PickInitials(line.Author),
            InitialsBrush = PickInitialsBrush(line.Author),
            RowBackground = rowBg,
            CopyShaCommand = new ClipboardCopyCommand(clipboard, line.CommitSha),
        };
    }

    private sealed class ClipboardCopyCommand(IClipboard? clipboard, string text) : ICommand
    {
        public event System.EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => clipboard is not null;
        public async void Execute(object? parameter)
        {
            if (clipboard is null) return;
            await clipboard.SetTextAsync(text);
        }
    }
}
