namespace TechTeaStudio.GitClient.Inspection;

using System.Text;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>LibGit2Sharp-backed per-line blame. Returns one <see cref="BlameLine"/>
/// per source line, joining each blame hunk with the file's text content at the
/// given commit so callers don't need a separate <c>ReadBlobAsync</c> trip.
///
/// Concurrency: serialises every call against the handle's async lock.</summary>
public sealed class LibGit2BlameService : IBlameService
{
    public async Task<IReadOnlyList<BlameLine>> BlameFileAsync(
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
            var repo = h.Repository;
            var commit = repo.Lookup<Commit>(commitSha)
                ?? throw new ArgumentException($"Could not resolve '{commitSha}' to a commit.", nameof(commitSha));

            var normalised = path.Replace('\\', '/');
            var entry = commit.Tree[normalised]
                ?? throw new ArgumentException($"Path '{path}' does not exist in commit {commit.Sha}.", nameof(path));
            if (entry.TargetType != TreeEntryTargetType.Blob)
                throw new ArgumentException($"Path '{path}' is not a blob in commit {commit.Sha}.", nameof(path));

            var blob = (Blob)entry.Target;
            var lines = SplitLines(blob);

            var blameOptions = new BlameOptions
            {
                StartingAt = commit,
            };
            var blame = repo.Blame(normalised, blameOptions);

            var result = new List<BlameLine>(lines.Count);
            foreach (var hunk in blame)
            {
                ct.ThrowIfCancellationRequested();
                var finalCommit = hunk.FinalCommit;
                var start = hunk.FinalStartLineNumber; // 0-based
                for (int i = 0; i < hunk.LineCount; i++)
                {
                    int lineIndex = start + i;
                    if (lineIndex < 0 || lineIndex >= lines.Count)
                        continue;
                    result.Add(new BlameLine
                    {
                        LineNumber = lineIndex + 1,
                        Content = lines[lineIndex],
                        CommitSha = finalCommit.Sha,
                        Author = finalCommit.Author.Name,
                        Email = finalCommit.Author.Email,
                        When = finalCommit.Author.When,
                    });
                }
            }

            return result;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static IReadOnlyList<string> SplitLines(Blob blob)
    {
        using var stream = blob.GetContentStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        if (text.Length == 0) return Array.Empty<string>();

        // Preserve a trailing-newline empty entry only if Git would consider it a
        // separate line. LibGit2Sharp's blame line count excludes the empty tail,
        // so we do too.
        var lines = text.Split('\n');
        var list = new List<string>(lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.EndsWith('\r')) line = line[..^1];
            if (i == lines.Length - 1 && line.Length == 0) break;
            list.Add(line);
        }
        return list;
    }
}
