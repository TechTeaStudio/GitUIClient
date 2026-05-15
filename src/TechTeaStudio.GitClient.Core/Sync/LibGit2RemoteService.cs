namespace TechTeaStudio.GitClient.Sync;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed remote management. <c>git ls-remote</c> via
/// <see cref="Network.ListReferences(Remote)"/> is the only entry point that
/// actually touches the network — and even that does no object transfer —
/// so we run it through <see cref="Task.Run(Action)"/>; everything else is
/// pure config-file work and stays on the calling thread via <see cref="Task.FromResult{TResult}(TResult)"/>.
///
/// Concurrency: takes the handle's async lock — see <see cref="LibGit2RepoHandle"/>.
/// </summary>
public sealed class LibGit2RemoteService : IRemoteService
{
    public async Task<IReadOnlyList<RemoteInfo>> ListRemotesAsync(
        IRepoHandle handle,
        CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var list = new List<RemoteInfo>();
            foreach (var r in h.Repository.Network.Remotes)
            {
                ct.ThrowIfCancellationRequested();
                list.Add(MapRemote(r));
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task AddRemoteAsync(
        IRepoHandle handle,
        string name,
        string url,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            h.Repository.Network.Remotes.Add(name, url);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task RemoveRemoteAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            h.Repository.Network.Remotes.Remove(name);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task RenameRemoteAsync(
        IRepoHandle handle,
        string oldName,
        string newName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldName);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Failure handler is required by the LibGit2Sharp signature; we swallow
            // refspec-rewrite failures and let LibGit2Sharp throw for harder errors.
            h.Repository.Network.Remotes.Rename(oldName, newName, _ => { });
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public Task<IReadOnlyList<RemoteRefInfo>> ListRemoteRefsAsync(
        IRepoHandle handle,
        string remoteName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);

        var h = LibGit2RepositoryService.CastHandle(handle);
        return Task.Run<IReadOnlyList<RemoteRefInfo>>(async () =>
        {
            await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var repo = h.Repository;
                var remote = repo.Network.Remotes[remoteName]
                    ?? throw new ArgumentException($"No remote named '{remoteName}'.", nameof(remoteName));

                var list = new List<RemoteRefInfo>();
                foreach (var reference in repo.Network.ListReferences(remote))
                {
                    ct.ThrowIfCancellationRequested();
                    list.Add(new RemoteRefInfo
                    {
                        Name = reference.CanonicalName,
                        Sha = reference.TargetIdentifier ?? string.Empty,
                        IsBranch = reference.IsLocalBranch || reference.IsRemoteTrackingBranch,
                        IsTag = reference.IsTag,
                    });
                }
                return list;
            }
            finally
            {
                h.AsyncLock.Release();
            }
        }, ct);
    }

    private static RemoteInfo MapRemote(Remote r) => new()
    {
        Name = r.Name,
        Url = r.Url,
        // Remote.PushUrl falls back to Url when unset, so we only surface the explicit value
        // when it actually differs (closer to git's mental model).
        PushUrl = string.Equals(r.PushUrl, r.Url, StringComparison.Ordinal) ? null : r.PushUrl,
    };
}
