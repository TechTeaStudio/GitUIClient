namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// A single Git tag.
///
/// Lightweight tags point straight at an object and have no extra metadata
/// (<see cref="IsAnnotated"/> = <c>false</c>, <see cref="Message"/> and
/// <see cref="Tagger"/> are <c>null</c>).
///
/// Annotated tags are stored as their own object (with a tagger signature and
/// message) which points at the target commit / object.
/// </summary>
public sealed record TagInfo
{
    /// <summary>The friendly tag name (without <c>refs/tags/</c> prefix).</summary>
    public required string Name { get; init; }

    /// <summary>SHA of the commit / object the tag points at (peeled).</summary>
    public required string TargetSha { get; init; }

    /// <summary><c>true</c> if this is an annotated tag, <c>false</c> for lightweight.</summary>
    public required bool IsAnnotated { get; init; }

    /// <summary>Tag message (annotated only — <c>null</c> for lightweight tags).</summary>
    public string? Message { get; init; }

    /// <summary>Tagger identity (annotated only — <c>null</c> for lightweight tags).</summary>
    public AuthorInfo? Tagger { get; init; }
}
