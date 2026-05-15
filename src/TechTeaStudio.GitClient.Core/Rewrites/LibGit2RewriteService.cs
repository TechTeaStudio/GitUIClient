namespace TechTeaStudio.GitClient.Rewrites;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

using LibGit2MergeOptions = LibGit2Sharp.MergeOptions;
using LibGit2RebaseStatus = LibGit2Sharp.RebaseStatus;
using MergeOptions = TechTeaStudio.GitClient.Models.MergeOptions;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IRewriteService"/>.
///
/// Concurrency: every method takes the handle's async lock — LibGit2Sharp's
/// <c>Repository</c> is not thread-safe. The work itself runs synchronously
/// inside the lock: LibGit2Sharp is purely sync, and these operations are too
/// short to justify a thread-pool hop.
/// </summary>
public sealed class LibGit2RewriteService : IRewriteService
{
    public async Task<MergeOutcome> MergeAsync(
        IRepoHandle handle,
        string branchOrSha,
        AuthorInfo merger,
        MergeOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchOrSha);
        ArgumentNullException.ThrowIfNull(merger);
        ArgumentException.ThrowIfNullOrWhiteSpace(merger.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(merger.Email);
        ArgumentNullException.ThrowIfNull(options);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var commit = ResolveCommit(repo, branchOrSha)
                ?? throw new ArgumentException($"Could not resolve '{branchOrSha}' to a commit.", nameof(branchOrSha));

            var sig = BuildSignature(merger);

            // LibGit2Sharp 0.31's MergeOptions has no Squash / CustomMessage
            // properties — the squash workflow is "merge --no-commit, stage,
            // commit manually". For v0.2.0 we honour FastForwardStrategy and
            // CommitOnSuccess; Squash and CustomMessage are accepted in the
            // request shape but not yet exercised end-to-end. Squash semantics
            // are queued for v0.3 — see PLAN follow-ups.
            _ = options.Squash;
            _ = options.CustomMessage;

            var libOpts = new LibGit2MergeOptions
            {
                FastForwardStrategy = MapFastForwardStrategy(options.FastForwardStrategy),
                CommitOnSuccess = options.CommitOnSuccess,
            };

            var result = repo.Merge(commit, sig, libOpts);
            return MapMergeStatus(result.Status);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<RebaseState> RebaseStartAsync(
        IRepoHandle handle,
        string branchOrUpstreamSha,
        AuthorInfo identity,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(branchOrUpstreamSha);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var upstreamBranch = repo.Branches[branchOrUpstreamSha]
                ?? throw new ArgumentException(
                    $"'{branchOrUpstreamSha}' is not a branch. Rebase requires a branch upstream in LibGit2Sharp 0.31.",
                    nameof(branchOrUpstreamSha));

            var libIdentity = new Identity(identity.Name, identity.Email);
            var rebaseOpts = new RebaseOptions();

            var result = repo.Rebase.Start(branch: null, upstream: upstreamBranch, onto: null, libIdentity, rebaseOpts);
            return MapRebaseStatus(result.Status);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<RebaseState> RebaseContinueAsync(
        IRepoHandle handle,
        AuthorInfo identity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var libIdentity = new Identity(identity.Name, identity.Email);
            var result = repo.Rebase.Continue(libIdentity, new RebaseOptions());
            return MapRebaseStatus(result.Status);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<RebaseState> RebaseSkipAsync(
        IRepoHandle handle,
        AuthorInfo identity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            // LibGit2Sharp 0.31 does not expose a native rebase-skip API. Approximate
            // "git rebase --skip" by hard-resetting the work tree (discarding the
            // current step's conflicted patch) and then continuing to the next step.
            if (repo.Head.Tip is { } headCommit)
                repo.Reset(ResetMode.Hard, headCommit);

            var libIdentity = new Identity(identity.Name, identity.Email);
            var result = repo.Rebase.Continue(libIdentity, new RebaseOptions());
            return MapRebaseStatus(result.Status);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task RebaseAbortAsync(IRepoHandle handle, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            h.Repository.Rebase.Abort();
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<CommitInfo> RevertAsync(
        IRepoHandle handle,
        string commitSha,
        AuthorInfo reverter,
        bool commitOnSuccess,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        ArgumentNullException.ThrowIfNull(reverter);
        ArgumentException.ThrowIfNullOrWhiteSpace(reverter.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(reverter.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var target = ResolveCommit(repo, commitSha)
                ?? throw new ArgumentException($"Could not resolve '{commitSha}' to a commit.", nameof(commitSha));

            var sig = BuildSignature(reverter);
            var result = repo.Revert(target, sig, new RevertOptions
            {
                CommitOnSuccess = commitOnSuccess,
            });

            if (result.Status == RevertStatus.Conflicts)
                throw new InvalidOperationException(
                    $"Revert of '{commitSha}' produced conflicts; resolve them in the working tree before retrying.");
            if (result.Status == RevertStatus.NothingToRevert)
                throw new InvalidOperationException(
                    $"Nothing to revert for '{commitSha}'.");

            var newHead = result.Commit ?? repo.Head.Tip
                ?? throw new InvalidOperationException("Revert succeeded but HEAD has no tip commit.");
            return MapCommit(newHead);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<CommitInfo> CherryPickAsync(
        IRepoHandle handle,
        string commitSha,
        AuthorInfo committer,
        bool commitOnSuccess,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        ArgumentNullException.ThrowIfNull(committer);
        ArgumentException.ThrowIfNullOrWhiteSpace(committer.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(committer.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var target = ResolveCommit(repo, commitSha)
                ?? throw new ArgumentException($"Could not resolve '{commitSha}' to a commit.", nameof(commitSha));

            var sig = BuildSignature(committer);
            var result = repo.CherryPick(target, sig, new CherryPickOptions
            {
                CommitOnSuccess = commitOnSuccess,
            });

            if (result.Status == CherryPickStatus.Conflicts)
                throw new InvalidOperationException(
                    $"Cherry-pick of '{commitSha}' produced conflicts; resolve them in the working tree before retrying.");

            var newHead = result.Commit ?? repo.Head.Tip
                ?? throw new InvalidOperationException("Cherry-pick succeeded but HEAD has no tip commit.");
            return MapCommit(newHead);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task ResetAsync(
        IRepoHandle handle,
        string targetSha,
        ResetKind mode,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var target = ResolveCommit(repo, targetSha)
                ?? throw new ArgumentException($"Could not resolve '{targetSha}' to a commit.", nameof(targetSha));

            repo.Reset(MapResetMode(mode), target);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    // -- helpers -----------------------------------------------------------

    private static Signature BuildSignature(AuthorInfo info) => new(
        info.Name,
        info.Email,
        info.When ?? DateTimeOffset.UtcNow);

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

    private static FastForwardStrategy MapFastForwardStrategy(string strategy)
    {
        if (string.IsNullOrWhiteSpace(strategy))
            return FastForwardStrategy.Default;

        return strategy.Trim().ToLowerInvariant() switch
        {
            "default" => FastForwardStrategy.Default,
            "fastforwardonly" or "fast-forward-only" or "ff-only" => FastForwardStrategy.FastForwardOnly,
            "nofastforward" or "no-fast-forward" or "no-ff" => FastForwardStrategy.NoFastForward,
            _ => throw new ArgumentException(
                $"Unknown fast-forward strategy '{strategy}'. Use 'default', 'fastForwardOnly', or 'noFastForward'.",
                nameof(strategy)),
        };
    }

    private static MergeOutcome MapMergeStatus(MergeStatus status) => status switch
    {
        MergeStatus.UpToDate => MergeOutcome.UpToDate,
        MergeStatus.FastForward => MergeOutcome.FastForward,
        MergeStatus.NonFastForward => MergeOutcome.NonFastForward,
        MergeStatus.Conflicts => MergeOutcome.Conflicts,
        _ => MergeOutcome.NonFastForward,
    };

    private static RebaseState MapRebaseStatus(LibGit2RebaseStatus status) => status switch
    {
        LibGit2RebaseStatus.Complete => RebaseState.Complete,
        LibGit2RebaseStatus.Conflicts => RebaseState.Conflicts,
        LibGit2RebaseStatus.Stop => RebaseState.InProgress,
        _ => RebaseState.InProgress,
    };

    private static ResetMode MapResetMode(ResetKind kind) => kind switch
    {
        ResetKind.Soft => ResetMode.Soft,
        ResetKind.Mixed => ResetMode.Mixed,
        ResetKind.Hard => ResetMode.Hard,
        _ => ResetMode.Mixed,
    };

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
