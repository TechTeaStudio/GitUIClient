namespace TechTeaStudio.GitClient.Models;

/// <summary>Structured per-file diff between two trees. Use <see cref="Hunks"/>
/// to render unified diff UI; bulk counters (<see cref="AddedLines"/> /
/// <see cref="DeletedLines"/>) are filled directly from LibGit2Sharp.</summary>
public sealed record FileDiff
{
    public required string Path { get; init; }
    public string? OldPath { get; init; }
    public required FileDiffKind Kind { get; init; }
    public required int AddedLines { get; init; }
    public required int DeletedLines { get; init; }
    public required IReadOnlyList<DiffHunk> Hunks { get; init; }
}

public enum FileDiffKind
{
    Added,
    Deleted,
    Modified,
    Renamed,
}
