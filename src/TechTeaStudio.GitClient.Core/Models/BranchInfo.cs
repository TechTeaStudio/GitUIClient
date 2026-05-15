namespace TechTeaStudio.GitClient.Models;

/// <summary>Branch reference + the sha its tip currently points at.</summary>
public sealed record BranchInfo
{
    public required string Name { get; init; }
    public required string TipSha { get; init; }
    public required bool IsCurrent { get; init; }
    public required bool IsRemote { get; init; }
}
