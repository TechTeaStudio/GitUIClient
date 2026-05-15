namespace TechTeaStudio.GitClient.Branching;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IBranchOpsService"/>.
///
/// Concurrency: every method takes the handle's async lock before touching
/// the underlying <see cref="Repository"/>. LibGit2Sharp is not thread-safe.
///
/// Most operations are synchronous-bound inside LibGit2Sharp — wrapping them
/// in <see cref="Task.FromResult{TResult}(TResult)"/> after the lock is honest
/// about that and matches the pattern in <see cref="LibGit2RepositoryService"/>.
/// </summary>
public sealed class LibGit2BranchOpsService : IBranchOpsService
{
    public async Task<BranchInfo> CreateBranchAsync(
        IRepoHandle handle,
        string name,
        string? startPointSha = null,
        bool checkout = false,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;

            Commit target;
            if (string.IsNullOrWhiteSpace(startPointSha))
            {
                target = repo.Head.Tip
                    ?? throw new InvalidOperationException("HEAD has no commits; create a commit before branching.");
            }
            else
            {
                target = repo.Lookup<Commit>(startPointSha)
                    ?? throw new ArgumentException(
                        $"Could not resolve '{startPointSha}' to a commit.",
                        nameof(startPointSha));
            }

            var branch = repo.CreateBranch(name, target);

            if (checkout)
            {
                Commands.Checkout(repo, branch);
            }

            return MapBranch(branch);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<BranchInfo> RenameBranchAsync(
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
            var repo = h.Repository;
            var existing = repo.Branches[oldName]
                ?? throw new ArgumentException(
                    $"Branch '{oldName}' does not exist.",
                    nameof(oldName));

            var renamed = repo.Branches.Rename(existing, newName);
            return MapBranch(renamed);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task DeleteBranchAsync(
        IRepoHandle handle,
        string name,
        bool force = false,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var branch = repo.Branches[name]
                ?? throw new ArgumentException(
                    $"Branch '{name}' does not exist.",
                    nameof(name));

            if (branch.IsCurrentRepositoryHead)
                throw new InvalidOperationException(
                    $"Cannot delete the currently-checked-out branch '{name}'.");

            if (!force && !IsMergedIntoHead(repo, branch))
            {
                throw new InvalidOperationException(
                    $"Branch '{name}' is not fully merged. Pass force=true to delete anyway.");
            }

            repo.Branches.Remove(branch);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task CheckoutBranchAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var branch = repo.Branches[name]
                ?? throw new ArgumentException(
                    $"Branch '{name}' does not exist.",
                    nameof(name));

            Commands.Checkout(repo, branch);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static BranchInfo MapBranch(Branch b) => new()
    {
        Name = b.FriendlyName,
        TipSha = b.Tip?.Sha ?? string.Empty,
        IsCurrent = b.IsCurrentRepositoryHead,
        IsRemote = b.IsRemote,
    };

    /// <summary>
    /// True when every commit reachable from <paramref name="branch"/>'s tip
    /// is also reachable from HEAD — i.e. the branch is "fully merged" into
    /// the current branch.
    /// </summary>
    private static bool IsMergedIntoHead(Repository repo, Branch branch)
    {
        var branchTip = branch.Tip;
        var headTip = repo.Head.Tip;
        if (branchTip is null) return true;
        if (headTip is null) return false;
        if (branchTip.Sha == headTip.Sha) return true;

        // ancestor commits: if the branch tip is an ancestor of HEAD's tip
        // then everything on the branch is reachable from HEAD.
        var mergeBase = repo.ObjectDatabase.FindMergeBase(branchTip, headTip);
        return mergeBase is not null && mergeBase.Sha == branchTip.Sha;
    }
}
