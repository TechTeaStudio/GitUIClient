namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.WorkingTree;

public sealed class StashServiceTests
{
    [Fact]
    public async Task SaveStashAsync_after_modifying_tracked_file_leaves_working_tree_clean()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var stashes = new LibGit2StashService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "original");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        // Modify the tracked file
        WriteFile(temp.Path, "a.txt", "modified");

        var entry = await stashes.SaveStashAsync(handle, "WIP", TestAuthor(), includeUntracked: false);

        Assert.Equal(0, entry.Index);
        Assert.Contains("WIP", entry.Message);

        var list = await stashes.ListStashesAsync(handle);
        Assert.Single(list);

        // After stashing the working tree should be clean.
        var status = await repos.GetStatusAsync(handle);
        Assert.True(status.IsClean);

        // And the file content should be restored to the committed version.
        Assert.Equal("original", File.ReadAllText(Path.Combine(temp.Path, "a.txt")));
    }

    [Fact]
    public async Task ApplyStashAsync_re_applies_changes()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var stashes = new LibGit2StashService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "original");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "a.txt", "modified");
        await stashes.SaveStashAsync(handle, "WIP", TestAuthor(), includeUntracked: false);

        Assert.Equal("original", File.ReadAllText(Path.Combine(temp.Path, "a.txt")));

        await stashes.ApplyStashAsync(handle, 0);

        Assert.Equal("modified", File.ReadAllText(Path.Combine(temp.Path, "a.txt")));

        // Apply does NOT drop — stash is still listed.
        var list = await stashes.ListStashesAsync(handle);
        Assert.Single(list);
    }

    [Fact]
    public async Task DropStashAsync_removes_entry()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var stashes = new LibGit2StashService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "original");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "a.txt", "modified");
        await stashes.SaveStashAsync(handle, "WIP", TestAuthor(), includeUntracked: false);
        Assert.Single(await stashes.ListStashesAsync(handle));

        await stashes.DropStashAsync(handle, 0);

        Assert.Empty(await stashes.ListStashesAsync(handle));
    }

    [Fact]
    public async Task PopStashAsync_applies_and_drops()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var stashes = new LibGit2StashService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "original");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        WriteFile(temp.Path, "a.txt", "modified");
        await stashes.SaveStashAsync(handle, "WIP", TestAuthor(), includeUntracked: false);

        await stashes.PopStashAsync(handle, 0);

        Assert.Equal("modified", File.ReadAllText(Path.Combine(temp.Path, "a.txt")));
        Assert.Empty(await stashes.ListStashesAsync(handle));
    }

    [Fact]
    public async Task SaveStashAsync_includeUntracked_captures_new_files()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var stashes = new LibGit2StashService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "seed.txt", "seed");
        await commits.CommitAsync(handle, "seed", TestAuthor());

        // New untracked file
        WriteFile(temp.Path, "fresh.txt", "fresh");

        await stashes.SaveStashAsync(handle, "with untracked", TestAuthor(), includeUntracked: true);

        Assert.False(File.Exists(Path.Combine(temp.Path, "fresh.txt")));

        await stashes.PopStashAsync(handle, 0);
        Assert.True(File.Exists(Path.Combine(temp.Path, "fresh.txt")));
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
