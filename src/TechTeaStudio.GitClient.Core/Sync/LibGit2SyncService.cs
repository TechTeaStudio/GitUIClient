namespace TechTeaStudio.GitClient.Sync;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

using Credentials = TechTeaStudio.GitClient.Models.Credentials;

/// <summary>
/// LibGit2Sharp-backed fetch / pull / push. All three are slow operations and
/// run inside <see cref="Task.Run(Action)"/> so the caller (typically the UI
/// thread) is never blocked.
///
/// Concurrency: takes the handle's async lock — LibGit2Sharp is not thread-safe.
/// </summary>
public sealed class LibGit2SyncService : ISyncService
{
    public Task FetchAsync(
        IRepoHandle handle,
        string remoteName,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);

        var h = LibGit2RepositoryService.CastHandle(handle);
        return Task.Run(async () =>
        {
            await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var repo = h.Repository;
                var remote = repo.Network.Remotes[remoteName]
                    ?? throw new ArgumentException($"No remote named '{remoteName}'.", nameof(remoteName));

                var fetchOptions = BuildFetchOptions(progress, credentials, ct);
                var refspecs = remote.FetchRefSpecs.Select(r => r.Specification).ToArray();

                try
                {
                    Commands.Fetch(repo, remote.Name, refspecs, fetchOptions, logMessage: null);
                }
                catch (UserCancelledException)
                {
                    ct.ThrowIfCancellationRequested();
                    throw new OperationCanceledException();
                }
            }
            finally
            {
                h.AsyncLock.Release();
            }
        }, ct);
    }

    public Task<PullResult> PullAsync(
        IRepoHandle handle,
        string remoteName,
        string? branchName,
        AuthorInfo mergeAuthor,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);
        ArgumentNullException.ThrowIfNull(mergeAuthor);
        ArgumentException.ThrowIfNullOrWhiteSpace(mergeAuthor.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(mergeAuthor.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        return Task.Run<PullResult>(async () =>
        {
            await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var repo = h.Repository;
                var remote = repo.Network.Remotes[remoteName]
                    ?? throw new ArgumentException($"No remote named '{remoteName}'.", nameof(remoteName));

                var fetchOptions = BuildFetchOptions(progress, credentials, ct);
                var refspecs = remote.FetchRefSpecs.Select(r => r.Specification).ToArray();

                try
                {
                    Commands.Fetch(repo, remote.Name, refspecs, fetchOptions, logMessage: null);
                }
                catch (UserCancelledException)
                {
                    ct.ThrowIfCancellationRequested();
                    throw new OperationCanceledException();
                }

                // Resolve which remote-tracking branch we want to merge in.
                var localBranchName = branchName ?? repo.Head.FriendlyName;
                var remoteTracking = ResolveRemoteTrackingBranch(repo, remote.Name, localBranchName)
                    ?? throw new InvalidOperationException(
                        $"Could not resolve remote-tracking branch for '{remoteName}/{localBranchName}'.");

                var sig = new Signature(
                    mergeAuthor.Name,
                    mergeAuthor.Email,
                    mergeAuthor.When ?? DateTimeOffset.UtcNow);

                var mergeResult = repo.Merge(remoteTracking, sig, new LibGit2Sharp.MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.Default,
                });

                return new PullResult
                {
                    Kind = mergeResult.Status switch
                    {
                        MergeStatus.UpToDate => PullKind.UpToDate,
                        MergeStatus.FastForward => PullKind.FastForward,
                        MergeStatus.NonFastForward => PullKind.NonFastForward,
                        MergeStatus.Conflicts => PullKind.Conflicts,
                        _ => PullKind.NonFastForward,
                    },
                    Sha = repo.Head.Tip?.Sha ?? string.Empty,
                };
            }
            finally
            {
                h.AsyncLock.Release();
            }
        }, ct);
    }

    public Task PushAsync(
        IRepoHandle handle,
        string remoteName,
        IReadOnlyList<string> branchNames,
        bool force = false,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);
        ArgumentNullException.ThrowIfNull(branchNames);
        if (branchNames.Count == 0)
            throw new ArgumentException("At least one branch is required.", nameof(branchNames));

        var h = LibGit2RepositoryService.CastHandle(handle);
        return Task.Run(async () =>
        {
            await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var repo = h.Repository;
                var remote = repo.Network.Remotes[remoteName]
                    ?? throw new ArgumentException($"No remote named '{remoteName}'.", nameof(remoteName));

                var pushOptions = new PushOptions
                {
                    CredentialsProvider = BuildCredentialsHandler(credentials),
                    OnPushTransferProgress = (current, total, bytes) =>
                    {
                        if (ct.IsCancellationRequested) return false;
                        progress?.Report(new SyncProgress
                        {
                            Stage = "push",
                            Received = current,
                            Total = total,
                            Bytes = bytes,
                        });
                        return true;
                    },
                };

                var prefix = force ? "+" : string.Empty;
                var refspecs = new List<string>(branchNames.Count);
                foreach (var name in branchNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                        throw new ArgumentException("Branch names cannot be blank.", nameof(branchNames));

                    var branch = repo.Branches[name]
                        ?? throw new ArgumentException($"No local branch named '{name}'.", nameof(branchNames));

                    // Use the branch's canonical name on both sides; libgit2 handles
                    // the local -> remote ref mapping itself.
                    refspecs.Add($"{prefix}{branch.CanonicalName}:{branch.CanonicalName}");
                }

                try
                {
                    repo.Network.Push(remote, refspecs, pushOptions);
                }
                catch (UserCancelledException)
                {
                    ct.ThrowIfCancellationRequested();
                    throw new OperationCanceledException();
                }
            }
            finally
            {
                h.AsyncLock.Release();
            }
        }, ct);
    }

    private static FetchOptions BuildFetchOptions(
        IProgress<SyncProgress>? progress,
        Credentials? credentials,
        CancellationToken ct)
    {
        return new FetchOptions
        {
            CredentialsProvider = BuildCredentialsHandler(credentials),
            OnTransferProgress = tp =>
            {
                if (ct.IsCancellationRequested) return false;
                progress?.Report(new SyncProgress
                {
                    Stage = "transfer",
                    Received = tp.ReceivedObjects,
                    Total = tp.TotalObjects,
                    Bytes = tp.ReceivedBytes,
                });
                return true;
            },
        };
    }

    private static LibGit2Sharp.Handlers.CredentialsHandler? BuildCredentialsHandler(Credentials? credentials)
    {
        if (credentials is null) return null;
        return (_, _, _) => new UsernamePasswordCredentials
        {
            Username = credentials.Username,
            Password = credentials.Password,
        };
    }

    private static Branch? ResolveRemoteTrackingBranch(Repository repo, string remoteName, string localBranchName)
    {
        // Strip any "refs/heads/" prefix the caller may have passed in.
        var shortName = localBranchName;
        const string headsPrefix = "refs/heads/";
        if (shortName.StartsWith(headsPrefix, StringComparison.Ordinal))
            shortName = shortName[headsPrefix.Length..];

        var qualified = $"{remoteName}/{shortName}";
        var candidate = repo.Branches[qualified];
        if (candidate is { IsRemote: true })
            return candidate;

        // Fallback: anything tagged as remote whose short name (after the remote prefix) matches.
        foreach (var b in repo.Branches)
        {
            if (!b.IsRemote) continue;
            if (string.Equals(b.FriendlyName, qualified, StringComparison.Ordinal))
                return b;
        }
        return null;
    }
}
