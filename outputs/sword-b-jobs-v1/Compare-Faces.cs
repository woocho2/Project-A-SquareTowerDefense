using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Read-only landmark comparison. It never warps or retouches the generated artwork.
public static class SwordFaceComparison
{
    private sealed class Edges
    {
        public int Width, Height;
        public float[] X, Y;
        public Edges(string path)
        {
            using (var image = new Bitmap(path))
            {
                Width = image.Width; Height = image.Height;
                var data = image.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bytes = new byte[data.Stride * Height];
                Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
                int stride = data.Stride;
                image.UnlockBits(data);
                var gray = new float[Width * Height];
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                    {
                        int i = y * stride + x * 4;
                        float alpha = bytes[i + 3] / 255f;
                        gray[y * Width + x] = (bytes[i + 2] * .299f + bytes[i + 1] * .587f + bytes[i] * .114f) * alpha + 255f * (1f - alpha);
                    }
                X = new float[gray.Length]; Y = new float[gray.Length];
                for (int y = 1; y < Height - 1; y++)
                    for (int x = 1; x < Width - 1; x++)
                    {
                        int i = y * Width + x;
                        X[i] = gray[i + 1] - gray[i - 1];
                        Y[i] = gray[i + Width] - gray[i - Width];
                    }
            }
        }
    }

    public static string Compare(string masterPath, string variantPath, string name)
    {
        var master = new Edges(masterPath);
        var variant = new Edges(variantPath);
        if (master.Width != variant.Width || master.Height != variant.Height)
            throw new Exception(name + ": canvas size differs from master.");
        string[] names = { "left_eye", "right_eye", "nose", "mouth", "chin", "left_jaw", "right_jaw" };
        Rectangle[] patches = {
            new Rectangle(450, 545, 150, 85), new Rectangle(650, 538, 150, 85),
            new Rectangle(595, 635, 50, 55), new Rectangle(585, 690, 90, 40),
            new Rectangle(586, 750, 90, 50), new Rectangle(394, 635, 75, 70),
            new Rectangle(755, 635, 75, 70)
        };
        var lines = new List<string>();
        for (int patch = 0; patch < patches.Length; patch++)
        {
            Rectangle rect = patches[patch];
            double best = -1;
            int bestX = 0, bestY = 0;
            for (int dy = -24; dy <= 24; dy++)
                for (int dx = -24; dx <= 24; dx++)
                {
                    double dot = 0, a2 = 0, b2 = 0;
                    for (int y = rect.Top; y < rect.Bottom; y += 2)
                        for (int x = rect.Left; x < rect.Right; x += 2)
                        {
                            int a = y * master.Width + x;
                            int b = (y + dy) * variant.Width + x + dx;
                            double ax = master.X[a], ay = master.Y[a], bx = variant.X[b], by = variant.Y[b];
                            dot += ax * bx + ay * by;
                            a2 += ax * ax + ay * ay;
                            b2 += bx * bx + by * by;
                        }
                    double correlation = dot / Math.Sqrt(Math.Max(1e-12, a2 * b2));
                    if (correlation > best) { best = correlation; bestX = dx; bestY = dy; }
                }
            double cellError = Math.Sqrt(bestX * bestX + bestY * bestY) * 480.0 / 1254.0;
            lines.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F4},{5:F3}", name, names[patch], bestX, bestY, best, cellError));
        }
        return String.Join(Environment.NewLine, lines);
    }
}
