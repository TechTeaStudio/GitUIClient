namespace TechTeaStudio.GitClient.App.Views;

using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Modal dialog gathering the parameters for an <c>ISyncService.FetchAsync</c>
/// call. On Fetch, returns a <see cref="FetchRequest"/> the orchestrator
/// hands off to the service. On Cancel, returns <c>null</c>.
///
/// Exposes a <see cref="Progress"/> property of type
/// <see cref="IProgress{T}"/> so the orchestrator can wire fetch progress
/// into the dialog's <see cref="ProgressBar"/>.
/// </summary>
public sealed partial class FetchDialog : Window
{
    /// <summary>Parameterless ctor for the XAML loader / designer.</summary>
    public FetchDialog() : this(Array.Empty<RemoteInfo>()) { }

    public FetchDialog(IReadOnlyList<RemoteInfo> remotes)
    {
        InitializeComponent();
        RemoteCombo.ItemsSource = remotes;
        if (remotes.Count > 0)
            RemoteCombo.SelectedIndex = 0;
    }

    /// <summary>Progress sink — drive the embedded <see cref="ProgressBar"/> from the orchestrator.</summary>
    public IProgress<SyncProgress> Progress { get; }
        // Avalonia ProgressBar lives on the UI thread; marshal updates through the dispatcher.
        = new Progress<SyncProgress>();

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Fetch_Click(object? sender, RoutedEventArgs e)
    {
        if (RemoteCombo.SelectedItem is not RemoteInfo remote)
            return;

        Credentials? creds = null;
        var user = UsernameBox.Text?.Trim();
        var pass = PasswordBox.Text;
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            creds = new Credentials(user, pass);

        Close(new FetchRequest(remote.Name, creds));
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
