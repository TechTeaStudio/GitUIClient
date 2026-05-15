namespace TechTeaStudio.GitClient.Repo;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IContributorsService"/>.
///
/// Walks every branch tip (de-duplicating shared commits) and groups
/// authors by lowered email. The display name is the <c>author.name</c>
/// from the most recent commit seen for that email.
///
/// Concurrency: takes the handle's async lock around the walk.
/// </summary>
public sealed class LibGit2ContributorsService : IContributorsService
{
    public async Task<IReadOnlyList<ContributorInfo>> ListContributorsAsync(
        IRepoHandle handle,
        int? maxCommits = null,
        CancellationToken ct = default)
    {
        if (maxCommits is <= 0)
            return Array.Empty<ContributorInfo>();

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var tips = new List<Commit>();
            var seenTip = new HashSet<string>(StringComparer.Ordinal);
            foreach (var branch in repo.Branches)
            {
                if (branch.Tip is { } tip && seenTip.Add(tip.Sha))
                    tips.Add(tip);
            }
            if (tips.Count == 0) return Array.Empty<ContributorInfo>();

            var filter = new CommitFilter
            {
                IncludeReachableFrom = tips,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            };

            // Aggregate while walking; cap at maxCommits if requested.
            var byEmail = new Dictionary<string, Aggregate>(StringComparer.OrdinalIgnoreCase);
            var processed = 0;
            foreach (var c in repo.Commits.QueryBy(filter))
            {
                ct.ThrowIfCancellationRequested();
                if (maxCommits is int cap && processed >= cap) break;
                processed++;

                var email = c.Author.Email ?? string.Empty;
                if (!byEmail.TryGetValue(email, out var agg))
                {
                    agg = new Aggregate
                    {
                        Name = c.Author.Name ?? string.Empty,
                        Email = email,
                        Count = 0,
                        // CommitFilter walks newest-first; the first commit we see is the LAST commit
                        // in calendar order. Initialise both endpoints from it, then move First back
                        // as we encounter older commits.
                        First = c.Author.When,
                        Last = c.Author.When,
                        Newest = c.Author.When,
                    };
                    byEmail[email] = agg;
                }

                agg.Count++;
                if (c.Author.When < agg.First) agg.First = c.Author.When;
                if (c.Author.When > agg.Last) agg.Last = c.Author.When;
                // Display name = name from the most-recent commit by this email.
                if (c.Author.When >= agg.Newest && !string.IsNullOrWhiteSpace(c.Author.Name))
                {
                    agg.Name = c.Author.Name!;
                    agg.Newest = c.Author.When;
                }
            }

            return byEmail.Values
                .OrderByDescending(a => a.Count)
                .ThenBy(a => a.Email, StringComparer.OrdinalIgnoreCase)
                .Select(a => new ContributorInfo
                {
                    Name = a.Name,
                    Email = a.Email,
                    CommitCount = a.Count,
                    FirstCommit = a.First,
                    LastCommit = a.Last,
                })
                .ToList();
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private sealed class Aggregate
    {
        public required string Name { get; set; }
        public required string Email { get; init; }
        public required int Count { get; set; }
        public required DateTimeOffset First { get; set; }
        public required DateTimeOffset Last { get; set; }
        public required DateTimeOffset Newest { get; set; }
    }
}
