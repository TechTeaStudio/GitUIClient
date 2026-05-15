namespace TechTeaStudio.GitClient.Sync;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Remote management — add / remove / rename remotes and list their advertised refs.
/// Network-touching operations (fetch / pull / push) live on <see cref="ISyncService"/>.
/// </summary>
public interface IRemoteService
{
    /// <summary>Enumerate the remotes configured for this repository.</summary>
    Task<IReadOnlyList<RemoteInfo>> ListRemotesAsync(
        IRepoHandle handle,
        CancellationToken ct = default);

    /// <summary>Register a new remote <paramref name="name"/> pointing at <paramref name="url"/>.</summary>
    Task AddRemoteAsync(
        IRepoHandle handle,
        string name,
        string url,
        CancellationToken ct = default);

    /// <summary>Drop a configured remote.</summary>
    Task RemoveRemoteAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default);

    /// <summary>Rename a configured remote.</summary>
    Task RenameRemoteAsync(
        IRepoHandle handle,
        string oldName,
        string newName,
        CancellationToken ct = default);

    /// <summary>
    /// Equivalent of <c>git ls-remote</c> — ask the remote for its advertised refs without
    /// downloading objects. Requires network access.
    /// </summary>
    Task<IReadOnlyList<RemoteRefInfo>> ListRemoteRefsAsync(
        IRepoHandle handle,
        string remoteName,
        CancellationToken ct = default);
}
