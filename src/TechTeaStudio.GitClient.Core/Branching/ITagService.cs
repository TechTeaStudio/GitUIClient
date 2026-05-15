namespace TechTeaStudio.GitClient.Branching;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Tag CRUD: list, create lightweight, create annotated, delete.
/// </summary>
public interface ITagService
{
    /// <summary>List every tag in the repository.</summary>
    Task<IReadOnlyList<TagInfo>> ListTagsAsync(
        IRepoHandle handle,
        CancellationToken ct = default);

    /// <summary>
    /// Create a lightweight tag (a plain reference to <paramref name="targetSha"/>
    /// with no tagger / message).
    /// </summary>
    Task<TagInfo> CreateLightweightTagAsync(
        IRepoHandle handle,
        string name,
        string targetSha,
        CancellationToken ct = default);

    /// <summary>
    /// Create an annotated tag: produces a tag object with the supplied
    /// <paramref name="message"/> and <paramref name="tagger"/> identity that
    /// points at <paramref name="targetSha"/>.
    /// </summary>
    Task<TagInfo> CreateAnnotatedTagAsync(
        IRepoHandle handle,
        string name,
        string targetSha,
        string message,
        AuthorInfo tagger,
        CancellationToken ct = default);

    /// <summary>Delete a tag by name.</summary>
    Task DeleteTagAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default);
}
