namespace TechTeaStudio.GitClient.Inspection;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>Per-line blame for a file at a given commit.</summary>
public interface IBlameService
{
    /// <summary>Compute blame for <paramref name="path"/> as it exists at
    /// <paramref name="commitSha"/>. Each returned <see cref="BlameLine"/> identifies
    /// the commit that last authored that source line.</summary>
    Task<IReadOnlyList<BlameLine>> BlameFileAsync(
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default);
}
