namespace TechTeaStudio.GitClient.Inspection;

using System.Text;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

using ModelsTreeEntry = TechTeaStudio.GitClient.Models.TreeEntry;
using LibGitTreeEntry = LibGit2Sharp.TreeEntry;
using LibGitTree = LibGit2Sharp.Tree;

/// <summary>LibGit2Sharp-backed tree inspection.
///
/// Concurrency: every method that touches the underlying <see cref="Repository"/>
/// takes the handle's async lock first — LibGit2Sharp is not thread-safe.</summary>
public sealed class LibGit2TreeService : ITreeService
{
    private const int BinaryProbeBytes = 8 * 1024;

    public async Task<IReadOnlyList<ModelsTreeEntry>> ListTreeAsync(
        IRepoHandle handle,
        string commitSha,
        string? path = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var commit = repo.Lookup<Commit>(commitSha)
                ?? throw new ArgumentException($"Could not resolve '{commitSha}' to a commit.", nameof(commitSha));

            var tree = ResolveTree(commit.Tree, path);
            if (tree is null)
                return Array.Empty<ModelsTreeEntry>();

            var list = new List<ModelsTreeEntry>();
            foreach (LibGitTreeEntry entry in tree)
            {
                ct.ThrowIfCancellationRequested();
                var fullPath = string.IsNullOrEmpty(path) ? entry.Name : $"{path.TrimEnd('/')}/{entry.Name}";
                if (entry.TargetType == TreeEntryTargetType.Tree)
                {
                    list.Add(new ModelsTreeEntry
                    {
                        Path = fullPath,
                        Name = entry.Name,
                        Kind = TreeEntryKind.Tree,
                        Size = null,
                    });
                }
                else if (entry.TargetType == TreeEntryTargetType.Blob)
                {
                    var blob = (Blob)entry.Target;
                    list.Add(new ModelsTreeEntry
                    {
                        Path = fullPath,
                        Name = entry.Name,
                        Kind = TreeEntryKind.Blob,
                        Size = blob.Size,
                    });
                }
                // GitLink / submodule → intentionally skipped in v0.2.
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<string?> ReadBlobAsync(
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var blob = LookupBlob(h.Repository, commitSha, path);
            if (blob is null) return null;
            if (IsBinaryBlob(blob)) return null;

            using var stream = blob.GetContentStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<bool> IsBinaryBlobAsync(
        IRepoHandle handle,
        string commitSha,
        string path,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var blob = LookupBlob(h.Repository, commitSha, path);
            if (blob is null) return false;
            return IsBinaryBlob(blob);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static Blob? LookupBlob(Repository repo, string commitSha, string path)
    {
        var commit = repo.Lookup<Commit>(commitSha);
        if (commit is null) return null;

        var entry = commit.Tree[path.Replace('\\', '/')];
        if (entry is null || entry.TargetType != TreeEntryTargetType.Blob)
            return null;
        return (Blob)entry.Target;
    }

    private static LibGitTree? ResolveTree(LibGitTree root, string? path)
    {
        if (string.IsNullOrEmpty(path)) return root;

        var entry = root[path.Replace('\\', '/').TrimEnd('/')];
        if (entry is null) return null;
        if (entry.TargetType != TreeEntryTargetType.Tree) return null;
        return (LibGitTree)entry.Target;
    }

    private static bool IsBinaryBlob(Blob blob)
    {
        using var stream = blob.GetContentStream();
        Span<byte> buffer = stackalloc byte[8192];
        int read = stream.Read(buffer);
        int probe = Math.Min(read, BinaryProbeBytes);
        for (int i = 0; i < probe; i++)
        {
            if (buffer[i] == 0) return true;
        }
        return false;
    }
}
