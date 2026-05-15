namespace TechTeaStudio.GitClient.App.Views;

using System.Collections.Generic;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;

using TechTeaStudio.GitClient.Models;

/// <summary>Result returned from <see cref="MergeDialog"/> on success.</summary>
public sealed record MergeRequest(
    string SourceBranch,
    bool NoFastForward,
    bool Squash,
    bool CommitOnSuccess,
    string? CustomMessage);

public sealed partial class MergeDialog : Window
{
    public MergeDialog()
        : this(System.Array.Empty<BranchInfo>())
    {
    }

    public MergeDialog(IReadOnlyList<BranchInfo> branches)
    {
        InitializeComponent();

        SourceBranchBox.ItemsSource = branches;

        // Default the source branch to the first non-current branch we know about.
        var preferred = branches.FirstOrDefault(b => !b.IsCurrent) ?? branches.FirstOrDefault();
        if (preferred is not null)
            SourceBranchBox.SelectedItem = preferred;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(null);

    private void Merge_Click(object? sender, RoutedEventArgs e)
    {
        if (SourceBranchBox.SelectedItem is not BranchInfo branch)
            return; // require a selection — leave dialog open

        var customMessage = string.IsNullOrWhiteSpace(CustomMessageBox.Text)
            ? null
            : CustomMessageBox.Text!.Trim();

        Close(new MergeRequest(
            SourceBranch: branch.Name,
            NoFastForward: NoFastForwardBox.IsChecked == true,
            Squash: SquashBox.IsChecked == true,
            CommitOnSuccess: CommitOnSuccessBox.IsChecked == true,
            CustomMessage: customMessage));
    }
}
