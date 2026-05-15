namespace TechTeaStudio.GitClient.Models;

/// <summary>A configured git remote — fetch and (optional) push URL.</summary>
public sealed record RemoteInfo
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public string? PushUrl { get; init; }
}
