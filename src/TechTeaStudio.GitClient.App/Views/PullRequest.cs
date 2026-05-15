namespace TechTeaStudio.GitClient.App.Views;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Result returned from the pull dialog on confirm. The orchestrator drives
/// the actual <c>ISyncService.PullAsync</c> call.
/// </summary>
public sealed record PullRequest(string RemoteName, string? BranchName, Credentials? Credentials);
