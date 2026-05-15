namespace TechTeaStudio.GitClient.App.Graph;

/// <summary>A single lane (column) at a row: the column index and the color used to paint it.</summary>
public readonly record struct GraphLane(int Index, int ColorIndex);
