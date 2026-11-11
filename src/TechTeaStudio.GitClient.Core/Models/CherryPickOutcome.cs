#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// What a cherry-pick did: created a commit (<see cref="Committed"/>), left the
/// change in the index (<see cref="Staged"/>) or stopped on conflicts
/// (<see cref="Conflicted"/>). Available on net11.0 only.
/// </summary>
public union CherryPickOutcome(Committed, Staged, Conflicted);
#endif
