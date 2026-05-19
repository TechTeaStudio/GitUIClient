namespace TechTeaStudio.GitClient.App.Views;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Material.Icons;
using Material.Icons.Avalonia;
using TechTeaStudio.GitClient.App.ViewModels;
using TechTeaStudio.GitClient.Models;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeToggleIcon();
    }

    // ────── Helpers ──────

    private MainViewModel? Vm => DataContext as MainViewModel;

    private bool RequireRepo()
    {
        if (Vm?.Handle is null)
        {
            if (Vm is not null) Vm.StatusMessage = "Open or clone a repository first.";
            return false;
        }
        return true;
    }

    private async Task<string?> PickFolderAsync(string? seedPath, string title)
    {
        var storage = StorageProvider;
        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(seedPath))
        {
            try { start = await storage.TryGetFolderFromPathAsync(seedPath); }
            catch { /* unreachable / denied */ }
        }
        var picked = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });
        return picked.Count > 0 ? picked[0].Path.LocalPath : null;
    }

    private async Task<string?> PickFileAsync(string title, string? rootPath)
    {
        var storage = StorageProvider;
        IStorageFolder? start = null;
        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            try { start = await storage.TryGetFolderFromPathAsync(rootPath); }
            catch { /* swallow */ }
        }
        var picked = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = start,
        });
        return picked.Count > 0 ? picked[0].Path.LocalPath : null;
    }

    private async Task RunAndReportAsync(Func<Task<string>> action)
    {
        if (Vm is null) return;
        try
        {
            var msg = await action();
            await Vm.AfterRepositoryMutationAsync(msg);
        }
        catch (Exception ex)
        {
            Vm.StatusMessage = ex.Message;
        }
    }

    // ────── Existing toolbar handlers (kept for quick-access buttons) ──────

    private async void Open_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var picked = await PickFolderAsync(Vm.Path, "Select repository folder");
        if (picked is null) return;
        Vm.Path = picked;
        await Vm.OpenAsync();
    }

    private async void Clone_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        var dlg = new CloneDialog { InitialUrl = Vm.CloneUrl, InitialPath = Vm.Path };
        var result = await dlg.ShowDialog<CloneRequest?>(this);
        if (result is null) return;
        await Vm.CloneAsync(result.Url, result.DestinationPath);
    }

    private async void Init_Click(object? sender, RoutedEventArgs e)
    {
        if (Vm?.Services is null) return;
        var dlg = new InitDialog();
        var req = await dlg.ShowDialog<InitRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            using var handle = await Vm.Services.Init.InitRepositoryAsync(req.Path, req.Bare);
            if (!req.Bare)
            {
                Vm.Path = req.Path;
                await Vm.OpenAsync();
            }
            return req.Bare ? $"Bare repo initialised at {req.Path}" : $"Repo initialised at {req.Path}";
        });
    }

    private void Exit_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Close();
    }

    // ────── Remotes / sync ──────

    private async void Fetch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var remotes = await Vm.Services.Remotes.ListRemotesAsync(Vm.Handle!);
        var dlg = new FetchDialog(remotes);
        var req = await dlg.ShowDialog<FetchRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Sync.FetchAsync(Vm.Handle!, req.RemoteName, dlg.Progress, req.Credentials, CancellationToken.None);
            return $"Fetched from {req.RemoteName}";
        });
    }

    private async void Pull_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var remotes = await Vm.Services.Remotes.ListRemotesAsync(Vm.Handle!);
        var dlg = new PullDialog(remotes);
        var req = await dlg.ShowDialog<PullRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var res = await Vm.Services.Sync.PullAsync(Vm.Handle!, req.RemoteName, req.BranchName, Vm.DefaultAuthor, dlg.Progress, req.Credentials, CancellationToken.None);
            return $"Pull result: {res.Kind}";
        });
    }

    private async void Push_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var remotes = await Vm.Services.Remotes.ListRemotesAsync(Vm.Handle!);
        var branches = Vm.Branches.Where(b => !MainViewModel.IsAllBranches(b) && !b.IsRemote).ToList();
        var dlg = new PushDialog(remotes, branches);
        var req = await dlg.ShowDialog<PushRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Sync.PushAsync(Vm.Handle!, req.RemoteName, req.BranchNames, req.Force, dlg.Progress, req.Credentials, CancellationToken.None);
            return $"Pushed {req.BranchNames.Count} branch(es) to {req.RemoteName}";
        });
    }

    private async void AddRemote_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new AddRemoteDialog();
        var info = await dlg.ShowDialog<RemoteInfo?>(this);
        if (info is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Remotes.AddRemoteAsync(Vm.Handle!, info.Name, info.Url);
            return $"Added remote {info.Name} → {info.Url}";
        });
    }

    private async void RenameRemote_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new RenameRemoteDialog();
        var newName = await dlg.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(newName)) return;
        Vm.StatusMessage = "Rename remote: rename UI needs an old-name selector — open remotes via Repo menu first.";
    }

    // ────── Submodules ──────

    private async void SubmoduleAdd_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new AddSubmoduleDialog();
        var req = await dlg.ShowDialog<AddSubmoduleRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Submodules.AddSubmoduleAsync(Vm.Handle!, req.Url, req.Path);
            return $"Added submodule {req.Path}";
        });
    }

    private async void SubmoduleUpdate_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var submodules = await Vm.Services.Submodules.ListSubmodulesAsync(Vm.Handle!);
        if (submodules.Count == 0) { Vm.StatusMessage = "No submodules in this repo."; return; }
        var dlg = new UpdateSubmoduleDialog(submodules[0].Name);
        var req = await dlg.ShowDialog<UpdateSubmoduleRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Submodules.UpdateSubmoduleAsync(Vm.Handle!, req.Name, req.Init);
            return $"Updated submodule {req.Name}";
        });
    }

    private async void ApplyPatch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new ApplyPatchDialog();
        var req = await dlg.ShowDialog<ApplyPatchRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Patches.ApplyPatchAsync(Vm.Handle!, req.PatchPath, req.IndexOnly);
            return $"Applied patch {req.PatchPath}";
        });
    }

    private async void Reflog_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var entries = await Vm.Services.Reflog.ReadReflogAsync(Vm.Handle!, "HEAD");
        Vm.StatusMessage = $"HEAD reflog: {entries.Count} entries. Newest: {entries.FirstOrDefault()?.Message ?? "(none)"}";
    }

    private async void Contributors_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var list = await Vm.Services.Contributors.ListContributorsAsync(Vm.Handle!);
        var top = string.Join(", ", list.Take(5).Select(c => $"{c.Name} ({c.CommitCount})"));
        Vm.StatusMessage = $"Top contributors: {top}";
    }

    // ────── Branch ops ──────

    private async void CreateBranch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new CreateBranchDialog();
        var req = await dlg.ShowDialog<CreateBranchRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var b = await Vm.Services.BranchOps.CreateBranchAsync(Vm.Handle!, req.Name, req.StartSha, req.Checkout);
            return $"Created branch {b.Name}" + (req.Checkout ? " (checked out)" : "");
        });
    }

    private async void RenameBranch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var current = Vm.SelectedBranch;
        if (current is null || MainViewModel.IsAllBranches(current))
        {
            Vm.StatusMessage = "Select a real branch in the dropdown first.";
            return;
        }
        var dlg = new RenameBranchDialog();
        var newName = await dlg.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(newName)) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.BranchOps.RenameBranchAsync(Vm.Handle!, current.Name, newName);
            return $"Renamed {current.Name} → {newName}";
        });
    }

    private async void DeleteBranch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var current = Vm.SelectedBranch;
        if (current is null || MainViewModel.IsAllBranches(current))
        {
            Vm.StatusMessage = "Select a real branch in the dropdown first.";
            return;
        }
        var dlg = new DeleteBranchDialog();
        var force = await dlg.ShowDialog<bool?>(this);
        if (force is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.BranchOps.DeleteBranchAsync(Vm.Handle!, current.Name, force.Value);
            return $"Deleted branch {current.Name}";
        });
    }

    private async void CheckoutBranch_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var current = Vm.SelectedBranch;
        if (current is null || MainViewModel.IsAllBranches(current))
        {
            Vm.StatusMessage = "Select a real branch in the dropdown first.";
            return;
        }
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.BranchOps.CheckoutBranchAsync(Vm.Handle!, current.Name);
            return $"Checked out {current.Name}";
        });
    }

    // ────── Tag ops ──────

    private async void CreateTag_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var sha = Vm.SelectedCommit?.Sha;
        if (string.IsNullOrEmpty(sha)) { Vm.StatusMessage = "Select a commit first."; return; }
        var dlg = new CreateTagDialog();
        var req = await dlg.ShowDialog<CreateTagRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            if (req.Annotated)
                await Vm.Services.Tags.CreateAnnotatedTagAsync(Vm.Handle!, req.Name, sha, req.Message ?? "", Vm.DefaultAuthor);
            else
                await Vm.Services.Tags.CreateLightweightTagAsync(Vm.Handle!, req.Name, sha);
            return $"Tagged {sha[..7]} as {req.Name}";
        });
    }

    private async void ListTags_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var tags = await Vm.Services.Tags.ListTagsAsync(Vm.Handle!);
        Vm.StatusMessage = $"Tags ({tags.Count}): {string.Join(", ", tags.Take(10).Select(t => t.Name))}";
    }

    // ────── Commit / working tree ──────

    private async void Amend_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new AmendDialog();
        var req = await dlg.ShowDialog<AmendRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var c = await Vm.Services.Amend.AmendAsync(Vm.Handle!, req.Message, Vm.DefaultAuthor);
            return $"Amended HEAD: {c.Sha[..7]}";
        });
    }

    private async void Stash_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new StashSaveDialog();
        var req = await dlg.ShowDialog<StashSaveRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var s = await Vm.Services.Stash.SaveStashAsync(Vm.Handle!, req.Message, Vm.DefaultAuthor, req.IncludeUntracked);
            return $"Stashed: {s.Message}";
        });
    }

    private async void StashApply_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var list = await Vm.Services.Stash.ListStashesAsync(Vm.Handle!);
        if (list.Count == 0) { Vm.StatusMessage = "No stashes."; return; }
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Stash.ApplyStashAsync(Vm.Handle!, 0);
            return "Applied stash 0";
        });
    }

    private async void StashPop_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var list = await Vm.Services.Stash.ListStashesAsync(Vm.Handle!);
        if (list.Count == 0) { Vm.StatusMessage = "No stashes."; return; }
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Stash.PopStashAsync(Vm.Handle!, 0);
            return "Popped stash 0";
        });
    }

    private async void Clean_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var untracked = await Vm.Services.Clean.ListUntrackedAsync(Vm.Handle!);
        var dlg = new CleanDialog();
        dlg.SetFiles(untracked);
        var req = await dlg.ShowDialog<CleanRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var n = await Vm.Services.Clean.CleanUntrackedAsync(Vm.Handle!, req.SelectedPaths);
            return $"Cleaned {n} untracked file(s)";
        });
    }

    private async void Conflicts_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var dlg = new ConflictsDialog(Vm.Services.Conflicts, Vm.Handle!);
        await dlg.ShowDialog(this);
        await Vm.AfterRepositoryMutationAsync("Conflicts dialog closed");
    }

    private async void AddNote_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var sha = Vm.SelectedCommit?.Sha;
        if (string.IsNullOrEmpty(sha)) { Vm.StatusMessage = "Select a commit first."; return; }
        var dlg = new AddNoteDialog(sha);
        var req = await dlg.ShowDialog<AddNoteRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Notes.AddNoteAsync(Vm.Handle!, req.Sha, req.Message, Vm.DefaultAuthor);
            return $"Added note on {req.Sha[..7]}";
        });
    }

    // ────── Rewrites ──────

    private async void Merge_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var branches = Vm.Branches.Where(b => !MainViewModel.IsAllBranches(b)).ToList();
        var dlg = new MergeDialog(branches);
        var req = await dlg.ShowDialog<MergeRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var opts = new TechTeaStudio.GitClient.Models.MergeOptions
            {
                FastForwardStrategy = req.NoFastForward ? "noFastForward" : "default",
                Squash = req.Squash,
                CommitOnSuccess = req.CommitOnSuccess,
                CustomMessage = req.CustomMessage,
            };
            var outcome = await Vm.Services.Rewrites.MergeAsync(Vm.Handle!, req.SourceBranch, Vm.DefaultAuthor, opts);
            return $"Merge: {outcome}";
        });
    }

    private async void Rebase_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var branches = Vm.Branches.Where(b => !MainViewModel.IsAllBranches(b)).ToList();
        var dlg = new RebaseDialog(branches);
        var req = await dlg.ShowDialog<RebaseStartRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            switch (req.Action)
            {
                case "start":
                    if (string.IsNullOrEmpty(req.UpstreamBranch)) return "No upstream branch chosen.";
                    var state = await Vm.Services.Rewrites.RebaseStartAsync(Vm.Handle!, req.UpstreamBranch, Vm.DefaultAuthor);
                    return $"Rebase start: {state}";
                case "continue":
                    var c = await Vm.Services.Rewrites.RebaseContinueAsync(Vm.Handle!, Vm.DefaultAuthor);
                    return $"Rebase continue: {c}";
                case "skip":
                    var sk = await Vm.Services.Rewrites.RebaseSkipAsync(Vm.Handle!, Vm.DefaultAuthor);
                    return $"Rebase skip: {sk}";
                case "abort":
                    await Vm.Services.Rewrites.RebaseAbortAsync(Vm.Handle!);
                    return "Rebase aborted";
                default:
                    return $"Unknown rebase action: {req.Action}";
            }
        });
    }

    private async void Revert_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a commit first."; return; }
        var dlg = new RevertDialog(c.Sha, c.Summary);
        var req = await dlg.ShowDialog<RevertRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var rc = await Vm.Services.Rewrites.RevertAsync(Vm.Handle!, req.Sha, Vm.DefaultAuthor, req.CommitOnSuccess);
            return $"Reverted: {rc.Sha[..7]}";
        });
    }

    private async void CherryPick_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a commit first."; return; }
        var dlg = new CherryPickDialog(c.Sha, c.Summary);
        var req = await dlg.ShowDialog<CherryPickRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            var cc = await Vm.Services.Rewrites.CherryPickAsync(Vm.Handle!, req.Sha, Vm.DefaultAuthor, req.CommitOnSuccess);
            return $"Cherry-picked: {cc.Sha[..7]}";
        });
    }

    private async void Reset_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a target commit first."; return; }
        var dlg = new ResetDialog(c.Sha);
        var req = await dlg.ShowDialog<ResetRequest?>(this);
        if (req is null) return;
        await RunAndReportAsync(async () =>
        {
            await Vm.Services.Rewrites.ResetAsync(Vm.Handle!, req.Sha, req.Mode);
            return $"Reset {req.Mode} → {req.Sha[..7]}";
        });
    }

    // ────── Inspect ──────

    private async void Diff_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a commit first."; return; }
        var diffs = await Vm.Services.FileDiff.CompareWithParentAsync(Vm.Handle!, c.Sha);
        var win = new DiffViewerWindow();
        win.SetDiffs(diffs, $"{c.ShortSha} {c.Summary}");
        await win.ShowDialog(this);
    }

    private async void Tree_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a commit first."; return; }
        var win = new TreeBrowserWindow();
        await win.LoadAsync(Vm.Services.Tree, Vm.Handle!, c.Sha);
        await win.ShowDialog(this);
    }

    private async void Blame_Click(object? sender, RoutedEventArgs e)
    {
        if (!RequireRepo() || Vm!.Services is null) return;
        var c = Vm.SelectedCommit;
        if (c is null) { Vm.StatusMessage = "Select a commit first."; return; }
        var picked = await PickFileAsync("Pick a file to blame (from working dir)", Vm.Path);
        if (picked is null) return;
        var rel = System.IO.Path.GetRelativePath(Vm.Path, picked).Replace('\\', '/');
        var win = new BlameWindow();
        await win.LoadAsync(Vm.Services.Blame, Vm.Handle!, c.Sha, rel);
        await win.ShowDialog(this);
    }

    // ────── Theme toggle ──────

    private void ThemeToggle_Click(object? sender, RoutedEventArgs e)
    {
        var app = Application.Current;
        if (app is null) return;

        var current = app.ActualThemeVariant;
        app.RequestedThemeVariant = current == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        UpdateThemeToggleIcon();
    }

    private void UpdateThemeToggleIcon()
    {
        if (this.FindControl<MaterialIcon>("ThemeToggleIcon") is { } icon)
        {
            icon.Kind = Application.Current?.ActualThemeVariant == ThemeVariant.Dark
                ? MaterialIconKind.WeatherSunny
                : MaterialIconKind.WeatherNight;
        }
    }

    // ────── Custom window chrome (SystemDecorations=None) ──────
    //
    // The title-bar Border in MainWindow.axaml routes its PointerPressed event
    // here. Left-click anywhere on the strip begins a window-move drag; a
    // double-click toggles maximize/restore (Windows convention). Other mouse
    // buttons pass through so the standard right-click menu still works on
    // controls inside the strip.

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        if (e.ClickCount >= 2)
        {
            ToggleMaximize();
            return;
        }
        BeginMoveDrag(e);
    }

    private void OnWindowMinimize(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnWindowMaximizeRestore(object? sender, RoutedEventArgs e)
        => ToggleMaximize();

    private void OnWindowClose(object? sender, RoutedEventArgs e) => Close();

    private void ToggleMaximize()
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
}
