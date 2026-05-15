namespace TechTeaStudio.GitClient.Models;

/// <summary>One reference advertised by a remote (output of <c>git ls-remote</c>).</summary>
public sealed record RemoteRefInfo
{
    public required string Name { get; init; }
    public required string Sha { get; init; }
    public required bool IsBranch { get; init; }
    public required bool IsTag { get; init; }
}
