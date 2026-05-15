namespace TechTeaStudio.GitClient.Core.Tests;

/// <summary>
/// Creates a uniquely-named directory under <see cref="Path.GetTempPath"/>;
/// recursively deletes it on <see cref="Dispose"/>, retrying past the usual
/// Windows file-locking flakes (LibGit2Sharp sometimes hangs onto the .git
/// folder for a moment after <see cref="LibGit2Sharp.Repository.Dispose"/>).
/// </summary>
public sealed class TempRepoDir : IDisposable
{
    public TempRepoDir(string prefix = "ttsgit")
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path))
            return;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                ForceDeleteReadOnly(Path);
                Directory.Delete(Path, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50 * (attempt + 1));
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(50 * (attempt + 1));
            }
        }
        // Last attempt — swallow any final exception so the test result isn't masked.
        try { Directory.Delete(Path, recursive: true); }
        catch { /* best effort */ }
    }

    private static void ForceDeleteReadOnly(string root)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attrs = File.GetAttributes(file);
                if (attrs.HasFlag(FileAttributes.ReadOnly))
                    File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);
            }
            catch { /* keep going */ }
        }
    }
}
