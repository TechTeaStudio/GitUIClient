namespace TechTeaStudio.GitClient.Models;

/// <summary>A contiguous hunk within a per-file diff. Coordinates mirror the
/// <c>@@ -OldStartLine,OldLineCount +NewStartLine,NewLineCount @@</c> hunk header.</summary>
public sealed record DiffHunk
{
    public required int OldStartLine { get; init; }
    public required int OldLineCount { get; init; }
    public required int NewStartLine { get; init; }
    public required int NewLineCount { get; init; }
    public required IReadOnlyList<DiffLine> Lines { get; init; }
}
