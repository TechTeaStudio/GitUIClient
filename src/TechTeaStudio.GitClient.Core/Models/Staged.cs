#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Outcome case of a cherry-pick or revert run with <c>commitOnSuccess: false</c>:
/// the change applied cleanly and sits in the index, no commit was made.
/// </summary>
public sealed record Staged
{
    /// <summary>HEAD as it stands; the operation did not move it.</summary>
    public required CommitInfo Head { get; init; }
}
#endif
