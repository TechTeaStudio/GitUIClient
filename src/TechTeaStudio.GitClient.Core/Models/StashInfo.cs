namespace TechTeaStudio.GitClient.Models;

/// <summary>One entry in the stash stack. <see cref="Index"/> is 0-based with 0 = newest.</summary>
public sealed record StashInfo
{
    public required int Index { get; init; }
    public required string Message { get; init; }
    public required string Sha { get; init; }
    public required DateTimeOffset When { get; init; }
}
