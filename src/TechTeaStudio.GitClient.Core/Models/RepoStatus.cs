namespace TechTeaStudio.GitClient.Models;

/// <summary>Working-tree status snapshot grouped by change state.</summary>
public sealed record RepoStatus
{
    public required IReadOnlyList<FileChange> Added { get; init; }
    public required IReadOnlyList<FileChange> Modified { get; init; }
    public required IReadOnlyList<FileChange> Deleted { get; init; }
    public required IReadOnlyList<FileChange> Untracked { get; init; }

    /// <summary><c>true</c> when every list is empty.</summary>
    public bool IsClean
        => Added.Count == 0 && Modified.Count == 0 && Deleted.Count == 0 && Untracked.Count == 0;

    public static RepoStatus Empty { get; } = new()
    {
        Added = Array.Empty<FileChange>(),
        Modified = Array.Empty<FileChange>(),
        Deleted = Array.Empty<FileChange>(),
        Untracked = Array.Empty<FileChange>(),
    };
}
