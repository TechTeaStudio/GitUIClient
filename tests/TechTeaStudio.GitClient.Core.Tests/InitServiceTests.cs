namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.WorkingTree;

public sealed class InitServiceTests
{
    [Fact]
    public async Task InitRepositoryAsync_non_bare_creates_dotgit_inside_path()
    {
        using var temp = new TempRepoDir();
        var init = new LibGit2InitService();

        using var handle = await init.InitRepositoryAsync(temp.Path, bare: false);

        Assert.True(Directory.Exists(Path.Combine(temp.Path, ".git")));
        // Working directory is set on non-bare repos.
        Assert.False(string.IsNullOrEmpty(handle.WorkingDirectory));
    }

    [Fact]
    public async Task InitRepositoryAsync_bare_creates_bare_repo_layout()
    {
        using var temp = new TempRepoDir();
        var init = new LibGit2InitService();

        using var handle = await init.InitRepositoryAsync(temp.Path, bare: true);

        // A bare repo has no .git subfolder; the repo's contents (HEAD, objects, refs)
        // live at the top level instead.
        Assert.False(Directory.Exists(Path.Combine(temp.Path, ".git")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "HEAD")));
        Assert.True(Directory.Exists(Path.Combine(temp.Path, "objects")));
        Assert.True(Directory.Exists(Path.Combine(temp.Path, "refs")));
    }

    [Fact]
    public async Task InitRepositoryAsync_throws_on_whitespace_path()
    {
        var init = new LibGit2InitService();
        await Assert.ThrowsAsync<ArgumentException>(() => init.InitRepositoryAsync(" ", bare: false));
    }
}
