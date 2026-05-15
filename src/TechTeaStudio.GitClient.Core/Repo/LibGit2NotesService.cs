namespace TechTeaStudio.GitClient.Repo;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="INotesService"/>.
///
/// The namespace argument follows LibGit2Sharp's convention: a bare name
/// (e.g. <c>"commits"</c>) is treated as <c>refs/notes/&lt;name&gt;</c>.
/// Note that LibGit2Sharp's <see cref="NoteCollection.Add"/> takes
/// <c>author</c> + <c>committer</c> as separate signatures; we use the
/// caller-supplied <see cref="AuthorInfo"/> for both.
///
/// Concurrency: takes the handle's async lock around every native call.
/// </summary>
public sealed class LibGit2NotesService : INotesService
{
    public async Task<string?> ReadNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string @namespace = "commits",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var oid = ParseObjectId(targetSha);
            var note = repo.Notes[@namespace, oid];
            return note?.Message;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task AddNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string message,
        AuthorInfo committer,
        string @namespace = "commits",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(committer);
        ArgumentException.ThrowIfNullOrWhiteSpace(committer.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(committer.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var oid = ParseObjectId(targetSha);
            var sig = new Signature(committer.Name, committer.Email, committer.When ?? DateTimeOffset.UtcNow);
            repo.Notes.Add(oid, message, sig, sig, @namespace);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task RemoveNoteAsync(
        IRepoHandle handle,
        string targetSha,
        string @namespace = "commits",
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(@namespace);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var oid = ParseObjectId(targetSha);
            // LibGit2Sharp insists on a real signature even for delete; the user
            // told us who they are via AuthorInfo at add-time, but we don't get
            // one on remove. Use a synthetic "notes" identity — matches how
            // `git notes remove` records the operation under a generated ident.
            var sig = new Signature("notes", "notes@local", DateTimeOffset.UtcNow);
            repo.Notes.Remove(oid, sig, sig, @namespace);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static ObjectId ParseObjectId(string sha)
    {
        try
        {
            return new ObjectId(sha);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new ArgumentException($"'{sha}' is not a valid SHA.", nameof(sha), ex);
        }
    }
}
