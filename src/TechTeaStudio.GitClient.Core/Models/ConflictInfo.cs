namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// One file currently in a merge / cherry-pick / rebase conflict.
///
/// Each SHA references the blob LibGit2Sharp staged in the corresponding
/// index slot (stage 1 = ancestor, 2 = ours, 3 = theirs). Any side can be
/// <c>null</c> when it isn't present in the conflict (e.g. add/add has no
/// ancestor; delete/modify has only two sides).
/// </summary>
public sealed record ConflictInfo
{
    /// <summary>Path of the conflicted file relative to the working tree root.</summary>
    public required string Path { get; init; }

    /// <summary>SHA of the "ours" blob (stage 2) — the current HEAD's side.</summary>
    public string? OursSha { get; init; }

    /// <summary>SHA of the "theirs" blob (stage 3) — the incoming side.</summary>
    public string? TheirsSha { get; init; }

    /// <summary>SHA of the common ancestor blob (stage 1).</summary>
    public string? AncestorSha { get; init; }
}
