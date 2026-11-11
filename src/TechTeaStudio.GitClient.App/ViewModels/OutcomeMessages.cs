namespace TechTeaStudio.GitClient.App.ViewModels;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Status-bar text for the union outcomes of cherry-pick and revert. Each
/// method is one exhaustive switch with no discard arm, so a case added to a
/// union stops the build here until it gets a text.
/// </summary>
public static class OutcomeMessages
{
    /// <summary>Text for a cherry-pick of <paramref name="requestedSha"/>.</summary>
    public static string CherryPick(CherryPickOutcome outcome, string requestedSha) => outcome switch
    {
        Committed c => $"Cherry-picked: {c.Commit.Sha[..7]}",
        Staged s => $"Cherry-picked: {s.Head.Sha[..7]}",
        Conflicted => $"Cherry-pick of '{requestedSha}' produced conflicts; resolve them in the working tree before retrying.",
    };

    /// <summary>Text for a revert of <paramref name="requestedSha"/>.</summary>
    public static string Revert(RevertOutcome outcome, string requestedSha) => outcome switch
    {
        Committed c => $"Reverted: {c.Commit.Sha[..7]}",
        Staged s => $"Reverted: {s.Head.Sha[..7]}",
        Conflicted => $"Revert of '{requestedSha}' produced conflicts; resolve them in the working tree before retrying.",
        NothingToRevert => $"Nothing to revert for '{requestedSha}'.",
    };
}
