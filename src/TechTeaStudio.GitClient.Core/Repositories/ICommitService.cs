namespace TechTeaStudio.GitClient.Repositories;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Write operation: stage <paramref name="pathSpec"/> (or all of working tree
/// when null/empty) and create a commit. Push/pull are explicitly deferred to
/// a future version.
/// </summary>
public interface ICommitService
{
    Task<CommitInfo> CommitAsync(
        IRepoHandle handle,
        string message,
        AuthorInfo author,
        IEnumerable<string>? pathSpec = null,
        CancellationToken ct = default);
}
