namespace TechTeaStudio.GitClient.App.Views;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Result returned from the push dialog on confirm.
/// </summary>
public sealed record PushRequest(
    string RemoteName,
    IReadOnlyList<string> BranchNames,
    bool Force,
    Credentials? Credentials);
