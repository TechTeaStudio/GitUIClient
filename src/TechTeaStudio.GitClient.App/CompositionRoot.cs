namespace TechTeaStudio.GitClient.App;

using TechTeaStudio.GitClient.App.Services;
using TechTeaStudio.GitClient.App.ViewModels;
using TechTeaStudio.GitClient.Branching;
using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.Rewrites;
using TechTeaStudio.GitClient.Sync;
using TechTeaStudio.GitClient.WorkingTree;

/// <summary>
/// Tiny manual-DI composition root. Avoids pulling in
/// Microsoft.Extensions.DependencyInjection for what amounts to ~20 singletons.
/// </summary>
internal static class CompositionRoot
{
    internal static MainViewModel BuildMainViewModel()
    {
        IRepositoryService repos = new LibGit2RepositoryService();
        ICommitService commits = new LibGit2CommitService();
        IGravatarService gravatar = new GravatarService();

        var services = new GitServices(
            Remotes: new LibGit2RemoteService(),
            Sync: new LibGit2SyncService(),
            BranchOps: new LibGit2BranchOpsService(),
            Tags: new LibGit2TagService(),
            Rewrites: new LibGit2RewriteService(),
            Stash: new LibGit2StashService(),
            Amend: new LibGit2AmendService(),
            Clean: new LibGit2CleanService(),
            Init: new LibGit2InitService(),
            Tree: new LibGit2TreeService(),
            Blame: new LibGit2BlameService(),
            FileDiff: new LibGit2FileDiffService(),
            Submodules: new LibGit2SubmoduleService(),
            Reflog: new LibGit2ReflogService(),
            Conflicts: new LibGit2ConflictsService(),
            Patches: new GitPatchService(),
            Contributors: new LibGit2ContributorsService(),
            Notes: new LibGit2NotesService());

        return new MainViewModel(repos, commits, gravatar, services);
    }
}
