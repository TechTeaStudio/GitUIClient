namespace TechTeaStudio.GitClient.App.ViewModels;

using Avalonia.Media.Imaging;
using TechTeaStudio.GitClient.App.Graph;
using TechTeaStudio.GitClient.Models;

/// <summary>UI projection of <see cref="CommitInfo"/> for the commit-list view.</summary>
public sealed class CommitListItem : ObservableObject
{
    private Bitmap? _avatar;
    private CommitGraphRow? _graphRow;

    public CommitListItem(CommitInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        Source = info;
        Sha = info.Sha;
        ShortSha = info.Sha.Length >= 7 ? info.Sha[..7] : info.Sha;
        Author = info.Author;
        Email = info.Email;
        Date = info.When.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
        Summary = info.Summary;
        Message = info.Message;
        Parents = info.Parents;
    }

    public CommitInfo Source { get; }
    public string Sha { get; }
    public string ShortSha { get; }
    public string Author { get; }
    public string Email { get; }
    public string Date { get; }
    public string Summary { get; }
    public string Message { get; }
    public IReadOnlyList<string> Parents { get; }
    public string ParentsText => Parents.Count == 0 ? "(root)" : string.Join(", ", Parents.Select(p => p.Length >= 7 ? p[..7] : p));

    public Bitmap? Avatar
    {
        get => _avatar;
        set => SetField(ref _avatar, value);
    }

    public CommitGraphRow? GraphRow
    {
        get => _graphRow;
        set => SetField(ref _graphRow, value);
    }
}
