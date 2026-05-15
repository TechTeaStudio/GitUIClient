namespace TechTeaStudio.GitClient.Models;

/// <summary>A single line inside a <see cref="DiffHunk"/>. Old/new line numbers
/// are null when the line did not exist in that side (e.g. added lines have no
/// old number, removed lines have no new number).</summary>
public sealed record DiffLine
{
    public required DiffLineKind Kind { get; init; }
    public required int? OldLineNumber { get; init; }
    public required int? NewLineNumber { get; init; }
    public required string Content { get; init; }
}

public enum DiffLineKind
{
    Context,
    Added,
    Removed,
    NoNewline,
}
