using System;
using System.Drawing;
using System.Drawing.Imaging;

// Keep the existing Common frame's straight-edge thickness and shading; omit corner ornaments.
public static class CommonThinFrameGenerator
{
    private static double RoundedRect(double x, double y, double halfSize, double radius)
    {
        double dx = Math.Abs(x) - halfSize + radius;
        double dy = Math.Abs(y) - halfSize + radius;
        return Math.Sqrt(Math.Max(dx, 0) * Math.Max(dx, 0) + Math.Max(dy, 0) * Math.Max(dy, 0))
            + Math.Min(Math.Max(dx, dy), 0) - radius;
    }

    public static int Main(string[] args)
    {
        if (args.Length != 2) return 1;
        const int size = 512;
        const double radius = 52; // Original frame's outer rounded-rectangle radius.
        using (var reference = new Bitmap(args[0]))
        using (var image = new Bitmap(size, size, PixelFormat.Format32bppArgb))
        {
            int inset = 0;
            while (inset < size / 2 && reference.GetPixel(size / 2, inset).A < 128) inset++;
            int thickness = 0;
            while (inset + thickness < size / 2 && reference.GetPixel(size / 2, inset + thickness).A >= 128)
                thickness++;
            if (inset >= size / 2 || thickness == 0) throw new InvalidOperationException("Cannot measure the existing frame.");
            var profile = new Color[thickness];
            for (int i = 0; i < thickness; i++) profile[i] = reference.GetPixel(size / 2, inset + i);
            double halfSize = size * 0.5 - inset;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                double px = x + 0.5 - size * 0.5, py = y + 0.5 - size * 0.5;
                double distance = RoundedRect(px, py, halfSize, radius);
                double outer = Math.Max(0, Math.Min(1, 0.5 - distance));
                double inner = Math.Max(0, Math.Min(1,
                    0.5 - RoundedRect(px, py, halfSize - thickness, radius - thickness)));
                int alpha = (int)Math.Round(Math.Max(0, outer - inner) * 255);
                alpha = Math.Min(alpha, reference.GetPixel(x, y).A);
                if (alpha > 0)
                {
                    int depth = Math.Max(0, Math.Min(thickness - 1, (int)Math.Floor(-distance)));
                    Color paint = profile[depth];
                    image.SetPixel(x, y, Color.FromArgb(alpha, paint.R, paint.G, paint.B));
                }
            }
            image.Save(args[1], ImageFormat.Png);
            Console.WriteLine("Existing frame matched: " + thickness + "px edge, " + inset + "px source inset, original grayscale profile.");
        }
        return 0;
    }
}
