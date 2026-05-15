namespace TechTeaStudio.GitClient.Models;

/// <summary>
/// A submodule registered in the parent repository.
///
/// The three SHA fields capture the different points of view a submodule
/// can be at: the commit recorded in the parent's HEAD tree
/// (<see cref="HeadSha"/>), the one staged in the parent index
/// (<see cref="IndexSha"/>), and the one checked out inside the submodule
/// working tree (<see cref="WorkDirSha"/>). They are <c>null</c> when the
/// submodule is missing on disk or the parent doesn't know about that side
/// yet.
/// </summary>
public sealed record SubmoduleInfo
{
    /// <summary>The name as registered in <c>.gitmodules</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Path of the submodule working tree relative to the parent repo.</summary>
    public required string Path { get; init; }

    /// <summary>Clone URL recorded in <c>.gitmodules</c>.</summary>
    public required string Url { get; init; }

    /// <summary>SHA the parent's HEAD tree records for the submodule (<c>null</c> if not tracked).</summary>
    public string? HeadSha { get; init; }

    /// <summary>SHA staged for the submodule in the parent index (<c>null</c> if missing).</summary>
    public string? IndexSha { get; init; }

    /// <summary>SHA actually checked out inside the submodule working tree (<c>null</c> if not initialised).</summary>
    public string? WorkDirSha { get; init; }

    /// <summary>Human-readable status: "in sync", "out of date", "missing", etc.</summary>
    public required string Status { get; init; }
}
