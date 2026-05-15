# Changelog

All notable changes to this project are documented here.
Format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] — 2026-05-15

### Added
- `IRepositoryService` + `LibGit2RepositoryService` — open / init / clone, list branches, list commits, get diff between two shas, working-tree status grouped by Added / Modified / Deleted / Untracked. Each repo handle owns a `SemaphoreSlim` async lock so LibGit2Sharp's not-thread-safe `Repository` is always touched from one thread.
- `ICommitService` + `LibGit2CommitService` — stage (all or a path-spec subset) and commit with author metadata.
- DTOs (records): `CommitInfo` (with `Summary` helper), `BranchInfo`, `RepoStatus`, `FileChange` + `FileChangeKind`, `AuthorInfo`, `CloneProgress` (with `Fraction` helper).
- Avalonia App (`net10.0`, WinExe) — single window with path box + Open/Clone buttons, branch dropdown, commit list, selected-commit detail, status panel with per-file checkboxes, commit-message box and "Commit selected files" button. Manual DI through `CompositionRoot.cs` (no Microsoft.Extensions.DependencyInjection dependency).
- `Directory.Build.props`, `dotnet.yml` CI (restore + build + test on push / PR to `product`), README, LICENSE, `.gitignore`, project `CLAUDE.md`, and `PLAN.md` documenting the three-worker partition.
- xUnit test suite in Core only (`RepositoryServiceTests`, `CommitServiceTests`, `MainViewModelTests`); temp-folder repos cleaned up on test exit.
