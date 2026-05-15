namespace TechTeaStudio.GitClient.Core.Tests;

using System.Text;

using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class FileDiffServiceTests
{
    [Fact]
    public async Task CompareCommitsAsync_modified_file_yields_one_modified_diff()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var diffSvc = new LibGit2FileDiffService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "line1\nline2\nline3\n");
        var c1 = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        WriteFile(temp.Path, "a.txt", "line1\nline2-edited\nline3\nline4\n");
        var c2 = await commitSvc.CommitAsync(handle, "edit", TestAuthor());

        var diffs = await diffSvc.CompareCommitsAsync(handle, c1.Sha, c2.Sha);

        var d = Assert.Single(diffs);
        Assert.Equal("a.txt", d.Path);
        Assert.Null(d.OldPath);
        Assert.Equal(FileDiffKind.Modified, d.Kind);
        Assert.Equal(2, d.AddedLines);   // line2-edited + line4
        Assert.Equal(1, d.DeletedLines); // line2

        // Hunks present and contain classified lines.
        Assert.NotEmpty(d.Hunks);
        var lines = d.Hunks.SelectMany(h => h.Lines).ToList();
        Assert.Contains(lines, l => l.Kind == DiffLineKind.Added && l.Content == "line2-edited");
        Assert.Contains(lines, l => l.Kind == DiffLineKind.Removed && l.Content == "line2");
        Assert.Contains(lines, l => l.Kind == DiffLineKind.Added && l.Content == "line4");

        // Old/new line numbers track the marker direction.
        var addedTwo = lines.First(l => l.Kind == DiffLineKind.Added && l.Content == "line2-edited");
        Assert.Null(addedTwo.OldLineNumber);
        Assert.Equal(2, addedTwo.NewLineNumber);
        var removedTwo = lines.First(l => l.Kind == DiffLineKind.Removed && l.Content == "line2");
        Assert.Null(removedTwo.NewLineNumber);
        Assert.Equal(2, removedTwo.OldLineNumber);
    }

    [Fact]
    public async Task CompareCommitsAsync_added_file_kind_is_added()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var diffSvc = new LibGit2FileDiffService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "keep.txt", "keep");
        var c1 = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        WriteFile(temp.Path, "new.txt", "fresh\n");
        var c2 = await commitSvc.CommitAsync(handle, "add", TestAuthor());

        var diffs = await diffSvc.CompareCommitsAsync(handle, c1.Sha, c2.Sha);

        var d = Assert.Single(diffs);
        Assert.Equal("new.txt", d.Path);
        Assert.Equal(FileDiffKind.Added, d.Kind);
        Assert.True(d.AddedLines >= 1);
    }

    [Fact]
    public async Task CompareWithParentAsync_root_commit_returns_added_entries()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var diffSvc = new LibGit2FileDiffService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "alpha\n");
        WriteFile(temp.Path, "b.txt", "beta\n");
        var c1 = await commitSvc.CommitAsync(handle, "first", TestAuthor());

        var diffs = await diffSvc.CompareWithParentAsync(handle, c1.Sha);

        Assert.Equal(2, diffs.Count);
        Assert.All(diffs, d => Assert.Equal(FileDiffKind.Added, d.Kind));
        Assert.Contains(diffs, d => d.Path == "a.txt");
        Assert.Contains(diffs, d => d.Path == "b.txt");
    }

    [Fact]
    public async Task CompareWithParentAsync_non_root_compares_against_first_parent()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var diffSvc = new LibGit2FileDiffService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "v1\n");
        await commitSvc.CommitAsync(handle, "v1", TestAuthor());

        WriteFile(temp.Path, "a.txt", "v2\n");
        var c2 = await commitSvc.CommitAsync(handle, "v2", TestAuthor());

        var diffs = await diffSvc.CompareWithParentAsync(handle, c2.Sha);

        var d = Assert.Single(diffs);
        Assert.Equal("a.txt", d.Path);
        Assert.Equal(FileDiffKind.Modified, d.Kind);
    }

    [Fact]
    public async Task CompareCommitsAsync_rejects_unknown_sha()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var diffSvc = new LibGit2FileDiffService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var c1 = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        await Assert.ThrowsAsync<ArgumentException>(
            () => diffSvc.CompareCommitsAsync(handle, c1.Sha, "0000000000000000000000000000000000000000"));
    }

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, Encoding.UTF8.GetBytes(content));
    }

    private static AuthorInfo TestAuthor() => new()
    {
        Name = "Test",
        Email = "test@example.com",
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
    };
}
