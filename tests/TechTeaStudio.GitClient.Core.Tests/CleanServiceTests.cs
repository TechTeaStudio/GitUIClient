namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.WorkingTree;

public sealed class CleanServiceTests
{
    [Fact]
    public async Task ListUntrackedAsync_returns_expected_files()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var clean = new LibGit2CleanService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "tracked.txt", "tracked");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "u1.txt", "u1");
        WriteFile(temp.Path, "u2.txt", "u2");

        var untracked = await clean.ListUntrackedAsync(handle);

        Assert.Contains("u1.txt", untracked);
        Assert.Contains("u2.txt", untracked);
        Assert.DoesNotContain("tracked.txt", untracked);
    }

    [Fact]
    public async Task CleanUntrackedAsync_deletes_specified_files()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var clean = new LibGit2CleanService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "tracked.txt", "tracked");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "u1.txt", "u1");
        WriteFile(temp.Path, "u2.txt", "u2");

        var removed = await clean.CleanUntrackedAsync(handle, new[] { "u1.txt", "u2.txt" });

        Assert.Equal(2, removed);
        Assert.False(File.Exists(Path.Combine(temp.Path, "u1.txt")));
        Assert.False(File.Exists(Path.Combine(temp.Path, "u2.txt")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "tracked.txt")));
    }

    [Fact]
    public async Task CleanUntrackedAsync_skips_missing_files()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var clean = new LibGit2CleanService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "tracked.txt", "tracked");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        var removed = await clean.CleanUntrackedAsync(handle, new[] { "does-not-exist.txt" });
        Assert.Equal(0, removed);
    }

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static AuthorInfo TestAuthor() => new()
    {
        Name = "Test",
        Email = "test@example.com",
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
    };
}
