namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Options for <c>IRewriteService.MergeAsync</c>.
///
/// <para><see cref="FastForwardStrategy"/> values:</para>
/// <list type="bullet">
///   <item><term><c>default</c></term><description>Fast-forward when possible, otherwise create a merge commit.</description></item>
///   <item><term><c>fastForwardOnly</c></term><description>Only succeed if a fast-forward is possible.</description></item>
///   <item><term><c>noFastForward</c></term><description>Always create a merge commit, even if a fast-forward is possible.</description></item>
/// </list>
/// </summary>
public sealed record MergeOptions
{
    /// <summary>One of <c>"default"</c>, <c>"fastForwardOnly"</c>, <c>"noFastForward"</c>. Case-insensitive.</summary>
    public string FastForwardStrategy { get; init; } = "default";

    /// <summary>Squash the source history into a single index update without a merge commit.</summary>
    public bool Squash { get; init; }

    /// <summary>Auto-create the merge commit on success (when applicable).</summary>
    public bool CommitOnSuccess { get; init; } = true;

    /// <summary>Optional custom merge-commit message. <c>null</c> uses LibGit2's default.</summary>
    public string? CustomMessage { get; init; }
}
