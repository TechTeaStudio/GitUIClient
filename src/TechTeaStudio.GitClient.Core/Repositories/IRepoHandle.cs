namespace TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Opaque handle to an open repository. Implementations wrap the underlying
/// LibGit2Sharp.Repository which is **not thread-safe**: a handle must only be
/// used from the thread that created it (the service serialises access).
/// </summary>
public interface IRepoHandle : IDisposable
{
    /// <summary>Absolute working-directory path of the repository.</summary>
    string WorkingDirectory { get; }
}
