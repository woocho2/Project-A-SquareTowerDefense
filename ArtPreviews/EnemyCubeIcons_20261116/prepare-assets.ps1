$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class EnemyBadgeFinishing
{
    public static void Prepare(string originalPath, string generatedPath, string atlasPath, string bossPath)
    {
        using (var original = new Bitmap(originalPath))
        using (var atlas = (Bitmap)original.Clone())
        {
            for (int cell = 1; cell <= 2; cell++)
            {
                int startX = cell * 128;
                Color oldBackground = original.GetPixel(startX + 64, 226);
                int oldMin = Math.Min(oldBackground.R, Math.Min(oldBackground.G, oldBackground.B));
                int target = cell == 1 ? 12 : 125;
                for (int y = 128; y < 256; y++)
                for (int x = startX; x < startX + 128; x++)
                {
                    Color p = original.GetPixel(x, y);
                    int lo = Math.Min(p.R, Math.Min(p.G, p.B));
                    int hi = Math.Max(p.R, Math.Max(p.G, p.B));
                    if (p.A == 0 || hi - lo < 5) continue;
                    double whiteCoverage = Math.Max(0, Math.Min(1, (lo - oldMin) / (255.0 - oldMin)));
                    int value = (int)Math.Round(target + (255 - target) * whiteCoverage);
                    atlas.SetPixel(x, y, Color.FromArgb(p.A, value, value, value));
                }
            }
            atlas.Save(atlasPath, ImageFormat.Png);
            using (var generated = new Bitmap(generatedPath))
            using (var glyph = new Bitmap(generated.Width, generated.Height, PixelFormat.Format32bppArgb))
            using (var badge = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
            {
                double cx = generated.Width * 0.5;
                double cy = generated.Height * 0.5;
                double radius = generated.Width * 0.22;
                for (int y = (int)(cy - radius); y <= cy + radius; y++)
                for (int x = (int)(cx - radius); x <= cx + radius; x++)
                {
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) > radius * radius) continue;
                    Color p = generated.GetPixel(x, y);
                    int light = Math.Min(p.R, Math.Min(p.G, p.B));
                    int alpha = (int)Math.Round(255 * Math.Max(0, Math.Min(1, (light - 40) / 170.0)));
                    if (alpha > 0) glyph.SetPixel(x, y, Color.FromArgb(alpha, 255, 255, 255));
                }
                using (Graphics g = Graphics.FromImage(badge))
                {
                    g.Clear(Color.Transparent);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (var rim = new SolidBrush(Color.FromArgb(220, 220, 220))) g.FillEllipse(rim, 51, 51, 154, 154);
                    using (var disk = new SolidBrush(Color.FromArgb(12, 12, 12))) g.FillEllipse(disk, 54, 54, 148, 148);
                    g.DrawImage(glyph, new Rectangle(52, 52, 152, 152), new Rectangle(282, 284, 686, 686), GraphicsUnit.Pixel);
                }
                badge.Save(bossPath, ImageFormat.Png);
            }
        }
    }
}
'@
$enemyProjectRoot = (Get-Location).Path
$enemyPreviewRoot = Join-Path $enemyProjectRoot 'ArtPreviews/EnemyCubeIcons_20261116'
[EnemyBadgeFinishing]::Prepare(
    (Join-Path $enemyProjectRoot 'ArtBackups/EnemyCubeVisual_20261116_01/Assets/4. Asset/4. Enemy/Enemy_State_Badges_Atlas.png'),
    (Join-Path $enemyPreviewRoot 'boss-badge-generated.png'),
    (Join-Path $enemyPreviewRoot 'Enemy_State_Badges_Atlas.png'),
    (Join-Path $enemyPreviewRoot 'Enemy_Boss_Gimmick_Badge.png'))
