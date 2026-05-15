namespace TechTeaStudio.GitClient.App;

using TechTeaStudio.GitClient.App.ViewModels;
using TechTeaStudio.GitClient.Repositories;

/// <summary>
/// Tiny manual-DI composition root. Avoids pulling in
/// Microsoft.Extensions.DependencyInjection for what amounts to four singletons.
/// Worker C may extend this — but the surface stays small.
/// </summary>
internal static class CompositionRoot
{
    internal static MainViewModel BuildMainViewModel()
    {
        IRepositoryService repos = new LibGit2RepositoryService();
        ICommitService commits = new LibGit2CommitService();
        return new MainViewModel(repos, commits);
    }
}
