namespace TechTeaStudio.GitClient.App.Views;

using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Modal dialog gathering the parameters for an <c>ISyncService.PullAsync</c>
/// call. On Pull, returns a <see cref="PullRequest"/> the orchestrator hands
/// off to the service. On Cancel, returns <c>null</c>.
/// </summary>
public sealed partial class PullDialog : Window
{
    /// <summary>Parameterless ctor for the XAML loader / designer.</summary>
    public PullDialog() : this(Array.Empty<RemoteInfo>()) { }

    public PullDialog(IReadOnlyList<RemoteInfo> remotes)
    {
        InitializeComponent();
        RemoteCombo.ItemsSource = remotes;
        if (remotes.Count > 0)
            RemoteCombo.SelectedIndex = 0;
    }

    public IProgress<SyncProgress> Progress { get; } = new Progress<SyncProgress>();

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Pull_Click(object? sender, RoutedEventArgs e)
    {
        if (RemoteCombo.SelectedItem is not RemoteInfo remote)
            return;

        var branch = BranchBox.Text?.Trim();
        if (string.IsNullOrEmpty(branch))
            branch = null;

        Credentials? creds = null;
        var user = UsernameBox.Text?.Trim();
        var pass = PasswordBox.Text;
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            creds = new Credentials(user, pass);

        Close(new PullRequest(remote.Name, branch, creds));
    }

    /// <summary>Push progress into the bar — call from the orchestrator's callback.</summary>
    public void ReportProgress(SyncProgress p)
    {
        if (Dispatcher.UIThread.CheckAccess())
            ProgressBar.Value = p.Fraction;
        else
            Dispatcher.UIThread.Post(() => ProgressBar.Value = p.Fraction);
    }
}
