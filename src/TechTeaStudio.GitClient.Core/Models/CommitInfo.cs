namespace TechTeaStudio.GitClient.Models;

/// <summary>One commit: sha, author, message, parent shas (for graph drawing).</summary>
public sealed record CommitInfo
{
    public required string Sha { get; init; }
    public required string Author { get; init; }
    public required string Email { get; init; }
    public required DateTimeOffset When { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<string> Parents { get; init; }

    /// <summary>First line of <see cref="Message"/>, useful for the commit-list UI.</summary>
    public string Summary
    {
        get
        {
            var s = Message ?? string.Empty;
            var newline = s.IndexOfAny(['\r', '\n']);
            return newline < 0 ? s.TrimEnd() : s[..newline].TrimEnd();
        }
    }
}
