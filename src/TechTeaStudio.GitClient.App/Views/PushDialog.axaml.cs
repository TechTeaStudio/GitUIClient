namespace TechTeaStudio.GitClient.App.Views;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Modal dialog gathering parameters for an <c>ISyncService.PushAsync</c>
/// call: target remote, set of local branches to push, force flag, and
/// optional credentials. Returns a <see cref="PushRequest"/> on confirm,
/// <c>null</c> on cancel.
/// </summary>
public sealed partial class PushDialog : Window
{
    private readonly ObservableCollection<BranchSelection> _branches = new();

    /// <summary>Parameterless ctor for the XAML loader / designer.</summary>
    public PushDialog() : this(Array.Empty<RemoteInfo>(), Array.Empty<BranchInfo>()) { }

    public PushDialog(IReadOnlyList<RemoteInfo> remotes, IReadOnlyList<BranchInfo> localBranches)
    {
        InitializeComponent();

        RemoteCombo.ItemsSource = remotes;
        if (remotes.Count > 0)
            RemoteCombo.SelectedIndex = 0;

        foreach (var b in localBranches)
        {
            if (b.IsRemote) continue; // only local branches can be pushed
            _branches.Add(new BranchSelection { Name = b.Name, IsSelected = b.IsCurrent });
        }
        BranchList.ItemsSource = _branches;
    }

    public IProgress<SyncProgress> Progress { get; } = new Progress<SyncProgress>();

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Push_Click(object? sender, RoutedEventArgs e)
    {
        if (RemoteCombo.SelectedItem is not RemoteInfo remote)
            return;

        var selected = _branches.Where(b => b.IsSelected).Select(b => b.Name).ToArray();
        if (selected.Length == 0)
            return;

        Credentials? creds = null;
        var user = UsernameBox.Text?.Trim();
        var pass = PasswordBox.Text;
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            creds = new Credentials(user, pass);

        Close(new PushRequest(remote.Name, selected, ForceCheck.IsChecked == true, creds));
    }

    /// <summary>Push progress into the bar — call from the orchestrator's callback.</summary>
    public void ReportProgress(SyncProgress p)
    {
        if (Dispatcher.UIThread.CheckAccess())
            ProgressBar.Value = p.Fraction;
        else
            Dispatcher.UIThread.Post(() => ProgressBar.Value = p.Fraction);
    }

    /// <summary>UI projection of a branch with a per-row checkbox state.</summary>
    private sealed class BranchSelection
    {
        public string Name { get; init; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}
