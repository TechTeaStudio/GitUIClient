# Implementation plan — TechTeaStudio.GitClient v0.1.0

Three workers run in parallel after the planner finishes. Shared types (DTOs in `Models/`, interfaces `IRepositoryService`, `ICommitService`, `IRepoHandle`) are already in place — workers consume them and must not modify the interfaces.

Convention:

- C# 12+ records with `init` setters for DTOs.
- `Nullable enable`, `ImplicitUsings enable` (in `Directory.Build.props`).
- One class per file (Microsoft layout).
- TDD: red -> green -> refactor where it makes sense. Tests live in `tests/TechTeaStudio.GitClient.Core.Tests/`.

## Worker A — `LibGit2RepositoryService`

### Files to create

- Replace stub in `src/TechTeaStudio.GitClient.Core/Repositories/LibGit2RepositoryService.cs` with real impl.
- Add `src/TechTeaStudio.GitClient.Core/Repositories/LibGit2RepoHandle.cs` — `sealed class` wrapping `LibGit2Sharp.Repository`, exposing a per-handle async lock (`SemaphoreSlim(1,1)`) accessed only by services in this assembly (`internal`). Implements `IRepoHandle`.
- `tests/TechTeaStudio.GitClient.Core.Tests/RepositoryServiceTests.cs`.

### Files NOT to touch

- Anything under `Models/` (consume only).
- `IRepositoryService.cs`, `ICommitService.cs`, `IRepoHandle.cs`.
- `LibGit2CommitService.cs` (Worker B owns).

### Implementation notes

- `OpenAsync(path)` -> `Repository.Discover(path)` -> if null throw `RepositoryNotFoundException`; otherwise `new Repository(discovered)`. Wrap in `LibGit2RepoHandle`.
- `InitAsync(path)` -> `Repository.Init(path)` -> open the returned `.git` dir.
- `CloneAsync(url, path, progress, ct)`:
  - Use `Repository.Clone` with `CloneOptions` whose `FetchOptions.OnTransferProgress` reports a `CloneProgress` (`stage = "transfer"`).
  - Honour `ct`: poll `ct.IsCancellationRequested` inside the progress callback; return `false` from `OnTransferProgress` to abort, then translate the resulting `UserCancelledException` into `OperationCanceledException`.
  - DO NOT touch real remotes in tests (use a second local `Repository.Init` as the "remote").
- `GetBranchesAsync(handle)` -> enumerate `Repository.Branches`, return `BranchInfo` for each (`IsRemote = b.IsRemote`, `IsCurrent = b.IsCurrentRepositoryHead`).
- `GetCommitsAsync(handle, ref, take)`:
  - Resolve ref via `repo.Lookup<Commit>(refOrSha)` or `repo.Branches[ref]?.Tip`. If neither resolves throw `ArgumentException`.
  - Walk via `repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = tip }).Take(take)`.
  - Map to `CommitInfo` with `Parents = c.Parents.Select(p => p.Sha).ToList()`.
- `GetDiffAsync(handle, fromSha, toSha)` -> resolve both as `Commit`, `repo.Diff.Compare<Patch>(fromTree, toTree)`, return `patch.Content`.
- `GetStatusAsync(handle)`:
  - `repo.RetrieveStatus(new StatusOptions { ... })`. Bucket each `StatusEntry` into Added / Modified / Deleted / Untracked. Untracked = `FileStatus.NewInWorkdir`; Added = `NewInIndex` / `RenamedInIndex`; Modified = `ModifiedInIndex | ModifiedInWorkdir`; Deleted = `DeletedFromIndex | DeletedFromWorkdir`.
  - Any state not bucketed above gets dropped (don't surface conflicts in v0.1.0).
- Concurrency: every method body that touches the underlying `Repository` does `await handle.AsyncLock.WaitAsync(ct)` ... `try { ... } finally { handle.AsyncLock.Release(); }`. The work itself runs synchronously via `Task.FromResult` / `Task.Run` — LibGit2Sharp is purely synchronous and we don't want to schedule work onto random ThreadPool threads either, so `Task.FromResult(...)` is the default unless an operation is actually I/O-heavy (clone).
- Use `await Task.Run(() => ...)` only for `CloneAsync` (the slow one).

### Acceptance criteria

For each test: create a unique temp dir via `Path.Combine(Path.GetTempPath(), $"ttsgit-{Guid.NewGuid():N}")`, `Directory.CreateDirectory`, then dispose with a try/finally that recursively deletes the dir even on test failure. The xUnit `IClassFixture` or a tiny `TempRepo : IDisposable` helper class is fine.

- `OpenAsync` on a non-existent path throws `RepositoryNotFoundException`.
- `InitAsync` -> a fresh `.git` dir exists.
- After `InitAsync` + `LibGit2CommitService.CommitAsync(...)` of a real file, `GetCommitsAsync(handle, "HEAD", 10)` returns at least one commit with the expected message.
- `GetBranchesAsync` after init+commit returns one branch (default `main` or `master` depending on libgit2 default — assert count >= 1, not a specific name).
- `GetStatusAsync` after writing an untracked file lists it under `Untracked`.
- `GetDiffAsync` between two commits where the file changed returns a non-empty patch containing both old and new content.
- `CloneAsync` from a local bare repo to a local destination yields a usable handle (assert at least one commit reachable from HEAD). Use `Repository.Init(srcDir, isBare: true)` as the "remote" so no network access is needed.

## Worker B — `LibGit2CommitService`

### Files to create

- Replace stub in `src/TechTeaStudio.GitClient.Core/Repositories/LibGit2CommitService.cs` with real impl.
- `tests/TechTeaStudio.GitClient.Core.Tests/CommitServiceTests.cs`.

### Files NOT to touch

- Anything under `Models/`.
- `LibGit2RepoHandle.cs` other than reading `.Repository` and `.AsyncLock` via the `internal` accessors Worker A added.
- `LibGit2RepositoryService.cs`.

### Implementation notes

- Cast `handle` to `LibGit2RepoHandle`; if cast fails throw `ArgumentException`.
- `await handle.AsyncLock.WaitAsync(ct)`.
- Stage:
  - When `pathSpec` is null or empty -> `Commands.Stage(repo, "*")`.
  - Otherwise call `Commands.Stage(repo, pathSpec.ToArray())`.
- Build `Signature`: `new Signature(author.Name, author.Email, author.When ?? DateTimeOffset.UtcNow)`.
- `repo.Commit(message, sig, sig)`. Catch `EmptyCommitException` and rethrow as `InvalidOperationException("No changes staged; commit would be empty.")`.
- Map the returned `LibGit2Sharp.Commit` to `CommitInfo` (sha, author name, author email, when, message, parent shas).

### Acceptance criteria

- Init repo, write one file, `CommitAsync("first commit", ...)` -> returns a `CommitInfo` with that message and `Parents.Count == 0`.
- A second `CommitAsync` -> `Parents.Count == 1` and `Parents[0]` is the first commit's sha.
- `CommitAsync` with `pathSpec = ["readme.md"]` only when only `readme.md` has changed -> works. With `pathSpec = ["does-not-exist"]` and nothing else staged -> throws `InvalidOperationException`.
- Passing a non-`LibGit2RepoHandle` (e.g. an `IRepoHandle` stub) -> throws `ArgumentException`.

## Worker C — Avalonia App + ViewModel tests

### Files to create / rewrite

- `src/TechTeaStudio.GitClient.App/Views/MainWindow.axaml` (and `.axaml.cs` if needed) - real layout with:
  - Top: Path TextBox + Open button + Clone button + (clone-only) URL TextBox.
  - Left: Branch ComboBox.
  - Centre: commit list (DataGrid or ListBox with template) showing Date / Author / Summary.
  - Right or bottom: selected-commit detail panel with full Message and Parents.
  - Status panel: four `ItemsControl` lists for Added / Modified / Deleted / Untracked, each with a checkbox per item.
  - Commit-message TextBox + "Commit selected files" button.
- `src/TechTeaStudio.GitClient.App/ViewModels/MainViewModel.cs` — replace placeholder. Use `INotifyPropertyChanged` (a tiny `ObservableObject` helper is fine — no need for ReactiveUI / CommunityToolkit.Mvvm dependency unless it pays for itself). Expose:
  - `string Path`, `string CloneUrl`, `string CommitMessage`.
  - `ObservableCollection<BranchInfo> Branches`, `BranchInfo? SelectedBranch`.
  - `ObservableCollection<CommitListItem> Commits`, `CommitListItem? SelectedCommit`.
  - `RepoStatus Status` (or four `ObservableCollection<FileSelection>`).
  - `ICommand OpenCommand`, `CloneCommand`, `CommitCommand`.
  - `string? StatusMessage` for the bottom-bar (error display).
- `src/TechTeaStudio.GitClient.App/ViewModels/CommitListItem.cs` — small UI projection of `CommitInfo` (`Date`, `Author`, `Summary`, `Sha`).
- `src/TechTeaStudio.GitClient.App/ViewModels/FileSelection.cs` — `FileChange` + bool `IsSelected` (for the commit-selected-files button).
- `src/TechTeaStudio.GitClient.App/ViewModels/ObservableObject.cs` — minimal `INotifyPropertyChanged` base (≈30 LOC).
- `src/TechTeaStudio.GitClient.App/ViewModels/RelayCommand.cs` — minimal `ICommand` (≈30 LOC).
- `tests/TechTeaStudio.GitClient.Core.Tests/MainViewModelTests.cs` (lives in the Core test project — references both Core and App via existing project refs).

### Files NOT to touch

- Anything under `src/TechTeaStudio.GitClient.Core/`.
- Other workers' test files.

### Implementation notes

- Open flow: button click -> `OpenCommand` -> `_repos.OpenAsync(Path, ct)` -> on success, `_handle = result;` then call internal `RefreshAsync` which populates Branches, picks the current branch, loads `GetCommitsAsync(handle, branch.Name, 50)` and `GetStatusAsync`. Catch exceptions, surface message on `StatusMessage`.
- Clone flow: similar but `CloneAsync(CloneUrl, Path, progress=null, ct)`.
- Commit flow: build `IEnumerable<string>` pathSpec from `Status.*` collections where `IsSelected`, then `_commits.CommitAsync(handle, CommitMessage, new AuthorInfo {...}, paths, ct)`. Default author: `new AuthorInfo { Name = Environment.UserName, Email = $"{Environment.UserName}@local" }`. Then re-run RefreshAsync.
- Cancellation: use a per-VM `CancellationTokenSource`; cancel previous before starting a new long-running command (idempotent navigation).
- The window grabs the ViewModel from `CompositionRoot.BuildMainViewModel()` (already in skeleton).

### Acceptance criteria

For the ViewModel tests, never instantiate a real Avalonia window — only `new MainViewModel(stubRepos, stubCommits)`. Stubs are local in-test classes. Use `xunit` `[Fact]`s.

- Construct VM with stubs -> `Greeting` is non-empty (sanity).
- `OpenCommand` with a `Path` that the stub maps to a known handle:
  - `IsBusy` is true mid-call (use a `TaskCompletionSource` in the stub to hold OpenAsync open).
  - After completion: `Branches` populated; `SelectedBranch` is non-null; `Commits` non-empty; `Status` populated.
- `OpenCommand` whose stub throws -> `StatusMessage` contains the exception message; `IsBusy` returns to false.
- `CommitCommand` is disabled when `CommitMessage` is empty or no files are selected (assert via `RelayCommand.CanExecute`).
- `CommitCommand` invoked with a selected file and non-empty message -> calls `_commits.CommitAsync` exactly once with the expected pathSpec.

## Coordination

- All workers run in parallel. Workers A and B share `LibGit2RepoHandle`; Worker A owns the file. Worker B treats it as read-only (only consumes `.Repository`/`.AsyncLock` via internal accessors Worker A creates). If Worker B finds the accessors aren't there, stop and raise it — don't fork the type.
- Tests live in `tests/TechTeaStudio.GitClient.Core.Tests`. Each worker adds their own file (`RepositoryServiceTests.cs`, `CommitServiceTests.cs`, `MainViewModelTests.cs`) so there are no merge conflicts. Delete `Placeholder.cs` once a real test exists.
- Temp dirs MUST be cleaned up — wrap repo creation in `using var temp = new TempRepoDir();` style. Even on test failure the directory should be wiped (`Directory.Delete(path, recursive: true)` in finally / IDisposable).
- After all workers finish, the architect reviews for: single-class-per-file, no horizontal-rule Markdown, thread-safety around `Repository`, temp-dir cleanup, deferred-feature documentation. Then the tester runs `dotnet build` and `dotnet test` against `GitClient.slnx`.
- Commit message: `vX.Y.Z <short>` — bump versions in **both** csprojs before committing. (Orchestrator does not commit unless asked.)
