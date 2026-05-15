namespace TechTeaStudio.GitClient.Core.Tests;

using System.Text;

using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

public sealed class TreeServiceTests
{
    [Fact]
    public async Task ListTreeAsync_root_returns_committed_files()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "readme.txt", "hello");
        WriteFile(temp.Path, "src/app.cs", "// app");
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        var root = await treeSvc.ListTreeAsync(handle, commit.Sha);

        Assert.Contains(root, e => e.Name == "readme.txt" && e.Kind == TreeEntryKind.Blob);
        Assert.Contains(root, e => e.Name == "src" && e.Kind == TreeEntryKind.Tree);
        var readme = root.Single(e => e.Name == "readme.txt");
        Assert.Equal("readme.txt", readme.Path);
        Assert.Equal(5, readme.Size);
    }

    [Fact]
    public async Task ListTreeAsync_subfolder_returns_children()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "src/app.cs", "// app");
        WriteFile(temp.Path, "src/util.cs", "// util");
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        var sub = await treeSvc.ListTreeAsync(handle, commit.Sha, "src");

        Assert.Equal(2, sub.Count);
        Assert.Contains(sub, e => e.Name == "app.cs" && e.Path == "src/app.cs");
        Assert.Contains(sub, e => e.Name == "util.cs" && e.Path == "src/util.cs");
    }

    [Fact]
    public async Task ReadBlobAsync_returns_utf8_content()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        // Avoid CRLF noise on Windows checkouts: write LF and read it back verbatim.
        const string content = "lineA\nlineB\nприветUTF8";
        WriteFile(temp.Path, "notes.md", content);
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        var read = await treeSvc.ReadBlobAsync(handle, commit.Sha, "notes.md");
        Assert.Equal(content, read);
    }

    [Fact]
    public async Task ReadBlobAsync_returns_null_for_missing_path()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        var read = await treeSvc.ReadBlobAsync(handle, commit.Sha, "does-not-exist.txt");
        Assert.Null(read);
    }

    [Fact]
    public async Task IsBinaryBlobAsync_true_for_blob_with_null_bytes()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        var bytes = new byte[] { 0x50, 0x4B, 0x00, 0x00, 0x01, 0x02 }; // ZIP-ish header with NUL
        File.WriteAllBytes(Path.Combine(temp.Path, "data.bin"), bytes);
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        Assert.True(await treeSvc.IsBinaryBlobAsync(handle, commit.Sha, "data.bin"));
        // ReadBlobAsync also returns null on a binary file.
        Assert.Null(await treeSvc.ReadBlobAsync(handle, commit.Sha, "data.bin"));
    }

    [Fact]
    public async Task IsBinaryBlobAsync_false_for_plain_text_blob()
    {
        using var temp = new TempRepoDir();
        var repoSvc = new LibGit2RepositoryService();
        var commitSvc = new LibGit2CommitService();
        var treeSvc = new LibGit2TreeService();

        using var handle = await repoSvc.InitAsync(temp.Path);
        WriteFile(temp.Path, "plain.txt", "alpha beta gamma");
        var commit = await commitSvc.CommitAsync(handle, "init", TestAuthor());

        Assert.False(await treeSvc.IsBinaryBlobAsync(handle, commit.Sha, "plain.txt"));
    }

    private static void WriteFile(string root, string relative, string content)
    {
        var full = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, Encoding.UTF8.GetBytes(content));
    }

    private static AuthorInfo TestAuthor() => new()
    {
        Name = "Test",
        Email = "test@example.com",
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
    };
}
