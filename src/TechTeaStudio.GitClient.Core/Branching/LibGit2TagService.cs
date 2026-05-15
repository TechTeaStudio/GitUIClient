namespace TechTeaStudio.GitClient.Branching;

using LibGit2Sharp;

using TechTeaStudio.GitClient.Models;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// LibGit2Sharp-backed implementation of <see cref="ITagService"/>.
///
/// Concurrency: every method takes the handle's async lock before touching
/// the underlying <see cref="Repository"/>. LibGit2Sharp is not thread-safe.
/// </summary>
public sealed class LibGit2TagService : ITagService
{
    public async Task<IReadOnlyList<TagInfo>> ListTagsAsync(
        IRepoHandle handle,
        CancellationToken ct = default)
    {
        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var list = new List<TagInfo>();
            foreach (var t in h.Repository.Tags)
            {
                ct.ThrowIfCancellationRequested();
                list.Add(MapTag(t));
            }
            return list;
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<TagInfo> CreateLightweightTagAsync(
        IRepoHandle handle,
        string name,
        string targetSha,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            // ApplyTag(name, objectish) -> lightweight tag pointing at the resolved object.
            var tag = repo.ApplyTag(name, targetSha);
            return MapTag(tag);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task<TagInfo> CreateAnnotatedTagAsync(
        IRepoHandle handle,
        string name,
        string targetSha,
        string message,
        AuthorInfo tagger,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(tagger);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagger.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagger.Email);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            var sig = new Signature(
                tagger.Name,
                tagger.Email,
                tagger.When ?? DateTimeOffset.UtcNow);

            var tag = repo.ApplyTag(name, targetSha, sig, message);
            return MapTag(tag);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    public async Task DeleteTagAsync(
        IRepoHandle handle,
        string name,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var h = LibGit2RepositoryService.CastHandle(handle);
        await h.AsyncLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var repo = h.Repository;
            if (repo.Tags[name] is null)
                throw new ArgumentException($"Tag '{name}' does not exist.", nameof(name));

            repo.Tags.Remove(name);
        }
        finally
        {
            h.AsyncLock.Release();
        }
    }

    private static TagInfo MapTag(Tag t)
    {
        // PeeledTarget walks through any annotation indirection to the underlying commit;
        // for a lightweight tag PeeledTarget == Target.
        var targetSha = t.PeeledTarget?.Sha ?? t.Target.Sha;

        if (!t.IsAnnotated || t.Annotation is null)
        {
            return new TagInfo
            {
                Name = t.FriendlyName,
                TargetSha = targetSha,
                IsAnnotated = false,
            };
        }

        var annotation = t.Annotation;
        var taggerSig = annotation.Tagger;
        var tagger = taggerSig is null
            ? null
            : new AuthorInfo
            {
                Name = taggerSig.Name,
                Email = taggerSig.Email,
                When = taggerSig.When,
            };

        return new TagInfo
        {
            Name = t.FriendlyName,
            TargetSha = targetSha,
            IsAnnotated = true,
            Message = annotation.Message,
            Tagger = tagger,
        };
    }
}
