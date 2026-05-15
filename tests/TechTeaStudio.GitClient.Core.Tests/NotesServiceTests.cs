namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

public sealed class NotesServiceTests
{
    [Fact]
    public async Task ReadNoteAsync_returns_null_when_no_note_attached()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        var note = await notes.ReadNoteAsync(handle, seed.Sha);

        Assert.Null(note);
    }

    [Fact]
    public async Task AddNoteAsync_then_ReadNoteAsync_roundtrips_the_message()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        await notes.AddNoteAsync(handle, seed.Sha, "hello note", TestAuthor());
        var note = await notes.ReadNoteAsync(handle, seed.Sha);

        Assert.NotNull(note);
        Assert.Contains("hello note", note);
    }

    [Fact]
    public async Task AddNoteAsync_replaces_existing_note_in_namespace()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        await notes.AddNoteAsync(handle, seed.Sha, "first",  TestAuthor());
        await notes.AddNoteAsync(handle, seed.Sha, "second", TestAuthor());

        var note = await notes.ReadNoteAsync(handle, seed.Sha);
        Assert.NotNull(note);
        Assert.Contains("second", note);
    }

    [Fact]
    public async Task RemoveNoteAsync_clears_the_note()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        await notes.AddNoteAsync(handle, seed.Sha, "to be removed", TestAuthor());
        Assert.NotNull(await notes.ReadNoteAsync(handle, seed.Sha));

        await notes.RemoveNoteAsync(handle, seed.Sha);

        Assert.Null(await notes.ReadNoteAsync(handle, seed.Sha));
    }

    [Fact]
    public async Task AddNoteAsync_supports_custom_namespace()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "a");
        var seed = await commits.CommitAsync(handle, "seed", TestAuthor());

        await notes.AddNoteAsync(handle, seed.Sha, "review", TestAuthor(), @namespace: "reviews");

        Assert.Null(await notes.ReadNoteAsync(handle, seed.Sha, @namespace: "commits"));
        Assert.NotNull(await notes.ReadNoteAsync(handle, seed.Sha, @namespace: "reviews"));
    }

    [Fact]
    public async Task ReadNoteAsync_rejects_invalid_sha()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var notes = new LibGit2NotesService();

        using var handle = await repos.InitAsync(temp.Path);

        await Assert.ThrowsAsync<ArgumentException>(
            () => notes.ReadNoteAsync(handle, "not-a-valid-sha"));
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
