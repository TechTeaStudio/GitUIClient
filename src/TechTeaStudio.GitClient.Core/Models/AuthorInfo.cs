namespace TechTeaStudio.GitClient.Models;

/// <summary>Author / committer identity used when creating a commit.</summary>
public sealed record AuthorInfo
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public DateTimeOffset? When { get; init; }
}
