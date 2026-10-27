using System;
using System.Drawing;
using System.Drawing.Imaging;

// Offline assembly and material approximation, not a Unity screenshot.
public static class BoardPreview
{
    static double Smooth(double a, double b, double x)
    {
        double t = Math.Max(0, Math.Min(1, (x - a) / (b - a)));
        return t * t * (3 - 2 * t);
    }
    public static void Main(string[] args)
    {
        const int cell = 192, columns = 8, rows = 6;
        using (var source = new Bitmap(args[0]))
        using (var result = new Bitmap(columns * cell, rows * cell))
        {
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.FromArgb(28, 39, 54));
                float size = source.Width / 3f;
                for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                {
                    int x = column == 0 ? 0 : column == columns - 1 ? 2 : 1;
                    int y = row == 0 ? 0 : row == rows - 1 ? 2 : 1;
                    graphics.DrawImage(source, new Rectangle(column * cell, row * cell, cell, cell),
                        new RectangleF(x * size, y * size, size, size), GraphicsUnit.Pixel);
                }
            }
            for (int y = 0; y < result.Height; y++)
            for (int x = 0; x < result.Width; x++)
            {
                Color color = result.GetPixel(x, y);
                double face = Smooth(0.87, 0.96, Math.Min(color.R, Math.Min(color.G, color.B)) / 255.0);
                if (face <= 0) continue;
                double distance = Math.Min(Math.Min(x % cell, cell - x % cell), Math.Min(y % cell, cell - y % cell));
                double line = 1 - Smooth(0.2, 0.85, distance);
                double amount = (line * 0.72 + Math.Pow(2, -distance * distance / 3) * 0.055) * face;
                int r = (int)Math.Round(color.R * (1 - amount) + 5.1 * amount);
                int g = (int)Math.Round(color.G * (1 - amount) + 198.9 * amount);
                int b = (int)Math.Round(color.B * (1 - amount) + 224.4 * amount);
                result.SetPixel(x, y, Color.FromArgb(r, g, b));
            }
            result.Save(args[1], ImageFormat.Png);
        }
    }
}
