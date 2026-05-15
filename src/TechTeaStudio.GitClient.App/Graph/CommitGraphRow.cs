namespace TechTeaStudio.GitClient.App.Graph;

/// <summary>
/// Per-row graph state for rendering: which lane this commit's dot sits on, the colour of that
/// dot, every lane that already existed above this row (pass-through verticals), the edges that
/// go from this row's dot down to the next row's lanes, and a "incoming from above" flag for
/// the top half of the dot's lane.
///
/// The edges are stored on the OUTGOING row (this one), so a row is self-contained for drawing
/// without needing to peek at the next row's data.
/// </summary>
public sealed record CommitGraphRow
{
    public required int LaneIndex { get; init; }
    public required int ColorIndex { get; init; }

    /// <summary>True iff this commit's lane was already alive in the previous row (a continuation).</summary>
    public required bool IncomingFromAbove { get; init; }

    /// <summary>
    /// Input-state lanes alive at the start of this row (pass-through verticals).
    /// Excludes the dot's lane if it was freshly allocated for this commit.
    /// </summary>
    public required IReadOnlyList<GraphLane> Lanes { get; init; }

    /// <summary>Edges from this row's lanes to the next row's lanes (drawn from row midpoint downward).</summary>
    public required IReadOnlyList<GraphEdge> Edges { get; init; }

    /// <summary>Total lane count across the whole graph — same for every row, drives canvas width.</summary>
    public required int MaxLanes { get; init; }
}
