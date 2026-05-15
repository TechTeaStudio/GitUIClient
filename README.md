<p align="center">
  <img src="https://raw.githubusercontent.com/TechTeaStudio/GitUIClient/product/icon.png" alt="TechTeaStudio.GitClient logo" width="160" />
</p>

<h1 align="center">TechTeaStudio.GitClient</h1>

<p align="center">
  Avalonia desktop Git client built on LibGit2Sharp. Branches, tags, remotes, sync, stash, rebase, merge, cherry-pick, blame, tree browser, diff viewer, conflicts, patches, submodules, notes &mdash; Material Design 3 UI, multi-targeted Core library, no system <code>git</code> on the read path.
</p>

<p align="center">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet&amp;logoColor=white" />
  <img alt="Avalonia" src="https://img.shields.io/badge/Avalonia-11.3-883?logo=avalonia&amp;logoColor=white" />
  <a href="https://github.com/TechTeaStudio/GitUIClient/actions/workflows/dotnet.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/TechTeaStudio/GitUIClient/dotnet.yml?branch=product&amp;logo=github&amp;label=build" /></a>
  <a href="LICENSE.txt"><img alt="License" src="https://img.shields.io/badge/license-MIT-blue.svg" /></a>
</p>

## Overview

`TechTeaStudio.GitClient` is a desktop Git GUI written from scratch in Avalonia 11 against `LibGit2Sharp` 0.31.0. The Core library wraps `LibGit2Sharp` behind a small set of interface-shaped services (one per concern: repository, branches, tags, commits, remotes, sync, rewrites, stash, conflicts, blame, tree, diff, submodules, reflog, contributors, notes, patches), each respecting LibGit2Sharp's not-thread-safe `Repository` by serialising access through a per-handle `SemaphoreSlim`. The Avalonia App composes those services through a hand-rolled `CompositionRoot` (no `Microsoft.Extensions.DependencyInjection` dependency) and exposes them through a top-level `Menu` plus 27 dialogs.

The product targets a niche left open after the original Gitter went unmaintained: a free, open-source, .NET-native, cross-platform Git GUI that doesn't shell out to the system `git` for ordinary read paths. Three specific operations &mdash; submodule add, patch apply, and `format-patch` &mdash; do shell out, because LibGit2Sharp does not expose a public API for them; everything else is in-process LibGit2.

## When to reach for it

You want this client when:

- You're on Windows / macOS / Linux and want a free, open-source Git GUI with the day-to-day surface of SourceTree or GitKraken (branches, commits, diff, status, stage, commit, push, pull, merge, rebase, cherry-pick, stash, tags, remotes, blame, tree browse).
- You're a .NET developer and would rather hack on a Git GUI written in C# / Avalonia than in Electron + JavaScript.
- You want a Core library you can embed in your own .NET app to talk to a local repository (open / clone / branch / commit / diff / status / etc.) without re-implementing the LibGit2Sharp threading dance.

You probably **don't** want this client when:

- You need the full feature set of GitKraken Pro (in-app issue trackers, pull-request review, GPG/SSH agent integration UIs, GitFlow visualiser, etc.) &mdash; those are not in scope.
- You're on a server and want a CLI-only experience &mdash; use the `git` CLI directly, or shell out to `LibGit2Sharp` from your script.
- You need NativeAOT today &mdash; LibGit2Sharp's native bindings have not been validated against AOT in this project.

## How it compares

| Capability | TechTeaStudio.GitClient | GitKraken | SourceTree | GitHub Desktop | git CLI |
|---|---|---|---|---|---|
| License | MIT (open source) | Proprietary, freemium | Proprietary, free | MIT (UI), proprietary backend | GPLv2 |
| Cross-platform | Win / macOS / Linux (Avalonia) | Win / macOS / Linux | Win / macOS | Win / macOS | All |
| Stack | C# / .NET 10 / Avalonia 11 | Electron / TypeScript | Qt / native | Electron / TypeScript | C |
| Engine | LibGit2 (in-process) | git CLI shell-out | libgit2 + git CLI | git CLI shell-out | git itself |
| Reusable Core library | Yes (`TechTeaStudio.GitClient.Core`) | No | No | No | No |
| Built-in conflict resolver | Partial (list + ours/theirs) | Yes | Yes | No | No |
| Visual commit graph (DAG) | Yes (lane-based) | Yes | Yes | No | No |
| Diff / blame / tree browser | Yes | Yes | Yes | Partial | Yes (terminal) |
| Submodules | Yes (uses `git` for add) | Yes | Yes | No | Yes |
| Patches (apply / format) | Yes (via `git`) | Partial | Yes | No | Yes |
| Pull-request UX | No (deferred) | Yes (Pro) | Yes (Bitbucket) | Yes (GitHub) | No |

The honest pitch: GitClient sits between **"a hand-rolled `LibGit2Sharp` script"** and **"a full GitKraken install"**. If you want a free .NET-stack desktop client that already does the daily 80% (open / branch / commit / diff / sync / stash / rebase / merge / cherry-pick / blame / tree / conflicts) without paying for a license or installing Electron, this is it.

## Run

The repository ships source only &mdash; no published binary releases yet. Build and run from source:

```powershell
dotnet build GitClient.slnx
dotnet run --project src/TechTeaStudio.GitClient.App
```

Requires .NET SDK 10. The Core library multi-targets `net8.0;net9.0;net10.0`; the App is `net10.0` (`WinExe`). LibGit2Sharp ships its own native libraries, so no system `git` install is required for read paths. The submodule-add, patch-apply, and `format-patch` operations do call out to the `git` CLI &mdash; install one if you intend to use them.

## Quick start (Core library)

The Core library is consumable from any .NET 8 / 9 / 10 project. It is **not** published to NuGet today; reference it as a `ProjectReference` from a sibling repository, or build a local NuGet package via `dotnet pack`.

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

`LibGit2Sharp` is **not** thread-safe &mdash; a single `IRepoHandle` must only be used from one thread at a time. Every public method on every Core service takes the handle's `SemaphoreSlim` async lock before touching the underlying `Repository`. Disposing the handle blocks until any in-flight call finishes.

## Service catalogue (v0.3.0)

| Concern | Interface | Implementation |
|---|---|---|
| Repository (open / init / clone / branches / commits / diff / status) | `IRepositoryService` | `LibGit2RepositoryService` |
| Repo handle (per-handle async lock) | `IRepoHandle` | `LibGit2RepoHandle` |
| Commits (stage + commit) | `ICommitService` | `LibGit2CommitService` |
| Branches (create / rename / delete / checkout / merged-check) | `IBranchOpsService` | `LibGit2BranchOpsService` |
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
| Conflicts (list + resolve ours/theirs) | `IConflictsService` | `LibGit2ConflictsService` |
| Patches (apply + format-patch) | `IPatchService` | `GitPatchService` (uses `git`) |
| Submodules (list + add) | `ISubmoduleService` | `LibGit2SubmoduleService` (uses `git` for add) |
| Reflog | `IReflogService` | `LibGit2ReflogService` |
| Contributors | `IContributorsService` | `LibGit2ContributorsService` |
| Notes | `INotesService` | `LibGit2NotesService` |

All 21 services are bundled into a `GitServices` record exposed through `MainViewModel.Services`, so dialogs can pull what they need by typed property.

## UI surface (v0.3.0)

- Top-level `Menu`: File / Repo / Branch / Tag / Commit / Rewrite / Inspect &mdash; 27 dialogs total.
- Three-column main window: branches + working tree (left), commit list with coloured DAG graph (centre), commit details with full message and parents (right).
- Material Design 3 theme via `Material.Avalonia 3.9.2` with the Hyperion MD3 palette (primary `#9D2499` purple, tertiary `#5EC6C6` teal, dark surfaces `#121218` / `#2A2A2A`). All named brushes mirror Hyperion Omni Client's `common.css` so the desktop client matches the web client visually.
- Material icons everywhere via `Material.Icons.Avalonia 2.4.0` (section headers, branch combobox items, status pill badges, action buttons, detail-field labels).
- Gravatar avatars in the commit list (32px) and the selected-commit panel (48px), resolved via `IGravatarService` with per-(email, size) memoisation and silent fallback on network failure.
- Three inspection windows: `DiffViewerWindow` (file list + unified diff with green/red row colouring), `TreeBrowserWindow` (lazy-loaded tree, read-only blob viewer, binary banner), `BlameWindow` (per-line grid with short-sha copy, avatar initials).

## Capabilities snapshot

| Area | What ships |
|---|---|
| Repository ops | Open / init / clone (with progress + cancellation) |
| Branches | List / create / rename / delete / checkout, current-branch tracking |
| Tags | List / create lightweight / create annotated / delete |
| Commits | List per ref / sha, parents, full diff between two shas, stage all or pathspec, commit |
| Working tree | Status grouped Added / Modified / Deleted / Untracked, conflicts (partial), clean |
| Remotes | List / add / remove / rename, `ls-remote` |
| Sync | Fetch / pull / push (with `--force`), `IProgress<SyncProgress>` reporting, optional `Credentials` |
| Rewrites | Merge / rebase (start, continue, skip, abort) / revert / cherry-pick / reset (Soft, Mixed, Hard) |
| Stash | Save / list / apply / pop / drop |
| Amend | HEAD message rewrite, optional re-stage |
| Submodules | List + add (CLI shell-out for add) |
| Patches | Apply, format-patch (CLI shell-out) |
| Inspection | Diff viewer, tree browser, blame, contributors, reflog, notes |
| Visual | Coloured commit graph (lane algorithm with per-row state), Material Design 3 theme, Gravatar avatars |

## Threading model

`LibGit2Sharp` is **not** thread-safe. The Core library's response is a per-handle async lock:

- `LibGit2RepoHandle` owns a `SemaphoreSlim(1, 1)` async lock.
- Every public method on every Core service calls `await handle.AsyncLock.WaitAsync(ct)` before touching the underlying `Repository`, and releases in `finally`.
- The `Repository` is `internal`-only on the handle; the UI layer can never reach it directly.
- `Dispose` on the handle synchronously waits on the lock, then disposes the `Repository`. Disposing while a service call is in flight blocks the disposing thread until the call completes &mdash; the safe choice, since disposing mid-flight would crash the libgit2 native side.
- Long operations (`CloneAsync`, `SyncService` push / pull / fetch) run on `Task.Run`. Synchronous wrappers (most read paths) return `Task.FromResult(...)` &mdash; the library is honest about LibGit2Sharp being synchronous-bound and does not pretend otherwise.

## Project layout

```
GitUIClient/
├── src/TechTeaStudio.GitClient.Core/                 <- reusable .NET library
│   ├── Models/                                       <- DTO records: CommitInfo, BranchInfo, RepoStatus,
│   │                                                    FileChange, CloneProgress, ...
│   ├── Repositories/                                 <- IRepositoryService, ICommitService, IRepoHandle
│   ├── Branching/                                    <- IBranchOpsService, ITagService
│   ├── Sync/                                         <- IRemoteService, ISyncService
│   ├── Rewrites/                                     <- IRewriteService
│   ├── WorkingTree/                                  <- IStashService, IAmendService, ICleanService, IInitService
│   ├── Inspection/                                   <- ITreeService, IBlameService, IFileDiffService
│   └── Repo/                                         <- IConflictsService, IPatchService, ISubmoduleService,
│                                                        IReflogService, IContributorsService, INotesService
├── src/TechTeaStudio.GitClient.App/                  <- Avalonia desktop app (net10.0, WinExe)
│   ├── App.axaml(.cs), Program.cs, CompositionRoot.cs
│   ├── Views/                                        <- MainWindow + 27 dialogs + 3 inspection windows
│   ├── ViewModels/                                   <- MainViewModel, CommitListItem, FileSelection,
│   │                                                    ObservableObject, RelayCommand
│   ├── Controls/, Graph/, Services/                  <- commit-graph row control, lane builder, IGravatarService
│   └── Assets/                                       <- icon placeholder
├── tests/TechTeaStudio.GitClient.Core.Tests/         <- xUnit suite (120 passing, 1 skipped)
├── .github/workflows/dotnet.yml                      <- CI (restore + build + test on push / PR to product)
├── CHANGELOG.md
├── LICENSE.txt
├── QUICKSTART.md
└── README.md
```

## Build &amp; test

```powershell
dotnet build GitClient.slnx
dotnet test  GitClient.slnx
```

Requires .NET SDK 10. The Core library multi-targets `net8.0;net9.0;net10.0`. The App is `net10.0` (`WinExe`). The test project hits real LibGit2 against temp-folder repos created via `Repository.Init` (no network access required) and cleans them up via `TempRepoDir`. Current state: **120 passing, 1 skipped** (the skipped one is a rebase end-to-end scenario intentionally deferred).

## Versioning &amp; release

Version lives in **both** `src/TechTeaStudio.GitClient.Core/TechTeaStudio.GitClient.Core.csproj` and `src/TechTeaStudio.GitClient.App/TechTeaStudio.GitClient.App.csproj` as a 3-part `<Version>X.Y.Z</Version>`. Both bump together. Bump rules:

- Bug fix &rarr; `Z + 1`
- New feature, source-compatible &rarr; `Y + 1`, reset `Z = 0`
- Breaking change in public service API &rarr; `X + 1` (after `1.0`), reset `Y = Z = 0`

Commit format is `vX.Y.Z <short description>` (one line, &le; 72 chars). Push to `product` triggers `.github/workflows/dotnet.yml`, which runs `dotnet restore` + `dotnet build` + `dotnet test`. The repository does **not** publish to NuGet &mdash; this is an application that bundles its own Core library.

See [CHANGELOG.md](CHANGELOG.md) for the full release history.

## Roadmap

The v0.3.0 release brings feature parity with the day-to-day surface of legacy Gitter. v0.4+ is targeting:

- Multi-host auth (GitHub / GitLab / Bitbucket OAuth, SSH key picker) &mdash; today, push needs a username/password `Credentials` object handed in by the caller.
- Console tab for raw `git` commands, sandboxed.
- File-explorer integration (Windows shell context menu, macOS Finder integration).
- Conflict resolver UI on top of the existing `IConflictsService` "ours/theirs" primitive (today: list + resolve only).
- App icon + installer / signed binaries.
- NativeAOT validation against the LibGit2Sharp native bindings.

## Further reading

- [QUICKSTART.md](QUICKSTART.md) &mdash; one-page getting-started for embedding the Core library in your own app.
- [CHANGELOG.md](CHANGELOG.md) &mdash; release notes for every version.

## License

Licensed under the [MIT License](LICENSE.txt). Copyright &copy; Tech Tea Studio.

<p align="center">
  Built as part of the Hyperion Ecosystem by <a href="https://techteastudio.cc">TechTeaStudio</a>.
</p>
