namespace TechTeaStudio.GitClient.Repo;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="IConflictsService"/>.
///
/// Resolution strategy: look up the chosen side's blob, write its content
/// into the working tree at the conflicted path, then <c>Commands.Stage</c>
/// the path. Staging clears the conflict's stage 1/2/3 entries and replaces
/// them with a normal stage-0 entry.
///
/// Concurrency: takes the handle's async lock around every native call.
/// </summary>
public sealed class LibGit2ConflictsService : IConflictsService
{
    public async Task<IReadOnlyList<ConflictInfo>> ListConflictsAsync(
        IRepoHandle handle,
        CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var list = new List<ConflictInfo>();
            foreach (var c in repo.Index.Conflicts)
            {
                ct.ThrowIfCancellationRequested();
                // Path can live in any of the three sides; ours/theirs win if present.
                var path = c.Ours?.Path ?? c.Theirs?.Path ?? c.Ancestor?.Path ?? string.Empty;
                list.Add(new ConflictInfo
                {
                    Path = path,
                    OursSha = c.Ours?.Id.Sha,
                    TheirsSha = c.Theirs?.Id.Sha,
                    AncestorSha = c.Ancestor?.Id.Sha,
                });
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public Task ResolveByOursAsync(IRepoHandle handle, string path, CancellationToken ct = default)
        => ResolveAsync(handle, path, ConflictSide.Ours, ct);

    public Task ResolveByTheirsAsync(IRepoHandle handle, string path, CancellationToken ct = default)
        => ResolveAsync(handle, path, ConflictSide.Theirs, ct);

    private async Task ResolveAsync(IRepoHandle handle, string path, ConflictSide side, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var conflict = repo.Index.Conflicts[path]
                ?? throw new ArgumentException($"No conflict registered for '{path}'.", nameof(path));

            var chosen = side == ConflictSide.Ours ? conflict.Ours : conflict.Theirs;
            var workdir = repo.Info.WorkingDirectory
                ?? throw new InvalidOperationException("Bare repositories cannot resolve conflicts.");
            var fullPath = Path.Combine(workdir, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            if (chosen is null)
            {
                // The chosen side is "deleted" -> drop the file on disk and remove from index.
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
                Commands.Remove(repo, path, removeFromWorkingDirectory: false);
            }
            else
            {
                var blob = repo.Lookup<Blob>(chosen.Id)
                    ?? throw new InvalidOperationException($"Blob {chosen.Id.Sha} missing from object DB.");
                using (var src = blob.GetContentStream())
                using (var dst = File.Create(fullPath))
                    await src.CopyToAsync(dst, ct).ConfigureAwait(false);
                Commands.Stage(repo, path);
            }
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private enum ConflictSide { Ours, Theirs }
}
