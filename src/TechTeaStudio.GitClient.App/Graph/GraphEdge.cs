namespace TechTeaStudio.GitClient.App.Graph;

/// <summary>
/// An edge drawn from this row to the row below: the lane index at the top
/// (this row), the lane index at the bottom (next row) and the color.
/// </summary>
public readonly record struct GraphEdge(int FromLane, int ToLane, int ColorIndex);
