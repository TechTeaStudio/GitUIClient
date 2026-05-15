namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Aggregated contributor statistics over a slice of repository history.
///
/// Authors are grouped by email (case-insensitive). The display name is the
/// most recent <c>author.name</c> seen on a commit by that email.
/// </summary>
public sealed record ContributorInfo
{
    /// <summary>Display name (last seen <c>author.name</c> for this email).</summary>
    public required string Name { get; init; }

    /// <summary>Author email — the grouping key.</summary>
    public required string Email { get; init; }

    /// <summary>Number of commits authored by this contributor in the slice.</summary>
    public required int CommitCount { get; init; }

    /// <summary>Author timestamp of the earliest commit in the slice.</summary>
    public required DateTimeOffset FirstCommit { get; init; }

    /// <summary>Author timestamp of the most recent commit in the slice.</summary>
    public required DateTimeOffset LastCommit { get; init; }
}
