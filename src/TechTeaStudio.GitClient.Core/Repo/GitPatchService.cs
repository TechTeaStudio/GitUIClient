namespace TechTeaStudio.GitClient.Repo;

using System.Diagnostics;
using System.Text;

using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Process-based implementation of <see cref="IPatchService"/>.
///
/// <para>
/// LibGit2Sharp 0.31.0 exposes neither <c>git apply</c> nor
/// <c>git format-patch</c> in a usable shape. Implementing them by hand
/// against <c>Diff.Compare</c> + the index is tractable but fragile (line-
/// ending normalisation, binary patches, partial-hunk rejection...).
/// Shelling out to the real <c>git</c> binary trades a <c>PATH</c> dep for
/// correctness; the operation throws cleanly if <c>git</c> isn't installed.
/// </para>
///
/// Concurrency: still takes the handle's async lock — the subprocess
/// mutates files inside the working tree, and we don't want a concurrent
/// LibGit2Sharp call racing against it.
/// </summary>
public sealed class GitPatchService : IPatchService
{
    public async Task ApplyPatchAsync(
        IRepoHandle handle,
        string patchFilePath,
        bool indexOnly,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchFilePath);
        if (!File.Exists(patchFilePath))
            throw new FileNotFoundException("Patch file not found.", patchFilePath);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var workdir = h.Repository.Info.WorkingDirectory
                ?? throw new InvalidOperationException("Bare repositories cannot apply patches.");

            // `git am` would also commit; we want `git apply`, which only updates
            // the index / working tree exactly like the user dropping in a diff.
            var args = indexOnly
                ? new[] { "apply", "--cached", patchFilePath }
                : new[] { "apply", patchFilePath };

            await GitProcess.RunAsync(workdir, args, ct).ConfigureAwait(false);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<string> FormatPatchAsync(
        IRepoHandle handle,
        string commitSha,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var workdir = h.Repository.Info.WorkingDirectory
                ?? throw new InvalidOperationException("Bare repositories cannot format patches.");

            // -1 = single commit; --stdout = emit on stdout instead of files.
            var (stdout, _) = await GitProcess.RunAsync(
                workdir,
                ["format-patch", "-1", "--stdout", commitSha],
                ct).ConfigureAwait(false);
            return stdout;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    /// <summary>Helper: spawn <c>git</c> in <paramref name="workingDir"/>, capture stdout/stderr.</summary>
    internal static class GitProcess
    {
        internal static async Task<(string Stdout, string Stderr)> RunAsync(
            string workingDir,
            IReadOnlyList<string> args,
            CancellationToken ct)
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

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var stdoutTask = Task.Run(async () =>
            {
                var buf = new char[4096];
                int read;
                while ((read = await proc.StandardOutput.ReadAsync(buf, ct).ConfigureAwait(false)) > 0)
                    stdout.Append(buf, 0, read);
            }, ct);
            var stderrTask = Task.Run(async () =>
            {
                var buf = new char[4096];
                int read;
                while ((read = await proc.StandardError.ReadAsync(buf, ct).ConfigureAwait(false)) > 0)
                    stderr.Append(buf, 0, read);
            }, ct);

            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
            await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

            if (proc.ExitCode != 0)
                throw new InvalidOperationException(
                    $"git {string.Join(' ', args)} exited with code {proc.ExitCode}: {stderr.ToString().Trim()}");

            return (stdout.ToString(), stderr.ToString());
        }
    }
}
