#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Outcome case of a cherry-pick or revert that stopped on conflicts: no commit
/// was made, HEAD is unchanged and the conflicts are in the index.
/// </summary>
public sealed record Conflicted
{
    /// <summary>Conflicted paths, relative to the working tree root.</summary>
    public required IReadOnlyList<string> Paths { get; init; }
}
#endif
