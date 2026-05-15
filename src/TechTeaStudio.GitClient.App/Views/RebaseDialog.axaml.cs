namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Result returned from <see cref="RebaseDialog"/>.
/// <see cref="Action"/> is one of <c>Start</c>, <c>Continue</c>, <c>Skip</c>, <c>Abort</c>.
/// <see cref="UpstreamBranch"/> is only populated for <c>Start</c>.
/// </summary>
public sealed record RebaseStartRequest(string Action, string? UpstreamBranch);

public sealed partial class RebaseDialog : Window
{
    public RebaseDialog()
        : this(System.Array.Empty<BranchInfo>())
    {
    }

    public RebaseDialog(IReadOnlyList<BranchInfo> branches)
    {
        InitializeComponent();

        UpstreamBranchBox.ItemsSource = branches;

        var preferred = branches.FirstOrDefault(b => !b.IsCurrent) ?? branches.FirstOrDefault();
        if (preferred is not null)
            UpstreamBranchBox.SelectedItem = preferred;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Start_Click(object? sender, RoutedEventArgs e)
    {
        if (UpstreamBranchBox.SelectedItem is not BranchInfo branch)
            return;
        Close(new RebaseStartRequest("Start", branch.Name));
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
        => Close(new RebaseStartRequest("Continue", null));

    private void Skip_Click(object? sender, RoutedEventArgs e)
        => Close(new RebaseStartRequest("Skip", null));

    private void Abort_Click(object? sender, RoutedEventArgs e)
        => Close(new RebaseStartRequest("Abort", null));
}
