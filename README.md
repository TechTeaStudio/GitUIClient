<p align="center">
  <img src="https://raw.githubusercontent.com/TechTeaStudio/GitUIClient/product/icon.png" alt="TechTeaStudio.GitClient logo" width="160" />
</p>

<h1 align="center">TechTeaStudio.GitClient</h1>

<p align="center">
  Desktop Git GUI on Avalonia 11 and LibGit2Sharp. Multi-targeted Core library, single-window Avalonia app with chromeless title bar and a liquid-glass acrylic backdrop.
</p>

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet&amp;logoColor=white" />
  <img alt="Avalonia" src="https://img.shields.io/badge/Avalonia-11.3-883?logo=avalonia&amp;logoColor=white" />
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue" />
  <a href="https://github.com/TechTeaStudio/GitUIClient/actions/workflows/dotnet.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/TechTeaStudio/GitUIClient/dotnet.yml?branch=product&amp;logo=github&amp;label=build" /></a>
  <a href="LICENSE.txt"><img alt="License" src="https://img.shields.io/badge/license-MIT-blue.svg" /></a>
  <img alt="Tests" src="https://img.shields.io/badge/tests-120%20passing-brightgreen" />
  <img alt="Version" src="https://img.shields.io/badge/version-0.4.0-9D2499" />
</p>

## About

Two-project solution. `TechTeaStudio.GitClient.Core` wraps LibGit2Sharp behind 21 interface-shaped services (one per concern: repositories, commits, branches, tags, remotes, sync, rewrites, stash, amend, clean, init, tree, blame, file diff, conflicts, patches, submodules, reflog, contributors, notes). Each handle owns a `SemaphoreSlim` so LibGit2Sharp's non-thread-safe `Repository` is always touched from one thread. `TechTeaStudio.GitClient.App` is an Avalonia desktop front-end that composes those services through `CompositionRoot` (no `Microsoft.Extensions.DependencyInjection`).

Read paths are in-process LibGit2. Three write paths shell out to the system `git` because LibGit2Sharp has no public API for them: submodule add, `git apply`, `git format-patch`.

## Run

```powershell
dotnet build GitClient.slnx
dotnet run --project src/TechTeaStudio.GitClient.App
dotnet test  GitClient.slnx
```

Requires .NET SDK 10. Core multi-targets `net8.0;net9.0;net10.0`. App is `net10.0` (`WinExe`). Tests run against temp-folder repos via `Repository.Init`; no network access required.

## Solution layout

```
src/TechTeaStudio.GitClient.Core/
├── Models/                  DTO records (CommitInfo, BranchInfo, RepoStatus, ...)
├── Repositories/            IRepositoryService, ICommitService, IRepoHandle
├── Branching/               IBranchOpsService, ITagService
├── Sync/                    IRemoteService, ISyncService
├── Rewrites/                IRewriteService
├── WorkingTree/             IStashService, IAmendService, ICleanService, IInitService
├── Inspection/              ITreeService, IBlameService, IFileDiffService
└── Repo/                    IConflictsService, IPatchService, ISubmoduleService,
                             IReflogService, IContributorsService, INotesService

src/TechTeaStudio.GitClient.App/
├── App.axaml, Program.cs, CompositionRoot.cs
├── Themes/HyperionTheme.axaml    palette tokens (Dark + Light)
├── Themes/Spinner.axaml          rotating MaterialIcon style
├── Views/                        MainWindow + 27 dialogs + 3 inspection windows
├── ViewModels/                   MainViewModel, CommitListItem, FileSelection, ...
├── Controls/, Graph/, Services/  commit-graph control, lane builder, IGravatarService
└── Assets/                       app.ico + Icons/*.png

tests/TechTeaStudio.GitClient.Core.Tests/    xUnit, 120 passing, 1 skipped
.github/workflows/dotnet.yml                  CI: restore + build + test on product
```

## Services

| Concern | Interface | Implementation |
|---|---|---|
| Repository (open / init / clone / branches / commits / diff / status) | `IRepositoryService` | `LibGit2RepositoryService` |
| Repo handle (per-handle async lock) | `IRepoHandle` | `LibGit2RepoHandle` |
| Commits (stage + commit) | `ICommitService` | `LibGit2CommitService` |
| Branches (create / rename / delete / checkout) | `IBranchOpsService` | `LibGit2BranchOpsService` |
| Tags (lightweight + annotated) | `ITagService` | `LibGit2TagService` |
| Remotes (list / add / remove / rename / `ls-remote`) | `IRemoteService` | `LibGit2RemoteService` |
| Sync (fetch / pull / push) | `ISyncService` | `LibGit2SyncService` |
| Rewrites (merge / rebase / revert / cherry-pick / reset) | `IRewriteService` | `LibGit2RewriteService` |
| Stash (save / list / apply / pop / drop) | `IStashService` | `LibGit2StashService` |
| Amend (HEAD message + re-stage) | `IAmendService` | `LibGit2AmendService` |
| Clean (list + delete untracked) | `ICleanService` | `LibGit2CleanService` |
| Init (non-bare or bare) | `IInitService` | `LibGit2InitService` |
| Tree (list at commit + read blob) | `ITreeService` | `LibGit2TreeService` |
| Blame (per-line) | `IBlameService` | `LibGit2BlameService` |
| File diff (hunks + classified lines) | `IFileDiffService` | `LibGit2FileDiffService` |
| Conflicts (list + resolve ours / theirs) | `IConflictsService` | `LibGit2ConflictsService` |
| Patches (apply + format-patch) | `IPatchService` | `GitPatchService` (uses `git`) |
| Submodules (list + add) | `ISubmoduleService` | `LibGit2SubmoduleService` (uses `git` for add) |
| Reflog | `IReflogService` | `LibGit2ReflogService` |
| Contributors | `IContributorsService` | `LibGit2ContributorsService` |
| Notes | `INotesService` | `LibGit2NotesService` |

All 21 services are bundled into a `GitServices` record exposed through `MainViewModel.Services`.

## Capabilities

| Area | What ships |
|---|---|
| Repository ops | Open / init / clone (progress + cancellation) |
| Branches | List / create / rename / delete / checkout |
| Tags | List / create lightweight / create annotated / delete |
| Commits | List per ref / sha, parents, full diff between two shas, stage all or pathspec, commit |
| Working tree | Status grouped Added / Modified / Deleted / Untracked, conflicts (partial), clean |
| Remotes | List / add / remove / rename, `ls-remote` |
| Sync | Fetch / pull / push (with `--force`), `IProgress<SyncProgress>`, optional `Credentials` |
| Rewrites | Merge / rebase (start, continue, skip, abort) / revert / cherry-pick / reset (Soft, Mixed, Hard) |
| Stash | Save / list / apply / pop / drop |
| Amend | HEAD message rewrite, optional re-stage |
| Submodules | List + add (CLI shell-out for add) |
| Patches | Apply, format-patch (CLI shell-out) |
| Inspection | Diff viewer, tree browser, blame, contributors, reflog, notes |

## UI

- `SystemDecorations="None"` + `ExtendClientAreaToDecorationsHint` + `ExtendClientAreaChromeHints="NoChrome"`. 36-px custom title bar carries the app icon, title, current repo path, and the minimize / maximize / close buttons. Drag anywhere on the bar to move the window; double-click to maximize.
- `ExperimentalAcrylicBorder` paints a tinted backdrop using the host-OS blur (`AcrylicBlur, Mica, Blur`). Cards over it use semi-transparent fills, soft rims, rounded corners. Falls back to a flat surface on platforms without acrylic.
- Single palette file: `Themes/HyperionTheme.axaml` holds every `Hyperion.*`, `Glass.*`, and `Chrome.*` token under `Dark` and `Light` `ThemeDictionaries`. App-bar toggle flips the variant.
- Spinners: rotating `MaterialIcon` style in `Themes/Spinner.axaml`. Big spinner on the empty state during Open / Clone / Init; small inline spinners next to the BRANCH and COMMITS section headers while history loads.
- Empty state: when no repo is open every repo-dependent menu is disabled (`IsEnabled="{Binding HasRepository}"`), the workspace is replaced by a centred Branches glyph plus OPEN / CLONE / INIT buttons.
- Three-column main window: branches + working tree (left), commit list with coloured DAG graph (centre), commit details (right).
- Three inspection windows: `DiffViewerWindow`, `TreeBrowserWindow`, `BlameWindow`.
- Gravatar avatars in the commit list (32px) and detail panel (48px) via `IGravatarService`, memoised per (email, size), silent fallback on network failure.

## Icons

Eight 512&times;512 PNGs in `Assets/Icons/`, wired into menu items, the commit button, the title bar, and the empty state.

| File | Used for |
|---|---|
| `branches.png` | App icon (`Assets/app.ico` is the multi-res 16..256 build), title-bar glyph, empty-state hero, BRANCH section header |
| `branch.png` | Branch → Create |
| `delete.png` | Branch → Delete |
| `merge.png` | Rewrite → Merge |
| `pull.png` | Repo → Pull |
| `deployment.png` | Repo → Push |
| `compare.png` | Inspect → Diff |
| `commit-git.png` | Commit button |

## Threading

LibGit2Sharp is not thread-safe. `LibGit2RepoHandle` owns a `SemaphoreSlim(1, 1)`; every service method takes the lock before touching the underlying `Repository` and releases in `finally`. The `Repository` is `internal`-only on the handle, so the UI layer cannot reach it directly. `Dispose` waits on the lock before disposing the `Repository`. `CloneAsync` and the sync operations run on `Task.Run`; the rest return `Task.FromResult(...)` rather than pretend the work is async.

## Embedding the Core

```csharp
using TechTeaStudio.GitClient.Repositories;
using TechTeaStudio.GitClient.Models;

var repo = new LibGit2RepositoryService();
using var handle = await repo.OpenAsync("D:/path/to/repo", ct);

var branches = await repo.GetBranchesAsync(handle, ct);
var commits  = await repo.GetCommitsAsync(handle, "refs/heads/main", take: 50, ct);
var diff     = await repo.GetDiffAsync(handle, fromSha, toSha, ct);
var status   = await repo.GetStatusAsync(handle, ct);

var commitSvc = new LibGit2CommitService();
await commitSvc.CommitAsync(handle,
    message : "v0.3.0 ...",
    author  : new AuthorInfo("You", "you@example.com"),
    pathSpec: null,
    ct);
```

Core is not published to NuGet. Reference it as a `ProjectReference` or build a local package with `dotnet pack`.

## Versioning

Two `<Version>X.Y.Z</Version>` declarations bump together:

- `src/TechTeaStudio.GitClient.Core/TechTeaStudio.GitClient.Core.csproj`
- `src/TechTeaStudio.GitClient.App/TechTeaStudio.GitClient.App.csproj`

Rules: bug fix &rarr; `Z + 1`; new feature, source-compatible &rarr; `Y + 1`, reset `Z`; breaking change after `1.0` &rarr; `X + 1`, reset `Y, Z`.

Commit format: `vX.Y.Z <short description>` (one line, &le; 72 chars).

Push to `product` triggers `.github/workflows/dotnet.yml` (restore + build + test). The repo does not publish to NuGet.

## Development

See [QUICKSTART.md](QUICKSTART.md) for embedding the Core library and [CHANGELOG.md](CHANGELOG.md) for release notes.

## License

[MIT](LICENSE.txt). Copyright &copy; Tech Tea Studio.

<p align="center">
  Part of the Hyperion Ecosystem by <a href="https://techteastudio.cc">TechTeaStudio</a>.
</p>
