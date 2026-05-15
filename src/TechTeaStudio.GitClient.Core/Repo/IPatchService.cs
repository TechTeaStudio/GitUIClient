namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Apply and emit textual patches.
///
/// LibGit2Sharp doesn't expose <c>git apply</c> / <c>git format-patch</c>, so
/// both methods shell out to the <c>git</c> binary. Callers should be ready
/// for the operation to throw when <c>git</c> is not on <c>PATH</c>.
/// </summary>
public interface IPatchService
{
    /// <summary>
    /// Apply <paramref name="patchFilePath"/> to the repo. When
    /// <paramref name="indexOnly"/> is <c>true</c>, the patch updates only
    /// the index (equivalent to <c>git apply --cached</c>); otherwise the
    /// working tree is updated as well.
    /// </summary>
    Task ApplyPatchAsync(
        IRepoHandle handle,
        string patchFilePath,
        bool indexOnly,
        CancellationToken ct = default);

    /// <summary>
    /// Produce a patch text for the single commit at <paramref name="commitSha"/>
    /// (equivalent to <c>git format-patch -1 --stdout</c>).
    /// </summary>
    Task<string> FormatPatchAsync(
        IRepoHandle handle,
        string commitSha,
        CancellationToken ct = default);
}
