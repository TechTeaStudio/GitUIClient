namespace TechTeaStudio.GitClient.App.ViewModels;

using System.Collections.ObjectModel;

using Avalonia.Threading;
using TechTeaStudio.GitClient.App.Graph;
using TechTeaStudio.GitClient.App.Services;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Main window ViewModel. Holds a single <see cref="IRepoHandle"/> (current
/// repo) and exposes path / branch / commits / status state plus the four
/// commands the UI binds to: Open / Clone / Refresh / Commit.
///
/// All long-running calls go through the injected services on a per-VM
/// <see cref="CancellationTokenSource"/> so a new operation supersedes an
/// in-flight one.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IRepositoryService _repos;
    private readonly ICommitService _commits;
    private readonly IGravatarService? _gravatar;

    private IRepoHandle? _handle;
    private CancellationTokenSource _cts = new();

    private string _path = string.Empty;
    private string _cloneUrl = string.Empty;
    private string _commitMessage = string.Empty;
    private string? _statusMessage;
    private bool _isBusy;
    private BranchInfo? _selectedBranch;
    private CommitListItem? _selectedCommit;

    public MainViewModel(IRepositoryService repositories, ICommitService commits, IGravatarService? gravatar = null)
    {
        _repos = repositories ?? throw new ArgumentNullException(nameof(repositories));
        _commits = commits ?? throw new ArgumentNullException(nameof(commits));
        _gravatar = gravatar;

        OpenCommand = new RelayCommand(OpenAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(Path));
        CloneCommand = new RelayCommand(CloneAsync,
            () => !IsBusy && !string.IsNullOrWhiteSpace(CloneUrl) && !string.IsNullOrWhiteSpace(Path));
        RefreshCommand = new RelayCommand(RefreshAsync, () => !IsBusy && _handle is not null);
        CommitCommand = new RelayCommand(CommitAsync, CanCommit);

        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Path)
                || e.PropertyName == nameof(CloneUrl)
                || e.PropertyName == nameof(IsBusy)
                || e.PropertyName == nameof(CommitMessage))
            {
                OpenCommand.RaiseCanExecuteChanged();
                CloneCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
                CommitCommand.RaiseCanExecuteChanged();
            }
        };
    }

    public string Greeting => "TechTeaStudio Git Client — v0.2.0";

    public string Path { get => _path; set => SetField(ref _path, value); }
    public string CloneUrl { get => _cloneUrl; set => SetField(ref _cloneUrl, value); }
    public string CommitMessage { get => _commitMessage; set => SetField(ref _commitMessage, value); }
    public string? StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }

    public ObservableCollection<BranchInfo> Branches { get; } = new();

    public BranchInfo? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (SetField(ref _selectedBranch, value))
                _ = LoadCommitsForSelectedBranchAsync();
        }
    }

    public ObservableCollection<CommitListItem> Commits { get; } = new();

    public CommitListItem? SelectedCommit
    {
        get => _selectedCommit;
        set => SetField(ref _selectedCommit, value);
    }

    public ObservableCollection<FileSelection> Added { get; } = new();
    public ObservableCollection<FileSelection> Modified { get; } = new();
    public ObservableCollection<FileSelection> Deleted { get; } = new();
    public ObservableCollection<FileSelection> Untracked { get; } = new();

    public RelayCommand OpenCommand { get; }
    public RelayCommand CloneCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand CommitCommand { get; }

    private bool CanCommit()
    {
        if (IsBusy || _handle is null) return false;
        if (string.IsNullOrWhiteSpace(CommitMessage)) return false;

        return Added.Any(f => f.IsSelected)
            || Modified.Any(f => f.IsSelected)
            || Deleted.Any(f => f.IsSelected)
            || Untracked.Any(f => f.IsSelected);
    }

    public async Task OpenAsync()
    {
        var ct = RestartCts();
        await RunBusy(async () =>
        {
            DisposeHandle();
            _handle = await _repos.OpenAsync(Path, ct).ConfigureAwait(false);
            await RefreshInternalAsync(ct).ConfigureAwait(false);
            StatusMessage = $"Opened: {_handle.WorkingDirectory}";
        }).ConfigureAwait(false);
    }

    public Task CloneAsync() => CloneAsync(CloneUrl, Path);

    public async Task CloneAsync(string url, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(destinationPath))
        {
            StatusMessage = "Clone needs both URL and destination path.";
            return;
        }

        CloneUrl = url;
        Path = destinationPath;

        var ct = RestartCts();
        await RunBusy(async () =>
        {
            DisposeHandle();
            _handle = await _repos.CloneAsync(url, destinationPath, progress: null, ct).ConfigureAwait(false);
            await RefreshInternalAsync(ct).ConfigureAwait(false);
            StatusMessage = $"Cloned into: {_handle.WorkingDirectory}";
        }).ConfigureAwait(false);
    }

    public async Task RefreshAsync()
    {
        if (_handle is null) return;
        var ct = RestartCts();
        await RunBusy(() => RefreshInternalAsync(ct)).ConfigureAwait(false);
    }

    public async Task CommitAsync()
    {
        if (_handle is null) return;
        var ct = RestartCts();

        var paths = Added.Concat(Modified).Concat(Deleted).Concat(Untracked)
            .Where(f => f.IsSelected)
            .Select(f => f.Path)
            .ToArray();

        if (paths.Length == 0)
        {
            StatusMessage = "No files selected.";
            return;
        }

        var author = new AuthorInfo
        {
            Name = Environment.UserName,
            Email = $"{Environment.UserName}@local",
        };

        await RunBusy(async () =>
        {
            var commit = await _commits.CommitAsync(_handle!, CommitMessage, author, paths, ct)
                .ConfigureAwait(false);
            StatusMessage = $"Committed {commit.Sha[..Math.Min(7, commit.Sha.Length)]}: {commit.Summary}";
            CommitMessage = string.Empty;
            await RefreshInternalAsync(ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>Sentinel branch entry meaning "every branch reachable in one DAG view".</summary>
    public static readonly BranchInfo AllBranchesSentinel = new()
    {
        Name = "(all branches)",
        TipSha = string.Empty,
        IsCurrent = false,
        IsRemote = false,
    };

    public static bool IsAllBranches(BranchInfo? b) => ReferenceEquals(b, AllBranchesSentinel);

    private async Task LoadCommitsForSelectedBranchAsync()
    {
        if (_handle is null || _selectedBranch is null) return;
        var ct = RestartCts();
        await RunBusy(async () =>
        {
            var commits = await FetchCommitsAsync(_selectedBranch, ct).ConfigureAwait(false);
            PopulateCommits(commits);
        }).ConfigureAwait(false);
    }

    private Task<IReadOnlyList<CommitInfo>> FetchCommitsAsync(BranchInfo branch, CancellationToken ct)
        => IsAllBranches(branch)
            ? _repos.GetAllCommitsAsync(_handle!, 200, ct)
            : _repos.GetCommitsAsync(_handle!, branch.Name, 200, ct);

    private async Task RefreshInternalAsync(CancellationToken ct)
    {
        if (_handle is null) return;

        var previousSelectionName = _selectedBranch?.Name;

        var branches = await _repos.GetBranchesAsync(_handle, ct).ConfigureAwait(false);
        Branches.Clear();
        Branches.Add(AllBranchesSentinel);
        foreach (var b in branches) Branches.Add(b);

        // Preserve the previous selection across refreshes when it still exists, otherwise
        // default to the unified all-branches view so the whole DAG is visible.
        _selectedBranch = previousSelectionName is null
            ? AllBranchesSentinel
            : Branches.FirstOrDefault(b => b.Name == previousSelectionName) ?? AllBranchesSentinel;
        OnPropertyChanged(nameof(SelectedBranch));

        var commits = await FetchCommitsAsync(_selectedBranch, ct).ConfigureAwait(false);
        PopulateCommits(commits);

        var status = await _repos.GetStatusAsync(_handle, ct).ConfigureAwait(false);
        Replace(Added, status.Added);
        Replace(Modified, status.Modified);
        Replace(Deleted, status.Deleted);
        Replace(Untracked, status.Untracked);
    }

    private void PopulateCommits(IReadOnlyList<CommitInfo> commits)
    {
        Commits.Clear();
        var items = new List<CommitListItem>(commits.Count);
        foreach (var c in commits) items.Add(new CommitListItem(c));

        var rows = CommitGraphBuilder.Build(commits);
        for (int i = 0; i < items.Count && i < rows.Count; i++)
        {
            items[i].GraphRow = rows[i];
        }

        foreach (var item in items) Commits.Add(item);
        SelectedCommit = Commits.FirstOrDefault();

        if (_gravatar is not null)
        {
            foreach (var item in items)
            {
                _ = DispatchAvatarFetchAsync(item);
            }
        }
    }

    private async Task DispatchAvatarFetchAsync(CommitListItem item)
    {
        if (_gravatar is null) return;
        try
        {
            var bitmap = await _gravatar.GetAvatarAsync(item.Email, size: 64).ConfigureAwait(false);
            if (bitmap is null) return;
            if (Dispatcher.UIThread.CheckAccess())
                item.Avatar = bitmap;
            else
                await Dispatcher.UIThread.InvokeAsync(() => item.Avatar = bitmap);
        }
        catch
        {
            // Avatar is decorative — silently swallow.
        }
    }

    private static void Replace(ObservableCollection<FileSelection> target, IReadOnlyList<FileChange> source)
    {
        target.Clear();
        foreach (var f in source) target.Add(new FileSelection(f));
    }

    private async Task RunBusy(Func<Task> body)
    {
        IsBusy = true;
        try
        {
            await body().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private CancellationToken RestartCts()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    private void DisposeHandle()
    {
        _handle?.Dispose();
        _handle = null;
        Branches.Clear();
        Commits.Clear();
        Added.Clear();
        Modified.Clear();
        Deleted.Clear();
        Untracked.Clear();
        _selectedBranch = null;
        OnPropertyChanged(nameof(SelectedBranch));
        SelectedCommit = null;
    }
}
