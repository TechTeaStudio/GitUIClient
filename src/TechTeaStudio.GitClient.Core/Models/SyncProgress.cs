namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// Progress payload for fetch / pull / push operations.
/// <see cref="Stage"/> is a free-form label such as "transfer", "indexing", or "push".
/// </summary>
public sealed record SyncProgress
{
    public required string Stage { get; init; }
    public required int Received { get; init; }
    public required int Total { get; init; }
    public required long Bytes { get; init; }

    /// <summary>0..1 — clamped fraction of objects received.</summary>
    public double Fraction
        => Total <= 0 ? 0.0 : Math.Clamp((double)Received / Total, 0.0, 1.0);
}
