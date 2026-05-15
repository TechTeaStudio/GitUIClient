namespace TechTeaStudio.GitClient.WorkingTree;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IStashService"/>.
///
/// Concurrency: every method takes the handle's async lock — see <see cref="LibGit2RepoHandle"/>.
/// </summary>
public sealed class LibGit2StashService : IStashService
{
    public async Task<StashInfo> SaveStashAsync(
        IRepoHandle handle,
        string? message,
        AuthorInfo stasher,
        bool includeUntracked,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stasher);
        ArgumentException.ThrowIfNullOrWhiteSpace(stasher.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(stasher.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var sig = new Signature(stasher.Name, stasher.Email, stasher.When ?? DateTimeOffset.UtcNow);

            var modifiers = includeUntracked
                ? StashModifiers.IncludeUntracked
                : StashModifiers.Default;

            var stash = repo.Stashes.Add(sig, message, modifiers)
                ?? throw new InvalidOperationException("Nothing to stash; working tree is clean.");

            // Newly added stash sits at index 0.
            return MapStash(stash, index: 0);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<IReadOnlyList<StashInfo>> ListStashesAsync(IRepoHandle handle, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var list = new List<StashInfo>();
            var index = 0;
            foreach (var stash in h.Repository.Stashes)
            {
                ct.ThrowIfCancellationRequested();
                list.Add(MapStash(stash, index));
                index++;
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task ApplyStashAsync(IRepoHandle handle, int index, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var result = h.Repository.Stashes.Apply(index);
            ThrowOnFailedApply(result);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task PopStashAsync(IRepoHandle handle, int index, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var result = h.Repository.Stashes.Pop(index);
            ThrowOnFailedApply(result);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task DropStashAsync(IRepoHandle handle, int index, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            h.Repository.Stashes.Remove(index);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static void ThrowOnFailedApply(StashApplyStatus status)
    {
        switch (status)
        {
            case StashApplyStatus.Applied:
                return;
            case StashApplyStatus.NotFound:
                throw new InvalidOperationException("Stash not found at the supplied index.");
            case StashApplyStatus.Conflicts:
                throw new InvalidOperationException("Stash applied with conflicts.");
            case StashApplyStatus.UncommittedChanges:
                throw new InvalidOperationException("Cannot apply stash: working tree has uncommitted changes.");
            default:
                throw new InvalidOperationException($"Stash apply failed: {status}.");
        }
    }

    private static StashInfo MapStash(Stash stash, int index)
    {
        // Stash.WorkTree is the commit that recorded the working-tree state.
        // Its author timestamp is what `git stash list` shows as the stash date.
        var work = stash.WorkTree;
        return new StashInfo
        {
            Index = index,
            Message = stash.Message ?? string.Empty,
            Sha = work?.Sha ?? string.Empty,
            When = work?.Author.When ?? DateTimeOffset.UtcNow,
        };
    }
}
