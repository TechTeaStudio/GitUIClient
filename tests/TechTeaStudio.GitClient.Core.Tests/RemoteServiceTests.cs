namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.Sync;

public sealed class RemoteServiceTests
{
    [Fact]
    public async Task ListRemotesAsync_returns_empty_for_fresh_repo()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        var remotes = await remoteSvc.ListRemotesAsync(handle);

        Assert.Empty(remotes);
    }

    [Fact]
    public async Task AddRemoteAsync_then_ListRemotesAsync_returns_it()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        await remoteSvc.AddRemoteAsync(handle, "origin", "https://example.com/repo.git");

        var remotes = await remoteSvc.ListRemotesAsync(handle);
        Assert.Single(remotes);
        Assert.Equal("origin", remotes[0].Name);
        Assert.Equal("https://example.com/repo.git", remotes[0].Url);
        Assert.Null(remotes[0].PushUrl); // not separately set
    }

    [Fact]
    public async Task RemoveRemoteAsync_removes_remote()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        await remoteSvc.AddRemoteAsync(handle, "upstream", "https://example.com/u.git");
        await remoteSvc.AddRemoteAsync(handle, "fork", "https://example.com/f.git");

        await remoteSvc.RemoveRemoteAsync(handle, "fork");

        var remaining = await remoteSvc.ListRemotesAsync(handle);
        Assert.Single(remaining);
        Assert.Equal("upstream", remaining[0].Name);
    }

    [Fact]
    public async Task RenameRemoteAsync_renames_remote()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        await remoteSvc.AddRemoteAsync(handle, "origin", "https://example.com/repo.git");

        await remoteSvc.RenameRemoteAsync(handle, "origin", "upstream");

        var remotes = await remoteSvc.ListRemotesAsync(handle);
        Assert.Single(remotes);
        Assert.Equal("upstream", remotes[0].Name);
        Assert.Equal("https://example.com/repo.git", remotes[0].Url);
    }

    [Fact]
    public async Task AddRemoteAsync_validates_arguments()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        await Assert.ThrowsAsync<ArgumentException>(() => remoteSvc.AddRemoteAsync(handle, "", "http://x"));
        await Assert.ThrowsAsync<ArgumentException>(() => remoteSvc.AddRemoteAsync(handle, "origin", ""));
    }

    [Fact]
    public async Task ListRemoteRefsAsync_against_local_bare_origin_returns_branches()
    {
        // Spin up a bare repo with one commit and use it as a synthetic "remote".
        using var seed = new TempRepoDir("ttsgit-seed");
        using var origin = new TempRepoDir("ttsgit-origin");
        using var working = new TempRepoDir("ttsgit-working");

        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var remoteSvc = new LibGit2RemoteService();

        using (var seedHandle = await repos.InitAsync(seed.Path))
        {
            WriteFile(seed.Path, "hello.txt", "world");
            await commitSvc.CommitAsync(seedHandle, "seed", TestAuthor());
        }

        Repository.Clone(seed.Path, origin.Path, new CloneOptions { IsBare = true });

        using var handle = await repos.InitAsync(working.Path);
        await remoteSvc.AddRemoteAsync(handle, "origin", origin.Path);

        var refs = await remoteSvc.ListRemoteRefsAsync(handle, "origin");
        Assert.NotEmpty(refs);
        Assert.Contains(refs, r => r.IsBranch);
    }

    [Fact]
    public async Task ListRemoteRefsAsync_throws_for_unknown_remote()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var remoteSvc = new LibGit2RemoteService();

        using var handle = await repos.InitAsync(temp.Path);
        await Assert.ThrowsAsync<ArgumentException>(
            () => remoteSvc.ListRemoteRefsAsync(handle, "does-not-exist"));
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
