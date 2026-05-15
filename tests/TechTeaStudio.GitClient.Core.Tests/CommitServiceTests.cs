namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class CommitServiceTests
{
    [Fact]
    public async Task First_commit_has_no_parents()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var commit = await commitSvc.CommitAsync(handle, "first commit", TestAuthor());

        Assert.Equal("first commit", commit.Message.Trim());
        Assert.Empty(commit.Parents);
        Assert.Equal("Test", commit.Author);
        Assert.Equal("test@example.com", commit.Email);
    }

    [Fact]
    public async Task Second_commit_parents_first()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var first = await commitSvc.CommitAsync(handle, "first", TestAuthor());

        WriteFile(temp.Path, "b.txt", "b");
        var second = await commitSvc.CommitAsync(handle, "second", TestAuthor());

        Assert.Single(second.Parents);
        Assert.Equal(first.Sha, second.Parents[0]);
    }

    [Fact]
    public async Task CommitAsync_with_path_spec_only_commits_matching_paths()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);

        // initial commit so we can diff
        WriteFile(temp.Path, "seed.txt", "seed");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        // two new files, only commit one of them
        WriteFile(temp.Path, "in.txt", "in");
        WriteFile(temp.Path, "out.txt", "out");

        var commit = await commitSvc.CommitAsync(handle, "partial", TestAuthor(), new[] { "in.txt" });

        Assert.Equal("partial", commit.Message.Trim());

        // out.txt should still be untracked
        var status = await svc.GetStatusAsync(handle);
        Assert.Contains(status.Untracked, f => f.Path == "out.txt");
        Assert.DoesNotContain(status.Untracked, f => f.Path == "in.txt");
    }

    [Fact]
    public async Task CommitAsync_throws_when_nothing_staged()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "seed.txt", "seed");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        // No new changes, and a pathSpec that doesn't match anything.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => commitSvc.CommitAsync(handle, "empty", TestAuthor(), new[] { "does-not-exist.txt" }));
    }

    [Fact]
    public async Task CommitAsync_rejects_non_libgit2_handle()
    {
        var commitSvc = new LibGit2CommitService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => commitSvc.CommitAsync(stub, "msg", TestAuthor()));
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
