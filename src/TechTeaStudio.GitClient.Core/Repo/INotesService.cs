namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Read / write / delete git notes attached to commits.
///
/// The <c>namespace</c> parameter maps to the underlying notes ref
/// (<c>refs/notes/&lt;namespace&gt;</c>). Defaults to <c>"commits"</c>,
/// matching git's default namespace.
/// </summary>
public interface INotesService
{
    /// <summary>
    /// Return the note attached to <paramref name="targetSha"/> in the
    /// given namespace, or <c>null</c> if no note is attached.
    /// </summary>
    Task<string?> ReadNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string @namespace = "commits",
        CancellationToken ct = default);

    /// <summary>
    /// Attach (or replace) a note on <paramref name="targetSha"/>.
    /// </summary>
    Task AddNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string message,
        AuthorInfo committer,
        string @namespace = "commits",
        CancellationToken ct = default);

    /// <summary>Remove the note attached to <paramref name="targetSha"/>.</summary>
    Task RemoveNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string @namespace = "commits",
        CancellationToken ct = default);
}
