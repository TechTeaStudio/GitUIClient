namespace TechTeaStudio.GitClient.Repositories;

using LibGit2Sharp;

/// <summary>
/// Wraps a LibGit2Sharp <see cref="Repository"/>. LibGit2Sharp is NOT thread-safe:
/// the embedded <see cref="SemaphoreSlim"/> serialises every service call against
/// this handle so the underlying <see cref="Repository"/> is only ever touched
/// from one thread at a time.
/// </summary>
public sealed class LibGit2RepoHandle : IRepoHandle
{
    private readonly Repository _repository;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _disposed;

    internal LibGit2RepoHandle(Repository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public string WorkingDirectory => _repository.Info.WorkingDirectory ?? _repository.Info.Path;

    /// <summary>Internal accessor — only the services in this assembly should read it.</summary>
    internal Repository Repository => _repository;

    /// <summary>Per-handle async lock. Services in this assembly take this before touching <see cref="Repository"/>.</summary>
    internal SemaphoreSlim AsyncLock => _lock;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try { _lock.Wait(); }
        catch (ObjectDisposedException) { /* already gone */ }

        try
        {
            _repository.Dispose();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
