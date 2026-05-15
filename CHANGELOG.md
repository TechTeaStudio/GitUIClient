# Changelog

All notable changes to this project are documented here.
Format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- `Directory.Build.props`, `dotnet.yml` CI (restore + build + test on push / PR to `product`), README, LICENSE, `.gitignore`, project `CLAUDE.md`, and `PLAN.md` documenting the three-worker partition.
- xUnit test suite in Core only (`RepositoryServiceTests`, `CommitServiceTests`, `MainViewModelTests`); temp-folder repos cleaned up on test exit.
