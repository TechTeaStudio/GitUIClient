namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Snapshot of a rebase operation: which step we're on, the total number of steps,
/// and the paths currently in conflict (empty when <see cref="State"/> is not
/// <see cref="RebaseState.Conflicts"/>).
/// </summary>
public sealed record RebaseStatus
{
    public required RebaseState State { get; init; }
    public required int CurrentStep { get; init; }
    public required int TotalSteps { get; init; }
    public required IReadOnlyList<string> ConflictedPaths { get; init; }

    public static RebaseStatus Empty { get; } = new()
    {
        State = RebaseState.Complete,
        CurrentStep = 0,
        TotalSteps = 0,
        ConflictedPaths = Array.Empty<string>(),
    };
}
