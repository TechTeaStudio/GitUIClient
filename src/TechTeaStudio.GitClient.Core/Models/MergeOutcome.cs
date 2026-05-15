namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Result of a merge operation. Mirrors LibGit2Sharp's <c>MergeStatus</c> but
/// narrowed to what the UI needs.
/// </summary>
public enum MergeOutcome
{
    /// <summary>Source is already reachable from HEAD — nothing to do.</summary>
    UpToDate,

    /// <summary>HEAD was fast-forwarded to the source tip.</summary>
    FastForward,

    /// <summary>A real merge commit was created (two-parent commit on HEAD).</summary>
    NonFastForward,

    /// <summary>Merge produced conflicts; the working tree is dirty and HEAD is unchanged.</summary>
    Conflicts,
}
