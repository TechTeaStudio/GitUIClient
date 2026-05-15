namespace TechTeaStudio.GitClient.Inspection;

using System.Globalization;
using System.Text.RegularExpressions;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>LibGit2Sharp-backed structured diff. Compares two trees, then parses
/// each per-file unified-diff hunk into <see cref="DiffLine"/>s with old/new line
/// numbers for the renderer.
///
/// Concurrency: takes the handle's async lock around <c>Diff.Compare</c>.</summary>
public sealed partial class LibGit2FileDiffService : IFileDiffService
{
    public async Task<IReadOnlyList<FileDiff>> CompareCommitsAsync(
        IRepoHandle handle,
        string fromSha,
        string toSha,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(toSha);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var from = repo.Lookup<Commit>(fromSha)
                ?? throw new ArgumentException($"Could not resolve '{fromSha}' to a commit.", nameof(fromSha));
            var to = repo.Lookup<Commit>(toSha)
                ?? throw new ArgumentException($"Could not resolve '{toSha}' to a commit.", nameof(toSha));

            return Compare(repo, from.Tree, to.Tree, ct);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<IReadOnlyList<FileDiff>> CompareWithParentAsync(
        IRepoHandle handle,
        string commitSha,
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

            var parent = commit.Parents.FirstOrDefault();
            return Compare(repo, parent?.Tree, commit.Tree, ct);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static IReadOnlyList<FileDiff> Compare(
        Repository repo,
        LibGit2Sharp.Tree? from,
        LibGit2Sharp.Tree to,
        CancellationToken ct)
    {
        using var patch = repo.Diff.Compare<Patch>(from, to);
        var result = new List<FileDiff>();
        foreach (var entry in patch)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(MapEntry(entry));
        }
        return result;
    }

    private static FileDiff MapEntry(PatchEntryChanges entry)
    {
        var kind = MapKind(entry.Status);
        var hunks = ParseHunks(entry.Patch);
        return new FileDiff
        {
            Path = entry.Path,
            OldPath = string.Equals(entry.OldPath, entry.Path, StringComparison.Ordinal) ? null : entry.OldPath,
            Kind = kind,
            AddedLines = entry.LinesAdded,
            DeletedLines = entry.LinesDeleted,
            Hunks = hunks,
        };
    }

    private static FileDiffKind MapKind(ChangeKind status) => status switch
    {
        ChangeKind.Added => FileDiffKind.Added,
        ChangeKind.Deleted => FileDiffKind.Deleted,
        ChangeKind.Renamed => FileDiffKind.Renamed,
        // Copied / TypeChanged / Modified all fall through to Modified for v0.2 UI.
        _ => FileDiffKind.Modified,
    };

    [GeneratedRegex(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", RegexOptions.CultureInvariant)]
    private static partial Regex HunkHeaderRegex();

    private static IReadOnlyList<DiffHunk> ParseHunks(string patch)
    {
        if (string.IsNullOrEmpty(patch)) return Array.Empty<DiffHunk>();

        var hunks = new List<DiffHunk>();
        var lines = patch.Split('\n');

        DiffHunkBuilder? current = null;
        int? oldLine = null;
        int? newLine = null;

        foreach (var raw in lines)
        {
            // Strip a single trailing CR (libgit2 returns LF-only, but be defensive).
            var line = raw.EndsWith('\r') ? raw[..^1] : raw;

            // Skip file-level headers — only hunk headers and hunk bodies are interesting.
            if (current is null)
            {
                var m = HunkHeaderRegex().Match(line);
                if (!m.Success) continue;
                current = StartHunk(m);
                oldLine = current.OldStartLine;
                newLine = current.NewStartLine;
                continue;
            }

            if (line.Length == 0)
            {
                // Trailing blank line at end of patch — leave it; hunk completes when next header arrives.
                continue;
            }

            // Next hunk?
            var header = HunkHeaderRegex().Match(line);
            if (header.Success)
            {
                hunks.Add(current.Build());
                current = StartHunk(header);
                oldLine = current.OldStartLine;
                newLine = current.NewStartLine;
                continue;
            }

            var marker = line[0];
            var content = line.Length > 1 ? line[1..] : string.Empty;

            switch (marker)
            {
                case '+':
                    current.Lines.Add(new DiffLine
                    {
                        Kind = DiffLineKind.Added,
                        OldLineNumber = null,
                        NewLineNumber = newLine,
                        Content = content,
                    });
                    newLine = (newLine ?? 0) + 1;
                    break;
                case '-':
                    current.Lines.Add(new DiffLine
                    {
                        Kind = DiffLineKind.Removed,
                        OldLineNumber = oldLine,
                        NewLineNumber = null,
                        Content = content,
                    });
                    oldLine = (oldLine ?? 0) + 1;
                    break;
                case ' ':
                    current.Lines.Add(new DiffLine
                    {
                        Kind = DiffLineKind.Context,
                        OldLineNumber = oldLine,
                        NewLineNumber = newLine,
                        Content = content,
                    });
                    oldLine = (oldLine ?? 0) + 1;
                    newLine = (newLine ?? 0) + 1;
                    break;
                case '\\':
                    // "\ No newline at end of file" — preserve as metadata; numbers unchanged.
                    current.Lines.Add(new DiffLine
                    {
                        Kind = DiffLineKind.NoNewline,
                        OldLineNumber = null,
                        NewLineNumber = null,
                        Content = line.Length > 2 ? line[2..] : line,
                    });
                    break;
                default:
                    // diff --git, index, ---, +++ headers between hunks → ignore.
                    break;
            }
        }

        if (current is not null)
            hunks.Add(current.Build());

        return hunks;
    }

    private static DiffHunkBuilder StartHunk(Match m)
    {
        int oldStart = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int oldCount = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 1;
        int newStart = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        int newCount = m.Groups[4].Success ? int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture) : 1;
        return new DiffHunkBuilder(oldStart, oldCount, newStart, newCount);
    }

    private sealed class DiffHunkBuilder(int oldStart, int oldCount, int newStart, int newCount)
    {
        public int OldStartLine { get; } = oldStart;
        public int OldLineCount { get; } = oldCount;
        public int NewStartLine { get; } = newStart;
        public int NewLineCount { get; } = newCount;
        public List<DiffLine> Lines { get; } = new();

        public DiffHunk Build() => new()
        {
            OldStartLine = OldStartLine,
            OldLineCount = OldLineCount,
            NewStartLine = NewStartLine,
            NewLineCount = NewLineCount,
            Lines = Lines,
        };
    }
}
