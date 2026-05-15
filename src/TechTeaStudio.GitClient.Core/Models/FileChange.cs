namespace TechTeaStudio.GitClient.Models;

/// <summary>One file's change state, repo-relative path included.</summary>
public sealed record FileChange
{
    public required string Path { get; init; }
    public required FileChangeKind Kind { get; init; }
}

public enum FileChangeKind
{
    Added,
    Modified,
    Deleted,
    Untracked,
    Renamed,
    TypeChange,
    Conflicted,
    Ignored,
    Unmodified,
}
