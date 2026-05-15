namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

public sealed class ContributorsServiceTests
{
    [Fact]
    public async Task ListContributorsAsync_two_commits_same_author_yields_single_entry_with_count_two()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "first",  TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(1)));
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "second", TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(2)));

        var list = await contributors.ListContributorsAsync(handle);

        Assert.Single(list);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal("alice@example.com", list[0].Email);
        Assert.Equal(2, list[0].CommitCount);
        Assert.True(list[0].FirstCommit <= list[0].LastCommit);
    }

    [Fact]
    public async Task ListContributorsAsync_two_commits_different_authors_yields_two_entries()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "alice's commit", TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(1)));
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "bob's commit",   TestAuthor("Bob",   "bob@example.com",   offset: TimeSpan.FromDays(2)));

        var list = await contributors.ListContributorsAsync(handle);

        Assert.Equal(2, list.Count);
        Assert.Contains(list, c => c.Email == "alice@example.com" && c.CommitCount == 1);
        Assert.Contains(list, c => c.Email == "bob@example.com" && c.CommitCount == 1);
    }

    [Fact]
    public async Task ListContributorsAsync_is_email_case_insensitive()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "first",  TestAuthor("Alice", "Alice@Example.com", offset: TimeSpan.FromDays(1)));
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "second", TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(2)));

        var list = await contributors.ListContributorsAsync(handle);

        Assert.Single(list);
        Assert.Equal(2, list[0].CommitCount);
    }

    [Fact]
    public async Task ListContributorsAsync_orders_by_count_descending()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "1", TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(1)));
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "2", TestAuthor("Bob",   "bob@example.com",   offset: TimeSpan.FromDays(2)));
        WriteFile(temp.Path, "c.txt", "c");
        await commits.CommitAsync(handle, "3", TestAuthor("Bob",   "bob@example.com",   offset: TimeSpan.FromDays(3)));

        var list = await contributors.ListContributorsAsync(handle);

        Assert.Equal("bob@example.com", list[0].Email);
        Assert.Equal(2, list[0].CommitCount);
    }

    [Fact]
    public async Task ListContributorsAsync_respects_maxCommits_cap()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "1", TestAuthor("Alice", "alice@example.com", offset: TimeSpan.FromDays(1)));
        WriteFile(temp.Path, "b.txt", "b");
        await commits.CommitAsync(handle, "2", TestAuthor("Bob", "bob@example.com", offset: TimeSpan.FromDays(2)));

        var list = await contributors.ListContributorsAsync(handle, maxCommits: 1);

        // Only the most recent commit is counted -> exactly one contributor with count=1.
        Assert.Single(list);
        Assert.Equal(1, list[0].CommitCount);
    }

    [Fact]
    public async Task ListContributorsAsync_empty_repo_returns_empty_list()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var contributors = new LibGit2ContributorsService();

        using var handle = await repos.InitAsync(temp.Path);

        var list = await contributors.ListContributorsAsync(handle);

        Assert.Empty(list);
    }

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static AuthorInfo TestAuthor(string name, string email, TimeSpan offset) => new()
    {
        Name = name,
        Email = email,
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero).Add(offset),
    };
}
