namespace TechTeaStudio.GitClient.App.Graph;

using TechTeaStudio.GitClient.Models;

/// <summary>
/// Builds per-row graph state from a list of commits ordered newest-first (HEAD walk order).
/// Standard lane algorithm: each lane tracks the next-expected SHA; on visiting a commit we look
/// up its lane, emit a row, then replace the lane's expected SHA with the first parent's. Extra
/// parents either merge into an existing lane or allocate a fresh one. Cost is O(n × lanes).
/// </summary>
public static class CommitGraphBuilder
{
    public const int ColorCount = 8;

    public static IReadOnlyList<CommitGraphRow> Build(IReadOnlyList<CommitInfo> commits)
    {
        ArgumentNullException.ThrowIfNull(commits);
        if (commits.Count == 0) return Array.Empty<CommitGraphRow>();

        var rows = new List<CommitGraphRow>(commits.Count);
        var laneSha = new List<string?>();
        var laneColor = new List<int>();
        var nextColor = 0;
        var maxLanes = 0;

        foreach (var c in commits)
        {
            var incomingFromAbove = laneSha.Contains(c.Sha);

            // Snapshot input lanes BEFORE any allocation so freshly-born lanes don't appear
            // as a pass-through vertical at this row.
            var rowLanes = new List<GraphLane>();
            for (int i = 0; i < laneSha.Count; i++)
            {
                if (laneSha[i] is not null) rowLanes.Add(new GraphLane(i, laneColor[i]));
            }

            int myLane;
            int myColor;
            if (incomingFromAbove)
            {
                myLane = laneSha.IndexOf(c.Sha);
                myColor = laneColor[myLane];
            }
            else
            {
                myLane = FirstFree(laneSha);
                if (myLane == -1)
                {
                    myLane = laneSha.Count;
                    laneSha.Add(null);
                    laneColor.Add(0);
                }
                myColor = nextColor++ % ColorCount;
                laneColor[myLane] = myColor;
                laneSha[myLane] = c.Sha;
            }

            var edges = new List<GraphEdge>();

            if (c.Parents.Count == 0)
            {
                laneSha[myLane] = null;
            }
            else
            {
                var firstParent = c.Parents[0];
                var existing = IndexOfExcept(laneSha, firstParent, except: myLane);
                if (existing != -1)
                {
                    // First parent already expected by another lane → this lane merges into it.
                    laneSha[myLane] = null;
                    edges.Add(new GraphEdge(myLane, existing, myColor));
                }
                else
                {
                    laneSha[myLane] = firstParent;
                    edges.Add(new GraphEdge(myLane, myLane, myColor));
                }

                for (int pi = 1; pi < c.Parents.Count; pi++)
                {
                    var p = c.Parents[pi];
                    var ex = laneSha.IndexOf(p);
                    if (ex != -1)
                    {
                        edges.Add(new GraphEdge(myLane, ex, laneColor[ex]));
                    }
                    else
                    {
                        var newLane = FirstFree(laneSha);
                        if (newLane == -1)
                        {
                            newLane = laneSha.Count;
                            laneSha.Add(null);
                            laneColor.Add(0);
                        }
                        var newColor = nextColor++ % ColorCount;
                        laneSha[newLane] = p;
                        laneColor[newLane] = newColor;
                        edges.Add(new GraphEdge(myLane, newLane, newColor));
                    }
                }
            }

            while (laneSha.Count > 0 && laneSha[^1] is null)
            {
                laneSha.RemoveAt(laneSha.Count - 1);
                laneColor.RemoveAt(laneColor.Count - 1);
            }

            var rightmost = laneSha.Count;
            if (rowLanes.Count > 0 && rowLanes[^1].Index + 1 > rightmost) rightmost = rowLanes[^1].Index + 1;
            if (myLane + 1 > rightmost) rightmost = myLane + 1;
            if (rightmost > maxLanes) maxLanes = rightmost;

            rows.Add(new CommitGraphRow
            {
                LaneIndex = myLane,
                ColorIndex = myColor,
                IncomingFromAbove = incomingFromAbove,
                Lanes = rowLanes,
                Edges = edges,
                MaxLanes = 0,
            });
        }

        var final = new List<CommitGraphRow>(rows.Count);
        foreach (var r in rows) final.Add(r with { MaxLanes = maxLanes });
        return final;
    }

    private static int FirstFree(List<string?> lanes)
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            if (lanes[i] is null) return i;
        }
        return -1;
    }

    private static int IndexOfExcept(List<string?> lanes, string sha, int except)
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            if (i != except && string.Equals(lanes[i], sha, StringComparison.Ordinal)) return i;
        }
        return -1;
    }
}
