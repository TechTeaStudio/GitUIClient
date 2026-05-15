namespace TechTeaStudio.GitClient.WorkingTree;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Discover and delete untracked files in the working tree (git clean).
/// </summary>
public interface ICleanService
{
    /// <summary>Repo-relative paths of every untracked file (excluding ignored).</summary>
    Task<IReadOnlyList<string>> ListUntrackedAsync(IRepoHandle handle, CancellationToken ct = default);

    /// <summary>
    /// Delete the supplied repo-relative paths from the working tree. Returns the count
    /// actually removed; paths that don't exist are silently skipped.
    /// </summary>
    Task<int> CleanUntrackedAsync(IRepoHandle handle, IEnumerable<string> paths, CancellationToken ct = default);
}
