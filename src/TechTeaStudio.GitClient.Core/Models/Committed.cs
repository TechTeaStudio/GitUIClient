#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Outcome case of a cherry-pick or revert: the operation applied cleanly and
/// a new commit was created on HEAD.
/// </summary>
public sealed record Committed
{
    /// <summary>The commit the operation created; HEAD now points at it.</summary>
    public required CommitInfo Commit { get; init; }
}
#endif
