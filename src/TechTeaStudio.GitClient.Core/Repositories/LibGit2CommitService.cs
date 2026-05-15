namespace TechTeaStudio.GitClient.Repositories;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// LibGit2Sharp-backed write side. Stages the requested path-spec (or all of
/// the working tree when null/empty) and creates a commit.
///
/// Concurrency: takes the handle's async lock — see <see cref="LibGit2RepoHandle"/>.
/// </summary>
public sealed class LibGit2CommitService : ICommitService
{
    public async Task<CommitInfo> CommitAsync(
        IRepoHandle handle,
        string message,
        AuthorInfo author,
        IEnumerable<string>? pathSpec = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;

            var paths = pathSpec?.ToArray();
            if (paths is null || paths.Length == 0)
            {
                Commands.Stage(repo, "*");
            }
            else
            {
                Commands.Stage(repo, paths);
            }

            var sig = new Signature(
                author.Name,
                author.Email,
                author.When ?? DateTimeOffset.UtcNow);

            try
            {
                var commit = repo.Commit(message, sig, sig);
                return new CommitInfo
                {
                    Sha = commit.Sha,
                    Author = commit.Author.Name,
                    Email = commit.Author.Email,
                    When = commit.Author.When,
                    Message = commit.Message ?? string.Empty,
                    Parents = commit.Parents.Select(p => p.Sha).ToList(),
                };
            }
            catch (EmptyCommitException ex)
            {
                throw new InvalidOperationException(
                    "No changes staged; commit would be empty.", ex);
            }
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }
}
