namespace TechTeaStudio.GitClient.App.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TechTeaStudio.GitClient.App.Graph;

/// <summary>
/// Renders a single commit graph row: pass-through lane verticals, the dot's incoming top half
/// (when the lane already existed above), the outgoing edges (straight verticals or diagonals
/// for merge/branch points), and the dot itself.
///
/// Stateless beyond the bound <see cref="Row"/> — designed to live inside a virtualizing list
/// row template so the cost stays O(visible rows).
/// </summary>
public sealed class CommitGraphRowControl : Control
{
    public static readonly StyledProperty<CommitGraphRow?> RowProperty =
        AvaloniaProperty.Register<CommitGraphRowControl, CommitGraphRow?>(nameof(Row));

    public static readonly StyledProperty<double> LaneWidthProperty =
        AvaloniaProperty.Register<CommitGraphRowControl, double>(nameof(LaneWidth), 14d);

    public static readonly StyledProperty<double> DotRadiusProperty =
        AvaloniaProperty.Register<CommitGraphRowControl, double>(nameof(DotRadius), 4.5d);

    public static readonly StyledProperty<double> LineThicknessProperty =
        AvaloniaProperty.Register<CommitGraphRowControl, double>(nameof(LineThickness), 2d);

    private static readonly Color[] Palette =
    {
        Color.Parse("#9D2499"), // primary purple
        Color.Parse("#5EC6C6"), // tertiary teal
        Color.Parse("#4CAF50"), // success green
        Color.Parse("#FF9800"), // warning orange
        Color.Parse("#29B6F6"), // info blue
        Color.Parse("#EF5350"), // danger red
        Color.Parse("#FFEB3B"), // yellow
        Color.Parse("#BA68C8"), // light purple
    };

    public static readonly StyledProperty<Color> DotEdgeColorProperty =
        AvaloniaProperty.Register<CommitGraphRowControl, Color>(nameof(DotEdgeColor), Color.Parse("#121218"));

    public Color DotEdgeColor
    {
        get => GetValue(DotEdgeColorProperty);
        set => SetValue(DotEdgeColorProperty, value);
    }

    static CommitGraphRowControl()
    {
        AffectsRender<CommitGraphRowControl>(RowProperty, LaneWidthProperty, DotRadiusProperty, LineThicknessProperty, DotEdgeColorProperty);
        AffectsMeasure<CommitGraphRowControl>(RowProperty, LaneWidthProperty);
    }

    public CommitGraphRow? Row
    {
        get => GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public double LaneWidth
    {
        get => GetValue(LaneWidthProperty);
        set => SetValue(LaneWidthProperty, value);
    }

    public double DotRadius
    {
        get => GetValue(DotRadiusProperty);
        set => SetValue(DotRadiusProperty, value);
    }

    public double LineThickness
    {
        get => GetValue(LineThicknessProperty);
        set => SetValue(LineThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var lanes = Row?.MaxLanes ?? 0;
        var width = Math.Max(LaneWidth, lanes * LaneWidth);
        return new Size(width, availableSize.Height);
    }

    public override void Render(DrawingContext context)
    {
        var row = Row;
        if (row is null) return;

        var height = Bounds.Height;
        if (height <= 0) return;

        var midY = height / 2.0;
        var laneW = LaneWidth;
        var thickness = LineThickness;

        double LaneX(int idx) => (idx + 0.5) * laneW;

        // 1. Pass-through lane verticals (full row height).
        foreach (var lane in row.Lanes)
        {
            if (lane.Index == row.LaneIndex) continue;
            var brush = new SolidColorBrush(Palette[lane.ColorIndex % Palette.Length]);
            var pen = new Pen(brush, thickness);
            var x = LaneX(lane.Index);
            context.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        // 2. Incoming-from-above half (top of row → dot) for the dot's own lane.
        if (row.IncomingFromAbove)
        {
            var brush = new SolidColorBrush(Palette[row.ColorIndex % Palette.Length]);
            var pen = new Pen(brush, thickness);
            var x = LaneX(row.LaneIndex);
            context.DrawLine(pen, new Point(x, 0), new Point(x, midY));
        }

        // 3. Outgoing edges (dot → next row's lanes).
        foreach (var edge in row.Edges)
        {
            var brush = new SolidColorBrush(Palette[edge.ColorIndex % Palette.Length]);
            var pen = new Pen(brush, thickness);
            var x0 = LaneX(edge.FromLane);
            var x1 = LaneX(edge.ToLane);
            if (Math.Abs(x0 - x1) < 0.01)
            {
                context.DrawLine(pen, new Point(x0, midY), new Point(x1, height));
            }
            else
            {
                // Smooth bezier so merges/branches look material, not jagged.
                var c1 = new Point(x0, height - 2);
                var c2 = new Point(x1, midY + 2);
                var geo = new StreamGeometry();
                using (var gctx = geo.Open())
                {
                    gctx.BeginFigure(new Point(x0, midY), isFilled: false);
                    gctx.CubicBezierTo(c1, c2, new Point(x1, height));
                    gctx.EndFigure(isClosed: false);
                }
                context.DrawGeometry(null, pen, geo);
            }
        }

        // 4. Dot.
        var dotBrush = new SolidColorBrush(Palette[row.ColorIndex % Palette.Length]);
        var dotPen = new Pen(new SolidColorBrush(DotEdgeColor), 1.5);
        var center = new Point(LaneX(row.LaneIndex), midY);
        context.DrawEllipse(dotBrush, dotPen, center, DotRadius, DotRadius);
    }
}
