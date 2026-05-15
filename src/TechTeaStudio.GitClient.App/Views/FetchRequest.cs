namespace TechTeaStudio.GitClient.App.Views;

using TechTeaStudio.GitClient.Models;

/// <summary>Result returned from the fetch dialog on confirm.</summary>
public sealed record FetchRequest(string RemoteName, Credentials? Credentials);
