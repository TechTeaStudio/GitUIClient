namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Reflog reader. Newest entry first.</summary>
public interface IReflogService
{
    /// <summary>
    /// Read the reflog for <paramref name="reference"/> (e.g. <c>"HEAD"</c>,
    /// <c>"refs/heads/main"</c>, or a branch friendly name). Returns the
    /// entries newest-first.
    /// </summary>
    Task<IReadOnlyList<ReflogEntry>> ReadReflogAsync(
        IRepoHandle handle,
        string reference,
        CancellationToken ct = default);
}
