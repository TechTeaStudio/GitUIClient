namespace TechTeaStudio.GitClient.Models;

/// <summary>One entry in a reference's reflog — a recorded HEAD/ref movement.</summary>
public sealed record ReflogEntry
{
    /// <summary>SHA the reference pointed at before this entry was recorded.</summary>
    public required string FromSha { get; init; }

    /// <summary>SHA the reference points at after this entry.</summary>
    public required string ToSha { get; init; }

    /// <summary>Reflog message (e.g. "commit: initial", "checkout: moving from main to feature/x").</summary>
    public required string Message { get; init; }

    /// <summary>When the entry was recorded.</summary>
    public required DateTimeOffset When { get; init; }

    /// <summary>Committer name as recorded by the reflog entry.</summary>
    public required string CommitterName { get; init; }

    /// <summary>Committer email as recorded by the reflog entry.</summary>
    public required string CommitterEmail { get; init; }
}
