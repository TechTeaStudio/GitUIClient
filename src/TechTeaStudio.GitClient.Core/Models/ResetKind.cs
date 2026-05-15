namespace TechTeaStudio.GitClient.Models;

/// <summary>Reset mode — maps directly onto LibGit2Sharp's <c>ResetMode</c>.</summary>
public enum ResetKind
{
    /// <summary>Move HEAD only; index and working tree are kept.</summary>
    Soft,

    /// <summary>Move HEAD and reset the index; working tree is kept (default <c>git reset</c>).</summary>
    Mixed,

    /// <summary>Move HEAD and reset both index and working tree (destructive).</summary>
    Hard,
}
