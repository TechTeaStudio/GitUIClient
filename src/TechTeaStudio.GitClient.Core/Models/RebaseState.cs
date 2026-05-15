namespace TechTeaStudio.GitClient.Models;

/// <summary>State of an in-progress (or just-finished) rebase, as reported by <c>IRewriteService</c>.</summary>
public enum RebaseState
{
    /// <summary>Rebase finished cleanly; HEAD is now on the rebased commits.</summary>
    Complete,

    /// <summary>Current step produced conflicts and is waiting on the user to resolve and continue.</summary>
    Conflicts,

    /// <summary>Rebase has more steps to run; call <c>RebaseContinueAsync</c> after resolving anything outstanding.</summary>
    InProgress,
}
