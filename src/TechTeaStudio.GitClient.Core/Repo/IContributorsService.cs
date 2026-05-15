namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Author aggregation over commit history. Walks every branch tip
/// (de-duplicating commits) and groups by author email.
/// </summary>
public interface IContributorsService
{
    /// <summary>
    /// List contributors ordered by commit count (descending). When
    /// <paramref name="maxCommits"/> is set, only the most recent N commits
    /// (across all branches) are considered.
    /// </summary>
    Task<IReadOnlyList<ContributorInfo>> ListContributorsAsync(
        IRepoHandle handle,
        int? maxCommits = null,
        CancellationToken ct = default);
}
