namespace TechTeaStudio.GitClient.Models;

/// <summary>Outcome of a pull (fetch + merge) operation.</summary>
public sealed record PullResult
{
    public required PullKind Kind { get; init; }

    /// <summary>HEAD after the pull. Empty when the local branch had no commits to begin with.</summary>
    public required string Sha { get; init; }
}

/// <summary>How a pull resolved against the local branch.</summary>
public enum PullKind
{
    /// <summary>Local branch was already up to date with the remote.</summary>
    UpToDate,

    /// <summary>Remote was a strict superset of local — HEAD fast-forwarded.</summary>
    FastForward,

    /// <summary>Histories diverged but merged cleanly into a new merge commit.</summary>
    NonFastForward,

    /// <summary>Merge produced conflicts — caller must resolve.</summary>
    Conflicts,
}
