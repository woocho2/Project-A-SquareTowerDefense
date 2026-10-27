using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Native source for the board's existing 192px nine-slice atlas.
// Only board plates are rendered here; playable cell artwork is separate.
public static class BoardFrameNeonGenerator
{
    private const int Cell = 192;
    private const int Plate = Cell * 3;
    private static readonly string[] Accents = {
        "00CDEC", "44C989", "91C94D", "7AB3F5", "38AEF2",
        "B98CFF", "70D8F5", "21D4C1", "FF9665", "F07ABD"
    };

    private static Color Hex(string hex) { return ColorTranslator.FromHtml("#" + hex); }

    private static GraphicsPath Chamfer(float x, float y, float width, float height, float corner)
    {
        var path = new GraphicsPath();
        path.AddPolygon(new[] {
            new PointF(x + corner, y), new PointF(x + width - corner, y),
            new PointF(x + width, y + corner), new PointF(x + width, y + height - corner),
            new PointF(x + width - corner, y + height), new PointF(x + corner, y + height),
            new PointF(x, y + height - corner), new PointF(x, y + corner)
        });
        path.CloseFigure();
        return path;
    }

    private static void NeonLine(Graphics graphics, Color color, PointF from, PointF to)
    {
        int[] widths = { 10, 6, 3, 1 };
        int[] opacity = { 12, 24, 100, 235 };
        for (int i = 0; i < widths.Length; i++)
        using (var pen = new Pen(Color.FromArgb(opacity[i], color), widths[i]))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            graphics.DrawLine(pen, from, to);
        }
    }

    private static void NeonPath(Graphics graphics, Color color, GraphicsPath path)
    {
        int[] widths = { 9, 5, 2 };
        int[] opacity = { 16, 35, 225 };
        for (int i = 0; i < widths.Length; i++)
        using (var pen = new Pen(Color.FromArgb(opacity[i], color), widths[i]))
            graphics.DrawPath(pen, path);
    }

    private static void DrawPlate(Graphics graphics, Color accent)
    {
        // The old plate's bounds are x134..442, y134..472. Keep the same footprint.
        using (var front = Chamfer(136, 162, 304, 308, 14))
        using (var shade = new LinearGradientBrush(new Rectangle(136, 162, 304, 308),
            Hex("D4DEE9"), Hex("8D9BAF"), 90f))
        using (var edge = new Pen(Hex("718296"), 2f))
        {
            graphics.FillPath(shade, front);
            graphics.DrawPath(edge, front);
        }

        using (var outer = Chamfer(136, 136, 304, 304, 15))
        using (var shell = new LinearGradientBrush(new Rectangle(136, 136, 304, 304),
            Color.White, Hex("D9E4EF"), 90f))
        using (var outline = new Pen(Hex("90A2B7"), 2f))
        {
            graphics.FillPath(shell, outer);
            graphics.DrawPath(outline, outer);
        }

        using (var floor = Chamfer(151, 151, 274, 274, 9))
        using (var floorBrush = new SolidBrush(Hex("F8FCFF")))
        {
            graphics.FillPath(floorBrush, floor);
            GraphicsState state = graphics.Save();
            graphics.SetClip(floor);
            // Lines at multiples of 192 meet across all repeated centre and edge slices.
            foreach (int coordinate in new[] { 192, 384 })
            {
                NeonLine(graphics, accent, new PointF(coordinate, 150), new PointF(coordinate, 426));
                NeonLine(graphics, accent, new PointF(150, coordinate), new PointF(426, coordinate));
            }
            graphics.Restore(state);
        }

        using (var rim = Chamfer(145, 145, 286, 286, 11))
            NeonPath(graphics, accent, rim);
        using (var innerEdge = Chamfer(152, 152, 272, 272, 8))
        using (var fine = new Pen(Color.FromArgb(105, accent), 1f))
            graphics.DrawPath(fine, innerEdge);

        // Small white ceramic corner caps frame a single board, not individual game cells.
        foreach (int x in new[] { 159, 417 })
        foreach (int y in new[] { 159, 417 })
        {
            using (var cap = Chamfer(x - 9, y - 9, 18, 18, 5))
            using (var capFill = new SolidBrush(Hex("EEF5FB")))
            using (var capEdge = new Pen(Hex("B0C0CF"), 1f))
            {
                graphics.FillPath(capFill, cap);
                graphics.DrawPath(capEdge, cap);
            }
            using (var light = new SolidBrush(accent))
                graphics.FillEllipse(light, x - 2, y - 2, 4, 4);
        }
        NeonLine(graphics, accent, new PointF(180, 450), new PointF(396, 450));
        using (var highlight = new Pen(Color.FromArgb(200, Color.White), 1f))
            graphics.DrawLine(highlight, 151, 139, 425, 139);
    }

    private static void Preview(Bitmap atlas, string destination)
    {
        const int columns = 8, rows = 6;
        using (var preview = new Bitmap(columns * Cell, rows * Cell, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(preview))
        {
            graphics.Clear(Hex("1C2736"));
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int sourceX = x == 0 ? 0 : x == columns - 1 ? 2 : 1;
                int sourceY = y == 0 ? 0 : y == rows - 1 ? 2 : 1;
                graphics.DrawImage(atlas, new Rectangle(x * Cell, y * Cell, Cell, Cell),
                    new Rectangle(sourceX * Cell, sourceY * Cell, Cell, Cell), GraphicsUnit.Pixel);
            }
            preview.Save(destination, ImageFormat.Png);
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length != 2) return 1;
        Directory.CreateDirectory(Path.GetDirectoryName(args[0]));
        Directory.CreateDirectory(Path.GetDirectoryName(args[1]));
        using (var atlas = new Bitmap(Plate * 5, Plate * 2, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(atlas))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            for (int i = 0; i < Accents.Length; i++)
            {
                GraphicsState state = graphics.Save();
                graphics.TranslateTransform(i % 5 * Plate, i / 5 * Plate);
                DrawPlate(graphics, Hex(Accents[i]));
                graphics.Restore(state);
            }
            atlas.Save(args[0], ImageFormat.Png);
            Preview(atlas, args[1]);
        }
        Console.WriteLine("White neon board atlas: 2880 x 1152, 90 unchanged 192px sprite rectangles.");
        return 0;
    }
}
