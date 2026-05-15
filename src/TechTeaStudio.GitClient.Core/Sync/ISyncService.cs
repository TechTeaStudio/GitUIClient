namespace TechTeaStudio.GitClient.Sync;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Network-touching operations: fetch / pull / push. All three are slow and
/// run on a worker thread (<see cref="Task.Run(Action)"/>) — callers should
/// drive them from a UI thread without fear.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Fetch from <paramref name="remoteName"/>. Updates remote-tracking refs only,
    /// never touches the local working tree.
    /// </summary>
    Task FetchAsync(
        IRepoHandle handle,
        string remoteName,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch from <paramref name="remoteName"/> then merge <paramref name="branchName"/>
    /// (or the upstream-tracked branch when null) into the current branch.
    /// </summary>
    /// <param name="mergeAuthor">Signature used when the merge produces a new merge commit.</param>
    Task<PullResult> PullAsync(
        IRepoHandle handle,
        string remoteName,
        string? branchName,
        AuthorInfo mergeAuthor,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default);

    /// <summary>
    /// Push one or more local branches to <paramref name="remoteName"/>. When
    /// <paramref name="force"/> is true the refspec is prefixed with <c>+</c>.
    /// </summary>
    Task PushAsync(
        IRepoHandle handle,
        string remoteName,
        IReadOnlyList<string> branchNames,
        bool force = false,
        IProgress<SyncProgress>? progress = null,
        Credentials? credentials = null,
        CancellationToken ct = default);
}
