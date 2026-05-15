# Architect review — TechTeaStudio.GitClient v0.1.0

Read-only pass over the worker output. Scope per the orchestrator brief:

1. Thread-safety around LibGit2Sharp (it's NOT thread-safe — repo handles must be used on one thread).
2. Temp-dir cleanup.
3. Out-of-scope features are documented as deferred, not hidden.
4. Conventions: single class per file, namespaces match folder layout, no `---` horizontal-rules in Markdown body.

## Verdict

**Pass — no critical issues.** Two minor issues fixed inline before the tester runs; three nice-to-haves logged as v0.2+ candidates.

## Critical issues

None.

## Inline fixes applied during review

1. **`ResolveCommit` swallows LibGit2 specification exceptions.** First test run failed two cases because `repo.Branches["not-a-real-ref"]` and `repo.Refs["not-a-real-ref"]` both throw `InvalidSpecificationException` on names that look like a non-ref-shaped string (e.g. a bare sha). I wrapped both probe steps in `try { ... } catch { }` so the method genuinely returns `null` when the string can't be resolved, and the public API throws the documented `ArgumentException`. Without this fix, callers see a noisy LibGit2 exception leak through. **Fixed**.
2. **xUnit1031 — `vm.OpenAsync().GetAwaiter().GetResult()` inside a `[Fact]`** in `MainViewModelTests.cs` triggered the "blocking task operations" analyzer warning. Switched that test to `async Task`. **Fixed**.

## Threading review

- `LibGit2RepoHandle` owns a `SemaphoreSlim(1, 1)` async lock. Every public method on `LibGit2RepositoryService` and `LibGit2CommitService` calls `await handle.AsyncLock.WaitAsync(ct)` before touching the embedded `Repository`, and releases in `finally`. **Correct.**
- `Repository` is `internal`-only on the handle, and the `internal AsyncLock` accessor is only readable by the services in the same assembly. The UI layer can't reach in. **Correct.**
- `CloneAsync` runs via `Task.Run` (legitimately slow); the other methods use `Task.FromResult(...)` which keeps work on the caller's synchronisation context. This is **honest** about LibGit2Sharp being synchronous-bound — it doesn't pretend operations are async when they aren't.
- `Dispose` on the handle takes the lock before disposing the `Repository`. If the lock is contended at dispose time (a service call is in flight) the disposing thread blocks until the call completes. This is the safe choice — disposing while a call is mid-flight would crash the libgit2 native side.
- One observed risk: if a caller `Dispose`s the handle from the UI thread while the same UI thread is awaiting a `LibGit2RepositoryService` call (impossible in this VM's design — `MainViewModel.DisposeHandle` only runs between calls), the synchronous `Wait()` inside `Dispose` would deadlock. **Acceptable** because the VM enforces the order, but `Dispose` could be made async-friendly in v0.2 if we ever expose disposal from a `RelayCommand`.

## Temp-dir cleanup

- `TempRepoDir` recursively deletes its directory, clearing the read-only bit on stubborn files (LibGit2 packs files as read-only) and retrying up to 5 times to tolerate the brief Windows file-lock that sometimes lingers right after `Repository.Dispose`. The last attempt swallows any final IO error so a real test failure doesn't get masked by a temp-dir cleanup exception.
- Every test in `RepositoryServiceTests` and `CommitServiceTests` wraps the temp dir in `using var temp = new TempRepoDir();` and the handle in `using var handle = ...`. Order is correct: handle disposes first (closing the `Repository`), then the directory. **Correct.**
- `CloneAsync` test uses **three** temp dirs (seed / origin / destination) all under `using`. **Correct.**

## Deferred-feature visibility

- `README.md` has a clear "v0.1.0 — what's in the box" section split into "Working today" and "Deferred to v0.2+" sub-headings. Push, pull, merge UI, multi-host auth, console tab, file-explorer integration are all listed explicitly. **Correct.**
- `CHANGELOG.md [0.1.0]` only claims what actually ships. **Correct.**
- `IRepositoryService` does not include `PushAsync` / `PullAsync` / merge APIs — they aren't stubbed and silently broken; they're absent. When v0.2 lands, the interface gets new methods. **Correct.**

## Convention review

- Single class per file: all `.cs` files hold one type (records, services, ViewModels). `FileChange.cs` also defines the `FileChangeKind` enum next to its consumer — acceptable in this codebase, matches the pilot's pattern of putting tightly-coupled small enums beside their owner record.
- Namespaces match folder layout (`TechTeaStudio.GitClient.Repositories` lives in `Core/Repositories/`, etc.). **Correct.**
- `Nullable enable`, `ImplicitUsings enable` from `Directory.Build.props` — no per-file `#nullable disable` escape hatches. **Correct.**
- `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace` used consistently for argument validation. **Correct.**
- No `---` horizontal-rule lines in any Markdown body (`README.md`, `CHANGELOG.md`, `CLAUDE.md`, `PLAN.md`, this file). **Correct** — per global CLAUDE.md "нейронные полоски" rule.
- Commit message format / version-bump rule: documented in project CLAUDE.md, not actually exercised since the orchestrator never commits.

## v0.2+ candidates (nice-to-haves logged, not blocking)

1. **Async `IRepoHandle.DisposeAsync`** — current `Dispose` synchronously waits on the per-handle lock. If a future caller needs to dispose from the UI thread mid-operation, that'll block. Adding `IAsyncDisposable` and using `WaitAsync` instead of `Wait` would fix it without any breaking change to the existing surface.
2. **Status-bucket completeness** — `GetStatusAsync` intentionally drops `Conflicted`, `Ignored`, `TypeChange`, and `Unaltered`. v0.2 should at least surface conflicts (they're the gateway to the merge-conflict UI).
3. **`AvaloniaResource` is empty** — `Assets/` only has a `.gitkeep` placeholder. No app icon yet. Not a v0.1.0 blocker but a real product needs one.

## Files audited

```
Directory.Build.props
GitClient.slnx
.gitignore
.github/workflows/dotnet.yml
README.md
CHANGELOG.md
CLAUDE.md
PLAN.md
LICENSE.txt
src/TechTeaStudio.GitClient.Core/
  TechTeaStudio.GitClient.Core.csproj
  Models/
    AuthorInfo.cs · BranchInfo.cs · CloneProgress.cs · CommitInfo.cs · FileChange.cs · RepoStatus.cs
  Repositories/
    ICommitService.cs · IRepoHandle.cs · IRepositoryService.cs
    LibGit2CommitService.cs · LibGit2RepoHandle.cs · LibGit2RepositoryService.cs
src/TechTeaStudio.GitClient.App/
  TechTeaStudio.GitClient.App.csproj · app.manifest · Program.cs
  App.axaml(.cs) · CompositionRoot.cs
  Assets/.gitkeep
  ViewModels/
    CommitListItem.cs · FileSelection.cs · MainViewModel.cs
    ObservableObject.cs · RelayCommand.cs
  Views/
    MainWindow.axaml(.cs)
tests/TechTeaStudio.GitClient.Core.Tests/
  TechTeaStudio.GitClient.Core.Tests.csproj
  CommitServiceTests.cs · MainViewModelTests.cs · RepositoryServiceTests.cs
  TempRepoDir.cs
```

## Summary table

| Area | Status |
| --- | --- |
| LibGit2Sharp threading | OK — per-handle `SemaphoreSlim`, services serialise access |
| Temp-dir cleanup | OK — `TempRepoDir` with retry + readonly-clear, every test uses `using` |
| Deferred features documented | OK — `README` lists them explicitly under "Deferred to v0.2+" |
| Conventions (1 class / file, namespace = folder, no `---`) | OK |
| Critical issues | **0** |
| Inline fixes applied | 2 (ResolveCommit hardening, xUnit1031 warning) |
| v0.2+ candidates | 3 (async dispose, conflict bucket, app icon) |
