namespace TechTeaStudio.GitClient.App.Views;

/// <summary>Result returned from the clone dialog on success.</summary>
public sealed record CloneRequest(string Url, string DestinationPath);
