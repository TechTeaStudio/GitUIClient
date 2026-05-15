namespace TechTeaStudio.GitClient.WorkingTree;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IAmendService"/>.
///
/// Concurrency: takes the handle's async lock — see <see cref="LibGit2RepoHandle"/>.
/// </summary>
public sealed class LibGit2AmendService : IAmendService
{
    public async Task<CommitInfo> AmendAsync(
        IRepoHandle handle,
        string? newMessage,
        AuthorInfo author,
        IEnumerable<string>? pathSpec = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;

            var head = repo.Head?.Tip
                ?? throw new InvalidOperationException("Cannot amend: HEAD has no commit yet.");

            // Optional re-stage of supplied paths before amending.
            var paths = pathSpec?.ToArray();
            if (paths is { Length: > 0 })
            {
                Commands.Stage(repo, paths);
            }

            var sig = new Signature(author.Name, author.Email, author.When ?? DateTimeOffset.UtcNow);
            var message = string.IsNullOrWhiteSpace(newMessage)
                ? head.Message ?? string.Empty
                : newMessage;

            var options = new CommitOptions { AmendPreviousCommit = true };

            Commit amended;
            try
            {
                amended = repo.Commit(message, sig, sig, options);
            }
            catch (EmptyCommitException ex)
            {
                throw new InvalidOperationException(
                    "Amend would produce an identical commit; stage a change or supply a new message.", ex);
            }

            return new CommitInfo
            {
                Sha = amended.Sha,
                Author = amended.Author.Name,
                Email = amended.Author.Email,
                When = amended.Author.When,
                Message = amended.Message ?? string.Empty,
                Parents = amended.Parents.Select(p => p.Sha).ToList(),
            };
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }
}
