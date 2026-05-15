# TechTeaStudio.GitClient

Avalonia desktop client + reusable Core library wrapping [LibGit2Sharp](https://github.com/libgit2/libgit2sharp). A read-mostly Git GUI: open a repository, browse branches and commits, inspect the working tree, stage selected files and create a commit.

## How it works

```
LibGit2Sharp.Repository
    └── LibGit2RepoHandle (per-handle async lock — LibGit2Sharp is NOT thread-safe)
            ├── LibGit2RepositoryService : IRepositoryService   (open / init / clone / branches / commits / diff / status)
            └── LibGit2CommitService     : ICommitService       (stage + commit)
                    │
                    ▼
            MainViewModel  ←→  MainWindow.axaml   (Avalonia)
```

## Install / build

```bash
dotnet build GitClient.slnx
dotnet test  GitClient.slnx
```

Requires .NET SDK 10. The Core library multi-targets `net8.0;net9.0;net10.0`; the App is `net10.0` (WinExe).

## v0.1.0 — what's in the box

### Working today

- Open an existing repository (`Repository.Discover` -> `Repository`).
- Initialise a new empty repository.
- Clone from a Git URL into a local path with progress callbacks.
- List local branches, with each branch's tip-commit sha.
- List the last N commits of a branch / ref / sha (date, author, summary, parents).
- Unified diff between two commit shas.
- Working-tree status grouped by Added / Modified / Deleted / Untracked.
- Create a commit (stage all OR a path-spec subset, then commit with author info).
- Avalonia desktop window: path box + Open / Clone buttons, branch dropdown, commit list, selected-commit detail, status panel, commit-message box and "Commit selected files" button.

### Deferred to v0.2+

- **Push / Pull** — both require credentials handling which is out of scope for v0.1.0.
- **Merge / merge-conflict UI** — non-trivial UX; deferred.
- **Multi-host auth** (GitHub / GitLab / Bitbucket OAuth, SSH keys) — deferred until push lands.
- **Console tab** for arbitrary `git` commands — deferred (security: arbitrary shell execution from a GUI needs sandboxing thought).
- **OS file-explorer integration** — deferred.
- **Commit-graph view** (visual DAG) — deferred; the v0.1.0 list shows parent shas in text.

## Architecture notes

- **LibGit2Sharp is NOT thread-safe** — a single `Repository` instance must be used from one thread at a time. `LibGit2RepoHandle` owns a `SemaphoreSlim(1, 1)` async lock; every public method on the services takes the lock before touching the underlying `Repository`. The UI layer never touches `Repository` directly — it always goes through the service interfaces.
- **Tests use temp folders, not network**. `Repository.Init` makes a local bare repo that stands in for a real remote; clone tests run entirely offline. Every test wraps the temp folder in a `using` so it's deleted even on failure.

## Release flow

CI builds and tests on every push / PR to the `product` branch. There is **no NuGet publish** — this is an application that bundles its own core library.

## License

MIT. See `LICENSE.txt`.
