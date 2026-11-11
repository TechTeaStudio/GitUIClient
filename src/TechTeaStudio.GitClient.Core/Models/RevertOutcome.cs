#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// What a revert did: created the inverse commit (<see cref="Committed"/>), left
/// the inverse in the index (<see cref="Staged"/>), stopped on conflicts
/// (<see cref="Conflicted"/>) or found nothing to undo
/// (<see cref="NothingToRevert"/>). Available on net11.0 only.
/// </summary>
public union RevertOutcome(Committed, Staged, Conflicted, NothingToRevert);
#endif
