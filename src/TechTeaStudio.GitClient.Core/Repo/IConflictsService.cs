namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// List and resolve index conflicts. Resolution writes the chosen side's
/// blob content to the working tree, removes the conflict from the index
/// and stages the result.
/// </summary>
public interface IConflictsService
{
    /// <summary>List every conflicted entry currently in the index.</summary>
    Task<IReadOnlyList<ConflictInfo>> ListConflictsAsync(
        IRepoHandle handle,
        CancellationToken ct = default);

    /// <summary>Resolve <paramref name="path"/> by keeping the "ours" (stage 2) side.</summary>
    Task ResolveByOursAsync(
        IRepoHandle handle,
        string path,
        CancellationToken ct = default);

    /// <summary>Resolve <paramref name="path"/> by keeping the "theirs" (stage 3) side.</summary>
    Task ResolveByTheirsAsync(
        IRepoHandle handle,
        string path,
        CancellationToken ct = default);
}
