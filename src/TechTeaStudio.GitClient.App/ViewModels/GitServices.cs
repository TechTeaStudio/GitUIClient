namespace TechTeaStudio.GitClient.App.ViewModels;

using TechTeaStudio.GitClient.Branching;
using TechTeaStudio.GitClient.Inspection;
using TechTeaStudio.GitClient.Repo;
using TechTeaStudio.GitClient.Rewrites;
using TechTeaStudio.GitClient.Sync;
using TechTeaStudio.GitClient.WorkingTree;

/// <summary>
/// Bundle of every git-domain service the main view model uses. Lets the constructor stay
/// signature-stable while we keep adding capabilities — and keeps the existing tests
/// (which pass null) working.
/// </summary>
public sealed record GitServices(
    IRemoteService Remotes,
    ISyncService Sync,
    IBranchOpsService BranchOps,
    ITagService Tags,
    IRewriteService Rewrites,
    IStashService Stash,
    IAmendService Amend,
    ICleanService Clean,
    IInitService Init,
    ITreeService Tree,
    IBlameService Blame,
    IFileDiffService FileDiff,
    ISubmoduleService Submodules,
    IReflogService Reflog,
    IConflictsService Conflicts,
    IPatchService Patches,
    IContributorsService Contributors,
    INotesService Notes);
