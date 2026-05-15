namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

public sealed class ReflogServiceTests
{
    [Fact]
    public async Task ReadReflogAsync_after_first_commit_has_at_least_one_entry()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var reflog = new LibGit2ReflogService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        var entries = await reflog.ReadReflogAsync(handle, "HEAD");

        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => e.Message.Contains("seed", StringComparison.OrdinalIgnoreCase));
        Assert.All(entries, e => Assert.NotEmpty(e.CommitterName));
    }

    [Fact]
    public async Task ReadReflogAsync_returns_entries_newest_first()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var reflog = new LibGit2ReflogService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "first", TestAuthor());
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "second", TestAuthor());

        var entries = await reflog.ReadReflogAsync(handle, "HEAD");

        Assert.True(entries.Count >= 2);
        // Newest first: index 0 should be the "second" commit, index 1 the "first".
        Assert.Contains("second", entries[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadReflogAsync_unknown_reference_throws_ArgumentException()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var reflog = new LibGit2ReflogService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        await Assert.ThrowsAsync<ArgumentException>(
            () => reflog.ReadReflogAsync(handle, "does-not-exist"));
    }

    [Fact]
    public async Task ReadReflogAsync_rejects_non_libgit2_handle()
    {
        var reflog = new LibGit2ReflogService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => reflog.ReadReflogAsync(stub, "HEAD"));
    }

    private sealed class StubHandle : IRepoHandle
    {
        public string WorkingDirectory => "/tmp/stub";
        public void Dispose() { }
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
