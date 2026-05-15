namespace TechTeaStudio.GitClient.Branching;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Write-side branch operations: create / rename / delete / checkout.
///
/// The read side (listing branches) stays on <see cref="IRepositoryService"/>.
/// </summary>
public interface IBranchOpsService
{
    /// <summary>
    /// Create a local branch named <paramref name="name"/> at
    /// <paramref name="startPointSha"/> (or HEAD if null). Optionally check it
    /// out so HEAD points at the new branch.
    /// </summary>
    Task<BranchInfo> CreateBranchAsync(
        IRepoHandle handle,
        string name,
        string? startPointSha = null,
        bool checkout = false,
        CancellationToken ct = default);

    /// <summary>Rename a local branch.</summary>
    Task<BranchInfo> RenameBranchAsync(
        IRepoHandle handle,
        string oldName,
        string newName,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a local branch. If the branch isn't merged into HEAD,
    /// <paramref name="force"/> must be <c>true</c> or the operation throws.
    /// </summary>
    Task DeleteBranchAsync(
        IRepoHandle handle,
        string name,
        bool force = false,
        CancellationToken ct = default);

    /// <summary>Check out a branch — moves HEAD and updates the working tree.</summary>
    Task CheckoutBranchAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default);
}
