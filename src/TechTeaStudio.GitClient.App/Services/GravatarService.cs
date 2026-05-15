namespace TechTeaStudio.GitClient.App.Services;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Media.Imaging;

/// <summary>
/// Resolves Gravatar avatars by author email. MD5(trim+lowercase(email)) — Gravatar's hashing
/// convention since the original API (the newer SHA-256 endpoint is supported too, but MD5
/// remains universally accepted and is what Gravatar publishes in its docs as the canonical hash).
///
/// Returns null on network / decode failure — callers should fall back to a placeholder. The
/// `d=identicon` query so unknown emails still get a unique generated tile instead of a
/// default head silhouette.
/// </summary>
public sealed class GravatarService : IGravatarService, IDisposable
{
    private const string BaseUrl = "https://www.gravatar.com/avatar/";
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly ConcurrentDictionary<string, Task<Bitmap?>> _cache = new(StringComparer.Ordinal);

    public GravatarService() : this(new HttpClient { Timeout = TimeSpan.FromSeconds(8) }, ownsHttp: true) { }

    public GravatarService(HttpClient http, bool ownsHttp = false)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _ownsHttp = ownsHttp;
    }

    public Task<Bitmap?> GetAvatarAsync(string email, int size = 64, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult<Bitmap?>(null);

        var hash = Md5Hex(email.Trim().ToLowerInvariant());
        var key = $"{hash}@{size}";
        return _cache.GetOrAdd(key, _ => FetchAsync(hash, size, ct));
    }

    private async Task<Bitmap?> FetchAsync(string hash, int size, CancellationToken ct)
    {
        try
        {
            var url = $"{BaseUrl}{hash}?s={size}&d=identicon";
            await using var stream = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
            ms.Position = 0;
            return new Bitmap(ms);
        }
        catch
        {
            return null;
        }
    }

    private static string Md5Hex(string input)
    {
        Span<byte> hash = stackalloc byte[16];
        MD5.HashData(Encoding.UTF8.GetBytes(input), hash);
        var sb = new StringBuilder(32);
        foreach (var b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}
