# Quickstart — TechTeaStudio.GitClient

Avalonia 11 desktop git client + Core library wrapping `LibGit2Sharp` 0.31.0. Read-mostly v0.1.0: open / init / clone repos, list branches, show commits and diffs, see status, make commits. Push / pull / merge are explicitly deferred to v0.2+.

## Prerequisites
- .NET SDK 10.0+
- (No git CLI required — LibGit2Sharp ships its own native libraries.)

## Build
```powershell
dotnet build GitClient.slnx
```

## Test
```powershell
dotnet test GitClient.slnx
```
Expected: 19 passed, 0 failed. Tests spin up temporary repos in `%TEMP%` and clean them up; no internet access needed.

## Run the app
```powershell
dotnet run --project src/TechTeaStudio.GitClient.App
```
The single window has:
- Path textbox + **Open** / **Clone** buttons
- Branch dropdown
- Commit list (date / author / first line of message)
- Selected-commit detail panel
- Status panel (Added / Modified / Deleted / Untracked)
- Commit-message textbox + **Commit selected files** button

## v0.1.0 capabilities
- Open existing repos · Init new repos · Clone (with progress + cancellation)
- List branches with tip-commit shas · List commits per ref · Get unified diff between two commits
- Per-file working-tree status · Local commit (all paths or specific pathspec)

## Deferred to v0.2+
- Push / Pull (need credential handling for GitHub / GitLab / Bitbucket OAuth + SSH)
- Merge + merge-conflict visualisation
- Console tab for raw git commands
- File-explorer integration
- Visual commit-graph DAG

## Embed the services in your own app
```csharp
using var repo = new LibGit2RepositoryService();
using var handle = await repo.OpenAsync("D:/path/to/repo", ct);

var branches = await repo.GetBranchesAsync(handle);
var commits  = await repo.GetCommitsAsync(handle, "refs/heads/main", take: 50);
var diff     = await repo.GetDiffAsync(handle, fromSha, toSha);
var status   = await repo.GetStatusAsync(handle);

var commitSvc = new LibGit2CommitService();
await commitSvc.CommitAsync(handle,
    message: "v0.1.0 …",
    author:  new AuthorInfo("You", "you@example.com"),
    pathSpec: null,
    ct);
```

## Threading note
`LibGit2Sharp` is **not** thread-safe — a single `IRepoHandle` must be used from one thread only. The Core services document this; the Avalonia App stays on the UI dispatcher for repo operations.
