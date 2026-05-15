namespace TechTeaStudio.GitClient.WorkingTree;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Stash stack operations: save (push), list, apply, pop, drop. Newest stash is index 0.
/// </summary>
public interface IStashService
{
    /// <summary>
    /// Save the current working-tree state into the stash stack and return the new entry.
    /// When <paramref name="includeUntracked"/> is true, untracked files are stashed too.
    /// </summary>
    Task<StashInfo> SaveStashAsync(
        IRepoHandle handle,
        string? message,
        AuthorInfo stasher,
        bool includeUntracked,
        CancellationToken ct = default);

    /// <summary>List the stash stack, newest first (index 0).</summary>
    Task<IReadOnlyList<StashInfo>> ListStashesAsync(IRepoHandle handle, CancellationToken ct = default);

    /// <summary>Apply the stash at <paramref name="index"/> onto the current working tree, without removing it from the stack.</summary>
    Task ApplyStashAsync(IRepoHandle handle, int index, CancellationToken ct = default);

    /// <summary>Apply the stash at <paramref name="index"/> and remove it from the stack on success.</summary>
    Task PopStashAsync(IRepoHandle handle, int index, CancellationToken ct = default);

    /// <summary>Remove the stash at <paramref name="index"/> without applying it.</summary>
    Task DropStashAsync(IRepoHandle handle, int index, CancellationToken ct = default);
}
