namespace TechTeaStudio.GitClient.Core.Tests;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.Sync;

public sealed class SyncServiceTests
{
    [Fact]
    public async Task FetchAsync_brings_new_commit_into_remote_tracking_branch()
    {
        using var seed = new TempRepoDir("ttsgit-seed");
        using var origin = new TempRepoDir("ttsgit-origin");
        using var working = new TempRepoDir("ttsgit-working");

        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var remoteSvc = new LibGit2RemoteService();
        var syncSvc = new LibGit2SyncService();

        // 1. Seed has commit A.
        string commitA;
        using (var seedHandle = await repos.InitAsync(seed.Path))
        {
            WriteFile(seed.Path, "a.txt", "alpha");
            var c = await commitSvc.CommitAsync(seedHandle, "alpha", TestAuthor());
            commitA = c.Sha;
        }

        // 2. Bare clone of seed = "origin".
        Repository.Clone(seed.Path, origin.Path, new CloneOptions { IsBare = true });

        // 3. Working repo is a clone of origin.
        using var workingHandle = await repos.CloneAsync(origin.Path, working.Path);
        // sanity: working repo sees commit A.
        var initialCommits = await repos.GetCommitsAsync(workingHandle, "HEAD", 10);
        Assert.Contains(initialCommits, c => c.Sha == commitA);

        // 4. Add commit B to seed and re-publish into origin.
        string commitB;
        using (var seedHandle = await repos.OpenAsync(seed.Path))
        {
            WriteFile(seed.Path, "b.txt", "bravo");
            var c = await commitSvc.CommitAsync(seedHandle, "bravo", TestAuthor());
            commitB = c.Sha;
        }
        // Push seed -> origin so origin holds commit B too.
        using (var rawSeed = new Repository(seed.Path))
        {
            var originRemote = rawSeed.Network.Remotes["origin"]
                ?? rawSeed.Network.Remotes.Add("origin", origin.Path);
            var head = rawSeed.Head.CanonicalName;
            rawSeed.Network.Push(originRemote, $"+{head}:{head}");
        }

        // 5. Fetch from origin and assert the new commit landed in working's object DB.
        await syncSvc.FetchAsync(workingHandle, "origin");

        // The remote-tracking branch should now point at commitB; commit B must exist
        // in the working repo's object database.
        var remoteTracking = await repos.GetBranchesAsync(workingHandle);
        Assert.Contains(remoteTracking, b => b.IsRemote && b.TipSha == commitB);
    }

    [Fact]
    public async Task PushAsync_publishes_local_branch_to_bare_origin()
    {
        using var origin = new TempRepoDir("ttsgit-origin");
        using var working = new TempRepoDir("ttsgit-working");

        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var remoteSvc = new LibGit2RemoteService();
        var syncSvc = new LibGit2SyncService();

        // 1. Empty bare repo to act as origin.
        Repository.Init(origin.Path, isBare: true);

        // 2. Initialise a working repo with one commit.
        using var workingHandle = await repos.InitAsync(working.Path);
        WriteFile(working.Path, "hello.txt", "world");
        var commit = await commitSvc.CommitAsync(workingHandle, "hello", TestAuthor());

        // 3. Configure origin and push.
        await remoteSvc.AddRemoteAsync(workingHandle, "origin", origin.Path);

        var localBranch = (await repos.GetBranchesAsync(workingHandle)).First(b => !b.IsRemote).Name;
        await syncSvc.PushAsync(workingHandle, "origin", new[] { localBranch }, force: false);

        // 4. Verify origin has the pushed commit at the corresponding ref.
        using var rawOrigin = new Repository(origin.Path);
        var canonical = $"refs/heads/{localBranch}";
        var refOnOrigin = rawOrigin.Refs[canonical];
        Assert.NotNull(refOnOrigin);
        Assert.Equal(commit.Sha, refOnOrigin!.TargetIdentifier);
    }

    [Fact]
    public async Task PullAsync_fast_forwards_when_only_remote_advances()
    {
        using var seed = new TempRepoDir("ttsgit-seed");
        using var origin = new TempRepoDir("ttsgit-origin");
        using var working = new TempRepoDir("ttsgit-working");

        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var syncSvc = new LibGit2SyncService();

        // Seed: commit A.
        string commitA;
        using (var seedHandle = await repos.InitAsync(seed.Path))
        {
            WriteFile(seed.Path, "a.txt", "alpha");
            var c = await commitSvc.CommitAsync(seedHandle, "alpha", TestAuthor());
            commitA = c.Sha;
        }

        // origin = bare clone of seed.
        Repository.Clone(seed.Path, origin.Path, new CloneOptions { IsBare = true });

        // working = clone of origin. Working's HEAD = commit A; no local diverging commits.
        using var workingHandle = await repos.CloneAsync(origin.Path, working.Path);

        // Advance seed with commit B, then push to origin.
        string commitB;
        using (var seedHandle = await repos.OpenAsync(seed.Path))
        {
            WriteFile(seed.Path, "b.txt", "bravo");
            var c = await commitSvc.CommitAsync(seedHandle, "bravo", TestAuthor());
            commitB = c.Sha;
        }
        using (var rawSeed = new Repository(seed.Path))
        {
            var originRemote = rawSeed.Network.Remotes["origin"]
                ?? rawSeed.Network.Remotes.Add("origin", origin.Path);
            var head = rawSeed.Head.CanonicalName;
            rawSeed.Network.Push(originRemote, $"+{head}:{head}");
        }

        // Pull from origin into working.
        var result = await syncSvc.PullAsync(
            workingHandle,
            "origin",
            branchName: null,
            mergeAuthor: TestAuthor());

        Assert.Equal(PullKind.FastForward, result.Kind);
        Assert.Equal(commitB, result.Sha);

        // working's HEAD now contains commit B.
        var commits = await repos.GetCommitsAsync(workingHandle, "HEAD", 10);
        Assert.Contains(commits, c => c.Sha == commitB);
        Assert.Contains(commits, c => c.Sha == commitA);
    }

    [Fact]
    public async Task PullAsync_reports_up_to_date_when_no_changes()
    {
        using var seed = new TempRepoDir("ttsgit-seed");
        using var origin = new TempRepoDir("ttsgit-origin");
        using var working = new TempRepoDir("ttsgit-working");

        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var syncSvc = new LibGit2SyncService();

        using (var seedHandle = await repos.InitAsync(seed.Path))
        {
            WriteFile(seed.Path, "a.txt", "alpha");
            await commitSvc.CommitAsync(seedHandle, "alpha", TestAuthor());
        }

        Repository.Clone(seed.Path, origin.Path, new CloneOptions { IsBare = true });

        using var workingHandle = await repos.CloneAsync(origin.Path, working.Path);

        var result = await syncSvc.PullAsync(
            workingHandle,
            "origin",
            branchName: null,
            mergeAuthor: TestAuthor());

        Assert.Equal(PullKind.UpToDate, result.Kind);
    }

    [Fact]
    public async Task FetchAsync_validates_remote_name()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var syncSvc = new LibGit2SyncService();

        using var handle = await repos.InitAsync(temp.Path);
        await Assert.ThrowsAsync<ArgumentException>(
            () => syncSvc.FetchAsync(handle, "no-such-remote"));
    }

    [Fact]
    public async Task PushAsync_requires_at_least_one_branch()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var syncSvc = new LibGit2SyncService();

        using var handle = await repos.InitAsync(temp.Path);
        await Assert.ThrowsAsync<ArgumentException>(
            () => syncSvc.PushAsync(handle, "origin", Array.Empty<string>()));
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
