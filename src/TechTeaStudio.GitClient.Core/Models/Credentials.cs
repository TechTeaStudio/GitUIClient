namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Username + password credentials passed to fetch/pull/push.
/// When <c>null</c> at the call site, libgit2 attempts anonymous / agent / cached creds.
/// </summary>
public sealed record Credentials(string Username, string Password);
