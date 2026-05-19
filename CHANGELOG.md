# Changelog

All notable changes to this project are documented here.
Format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] — 2026-05-19

UI overhaul: chromeless custom title bar, liquid-glass acrylic backdrop, single-file palette, repo-aware gating, custom icon set, loading spinners.

### Added
- Custom window chrome: `SystemDecorations="None"` + `ExtendClientAreaToDecorationsHint`. Own 36-px title bar with drag region, app icon, title, repo path, and min / max / close buttons. Double-click toggles maximize.
- Liquid-glass backdrop: `ExperimentalAcrylicBorder` with host-OS blur (`AcrylicBlur, Mica, Blur`); semi-transparent cards with soft rims, rounded corners, drop shadows.
- `Themes/HyperionTheme.axaml`: single source of truth for all palette tokens (`Hyperion.*`, `Glass.*`, `Chrome.*`) under `Dark` + `Light` `ThemeDictionaries`. App-bar toggle flips the active variant.
- `Themes/Spinner.axaml`: rotating `MaterialIcon` style. Big spinner on empty state during Open / Clone / Init; inline spinners next to BRANCH and COMMITS headers while history loads.
- `MainViewModel.HasRepository` property. Every repo-dependent menu (Repo / Branch / Tag / Commit / Rewrite / Inspect) is gated via `IsEnabled="{Binding HasRepository}"`. Workspace is replaced with a centred Branches glyph and OPEN / CLONE / INIT buttons when no repo is open.
- Custom icon set in `Assets/Icons/`: 8 × 512 px PNGs (`branch`, `branches`, `commit-git`, `compare`, `delete`, `deployment`, `merge`, `pull`) wired into menu items and the Commit button.
- `Assets/app.ico`: multi-resolution Windows icon (16 / 24 / 32 / 48 / 64 / 128 / 256) built from `branches.png`. Set as `<ApplicationIcon>` so the EXE / taskbar / file explorer pick it up. `branches.png` is the `Window.Icon` and the README header logo.
- `RunBusy` yields once after setting `IsBusy = true` so the UI dispatcher renders the busy state before LibGit2's synchronous work starts.

### Changed
- `App.axaml`: palette tokens extracted to `Themes/HyperionTheme.axaml`; only Material brush overrides and the `Window` selector remain inline.
- README rewritten in the HOC dry style: dropped "When to reach for it", "How it compares", "Roadmap" sections and the long tagline. Factual sections only.
- `Window.Background="Transparent"` plus `TransparencyLevelHint="AcrylicBlur, Mica, Blur, None"` so the acrylic backdrop shows through.

### Removed
- `PLAN.md`, `ARCHITECT_REVIEW.md` (sub-agent pipeline artefacts).
- The CHANGELOG `[0.1.0]` line mentioning `PLAN.md` documenting the three-worker partition.

## [0.3.0] — 2026-05-15

Feature-parity push toward Gitter_TTS. Six clusters built in parallel by sub-agents (each in its own git worktree), then merged into `product`.

### Added — Remotes & Sync (cluster A)
- `IRemoteService` + `LibGit2RemoteService` — list / add / remove / rename remotes, `ls-remote`.
- `ISyncService` + `LibGit2SyncService` — fetch / pull / push (with `--force`), with `IProgress<SyncProgress>` reporting and optional `Credentials` (`UsernamePasswordCredentials` via libgit2 callback).
- DTOs: `RemoteInfo`, `RemoteRefInfo`, `SyncProgress`, `PullResult` + `PullKind`, `Credentials`.
- Dialogs: `AddRemoteDialog`, `RenameRemoteDialog`, `FetchDialog`, `PullDialog`, `PushDialog`.

### Added — Branch ops & Tags (cluster B)
- `IBranchOpsService` + `LibGit2BranchOpsService` — create / rename / delete / checkout. Merged-branch check uses `repo.ObjectDatabase.FindMergeBase`.
- `ITagService` + `LibGit2TagService` — list / create lightweight / create annotated / delete.
- DTO: `TagInfo` (with annotated metadata).
- Dialogs: `CreateBranchDialog`, `RenameBranchDialog`, `DeleteBranchDialog`, `CreateTagDialog`.

### Added — Rewrites (cluster C)
- `IRewriteService` + `LibGit2RewriteService` — merge / rebase (start / continue / skip / abort) / revert / cherry-pick / reset (Soft / Mixed / Hard).
- DTOs: `MergeOptions`, `MergeOutcome`, `RebaseState`, `RebaseStatus`, `ResetKind`.
- Dialogs: `MergeDialog`, `RebaseDialog`, `RevertDialog`, `CherryPickDialog`, `ResetDialog`.

### Added — Stash + Init + Amend + Clean (cluster D)
- `IStashService` + `LibGit2StashService` — save / list / apply / pop / drop.
- `IAmendService` + `LibGit2AmendService` — amend HEAD with optional new message and optional re-stage.
- `ICleanService` + `LibGit2CleanService` — list untracked, delete selected paths from workdir.
- `IInitService` + `LibGit2InitService` — init non-bare or bare repo.
- DTO: `StashInfo`.
- Dialogs: `StashSaveDialog`, `InitDialog`, `AmendDialog`, `CleanDialog`.

### Added — Diff viewer + Tree browser + Blame (cluster E)
- `ITreeService` + `LibGit2TreeService` — list tree at commit (with sub-path), read blob (text vs binary detection).
- `IBlameService` + `LibGit2BlameService` — produce dense per-line `BlameLine` records.
- `IFileDiffService` + `LibGit2FileDiffService` — structured per-file `FileDiff` with hunks + classified lines; `CompareWithParentAsync` handles root-commit (vs empty tree).
- DTOs: `TreeEntry`, `FileDiff`, `DiffHunk`, `DiffLine`, `BlameLine`.
- Windows: `DiffViewerWindow` (file list + unified diff with line numbers, green/red row coloring), `TreeBrowserWindow` (lazy-loaded tree, read-only blob viewer, binary banner), `BlameWindow` (per-line grid with short-sha copy, avatar initials).

### Added — Submodules + Reflog + Conflicts + Patches + Contributors + Notes (cluster F)
- `ISubmoduleService` + `LibGit2SubmoduleService` (uses `git` CLI for add, since LibGit2Sharp lacks a public Add API).
- `IReflogService` + `LibGit2ReflogService`.
- `IConflictsService` + `LibGit2ConflictsService` — list + resolve-by-ours / resolve-by-theirs.
- `IPatchService` + `GitPatchService` (shells out to `git apply` / `git format-patch`).
- `IContributorsService` + `LibGit2ContributorsService`.
- `INotesService` + `LibGit2NotesService`.
- DTOs: `SubmoduleInfo`, `ReflogEntry`, `ConflictInfo`, `ContributorInfo`.
- Dialogs: `AddSubmoduleDialog`, `UpdateSubmoduleDialog`, `ConflictsDialog`, `ApplyPatchDialog`, `AddNoteDialog`.

### Changed — UI integration
- `MainWindow.axaml` gains a top-level `Menu` with categorised actions (File / Repo / Branch / Tag / Commit / Rewrite / Inspect) wired to handlers in `MainWindow.axaml.cs`.
- `MainViewModel` gains a `Services` property of new `GitServices` record (bundle of all 18 new service interfaces) plus a public `Handle` and `DefaultAuthor`; `AfterRepositoryMutationAsync(msg)` refreshes state after dialog-driven operations.
- `CompositionRoot` instantiates and wires all 18 services into the `GitServices` bundle.
- Existing tests (clone / repo / commit / graph / VM) untouched; full suite: **120 passing, 1 skipped** (rebase E2E intentionally deferred).

## [0.2.0] — 2026-05-15

### Added
- Material Design theme via `Material.Avalonia 3.9.2` with a custom Hyperion MD3 palette (primary `#9D2499` purple, tertiary `#5EC6C6` teal, dark surfaces `#121218` / `#2A2A2A`, semantic colours, MD3 radii and elevations). All named brushes mirror `HyperionOmniClient`'s `common.css` so the desktop client matches the web client visually.
- Gravatar avatars: `IGravatarService` / `GravatarService` resolves a `Bitmap` from `https://www.gravatar.com/avatar/{md5(email)}?d=identicon` with per-(email, size) memoisation and silent fallback on network failure. The commit list shows a 32px round avatar; the selected-commit panel shows 48px.
- Coloured commit graph: `CommitGraphBuilder` runs the standard lane algorithm (per-lane next-expected SHA), emitting per-row state (own lane, pass-through lanes, outgoing edges, incoming-from-above flag, max lanes). Merge commits emit two outgoing edges; branch-tip allocations get a new colour from an 8-entry palette. The `CommitGraphRowControl` renders one row at a time inside the virtualised commit list — pass-through verticals, top-half continuation, cubic-Bezier diagonals for merge/branch edges, and the dot on top.
- Test: `CommitGraphBuilderTests` covers empty input, single root, linear chain, branch-off, and 2-parent merge.

### Changed
- `MainWindow.axaml` rewritten as a `DockPanel` with an app bar, three card-based main columns (branches+working-tree / commits / details), and a card-based commit row. Status buckets render as MD3 pill badges.
- `CommitListItem` becomes observable (extends `ObservableObject`) and gains `Avatar`, `Email`, and `GraphRow` properties.
- `MainViewModel` takes an optional `IGravatarService` and dispatches avatar fetches after each commit list populate, marshalling back to the UI thread.
- Material icons everywhere via `Material.Icons.Avalonia 2.4.0`: section headers (BRANCH / WORKING TREE / COMMITS / COMMIT DETAILS), branch combobox items, status pill badges (pencil / plus / minus / question), action buttons (folder-open / cloud-download / refresh / check), and detail-field labels (sha / clock / arrow-up / message / email).
- Outlined text fields use `TextFieldAssist.Label` only — the conflicting Avalonia `Watermark` was removed so the floating label is the sole hint (the previous version stacked watermark text over the label, making both unreadable when the field was empty and unfocused).
- Folder picker: round icon button next to the "Repository path" field opens the native system folder dialog (Avalonia `StorageProvider.OpenFolderPickerAsync`) and writes the chosen path back to the view model; pre-seeds the dialog at the current `Path` when set.

## [0.1.0] — 2026-05-15

### Added
- `IRepositoryService` + `LibGit2RepositoryService` — open / init / clone, list branches, list commits, get diff between two shas, working-tree status grouped by Added / Modified / Deleted / Untracked. Each repo handle owns a `SemaphoreSlim` async lock so LibGit2Sharp's not-thread-safe `Repository` is always touched from one thread.
- `ICommitService` + `LibGit2CommitService` — stage (all or a path-spec subset) and commit with author metadata.
- DTOs (records): `CommitInfo` (with `Summary` helper), `BranchInfo`, `RepoStatus`, `FileChange` + `FileChangeKind`, `AuthorInfo`, `CloneProgress` (with `Fraction` helper).
- Avalonia App (`net10.0`, WinExe) — single window with path box + Open/Clone buttons, branch dropdown, commit list, selected-commit detail, status panel with per-file checkboxes, commit-message box and "Commit selected files" button. Manual DI through `CompositionRoot.cs` (no Microsoft.Extensions.DependencyInjection dependency).
- `Directory.Build.props`, `dotnet.yml` CI (restore + build + test on push / PR to `product`), README, LICENSE, `.gitignore`, and project `CLAUDE.md`.
- xUnit test suite in Core only (`RepositoryServiceTests`, `CommitServiceTests`, `MainViewModelTests`); temp-folder repos cleaned up on test exit.
