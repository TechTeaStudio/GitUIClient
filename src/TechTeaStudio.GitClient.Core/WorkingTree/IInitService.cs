namespace TechTeaStudio.GitClient.WorkingTree;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Initialize a new repository on disk. Supports the "bare" option that
/// <see cref="IRepositoryService.InitAsync"/> does not expose.
/// </summary>
public interface IInitService
{
    /// <summary>
    /// Initialize a repo at <paramref name="path"/>. When <paramref name="bare"/> is true
    /// the result has no working directory (server-style remote). Returns a handle that
    /// must be disposed by the caller.
    /// </summary>
    Task<IRepoHandle> InitRepositoryAsync(string path, bool bare, CancellationToken ct = default);
}
