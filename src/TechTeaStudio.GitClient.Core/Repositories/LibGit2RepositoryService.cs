namespace TechTeaStudio.GitClient.Repositories;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IRepositoryService"/>.
///
/// Concurrency: every method that touches the underlying <see cref="Repository"/>
/// takes the handle's async lock first. LibGit2Sharp is not thread-safe; a single
/// repo must be used from one thread at a time.
///
/// Most operations are synchronous-bound inside LibGit2Sharp — wrapping them in
/// <see cref="Task.FromResult{TResult}(TResult)"/> is honest about that. Only
/// <see cref="CloneAsync"/> goes through <see cref="Task.Run(Action)"/> because
/// it is slow and benefits from running off the UI thread.
/// </summary>
public sealed class LibGit2RepositoryService : IRepositoryService
{
    public async Task<IRepoHandle> OpenAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();

        var discovered = Repository.Discover(path);
        if (string.IsNullOrEmpty(discovered))
            throw new RepositoryNotFoundException($"No git repository found at or above: {path}");

        var repo = new Repository(discovered);
        return await Task.FromResult<IRepoHandle>(new LibGit2RepoHandle(repo)).ConfigureAwait(false);
    }

    public async Task<IRepoHandle> InitAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(path);
        var gitDir = Repository.Init(path);
        var repo = new Repository(gitDir);
        return await Task.FromResult<IRepoHandle>(new LibGit2RepoHandle(repo)).ConfigureAwait(false);
    }

    public Task<IRepoHandle> CloneAsync(
        string url,
        string path,
        IProgress<CloneProgress>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();

        return Task.Run<IRepoHandle>(() =>
        {
            var options = new CloneOptions
            {
                FetchOptions =
                {
                    OnTransferProgress = tp =>
                    {
                        if (ct.IsCancellationRequested)
                            return false;

                        progress?.Report(new CloneProgress
                        {
                            Stage = "transfer",
                            ReceivedObjects = tp.ReceivedObjects,
                            TotalObjects = tp.TotalObjects,
                            ReceivedBytes = tp.ReceivedBytes,
                        });
                        return true;
                    },
                },
            };

            string cloned;
            try
            {
                cloned = Repository.Clone(url, path, options);
            }
            catch (UserCancelledException)
            {
                ct.ThrowIfCancellationRequested();
                throw new OperationCanceledException();
            }

            var repo = new Repository(cloned);
            return new LibGit2RepoHandle(repo);
        }, ct);
    }

    public async Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(IRepoHandle handle, CancellationToken ct = default)
    {
        var h = CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var list = new List<BranchInfo>();
            foreach (var b in h.Repository.Branches)
            {
                var tipSha = b.Tip?.Sha ?? string.Empty;
                list.Add(new BranchInfo
                {
                    Name = b.FriendlyName,
                    TipSha = tipSha,
                    IsCurrent = b.IsCurrentRepositoryHead,
                    IsRemote = b.IsRemote,
                });
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<IReadOnlyList<CommitInfo>> GetCommitsAsync(
        IRepoHandle handle,
        string branchOrRef,
        int take,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchOrRef);
        if (take <= 0) return Array.Empty<CommitInfo>();

        var h = CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var tip = ResolveCommit(repo, branchOrRef)
                ?? throw new ArgumentException($"Could not resolve '{branchOrRef}' to a commit.", nameof(branchOrRef));

            var filter = new CommitFilter { IncludeReachableFrom = tip };
            var list = new List<CommitInfo>(take);
            foreach (var c in repo.Commits.QueryBy(filter).Take(take))
            {
                ct.ThrowIfCancellationRequested();
                list.Add(MapCommit(c));
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<string> GetDiffAsync(IRepoHandle handle, string fromSha, string toSha, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(toSha);

        var h = CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var from = ResolveCommit(repo, fromSha)
                ?? throw new ArgumentException($"Could not resolve '{fromSha}' to a commit.", nameof(fromSha));
            var to = ResolveCommit(repo, toSha)
                ?? throw new ArgumentException($"Could not resolve '{toSha}' to a commit.", nameof(toSha));

            using var patch = repo.Diff.Compare<Patch>(from.Tree, to.Tree);
            return patch.Content;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<RepoStatus> GetStatusAsync(IRepoHandle handle, CancellationToken ct = default)
    {
        var h = CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var added = new List<FileChange>();
            var modified = new List<FileChange>();
            var deleted = new List<FileChange>();
            var untracked = new List<FileChange>();

            var status = h.Repository.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                IncludeIgnored = false,
                RecurseUntrackedDirs = true,
            });

            foreach (var entry in status)
            {
                ct.ThrowIfCancellationRequested();
                var s = entry.State;
                if (s.HasFlag(FileStatus.NewInWorkdir))
                    untracked.Add(new FileChange { Path = entry.FilePath, Kind = FileChangeKind.Untracked });
                else if (s.HasFlag(FileStatus.NewInIndex) || s.HasFlag(FileStatus.RenamedInIndex))
                    added.Add(new FileChange { Path = entry.FilePath, Kind = FileChangeKind.Added });
                else if (s.HasFlag(FileStatus.ModifiedInIndex) || s.HasFlag(FileStatus.ModifiedInWorkdir))
                    modified.Add(new FileChange { Path = entry.FilePath, Kind = FileChangeKind.Modified });
                else if (s.HasFlag(FileStatus.DeletedFromIndex) || s.HasFlag(FileStatus.DeletedFromWorkdir))
                    deleted.Add(new FileChange { Path = entry.FilePath, Kind = FileChangeKind.Deleted });
                // Conflicted / Ignored / TypeChange / Unaltered are intentionally dropped in v0.1.0.
            }

            return new RepoStatus
            {
                Added = added,
                Modified = modified,
                Deleted = deleted,
                Untracked = untracked,
            };
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    internal static LibGit2RepoHandle CastHandle(IRepoHandle handle)
    {
        if (handle is null) throw new ArgumentNullException(nameof(handle));
        if (handle is not LibGit2RepoHandle h)
            throw new ArgumentException(
                $"Handle must be a {nameof(LibGit2RepoHandle)} produced by this service.",
                nameof(handle));
        return h;
    }

    private static Commit? ResolveCommit(Repository repo, string reference)
    {
        // Branch -> tip
        try
        {
            var branch = repo.Branches[reference];
            if (branch?.Tip is not null)
                return branch.Tip;
        }
        catch { /* invalid branch name; fall through */ }

        // HEAD or arbitrary ref name (refs/heads/main, etc.)
        try
        {
            var refObj = repo.Refs[reference];
            if (refObj is not null && repo.Lookup<Commit>(refObj.TargetIdentifier) is { } refCommit)
                return refCommit;
        }
        catch { /* invalid ref name; fall through */ }

        // Direct lookup (sha, sha prefix, "HEAD")
        try
        {
            return repo.Lookup<Commit>(reference);
        }
        catch
        {
            return null;
        }
    }

    private static CommitInfo MapCommit(Commit c) => new()
    {
        Sha = c.Sha,
        Author = c.Author.Name,
        Email = c.Author.Email,
        When = c.Author.When,
        Message = c.Message ?? string.Empty,
        Parents = c.Parents.Select(p => p.Sha).ToList(),
    };
}
