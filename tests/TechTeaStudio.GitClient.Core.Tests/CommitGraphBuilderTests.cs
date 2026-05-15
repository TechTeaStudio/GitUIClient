namespace TechTeaStudio.GitClient.Core.Tests;

using TechTeaStudio.GitClient.App.Graph;
using TechTeaStudio.GitClient.Models;

public sealed class CommitGraphBuilderTests
{
    [Fact]
    public void Empty_input_yields_empty_output()
    {
        var rows = CommitGraphBuilder.Build(Array.Empty<CommitInfo>());
        Assert.Empty(rows);
    }

    [Fact]
    public void Single_root_commit_sits_on_lane_zero_with_no_edges()
    {
        var commits = new[] { Commit("a", parents: Array.Empty<string>()) };
        var rows = CommitGraphBuilder.Build(commits);

        var row = Assert.Single(rows);
        Assert.Equal(0, row.LaneIndex);
        Assert.False(row.IncomingFromAbove);
        Assert.Empty(row.Lanes); // freshly-allocated lane is excluded from snapshot
        Assert.Empty(row.Edges); // no parents → no outgoing
        Assert.Equal(1, row.MaxLanes);
    }

    [Fact]
    public void Linear_chain_keeps_a_single_lane_and_continues_color()
    {
        // newest → oldest: c → b → a (root)
        var commits = new[]
        {
            Commit("c", parents: new[] { "b" }),
            Commit("b", parents: new[] { "a" }),
            Commit("a", parents: Array.Empty<string>()),
        };
        var rows = CommitGraphBuilder.Build(commits);

        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal(0, r.LaneIndex));
        Assert.All(rows, r => Assert.Equal(rows[0].ColorIndex, r.ColorIndex));

        Assert.False(rows[0].IncomingFromAbove); // newest tip
        Assert.True(rows[1].IncomingFromAbove);
        Assert.True(rows[2].IncomingFromAbove);

        // c → b (same lane) and b → a (same lane), a has no edges.
        Assert.Single(rows[0].Edges, e => e.FromLane == 0 && e.ToLane == 0);
        Assert.Single(rows[1].Edges, e => e.FromLane == 0 && e.ToLane == 0);
        Assert.Empty(rows[2].Edges);

        Assert.Equal(1, rows[0].MaxLanes);
    }

    [Fact]
    public void Branch_off_creates_a_second_lane_with_a_different_color()
    {
        // c (parents: b) is on lane 0 (started). Meanwhile we have an independent tip d (no
        // shared parents) that starts on lane 1.
        //
        //   c        d        ← newest layer (two tips)
        //   |        |
        //   b        e        ← b is parent of c; e is parent of d
        //   |        |
        //   a (root) f (root)
        //
        // Walk newest-first: c, d, b, e, a, f.
        var commits = new[]
        {
            Commit("c", parents: new[] { "b" }),
            Commit("d", parents: new[] { "e" }),
            Commit("b", parents: new[] { "a" }),
            Commit("e", parents: new[] { "f" }),
            Commit("a", parents: Array.Empty<string>()),
            Commit("f", parents: Array.Empty<string>()),
        };
        var rows = CommitGraphBuilder.Build(commits);

        Assert.Equal(6, rows.Count);
        Assert.Equal(0, rows[0].LaneIndex); // c
        Assert.Equal(1, rows[1].LaneIndex); // d sits on a new lane
        Assert.Equal(0, rows[2].LaneIndex); // b on lane 0
        Assert.Equal(1, rows[3].LaneIndex); // e on lane 1
        Assert.NotEqual(rows[0].ColorIndex, rows[1].ColorIndex); // different branches → distinct colors
        Assert.Equal(2, rows[0].MaxLanes);
    }

    [Fact]
    public void Merge_commit_two_parents_emits_two_outgoing_edges()
    {
        // m has two parents: x and y. x is on lane 0, y is on lane 1 (started by the merge).
        //   m (parents: x, y)
        //   |\
        //   x y
        //   | |
        //   r (root, shared) — but the algorithm walks newest first; r appears once, with
        //                       both x and y pointing to it. When we visit r, the lane
        //                       expectations have already collapsed.
        //
        // Walk newest-first: m, x, y, r.
        var commits = new[]
        {
            Commit("m", parents: new[] { "x", "y" }),
            Commit("x", parents: new[] { "r" }),
            Commit("y", parents: new[] { "r" }),
            Commit("r", parents: Array.Empty<string>()),
        };
        var rows = CommitGraphBuilder.Build(commits);

        Assert.Equal(4, rows.Count);

        // m row: two outgoing edges — one straight to lane 0 (x continues there) and one
        // diagonal to a newly-allocated lane 1 (y starts there).
        Assert.Equal(0, rows[0].LaneIndex);
        Assert.Equal(2, rows[0].Edges.Count);
        Assert.Contains(rows[0].Edges, e => e.FromLane == 0 && e.ToLane == 0);
        Assert.Contains(rows[0].Edges, e => e.FromLane == 0 && e.ToLane == 1);

        // y row sits on lane 1.
        Assert.Equal(1, rows[2].LaneIndex);
        Assert.True(rows[2].IncomingFromAbove);

        // r row: when we visit r both x and y had it pending. x is processed first (row 1)
        // and sets lane 0 → "r"; y is processed next (row 2) and sees lane 0 already owns
        // "r", so y's lane 1 dies via a merge edge into lane 0.
        var mergeEdge = Assert.Single(rows[2].Edges);
        Assert.Equal(1, mergeEdge.FromLane);
        Assert.Equal(0, mergeEdge.ToLane);

        // After lane 1 dies, r itself emits zero edges (root) and the graph collapses to width 1
        // again — but MaxLanes for the whole walk is 2.
        Assert.Equal(2, rows[0].MaxLanes);
    }

    private static CommitInfo Commit(string sha, string[] parents)
        => new()
        {
            Sha = sha,
            Author = "Test",
            Email = "test@example.com",
            When = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            Message = $"commit {sha}",
            Parents = parents,
        };
}
