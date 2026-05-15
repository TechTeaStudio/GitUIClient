namespace TechTeaStudio.GitClient.WorkingTree;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Amend the most recent commit (rewrite HEAD). Optionally re-stages files via <c>pathSpec</c>
/// before amending; if <paramref name="newMessage"/> is null/empty the original message is kept.
/// </summary>
public interface IAmendService
{
    Task<CommitInfo> AmendAsync(
        IRepoHandle handle,
        string? newMessage,
        AuthorInfo author,
        IEnumerable<string>? pathSpec = null,
        CancellationToken ct = default);
}
