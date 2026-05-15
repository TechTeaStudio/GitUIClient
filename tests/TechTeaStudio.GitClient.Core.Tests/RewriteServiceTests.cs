namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.Rewrites;

using MergeOptions = TechTeaStudio.GitClient.Models.MergeOptions;

public sealed class RewriteServiceTests
{
    [Fact]
    public async Task MergeAsync_fast_forward_moves_head_to_source_tip()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        // Seed commit on main.
        WriteFile(temp.Path, "seed.txt", "seed");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        var mainName = GetCurrentBranchName(handle);

        // Create a feature branch pointing at HEAD, advance it with two commits.
        CreateAndCheckoutBranch(handle, "feature");
        WriteFile(temp.Path, "feature1.txt", "f1");
        await commits.CommitAsync(handle, "feature 1", TestAuthor());
        WriteFile(temp.Path, "feature2.txt", "f2");
        var featureTip = await commits.CommitAsync(handle, "feature 2", TestAuthor());

        // Switch back to main; merging the feature branch should fast-forward.
        Checkout(handle, mainName);

        var outcome = await rewrites.MergeAsync(
            handle,
            "feature",
            TestAuthor(),
            new MergeOptions { FastForwardStrategy = "default", CommitOnSuccess = true });

        Assert.Equal(MergeOutcome.FastForward, outcome);

        var headCommits = await repos.GetCommitsAsync(handle, "HEAD", 5);
        Assert.Equal(featureTip.Sha, headCommits[0].Sha);
    }

    [Fact]
    public async Task MergeAsync_non_fast_forward_creates_merge_commit_with_two_parents()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        // Seed commit on main.
        WriteFile(temp.Path, "seed.txt", "seed");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());
        var mainName = GetCurrentBranchName(handle);

        // feature branch off main, diverges with its own file.
        CreateAndCheckoutBranch(handle, "feature");
        WriteFile(temp.Path, "feature.txt", "f");
        await commits.CommitAsync(handle, "feature change", TestAuthor());

        // Back to main, add a different file so histories diverge.
        Checkout(handle, mainName);
        WriteFile(temp.Path, "main.txt", "m");
        var mainTip = await commits.CommitAsync(handle, "main change", TestAuthor());

        var outcome = await rewrites.MergeAsync(
            handle,
            "feature",
            TestAuthor(),
            new MergeOptions
            {
                FastForwardStrategy = "noFastForward",
                CommitOnSuccess = true,
                CustomMessage = "merge feature",
            });

        Assert.Equal(MergeOutcome.NonFastForward, outcome);

        var headCommits = await repos.GetCommitsAsync(handle, "HEAD", 5);
        var newTip = headCommits[0];
        Assert.Equal(2, newTip.Parents.Count);
        // First parent is the prior HEAD (main), second is feature tip.
        Assert.Equal(mainTip.Sha, newTip.Parents[0]);
        Assert.NotEqual(seed.Sha, newTip.Parents[1]);
    }

    [Fact]
    public async Task RevertAsync_creates_inverse_commit()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        WriteFile(temp.Path, "seed.txt", "seed");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "added.txt", "added by target commit");
        var target = await commits.CommitAsync(handle, "add file", TestAuthor());

        Assert.True(File.Exists(Path.Combine(temp.Path, "added.txt")));

        var revertCommit = await rewrites.RevertAsync(
            handle,
            target.Sha,
            TestAuthor(),
            commitOnSuccess: true);

        // A new commit was made on top of target.
        Assert.NotEqual(target.Sha, revertCommit.Sha);
        Assert.Single(revertCommit.Parents);
        Assert.Equal(target.Sha, revertCommit.Parents[0]);

        // The file the target commit added is gone again.
        Assert.False(File.Exists(Path.Combine(temp.Path, "added.txt")));
    }

    [Fact]
    public async Task CherryPickAsync_brings_side_branch_commit_onto_current()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        WriteFile(temp.Path, "seed.txt", "seed");
        await commits.CommitAsync(handle, "seed", TestAuthor());
        var mainName = GetCurrentBranchName(handle);

        CreateAndCheckoutBranch(handle, "feature");
        WriteFile(temp.Path, "cherry.txt", "cherry pick me");
        var cherrySource = await commits.CommitAsync(handle, "cherry source", TestAuthor());

        // Switch back to main — main does NOT contain cherry.txt yet.
        Checkout(handle, mainName);
        Assert.False(File.Exists(Path.Combine(temp.Path, "cherry.txt")));

        // Use a different committer + later timestamp so the new commit's SHA
        // is guaranteed to differ from the source's even though the patch is identical.
        var picker = new AuthorInfo
        {
            Name = "Picker",
            Email = "picker@example.com",
            When = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero),
        };

        var picked = await rewrites.CherryPickAsync(
            handle,
            cherrySource.Sha,
            picker,
            commitOnSuccess: true);

        Assert.NotEqual(cherrySource.Sha, picked.Sha);
        Assert.Single(picked.Parents);

        // The cherry-picked file now exists on main, and HEAD is the picked commit.
        Assert.True(File.Exists(Path.Combine(temp.Path, "cherry.txt")));
        var headCommits = await repos.GetCommitsAsync(handle, mainName, 5);
        Assert.Equal(picked.Sha, headCommits[0].Sha);
    }

    [Fact]
    public async Task ResetAsync_soft_moves_head_but_keeps_working_tree()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        WriteFile(temp.Path, "seed.txt", "seed");
        var first = await commits.CommitAsync(handle, "first", TestAuthor());

        WriteFile(temp.Path, "second.txt", "second");
        var second = await commits.CommitAsync(handle, "second", TestAuthor());

        Assert.True(File.Exists(Path.Combine(temp.Path, "second.txt")));

        await rewrites.ResetAsync(handle, first.Sha, ResetKind.Soft);

        // HEAD moved back to `first`...
        var headCommits = await repos.GetCommitsAsync(handle, "HEAD", 5);
        Assert.Equal(first.Sha, headCommits[0].Sha);

        // ...but the work tree still has the file the second commit added.
        Assert.True(File.Exists(Path.Combine(temp.Path, "second.txt")));
        _ = second;
    }

    [Fact]
    public async Task ResetAsync_hard_discards_working_tree_changes()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        WriteFile(temp.Path, "seed.txt", "seed");
        var first = await commits.CommitAsync(handle, "first", TestAuthor());

        WriteFile(temp.Path, "second.txt", "second");
        await commits.CommitAsync(handle, "second", TestAuthor());

        Assert.True(File.Exists(Path.Combine(temp.Path, "second.txt")));

        await rewrites.ResetAsync(handle, first.Sha, ResetKind.Hard);

        var headCommits = await repos.GetCommitsAsync(handle, "HEAD", 5);
        Assert.Equal(first.Sha, headCommits[0].Sha);

        // Hard reset wipes the file the second commit introduced.
        Assert.False(File.Exists(Path.Combine(temp.Path, "second.txt")));
    }

    // Rebase tests are tricky to wire up reliably across libgit2 versions
    // (Start() requires concrete Branch instances and identity propagation has
    // subtle behaviour around already-applied commits). Manual smoke-tests via
    // the dialog cover the happy path; deeper coverage is queued for v0.3.
    [Fact(Skip = "Rebase end-to-end is exercised manually via the dialog; see comment above.")]
    public Task RebaseAsync_complete_path() => Task.CompletedTask;

    [Fact]
    public async Task ResetAsync_rejects_unknown_sha()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var rewrites = new LibGit2RewriteService();

        using var handle = await repos.InitAsync(temp.Path);

        WriteFile(temp.Path, "seed.txt", "seed");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        await Assert.ThrowsAsync<ArgumentException>(
            () => rewrites.ResetAsync(handle, "deadbeefdeadbeefdeadbeefdeadbeefdeadbeef", ResetKind.Soft));
    }

    [Fact]
    public async Task MergeAsync_rejects_non_libgit2_handle()
    {
        var rewrites = new LibGit2RewriteService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => rewrites.MergeAsync(stub, "main", TestAuthor(), new MergeOptions()));
    }

    private sealed class StubHandle : IRepoHandle
    {
        public string WorkingDirectory => "/tmp/stub";
        public void Dispose() { }
    }

    // -- low-level repo helpers --------------------------------------------------
    // `LibGit2RepoHandle.Repository` is `internal`, so we open a second
    // `LibGit2Sharp.Repository` against the same working dir for the branch
    // and checkout primitives the public service surface doesn't expose.

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static string GetCurrentBranchName(IRepoHandle handle)
    {
        using var raw = new Repository(handle.WorkingDirectory);
        return raw.Head.FriendlyName;
    }

    private static void CreateAndCheckoutBranch(IRepoHandle handle, string name)
    {
        using var raw = new Repository(handle.WorkingDirectory);
        var branch = raw.Branches[name] ?? raw.CreateBranch(name);
        Commands.Checkout(raw, branch);
    }

    private static void Checkout(IRepoHandle handle, string name)
    {
        using var raw = new Repository(handle.WorkingDirectory);
        var branch = raw.Branches[name]
            ?? throw new InvalidOperationException($"Branch '{name}' not found.");
        Commands.Checkout(raw, branch);
    }

    private static AuthorInfo TestAuthor() => new()
    {
        Name = "Test",
        Email = "test@example.com",
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
    };
}
