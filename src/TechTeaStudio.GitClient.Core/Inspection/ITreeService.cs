namespace TechTeaStudio.GitClient.Inspection;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Reads tree entries (folder listings) and blob contents from a commit's tree.</summary>
public interface ITreeService
{
    /// <summary>List entries under <paramref name="path"/> at the given commit. Pass
    /// <c>null</c> or empty to list the commit's root tree. Returns entries with both
    /// blobs and sub-trees.</summary>
    Task<IReadOnlyList<TreeEntry>> ListTreeAsync(
        IRepoHandle handle,
        string commitSha,
        string? path = null,
        CancellationToken ct = default);

    /// <summary>Read a blob's text content. Returns <c>null</c> when the blob is binary
    /// (heuristic, see <see cref="IsBinaryBlobAsync"/>) or the path does not exist in
    /// the commit's tree.</summary>
    Task<string?> ReadBlobAsync(
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default);

    /// <summary>Heuristic: returns true when the first ~8 KiB of the blob contains a
    /// NUL byte. Used to skip text rendering for binary files in the tree browser.</summary>
    Task<bool> IsBinaryBlobAsync(
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default);
}
