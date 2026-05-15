namespace TechTeaStudio.GitClient.Repo;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

using ReflogEntry = TechTeaStudio.GitClient.Models.ReflogEntry;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IReflogService"/>.
///
/// Concurrency: takes the handle's async lock around every native call —
/// LibGit2Sharp's <c>Repository</c> is not thread-safe.
/// </summary>
public sealed class LibGit2ReflogService : IReflogService
{
    public async Task<IReadOnlyList<ReflogEntry>> ReadReflogAsync(
        IRepoHandle handle,
        string reference,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var canonical = ResolveCanonicalName(repo, reference)
                ?? throw new ArgumentException(
                    $"Could not resolve '{reference}' to a reference.", nameof(reference));

            var reflog = repo.Refs.Log(canonical);
            var list = new List<ReflogEntry>();
            foreach (var entry in reflog)
            {
                ct.ThrowIfCancellationRequested();
                list.Add(new ReflogEntry
                {
                    FromSha = entry.From?.Sha ?? string.Empty,
                    ToSha = entry.To?.Sha ?? string.Empty,
                    Message = entry.Message ?? string.Empty,
                    When = entry.Committer.When,
                    CommitterName = entry.Committer.Name ?? string.Empty,
                    CommitterEmail = entry.Committer.Email ?? string.Empty,
                });
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static string? ResolveCanonicalName(Repository repo, string reference)
    {
        // Already canonical (refs/heads/main, refs/tags/v1, HEAD)?
        try
        {
            var direct = repo.Refs[reference];
            if (direct is not null) return direct.CanonicalName;
        }
        catch (InvalidSpecificationException) { /* not a valid ref name; try branch lookup */ }
        catch (LibGit2SharpException)         { /* fall through */ }

        // Friendly branch name -> canonical
        try
        {
            var branch = repo.Branches[reference];
            if (branch is not null) return branch.CanonicalName;
        }
        catch (LibGit2SharpException) { /* fall through */ }

        return null;
    }
}
