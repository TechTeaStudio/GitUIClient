#if NET11_0_OR_GREATER
namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Outcome case of a revert whose inverse changes nothing: the commit's effect
/// is already absent from HEAD. No commit was made and the index is untouched.
/// </summary>
public sealed record NothingToRevert;
#endif
