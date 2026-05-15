namespace TechTeaStudio.GitClient.Repositories;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Read-mostly repository operations: open, init, shallow-clone, and the
/// inspection APIs the UI needs (branches, commits, diff, status).
/// </summary>
public interface IRepositoryService
{
    /// <summary>Open an existing repository at <paramref name="path"/>.</summary>
    Task<IRepoHandle> OpenAsync(string path, CancellationToken ct = default);

    /// <summary>Initialise a new empty repository at <paramref name="path"/>.</summary>
    Task<IRepoHandle> InitAsync(string path, CancellationToken ct = default);

    /// <summary>Clone <paramref name="url"/> into <paramref name="path"/>.</summary>
    Task<IRepoHandle> CloneAsync(
        string url,
        string path,
        IProgress<CloneProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>List local branches with their tip-commit sha.</summary>
    Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(IRepoHandle handle, CancellationToken ct = default);

    /// <summary>Most-recent <paramref name="take"/> commits reachable from a branch or ref.</summary>
    Task<IReadOnlyList<CommitInfo>> GetCommitsAsync(
        IRepoHandle handle,
        string branchOrRef,
        int take,
        CancellationToken ct = default);

    /// <summary>Unified diff (patch) between two commit shas.</summary>
    Task<string> GetDiffAsync(IRepoHandle handle, string fromSha, string toSha, CancellationToken ct = default);

    /// <summary>Working-tree status: file lists keyed by change state.</summary>
    Task<RepoStatus> GetStatusAsync(IRepoHandle handle, CancellationToken ct = default);
}
