namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Branching;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class BranchOpsServiceTests
{
    [Fact]
    public async Task CreateBranchAsync_from_HEAD_creates_branch_pointing_at_HEAD_tip()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        var created = await ops.CreateBranchAsync(handle, "feature/x");

        Assert.Equal("feature/x", created.Name);
        Assert.Equal(seed.Sha, created.TipSha);
        Assert.False(created.IsCurrent); // we didn't ask for checkout
        Assert.False(created.IsRemote);

        var branches = await repos.GetBranchesAsync(handle);
        Assert.Contains(branches, b => b.Name == "feature/x");
    }

    [Fact]
    public async Task CreateBranchAsync_with_checkout_switches_HEAD()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        var created = await ops.CreateBranchAsync(handle, "feature/x", startPointSha: null, checkout: true);

        Assert.True(created.IsCurrent);

        var branches = await repos.GetBranchesAsync(handle);
        var current = branches.Single(b => b.IsCurrent);
        Assert.Equal("feature/x", current.Name);
    }

    [Fact]
    public async Task CreateBranchAsync_at_explicit_sha_uses_that_starting_point()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var first = await commitSvc.CommitAsync(handle, "first", TestAuthor());
        WriteFile(temp.Path, "b.txt", "b");
        var second = await commitSvc.CommitAsync(handle, "second", TestAuthor());

        var branch = await ops.CreateBranchAsync(handle, "back-then", startPointSha: first.Sha);

        Assert.Equal(first.Sha, branch.TipSha);
        Assert.NotEqual(second.Sha, branch.TipSha);
    }

    [Fact]
    public async Task RenameBranchAsync_renames_in_place()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());
        await ops.CreateBranchAsync(handle, "old-name");

        var renamed = await ops.RenameBranchAsync(handle, "old-name", "new-name");

        Assert.Equal("new-name", renamed.Name);

        var branches = await repos.GetBranchesAsync(handle);
        Assert.Contains(branches, b => b.Name == "new-name");
        Assert.DoesNotContain(branches, b => b.Name == "old-name");
    }

    [Fact]
    public async Task DeleteBranchAsync_removes_merged_branch()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());
        await ops.CreateBranchAsync(handle, "kill-me");

        await ops.DeleteBranchAsync(handle, "kill-me");

        var branches = await repos.GetBranchesAsync(handle);
        Assert.DoesNotContain(branches, b => b.Name == "kill-me");
    }

    [Fact]
    public async Task DeleteBranchAsync_unmerged_without_force_throws()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        // Create + check out side branch, give it a unique commit, then jump
        // back to the original branch so the side branch has unmerged work.
        await ops.CreateBranchAsync(handle, "side", checkout: true);
        WriteFile(temp.Path, "side.txt", "side");
        await commitSvc.CommitAsync(handle, "side work", TestAuthor());

        // Back to the original branch (whatever it's named — main/master).
        var originalName = (await repos.GetBranchesAsync(handle))
            .First(b => b.Name != "side").Name;
        await ops.CheckoutBranchAsync(handle, originalName);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ops.DeleteBranchAsync(handle, "side"));

        // Still there.
        Assert.Contains(await repos.GetBranchesAsync(handle), b => b.Name == "side");
    }

    [Fact]
    public async Task DeleteBranchAsync_unmerged_with_force_removes_branch()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        await ops.CreateBranchAsync(handle, "side", checkout: true);
        WriteFile(temp.Path, "side.txt", "side");
        await commitSvc.CommitAsync(handle, "side work", TestAuthor());

        var originalName = (await repos.GetBranchesAsync(handle))
            .First(b => b.Name != "side").Name;
        await ops.CheckoutBranchAsync(handle, originalName);

        await ops.DeleteBranchAsync(handle, "side", force: true);

        Assert.DoesNotContain(await repos.GetBranchesAsync(handle), b => b.Name == "side");
    }

    [Fact]
    public async Task CheckoutBranchAsync_moves_HEAD_to_branch_tip()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var ops = new LibGit2BranchOpsService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        // create-without-checkout, then check out — proves CheckoutBranchAsync moved HEAD.
        var side = await ops.CreateBranchAsync(handle, "feature");
        await ops.CheckoutBranchAsync(handle, "feature");

        var branches = await repos.GetBranchesAsync(handle);
        var current = branches.Single(b => b.IsCurrent);
        Assert.Equal("feature", current.Name);
        Assert.Equal(side.TipSha, current.TipSha);
    }

    [Fact]
    public async Task CreateBranchAsync_rejects_non_libgit2_handle()
    {
        var ops = new LibGit2BranchOpsService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => ops.CreateBranchAsync(stub, "x"));
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
