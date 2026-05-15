namespace TechTeaStudio.GitClient.Inspection;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Structured (per-file, per-hunk, per-line) diff comparison. Complements
/// the textual <c>IRepositoryService.GetDiffAsync</c> with rendering-friendly data.</summary>
public interface IFileDiffService
{
    /// <summary>Compare two commits' trees. Returns one <see cref="FileDiff"/> per
    /// changed file, with hunks and per-line classification.</summary>
    Task<IReadOnlyList<FileDiff>> CompareCommitsAsync(
        IRepoHandle handle,
        string fromSha,
        string toSha,
        CancellationToken ct = default);

    /// <summary>Compare a commit against its first parent. For a root commit (no
    /// parents) this returns the diff against an empty tree — i.e. every blob as
    /// <see cref="FileDiffKind.Added"/>.</summary>
    Task<IReadOnlyList<FileDiff>> CompareWithParentAsync(
        IRepoHandle handle,
        string commitSha,
        CancellationToken ct = default);
}
