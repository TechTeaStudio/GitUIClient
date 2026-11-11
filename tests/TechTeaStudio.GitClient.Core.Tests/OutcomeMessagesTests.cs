namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.App.ViewModels;
using TechTeaStudio.GitClient.Models;

/// <summary>
/// The status-bar texts for cherry-pick and revert must stay exactly what
/// 0.4.1 showed: the success prefix plus a 7-char sha, or the message the
/// service throws from its exception-based methods.
/// </summary>
public sealed class OutcomeMessagesTests
{
    private const string Requested = "0123456789abcdef0123456789abcdef01234567";
    private const string CommitSha = "abcdef1234567890abcdef1234567890abcdef12";
    private const string HeadSha = "fedcba9876543210fedcba9876543210fedcba98";

    [Fact]
    public void CherryPick_Committed_shows_the_new_commit()
    {
        CherryPickOutcome outcome = new Committed { Commit = Commit(CommitSha) };

        Assert.Equal("Cherry-picked: abcdef1", OutcomeMessages.CherryPick(outcome, Requested));
    }

    [Fact]
    public void CherryPick_Staged_shows_the_unchanged_head()
    {
        CherryPickOutcome outcome = new Staged { Head = Commit(HeadSha) };

        Assert.Equal("Cherry-picked: fedcba9", OutcomeMessages.CherryPick(outcome, Requested));
    }

    [Fact]
    public void CherryPick_Conflicted_shows_the_service_message()
    {
        CherryPickOutcome outcome = new Conflicted { Paths = ["file.txt"] };

        Assert.Equal(
            $"Cherry-pick of '{Requested}' produced conflicts; resolve them in the working tree before retrying.",
            OutcomeMessages.CherryPick(outcome, Requested));
    }

    [Fact]
    public void Revert_Committed_shows_the_new_commit()
    {
        RevertOutcome outcome = new Committed { Commit = Commit(CommitSha) };

        Assert.Equal("Reverted: abcdef1", OutcomeMessages.Revert(outcome, Requested));
    }

    [Fact]
    public void Revert_Staged_shows_the_unchanged_head()
    {
        RevertOutcome outcome = new Staged { Head = Commit(HeadSha) };

        Assert.Equal("Reverted: fedcba9", OutcomeMessages.Revert(outcome, Requested));
    }

    [Fact]
    public void Revert_Conflicted_shows_the_service_message()
    {
        RevertOutcome outcome = new Conflicted { Paths = ["file.txt"] };

        Assert.Equal(
            $"Revert of '{Requested}' produced conflicts; resolve them in the working tree before retrying.",
            OutcomeMessages.Revert(outcome, Requested));
    }

    [Fact]
    public void Revert_NothingToRevert_shows_the_service_message()
    {
        RevertOutcome outcome = new NothingToRevert();

        Assert.Equal($"Nothing to revert for '{Requested}'.", OutcomeMessages.Revert(outcome, Requested));
    }

    private static CommitInfo Commit(string sha) => new()
    {
        Sha = sha,
        Author = "Test",
        Email = "test@example.com",
        When = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero),
        Message = "message",
        Parents = [],
    };
}
