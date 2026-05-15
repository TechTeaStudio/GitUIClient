namespace TechTeaStudio.GitClient.Models;

/// <summary>Progress callback payload during a clone operation.</summary>
public sealed record CloneProgress
{
    public required string Stage { get; init; }
    public required int ReceivedObjects { get; init; }
    public required int TotalObjects { get; init; }
    public required long ReceivedBytes { get; init; }

    /// <summary>0..1 — clamped fraction of objects received.</summary>
    public double Fraction
        => TotalObjects <= 0 ? 0.0 : Math.Clamp((double)ReceivedObjects / TotalObjects, 0.0, 1.0);
}
