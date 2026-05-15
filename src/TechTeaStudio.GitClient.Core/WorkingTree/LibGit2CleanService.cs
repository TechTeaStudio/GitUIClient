namespace TechTeaStudio.GitClient.WorkingTree;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="ICleanService"/>.
///
/// Concurrency: takes the handle's async lock — see <see cref="LibGit2RepoHandle"/>.
/// </summary>
public sealed class LibGit2CleanService : ICleanService
{
    public async Task<IReadOnlyList<string>> ListUntrackedAsync(IRepoHandle handle, CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var status = h.Repository.RetrieveStatus(new StatusOptions
            {
                IncludeUntracked = true,
                IncludeIgnored = false,
                RecurseUntrackedDirs = true,
            });

            var list = new List<string>();
            foreach (var entry in status)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.State.HasFlag(FileStatus.NewInWorkdir))
                    list.Add(entry.FilePath);
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<int> CleanUntrackedAsync(IRepoHandle handle, IEnumerable<string> paths, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var workingDir = h.Repository.Info.WorkingDirectory
                ?? throw new InvalidOperationException("Repository has no working directory (bare repo).");

            var removed = 0;
            foreach (var rel in paths)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(rel))
                    continue;

                var full = Path.GetFullPath(Path.Combine(workingDir, rel));

                // Defence against `..` escaping the working tree.
                var root = Path.GetFullPath(workingDir);
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (File.Exists(full))
                    {
                        // Clear any read-only attribute so File.Delete doesn't fail.
                        var attrs = File.GetAttributes(full);
                        if (attrs.HasFlag(FileAttributes.ReadOnly))
                            File.SetAttributes(full, attrs & ~FileAttributes.ReadOnly);

                        File.Delete(full);
                        removed++;
                    }
                    else if (Directory.Exists(full))
                    {
                        Directory.Delete(full, recursive: true);
                        removed++;
                    }
                }
                catch (IOException)
                {
                    // Best-effort; locked files are skipped.
                }
                catch (UnauthorizedAccessException)
                {
                    // Best-effort; permission-denied paths are skipped.
                }
            }
            return removed;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }
}
