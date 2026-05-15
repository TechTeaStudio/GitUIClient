namespace TechTeaStudio.GitClient.Repo;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Read + write submodule operations.
///
/// LibGit2Sharp's submodule API is intentionally narrow (init / update / list);
/// <c>git submodule add</c> isn't exposed at all, so <see cref="AddSubmoduleAsync"/>
/// shells out to the <c>git</c> binary. Callers should be ready for it to throw
/// when <c>git</c> is not on <c>PATH</c>.
/// </summary>
public interface ISubmoduleService
{
    /// <summary>List every submodule registered in the parent repo.</summary>
    Task<IReadOnlyList<SubmoduleInfo>> ListSubmodulesAsync(
        IRepoHandle handle,
        CancellationToken ct = default);

    /// <summary>
    /// Register a new submodule at <paramref name="path"/> tracking
    /// <paramref name="url"/>. Implemented via <c>git submodule add</c>.
    /// </summary>
    Task AddSubmoduleAsync(
        IRepoHandle handle,
        string url,
        string path,
        CancellationToken ct = default);

    /// <summary>
    /// Update (clone-or-fetch + checkout) a registered submodule.
    /// When <paramref name="init"/> is <c>true</c>, run init first.
    /// </summary>
    Task UpdateSubmoduleAsync(
        IRepoHandle handle,
        string name,
        bool init,
        CancellationToken ct = default);
}
