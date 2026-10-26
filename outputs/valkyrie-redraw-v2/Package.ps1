param([Parameter(Mandatory = $true)][string]$SourceImage)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetFolder = Join-Path $repoRoot 'Assets/4. Asset/4. Enemy/Asgard'
$beforeFolder = Join-Path $PSScriptRoot 'before'
if (-not (Test-Path -LiteralPath $beforeFolder)) {
    [IO.Directory]::CreateDirectory($beforeFolder) | Out-Null
    foreach ($name in @('Valkyrie_Character.png', 'Valkyrie_Character.png.meta', 'Valkyrie_EquipmentMask.png', 'Valkyrie_EquipmentMask.png.meta', 'Valkyrie_Appearance.asset', 'Valkyrie_Appearance.asset.meta')) {
        Copy-Item -LiteralPath (Join-Path $assetFolder $name) -Destination (Join-Path $beforeFolder $name)
    }
}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
public static class ValkyrieRedrawPackage {
    static Point[] Points(params int[] coordinates) {
        var points = new Point[coordinates.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = new Point(coordinates[i * 2], coordinates[i * 2 + 1]);
        return points;
    }
    static bool Inside(int x, int y, Point[] points) {
        bool hit = false;
        for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
            if ((points[i].Y > y) != (points[j].Y > y)
                && x < (points[j].X - points[i].X) * (y - points[i].Y) / (double)(points[j].Y - points[i].Y) + points[i].X) hit = !hit;
        return hit;
    }
    static readonly Point[] Wing = Points(133,180,139,195,151,212,158,224,166,228,172,243,168,255,153,269,144,267,144,253,126,249,120,241,128,241,118,229,115,206,126,213,127,194);
    static readonly Point[] Band = Points(168,227,208,232,236,226,257,213,275,226,302,233,344,228,343,241,278,242,257,253,237,242,208,243,169,240);
    static readonly Point[] Collar = Points(217,370,232,377,256,382,280,378,298,370,309,383,303,410,293,425,278,437,257,452,234,433,216,415,215,386);
    static readonly Point[] Chest = Points(213,421,236,432,256,423,276,432,299,419,314,438,303,461,306,477,332,493,334,502,179,502,179,493,204,477,209,462,199,449);
    static readonly Point[] Shoulder = Points(117,391,134,392,150,400,165,417,175,435,174,445,161,451,152,462,143,479,144,502,33,502,46,486,68,472,75,441,91,410);
    public static void Build(string source, string output) {
        using (var original = new Bitmap(source))
        using (var portrait = new Bitmap(512, 512, PixelFormat.Format32bppArgb))
        using (var mask = new Bitmap(512, 512, PixelFormat.Format32bppArgb))
        using (var tinted = new Bitmap(512, 512, PixelFormat.Format32bppArgb)) {
            using (var g = Graphics.FromImage(portrait)) {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(original, new Rectangle(0, 0, 512, 512));
            }
            int selected = 0, minX = 512, minY = 512, maxX = -1, maxY = -1;
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++) {
                var p = portrait.GetPixel(x, y);
                if (p.A > 16) {
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
                int high = Math.Max(p.R, Math.Max(p.G, p.B));
                int low = Math.Min(p.R, Math.Min(p.G, p.B));
                // Metal is neutral; lavender hair, skin, and eyes retain their colors.
                // Restrict selection to the circlet/wings and the armored torso.
                bool equipmentZone = Inside(x,y,Wing) || Inside(512-x,y,Wing) || Inside(x,y,Band)
                    || Inside(x,y,Collar) || Inside(x,y,Chest)
                    || Inside(x,y,Shoulder) || Inside(512-x,y,Shoulder);
                bool gear = p.A > 0 && equipmentZone && high - low <= 40
                    && p.B - p.R <= 10 && p.B - p.G <= 10;
                mask.SetPixel(x, y, gear ? Color.White : Color.Black);
                if (gear) selected++;
                tinted.SetPixel(x, y, gear
                    ? Color.FromArgb(p.A, p.R * 241 / 255, p.G * 137 / 255, p.B * 158 / 255) : p);
            }
            if (portrait.GetPixel(0, 0).A != 0 || portrait.GetPixel(256, 0).A != 0
                || minX < 8 || maxX > 503 || minY < 100 || maxY > 503 || selected < 6000)
                throw new Exception("Portrait validation failed. Bounds=" + minX + "," + minY + ".." + maxX + "," + maxY + "; mask=" + selected);
            portrait.Save(Path.Combine(output, "Valkyrie_Character.png"), ImageFormat.Png);
            mask.Save(Path.Combine(output, "Valkyrie_EquipmentMask.png"), ImageFormat.Png);
            using (var before = new Bitmap(Path.Combine(output, "before/Valkyrie_Character.png")))
            using (var preview = new Bitmap(1536, 544, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(preview))
            using (var font = new Font("Segoe UI", 14, FontStyle.Regular)) {
                g.Clear(Color.FromArgb(48, 49, 60));
                g.DrawImage(before, new Rectangle(0, 32, 512, 512));
                g.DrawImageUnscaled(portrait, 512, 32);
                g.DrawImageUnscaled(tinted, 1024, 32);
                g.DrawString("Before", font, Brushes.White, 16, 4);
                g.DrawString("Redrawn", font, Brushes.White, 528, 4);
                g.DrawString("Equipment tint", font, Brushes.White, 1040, 4);
                preview.Save(Path.Combine(output, "comparison.png"), ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(output, "verification.txt"),
                "512x512 RGBA portrait; transparent exterior and badge margin.\n"
                + "Visible alpha bounds: " + minX + "," + minY + " .. " + maxX + "," + maxY + "\n"
                + "Equipment mask selected pixels: " + selected + "\n"
                + "Mask is opaque black/white and selects only visible portrait pixels.\n"
                + "Unity .meta and appearance references preserved on application.\n");
        }
    }
}
'@
[ValkyrieRedrawPackage]::Build([IO.Path]::GetFullPath($SourceImage), $PSScriptRoot)
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt')
