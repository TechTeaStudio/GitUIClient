namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Branching;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class TagServiceTests
{
    [Fact]
    public async Task ListTagsAsync_empty_on_fresh_repo()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var tags = new LibGit2TagService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        var listed = await tags.ListTagsAsync(handle);
        Assert.Empty(listed);
    }

    [Fact]
    public async Task CreateLightweightTagAsync_creates_and_returns_it_from_list()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var tags = new LibGit2TagService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        var tag = await tags.CreateLightweightTagAsync(handle, "v0.1.0", seed.Sha);
        Assert.Equal("v0.1.0", tag.Name);
        Assert.Equal(seed.Sha, tag.TargetSha);
        Assert.False(tag.IsAnnotated);
        Assert.Null(tag.Message);
        Assert.Null(tag.Tagger);

        var listed = await tags.ListTagsAsync(handle);
        Assert.Single(listed);
        Assert.Equal("v0.1.0", listed[0].Name);
        Assert.False(listed[0].IsAnnotated);
        Assert.Equal(seed.Sha, listed[0].TargetSha);
    }

    [Fact]
    public async Task CreateAnnotatedTagAsync_round_trips_message_and_tagger()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var tags = new LibGit2TagService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        var tagger = TestAuthor();
        var tag = await tags.CreateAnnotatedTagAsync(handle, "v1.0", seed.Sha, "release notes", tagger);

        Assert.Equal("v1.0", tag.Name);
        Assert.Equal(seed.Sha, tag.TargetSha);
        Assert.True(tag.IsAnnotated);
        Assert.NotNull(tag.Message);
        Assert.Contains("release notes", tag.Message);
        Assert.NotNull(tag.Tagger);
        Assert.Equal(tagger.Name, tag.Tagger!.Name);
        Assert.Equal(tagger.Email, tag.Tagger!.Email);

        var listed = await tags.ListTagsAsync(handle);
        var fetched = Assert.Single(listed);
        Assert.True(fetched.IsAnnotated);
        Assert.Contains("release notes", fetched.Message);
        Assert.Equal(tagger.Name, fetched.Tagger!.Name);
        Assert.Equal(tagger.Email, fetched.Tagger!.Email);
        Assert.Equal(seed.Sha, fetched.TargetSha);
    }

    [Fact]
    public async Task DeleteTagAsync_removes_the_tag()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var tags = new LibGit2TagService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        await tags.CreateLightweightTagAsync(handle, "v0.1.0", seed.Sha);
        Assert.Single(await tags.ListTagsAsync(handle));

        await tags.DeleteTagAsync(handle, "v0.1.0");
        Assert.Empty(await tags.ListTagsAsync(handle));
    }

    [Fact]
    public async Task ListTagsAsync_returns_mix_of_lightweight_and_annotated()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var tags = new LibGit2TagService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commitSvc.CommitAsync(handle, "seed", TestAuthor());

        await tags.CreateLightweightTagAsync(handle, "light", seed.Sha);
        await tags.CreateAnnotatedTagAsync(handle, "annot", seed.Sha, "msg", TestAuthor());

        var listed = await tags.ListTagsAsync(handle);
        Assert.Equal(2, listed.Count);
        Assert.Contains(listed, t => t.Name == "light" && !t.IsAnnotated);
        Assert.Contains(listed, t => t.Name == "annot" && t.IsAnnotated);
    }

    [Fact]
    public async Task CreateLightweightTagAsync_rejects_non_libgit2_handle()
    {
        var tags = new LibGit2TagService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => tags.CreateLightweightTagAsync(stub, "v0", "deadbeef"));
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
