namespace TechTeaStudio.GitClient.App.ViewModels;

using TechTeaStudio.GitClient.Models;

/// <summary>UI projection of <see cref="CommitInfo"/> for the commit-list view.</summary>
public sealed class CommitListItem
{
    public CommitListItem(CommitInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        Source = info;
        Sha = info.Sha;
        ShortSha = info.Sha.Length >= 7 ? info.Sha[..7] : info.Sha;
        Author = info.Author;
        Date = info.When.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        Summary = info.Summary;
        Message = info.Message;
        Parents = info.Parents;
    }

    public CommitInfo Source { get; }
    public string Sha { get; }
    public string ShortSha { get; }
    public string Author { get; }
    public string Date { get; }
    public string Summary { get; }
    public string Message { get; }
    public IReadOnlyList<string> Parents { get; }
    public string ParentsText => Parents.Count == 0 ? "(root)" : string.Join(", ", Parents.Select(p => p.Length >= 7 ? p[..7] : p));
}
