namespace TechTeaStudio.GitClient.Models;

/// <summary>One line of a file with the commit that last authored it. Output of
/// <c>IBlameService.BlameFileAsync</c>.</summary>
public sealed record BlameLine
{
    public required int LineNumber { get; init; }
    public required string Content { get; init; }
    public required string CommitSha { get; init; }
    public required string Author { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset When { get; init; }
}
