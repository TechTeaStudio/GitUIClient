namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.WorkingTree;

public sealed class AmendServiceTests
{
    [Fact]
    public async Task AmendAsync_rewrites_head_with_new_message()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var amend = new LibGit2AmendService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var original = await commits.CommitAsync(handle, "original message", TestAuthor());

        var amended = await amend.AmendAsync(handle, "rewritten message", TestAuthor());

        Assert.Contains("rewritten message", amended.Message);
        Assert.NotEqual(original.Sha, amended.Sha);

        // The previous commit's sha must no longer be reachable from HEAD.
        var history = await repos.GetCommitsAsync(handle, "HEAD", 10);
        Assert.DoesNotContain(history, c => c.Sha == original.Sha);
        Assert.Contains(history, c => c.Sha == amended.Sha);
    }

    [Fact]
    public async Task AmendAsync_can_keep_original_message_when_new_message_is_null()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var amend = new LibGit2AmendService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var original = await commits.CommitAsync(handle, "keep me", TestAuthor());

        // change a file then amend without supplying a new message
        WriteFile(temp.Path, "a.txt", "modified");
        var amended = await amend.AmendAsync(handle, newMessage: null, TestAuthor(), new[] { "a.txt" });

        Assert.Contains("keep me", amended.Message);
        Assert.NotEqual(original.Sha, amended.Sha);
    }

    [Fact]
    public async Task AmendAsync_stages_path_spec_before_committing()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var amend = new LibGit2AmendService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "b.txt", "b");
        var amended = await amend.AmendAsync(handle, "amended with b.txt", TestAuthor(), new[] { "b.txt" });

        Assert.Contains("amended with b.txt", amended.Message);

        // No further changes pending.
        var status = await repos.GetStatusAsync(handle);
        Assert.True(status.IsClean);
    }

    [Fact]
    public async Task AmendAsync_throws_when_no_head()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var amend = new LibGit2AmendService();

        using var handle = await repos.InitAsync(temp.Path);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => amend.AmendAsync(handle, "no head yet", TestAuthor()));
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
