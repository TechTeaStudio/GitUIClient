namespace TechTeaStudio.GitClient.Rewrites;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// History-rewriting operations: merge, rebase, revert, cherry-pick, reset.
/// Every method serialises on the handle's async lock — LibGit2Sharp's
/// <c>Repository</c> is not thread-safe.
/// </summary>
public interface IRewriteService
{
    /// <summary>
    /// Merge <paramref name="branchOrSha"/> into the current HEAD using
    /// <paramref name="options"/> to control fast-forward / squash behaviour.
    /// Returns the resulting <see cref="MergeOutcome"/>.
    /// </summary>
    Task<MergeOutcome> MergeAsync(
        IRepoHandle handle,
        string branchOrSha,
        AuthorInfo merger,
        MergeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Start a rebase of the current branch onto <paramref name="branchOrUpstreamSha"/>.
    /// Returns the rebase state after the first batch of steps runs.
    /// </summary>
    Task<RebaseState> RebaseStartAsync(
        IRepoHandle handle,
        string branchOrUpstreamSha,
        AuthorInfo identity,
        CancellationToken ct = default);

    /// <summary>Continue a paused (conflicted) rebase after the user resolves it.</summary>
    Task<RebaseState> RebaseContinueAsync(
        IRepoHandle handle,
        AuthorInfo identity,
        CancellationToken ct = default);

    /// <summary>Skip the current rebase step.</summary>
    Task<RebaseState> RebaseSkipAsync(
        IRepoHandle handle,
        AuthorInfo identity,
        CancellationToken ct = default);

    /// <summary>Abort the in-progress rebase and restore the pre-rebase HEAD.</summary>
    Task RebaseAbortAsync(IRepoHandle handle, CancellationToken ct = default);

    /// <summary>
    /// Revert <paramref name="commitSha"/> by applying its inverse on top of HEAD.
    /// When <paramref name="commitOnSuccess"/> is <c>true</c>, an automatic revert
    /// commit is created and returned; otherwise the inverse is left in the index
    /// and the returned <see cref="CommitInfo"/> describes HEAD as it stands.
    /// </summary>
    Task<CommitInfo> RevertAsync(
        IRepoHandle handle,
        string commitSha,
        AuthorInfo reverter,
        bool commitOnSuccess,
        CancellationToken ct = default);

    /// <summary>
    /// Cherry-pick <paramref name="commitSha"/> onto HEAD. Behaviour around
    /// <paramref name="commitOnSuccess"/> matches <see cref="RevertAsync"/>.
    /// </summary>
    Task<CommitInfo> CherryPickAsync(
        IRepoHandle handle,
        string commitSha,
        AuthorInfo committer,
        bool commitOnSuccess,
        CancellationToken ct = default);

    /// <summary>
    /// Reset HEAD to <paramref name="targetSha"/> with the given <paramref name="mode"/>.
    /// <see cref="ResetKind.Hard"/> discards working-tree changes — destructive.
    /// </summary>
    Task ResetAsync(
        IRepoHandle handle,
        string targetSha,
        ResetKind mode,
        CancellationToken ct = default);
}
