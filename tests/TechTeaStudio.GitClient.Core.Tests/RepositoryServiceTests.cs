namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class RepositoryServiceTests
{
    [Fact]
    public async Task OpenAsync_throws_on_non_repository()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();

        await Assert.ThrowsAsync<RepositoryNotFoundException>(() => svc.OpenAsync(temp.Path));
    }

    [Fact]
    public async Task InitAsync_creates_git_directory()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();

        using var handle = await svc.InitAsync(temp.Path);
        Assert.True(Directory.Exists(Path.Combine(temp.Path, ".git")));
    }

    [Fact]
    public async Task GetBranchesAsync_after_first_commit_returns_at_least_one()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "readme.md", "hello");
        await commitSvc.CommitAsync(handle, "initial", TestAuthor());

        var branches = await svc.GetBranchesAsync(handle);
        Assert.NotEmpty(branches);
        Assert.Contains(branches, b => b.IsCurrent);
        Assert.False(branches[0].IsRemote);
    }

    [Fact]
    public async Task GetCommitsAsync_returns_committed_message()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "readme.md", "hello");
        var first = await commitSvc.CommitAsync(handle, "first commit", TestAuthor());

        var commits = await svc.GetCommitsAsync(handle, "HEAD", 10);
        Assert.Single(commits);
        Assert.Equal(first.Sha, commits[0].Sha);
        Assert.Contains("first commit", commits[0].Message);
        Assert.Empty(commits[0].Parents);
    }

    [Fact]
    public async Task GetCommitsAsync_with_invalid_ref_throws()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "a", TestAuthor());

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.GetCommitsAsync(handle, "not-a-real-ref", 10));
    }

    [Fact]
    public async Task GetStatusAsync_lists_untracked_files()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "new.txt", "data");

        var status = await svc.GetStatusAsync(handle);
        Assert.Contains(status.Untracked, f => f.Path == "new.txt");
        Assert.False(status.IsClean);
    }

    [Fact]
    public async Task GetStatusAsync_clean_after_commit()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "a", TestAuthor());

        var status = await svc.GetStatusAsync(handle);
        Assert.True(status.IsClean);
    }

    [Fact]
    public async Task GetDiffAsync_between_two_commits_contains_added_text()
    {
        using var temp = new TempRepoDir();
        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        using var handle = await svc.InitAsync(temp.Path);
        WriteFile(temp.Path, "doc.txt", "alpha\n");
        var first = await commitSvc.CommitAsync(handle, "first", TestAuthor());

        WriteFile(temp.Path, "doc.txt", "alpha\nbeta\n");
        var second = await commitSvc.CommitAsync(handle, "second", TestAuthor());

        var diff = await svc.GetDiffAsync(handle, first.Sha, second.Sha);
        Assert.Contains("beta", diff);
    }

    [Fact]
    public async Task CloneAsync_from_local_bare_repo_works()
    {
        // origin: a bare local repo, populated by cloning from a tiny seed.
        using var seed = new TempRepoDir("ttsgit-seed");
        using var origin = new TempRepoDir("ttsgit-origin");
        using var destination = new TempRepoDir("ttsgit-dest");

        var svc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();

        // seed -> one commit
        using (var seedHandle = await svc.InitAsync(seed.Path))
        {
            WriteFile(seed.Path, "hello.txt", "world");
            await commitSvc.CommitAsync(seedHandle, "seed", TestAuthor());
        }

        // bare clone of seed via libgit2 (acts as a synthetic "remote")
        // We use Repository.Clone directly here because the service deliberately
        // doesn't expose a "bare clone" option in v0.1.0.
        Repository.Clone(seed.Path, origin.Path, new CloneOptions { IsBare = true });

        // clone bare -> destination via our service (the API under test)
        using var destHandle = await svc.CloneAsync(origin.Path, destination.Path);
        Assert.True(Directory.Exists(Path.Combine(destination.Path, ".git")));

        var commits = await svc.GetCommitsAsync(destHandle, "HEAD", 10);
        Assert.NotEmpty(commits);
        Assert.Contains("seed", commits[0].Message);
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
