namespace TechTeaStudio.GitClient.Rewrites;

using TechTeaStudio.GitClient.Models;

/// <summary>What a single cherry-pick or revert did to the repository.</summary>
internal enum RewriteStepKind
{
    /// <summary>A new commit was created on HEAD.</summary>
    CommitCreated,

    /// <summary>The change applied cleanly and sits in the index; HEAD did not move.</summary>
    LeftInIndex,

    /// <summary>The operation stopped on conflicts; HEAD did not move.</summary>
    Conflicts,

    /// <summary>Revert only: the inverse changes nothing.</summary>
    NothingToRevert,
}

/// <summary>
/// Plain result of the shared cherry-pick / revert core in
/// <see cref="LibGit2RewriteService"/>. The exception-based methods and the
/// union-returning methods are both mapped from it.
/// </summary>
internal sealed record RewriteStepResult
{
    public required RewriteStepKind Kind { get; init; }

    /// <summary>The created commit; set only for <see cref="RewriteStepKind.CommitCreated"/>.</summary>
    public CommitInfo? Commit { get; init; }

    /// <summary>
    /// HEAD after the operation; set for <see cref="RewriteStepKind.CommitCreated"/>
    /// (equal to <see cref="Commit"/>) and <see cref="RewriteStepKind.LeftInIndex"/>.
    /// </summary>
    public CommitInfo? Head { get; init; }

    /// <summary>Conflicted paths relative to the working tree; empty unless <see cref="RewriteStepKind.Conflicts"/>.</summary>
    public IReadOnlyList<string> ConflictedPaths { get; init; } = Array.Empty<string>();
}
