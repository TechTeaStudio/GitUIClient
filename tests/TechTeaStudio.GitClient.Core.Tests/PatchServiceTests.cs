namespace TechTeaStudio.GitClient.Core.Tests;

using System.Diagnostics;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;

public sealed class PatchServiceTests
{
    [Fact]
    public async Task FormatPatchAsync_returns_text_containing_the_commit_message()
    {
        if (!GitIsOnPath()) return;

        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var patches = new GitPatchService();

        using var handle = await repos.InitAsync(temp.Path);
        WriteFile(temp.Path, "a.txt", "alpha\n");
        var seed = await commits.CommitAsync(handle, "demo patch message", TestAuthor());

        var patch = await patches.FormatPatchAsync(handle, seed.Sha);

        Assert.False(string.IsNullOrWhiteSpace(patch));
        Assert.Contains("demo patch message", patch);
        Assert.Contains("alpha", patch);
    }

    [Fact]
    public async Task ApplyPatchAsync_roundtrips_a_diff_between_two_repos()
    {
        if (!GitIsOnPath()) return;

        using var src = new TempRepoDir("ttsgit-src");
        using var dst = new TempRepoDir("ttsgit-dst");

        var repos = new LibGit2RepositoryService();
        var commits = new LibGit2CommitService();
        var patches = new GitPatchService();

        // Source repo: two commits — base + a follow-up that adds a line.
        using var srcHandle = await repos.InitAsync(src.Path);
        WriteFile(src.Path, "doc.txt", "line-1\n");
        await commits.CommitAsync(srcHandle, "base", TestAuthor());
        WriteFile(src.Path, "doc.txt", "line-1\nline-2\n");
        var followUp = await commits.CommitAsync(srcHandle, "add line-2", TestAuthor());

        // Destination repo: same starting state, no follow-up.
        using var dstHandle = await repos.InitAsync(dst.Path);
        WriteFile(dst.Path, "doc.txt", "line-1\n");
        await commits.CommitAsync(dstHandle, "base", TestAuthor());

        var patchText = await patches.FormatPatchAsync(srcHandle, followUp.Sha);
        var patchFile = Path.Combine(Path.GetTempPath(), $"ttsgit-patch-{Guid.NewGuid():N}.patch");
        try
        {
            File.WriteAllText(patchFile, patchText);
            await patches.ApplyPatchAsync(dstHandle, patchFile, indexOnly: false);

            var afterApply = File.ReadAllText(Path.Combine(dst.Path, "doc.txt"));
            Assert.Contains("line-2", afterApply);
        }
        finally
        {
            if (File.Exists(patchFile))
                File.Delete(patchFile);
        }
    }

    [Fact]
    public async Task ApplyPatchAsync_throws_FileNotFoundException_when_patch_file_missing()
    {
        using var temp = new TempRepoDir();
        var repos = new LibGit2RepositoryService();
        var patches = new GitPatchService();

        using var handle = await repos.InitAsync(temp.Path);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => patches.ApplyPatchAsync(handle, Path.Combine(Path.GetTempPath(), "no-such.patch"), indexOnly: false));
    }

    [Fact]
    public async Task FormatPatchAsync_rejects_non_libgit2_handle()
    {
        var patches = new GitPatchService();
        var stub = new StubHandle();

        await Assert.ThrowsAsync<ArgumentException>(
            () => patches.FormatPatchAsync(stub, "abc1234"));
    }

    private sealed class StubHandle : IRepoHandle
    {
        public string WorkingDirectory => "/tmp/stub";
        public void Dispose() { }
    }

    private static bool GitIsOnPath()
    {
        try
        {
            var psi = new ProcessStartInfo("git", "--version")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(5000);
            return proc.HasExited && proc.ExitCode == 0;
        }
        catch
        {
            return false;
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
