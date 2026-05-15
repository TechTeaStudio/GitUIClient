namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Lazy-load tree browser. Sub-tree children are fetched on first
/// display so opening even a big repo's root is cheap.</summary>
public sealed partial class TreeBrowserWindow : Window
{
    private static readonly IBrush FolderBrush = Brush.Parse("#FFB74D");
    private static readonly IBrush FileBrush = Brush.Parse("#9D2499");

    private ITreeService? _treeService;
    private IRepoHandle? _handle;
    private string? _commitSha;

    public TreeBrowserWindow() => InitializeComponent();

    public async Task LoadAsync(ITreeService treeService, IRepoHandle handle, string commitSha, CancellationToken ct = default)
    {
        _treeService = treeService;
        _handle = handle;
        _commitSha = commitSha;
        CommitLabel.Text = commitSha.Length > 12 ? commitSha[..12] : commitSha;

        var root = await treeService.ListTreeAsync(handle, commitSha, null, ct).ConfigureAwait(true);
        var rootItems = new ObservableCollection<TreeNode>();
        foreach (var entry in root)
            rootItems.Add(TreeNode.FromEntry(entry, this));

        Tree.ItemsSource = rootItems;
    }

    private async void Tree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Tree.SelectedItem is not TreeNode node || _treeService is null || _handle is null || _commitSha is null)
        {
            ShowEmpty();
            return;
        }

        if (node.Entry.Kind == TreeEntryKind.Tree)
        {
            ShowEmpty();
            return;
        }

        ContentPath.Text = node.Entry.Path;
        var isBinary = await _treeService.IsBinaryBlobAsync(_handle, _commitSha, node.Entry.Path).ConfigureAwait(true);
        if (isBinary)
        {
            ShowBinary(node.Entry.Size ?? 0);
            return;
        }

        var text = await _treeService.ReadBlobAsync(_handle, _commitSha, node.Entry.Path).ConfigureAwait(true);
        ShowText(text ?? string.Empty);
    }

    internal async Task LoadChildrenAsync(TreeNode parent)
    {
        if (_treeService is null || _handle is null || _commitSha is null) return;
        if (parent.ChildrenLoaded) return;

        var children = await _treeService.ListTreeAsync(_handle, _commitSha, parent.Entry.Path).ConfigureAwait(true);
        parent.Children.Clear();
        foreach (var entry in children)
            parent.Children.Add(TreeNode.FromEntry(entry, this));
        parent.ChildrenLoaded = true;
    }

    private void ShowEmpty()
    {
        ContentBox.IsVisible = false;
        BinaryBanner.IsVisible = false;
        EmptyBanner.IsVisible = true;
        ContentPath.Text = string.Empty;
    }

    private void ShowText(string text)
    {
        EmptyBanner.IsVisible = false;
        BinaryBanner.IsVisible = false;
        ContentBox.Text = text;
        ContentBox.IsVisible = true;
    }

    private void ShowBinary(long size)
    {
        EmptyBanner.IsVisible = false;
        ContentBox.IsVisible = false;
        BinarySize.Text = $"{size:N0} bytes";
        BinaryBanner.IsVisible = true;
    }

    internal sealed class TreeNode
    {
        public TreeEntry Entry { get; }
        public ObservableCollection<TreeNode> Children { get; } = new();
        public string IconKind { get; }
        public IBrush IconBrush { get; }
        public string Name { get; }
        public string SizeLabel { get; }
        public bool ChildrenLoaded { get; set; }
        private TreeBrowserWindow? Owner { get; }

        private TreeNode(TreeEntry entry, TreeBrowserWindow? owner)
        {
            Entry = entry;
            Owner = owner;
            IconKind = entry.Kind == TreeEntryKind.Tree ? "FolderOutline" : "FileOutline";
            IconBrush = entry.Kind == TreeEntryKind.Tree ? FolderBrush : FileBrush;
            Name = entry.Name;
            SizeLabel = entry.Size is { } s ? FormatSize(s) : string.Empty;
        }

        public static TreeNode FromEntry(TreeEntry entry, TreeBrowserWindow owner)
        {
            var node = new TreeNode(entry, owner);
            if (entry.Kind == TreeEntryKind.Tree)
            {
                // Defer fetching children until the node is rendered by the TreeView —
                // running on Background priority keeps the initial open snappy.
                _ = Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (node.Owner is null) return;
                    await node.Owner.LoadChildrenAsync(node);
                }, DispatcherPriority.Background);
            }
            else
            {
                node.ChildrenLoaded = true;
            }
            return node;
        }

        private static string FormatSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024L * 1024 => $"{bytes / 1024.0:0.#} KB",
            _ => $"{bytes / (1024.0 * 1024):0.#} MB",
        };
    }
}
