namespace TechTeaStudio.GitClient.App.Services;

using Avalonia.Media.Imaging;

/// <summary>
/// Resolves Gravatar avatars by email. Returns null on network / decode failure so
/// the UI can fall back to a placeholder. Implementations cache by (email, size).
/// </summary>
public interface IGravatarService
{
    Task<Bitmap?> GetAvatarAsync(string email, int size = 64, CancellationToken ct = default);
}
