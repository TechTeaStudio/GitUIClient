namespace TechTeaStudio.GitClient.WorkingTree;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IInitService"/>.
///
/// Wraps <see cref="Repository.Init(string, bool)"/> and returns a handle in the
/// same family as <see cref="LibGit2RepositoryService"/> so callers can use any
/// service that takes <see cref="IRepoHandle"/>.
/// </summary>
public sealed class LibGit2InitService : IInitService
{
    public Task<IRepoHandle> InitRepositoryAsync(string path, bool bare, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();

        Directory.CreateDirectory(path);
        var gitDir = Repository.Init(path, isBare: bare);
        var repo = new Repository(gitDir);
        return Task.FromResult<IRepoHandle>(new LibGit2RepoHandle(repo));
    }
}
