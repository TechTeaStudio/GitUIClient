namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

public sealed class ConflictsServiceTests
{
    [Fact]
    public async Task ListConflictsAsync_returns_empty_when_no_conflicts()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var conflicts = new LibGit2ConflictsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        var list = await conflicts.ListConflictsAsync(handle);

        Assert.Empty(list);
    }

    [Fact]
    public async Task ListConflictsAsync_after_conflicting_merge_returns_path_and_three_sides()
    {
        using var temp = new TempRepoDir();

        // Set up conflict using a fresh Repository (LibGit2Sharp caches index in
        // each Repository instance — if we held a handle open, it wouldn't see the
        // merge state on disk). After setup we open a fresh handle to read.
        SetupConflict(temp.Path);

        var repos = new LibGit2RepositoryService();
        var conflicts = new LibGit2ConflictsService();
        using var handle = await repos.OpenAsync(temp.Path);

        var list = await conflicts.ListConflictsAsync(handle);
        Assert.NotEmpty(list);
        var conflict = Assert.Single(list);
        Assert.Equal("file.txt", conflict.Path);
        Assert.NotNull(conflict.OursSha);
        Assert.NotNull(conflict.TheirsSha);
        Assert.NotNull(conflict.AncestorSha);
    }

    [Fact]
    public async Task ResolveByOursAsync_writes_ours_content_and_clears_conflict()
    {
        using var temp = new TempRepoDir();
        SetupConflict(temp.Path);

        var repos = new LibGit2RepositoryService();
        var conflicts = new LibGit2ConflictsService();
        using var handle = await repos.OpenAsync(temp.Path);

        await conflicts.ResolveByOursAsync(handle, "file.txt");

        Assert.Empty(await conflicts.ListConflictsAsync(handle));
        var onDisk = File.ReadAllText(Path.Combine(temp.Path, "file.txt"));
        Assert.Equal("ours\n", onDisk);
    }

    [Fact]
    public async Task ResolveByTheirsAsync_writes_theirs_content_and_clears_conflict()
    {
        using var temp = new TempRepoDir();
        SetupConflict(temp.Path);

        var repos = new LibGit2RepositoryService();
        var conflicts = new LibGit2ConflictsService();
        using var handle = await repos.OpenAsync(temp.Path);

        await conflicts.ResolveByTheirsAsync(handle, "file.txt");

        Assert.Empty(await conflicts.ListConflictsAsync(handle));
        var onDisk = File.ReadAllText(Path.Combine(temp.Path, "file.txt"));
        Assert.Equal("theirs\n", onDisk);
    }

    [Fact]
    public async Task ResolveByOursAsync_unknown_path_throws_ArgumentException()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var conflicts = new LibGit2ConflictsService();

        using var handle = await repos.InitAsync(temp.Path);

        await Assert.ThrowsAsync<ArgumentException>(
            () => conflicts.ResolveByOursAsync(handle, "nonexistent.txt"));
    }

    /// <summary>
    /// Create a repo at <paramref name="repoPath"/> with two branches whose tip
    /// commits both edit <c>file.txt</c> differently — a guaranteed merge conflict.
    /// On return the repo is mid-merge (HEAD = main tip, MERGE_HEAD = side tip).
    /// </summary>
    private static void SetupConflict(string repoPath)
    {
        Repository.Init(repoPath);
        var sig = new Signature("Test", "test@example.com", DateTimeOffset.UtcNow);

        using (var repo = new Repository(repoPath))
        {
            // base
            File.WriteAllText(Path.Combine(repoPath, "file.txt"), "base\n");
            Commands.Stage(repo, "*");
            repo.Commit("base", sig, sig);

            // side branch: write theirs
            var side = repo.CreateBranch("side");
            Commands.Checkout(repo, side);
            File.WriteAllText(Path.Combine(repoPath, "file.txt"), "theirs\n");
            Commands.Stage(repo, "*");
            repo.Commit("theirs", sig, sig);

            // back to main (not "side") and write ours
            var main = repo.Branches.First(b => b.FriendlyName != "side");
            Commands.Checkout(repo, main, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
            File.WriteAllText(Path.Combine(repoPath, "file.txt"), "ours\n");
            Commands.Stage(repo, "*");
            repo.Commit("ours", sig, sig);

            // trigger merge — conflicts go into the index, HEAD stays put.
            repo.Merge(repo.Branches["side"], sig, new LibGit2Sharp.MergeOptions
            {
                CommitOnSuccess = false,
                FastForwardStrategy = FastForwardStrategy.NoFastForward,
            });
        }
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
