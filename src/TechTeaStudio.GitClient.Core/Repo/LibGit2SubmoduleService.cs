namespace TechTeaStudio.GitClient.Repo;

using System.Diagnostics;
using System.Text;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="ISubmoduleService"/>.
///
/// <para>
/// Listing and updating use LibGit2Sharp directly. Adding a submodule does
/// NOT have a clean LibGit2Sharp API in 0.31.0, so we shell out to the
/// <c>git</c> binary (<c>git submodule add &lt;url&gt; &lt;path&gt;</c>).
/// The caller needs <c>git</c> on <c>PATH</c>; otherwise this throws.
/// </para>
///
/// Concurrency: takes the handle's async lock around every native call —
/// LibGit2Sharp's <c>Repository</c> is not thread-safe.
/// </summary>
public sealed class LibGit2SubmoduleService : ISubmoduleService
{
    public async Task<IReadOnlyList<SubmoduleInfo>> ListSubmodulesAsync(
        IRepoHandle handle,
        CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var list = new List<SubmoduleInfo>();
            foreach (var sm in repo.Submodules)
            {
                ct.ThrowIfCancellationRequested();
                var status = sm.RetrieveStatus();
                list.Add(new SubmoduleInfo
                {
                    Name = sm.Name,
                    Path = sm.Path,
                    Url = sm.Url ?? string.Empty,
                    HeadSha = sm.HeadCommitId?.Sha,
                    IndexSha = sm.IndexCommitId?.Sha,
                    WorkDirSha = sm.WorkDirCommitId?.Sha,
                    Status = DescribeStatus(status),
                });
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task AddSubmoduleAsync(
        IRepoHandle handle,
        string url,
        string path,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var workdir = h.Repository.Info.WorkingDirectory
                ?? throw new InvalidOperationException("Bare repositories cannot host submodules.");

            // LibGit2Sharp 0.31 has no public "add submodule" API, so shell out.
            await RunGitAsync(workdir, ["submodule", "add", url, path], ct).ConfigureAwait(false);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task UpdateSubmoduleAsync(
        IRepoHandle handle,
        string name,
        bool init,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var sm = repo.Submodules[name]
                ?? throw new ArgumentException($"Submodule '{name}' is not registered.", nameof(name));

            var options = new SubmoduleUpdateOptions { Init = init };
            repo.Submodules.Update(name, options);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static string DescribeStatus(SubmoduleStatus s)
    {
        if (s == SubmoduleStatus.Unmodified)
            return "in sync";
        if (s.HasFlag(SubmoduleStatus.WorkDirUninitialized))
            return "missing";
        if (s.HasFlag(SubmoduleStatus.WorkDirModified) ||
            s.HasFlag(SubmoduleStatus.WorkDirFilesModified) ||
            s.HasFlag(SubmoduleStatus.WorkDirFilesIndexDirty) ||
            s.HasFlag(SubmoduleStatus.WorkDirFilesUntracked))
            return "dirty";
        if (s.HasFlag(SubmoduleStatus.IndexModified) ||
            s.HasFlag(SubmoduleStatus.IndexAdded) ||
            s.HasFlag(SubmoduleStatus.IndexDeleted))
            return "out of date";
        return s.ToString();
    }

    private static async Task RunGitAsync(string workingDir, string[] args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = new Process { StartInfo = psi };
        try
        {
            proc.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new InvalidOperationException(
                "The 'git' executable was not found on PATH. Install git and try again.", ex);
        }

        var stderr = new StringBuilder();
        var stderrTask = Task.Run(async () =>
        {
            string? line;
            while ((line = await proc.StandardError.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
                stderr.AppendLine(line);
        }, ct);

        var stdoutTask = Task.Run(async () =>
        {
            // drain stdout so a chatty subprocess doesn't deadlock on a full pipe
            string? line;
            while ((line = await proc.StandardOutput.ReadLineAsync(ct).ConfigureAwait(false)) is not null) { _ = line; }
        }, ct);

        await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"git {string.Join(' ', args)} exited with code {proc.ExitCode}: {stderr.ToString().Trim()}");
    }
}
