namespace TechTeaStudio.GitClient.Models;

/// <summary>One entry inside a tree (a folder listing). The <see cref="Size"/> is
/// the blob byte count for files, <c>null</c> for sub-trees.</summary>
public sealed record TreeEntry
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public required TreeEntryKind Kind { get; init; }
    public long? Size { get; init; }
}

public enum TreeEntryKind
{
    Blob,
    Tree,
}
