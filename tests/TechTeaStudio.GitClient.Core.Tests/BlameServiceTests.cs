namespace TechTeaStudio.GitClient.Core.Tests;

using System.Text;

using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class BlameServiceTests
{
    [Fact]
    public async Task BlameFileAsync_attributes_lines_to_their_authoring_commits()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var blameSvc = new LibGit2BlameService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "code.txt", "line-one\nline-two\nline-three\n");
        var c1 = await commitSvc.CommitAsync(handle, "first", TestAuthor("Alice", "alice@example.com"));

        // Edit only the middle line.
        WriteFile(temp.Path, "code.txt", "line-one\nLINE-TWO-EDITED\nline-three\n");
        var c2 = await commitSvc.CommitAsync(handle, "edit middle", TestAuthor("Bob", "bob@example.com"));

        var blame = await blameSvc.BlameFileAsync(handle, c2.Sha, "code.txt");

        Assert.Equal(3, blame.Count);
        Assert.Equal(c1.Sha, blame[0].CommitSha);
        Assert.Equal("Alice", blame[0].Author);
        Assert.Equal(c2.Sha, blame[1].CommitSha);
        Assert.Equal("Bob", blame[1].Author);
        Assert.Equal("LINE-TWO-EDITED", blame[1].Content);
        Assert.Equal(c1.Sha, blame[2].CommitSha);
        // Line numbers are 1-based and dense.
        Assert.Equal(1, blame[0].LineNumber);
        Assert.Equal(2, blame[1].LineNumber);
        Assert.Equal(3, blame[2].LineNumber);
    }

    [Fact]
    public async Task BlameFileAsync_at_first_commit_attributes_all_lines_to_it()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var blameSvc = new LibGit2BlameService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "x\ny\n");
        var c1 = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        var blame = await blameSvc.BlameFileAsync(handle, c1.Sha, "a.txt");

        Assert.Equal(2, blame.Count);
        Assert.All(blame, b => Assert.Equal(c1.Sha, b.CommitSha));
    }

    [Fact]
    public async Task BlameFileAsync_throws_on_missing_path()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var blameSvc = new LibGit2BlameService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "exists.txt", "x");
        var c = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        await Assert.ThrowsAsync<ArgumentException>(
            () => blameSvc.BlameFileAsync(handle, c.Sha, "nope.txt"));
    }

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, Encoding.UTF8.GetBytes(content));
    }

    private static AuthorInfo TestAuthor(string name = "Test", string email = "test@example.com") => new()
    {
        Name = name,
        Email = email,
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
    };
}
