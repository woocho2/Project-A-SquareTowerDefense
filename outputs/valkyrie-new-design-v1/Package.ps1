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
public static class NewValkyriePackage {
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
                // Exclude the eyes and select neutral metal rather than golden hair or blue fabric.
                bool equipmentZone = y <= 282 || (y <= 306 && (x < 185 || x > 327)) || y >= 369;
                equipmentZone &= !(y >= 270 && y <= 375 && x >= 170 && x <= 342);
                bool gear = p.A > 0 && equipmentZone && high - low <= 32
                    && p.B - p.R <= 18 && p.R - p.B <= 26 && p.B - p.G <= 18;
                mask.SetPixel(x, y, gear ? Color.White : Color.Black);
                if (gear) selected++;
                tinted.SetPixel(x, y, gear
                    ? Color.FromArgb(p.A, p.R * 241 / 255, p.G * 137 / 255, p.B * 158 / 255) : p);
            }
            // Close tiny gaps caused by the reference style's textured metal shading.
            const int radius = 3;
            var dilated = new bool[512 * 512];
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++) {
                bool hit = false;
                for (int dy = -radius; dy <= radius && !hit; dy++)
                    for (int dx = -radius; dx <= radius; dx++) {
                        int xx = x + dx, yy = y + dy;
                        if (xx >= 0 && xx < 512 && yy >= 0 && yy < 512 && mask.GetPixel(xx, yy).R > 127) { hit = true; break; }
                    }
                dilated[y * 512 + x] = hit;
            }
            selected = 0;
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++) {
                bool hit = true;
                for (int dy = -radius; dy <= radius && hit; dy++)
                    for (int dx = -radius; dx <= radius; dx++) {
                        int xx = x + dx, yy = y + dy;
                        if (xx < 0 || xx >= 512 || yy < 0 || yy >= 512 || !dilated[yy * 512 + xx]) { hit = false; break; }
                    }
                var p = portrait.GetPixel(x, y);
                hit = hit && p.A > 0 && p.R - p.B <= 32 && p.B - p.R <= 24;
                hit &= !(y >= 270 && y <= 375 && x >= 170 && x <= 342);
                mask.SetPixel(x, y, hit ? Color.White : Color.Black);
                if (hit) selected++;
                tinted.SetPixel(x, y, hit
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
                g.DrawImage(portrait, new Rectangle(512, 32, 512, 512));
                g.DrawImage(tinted, new Rectangle(1024, 32, 512, 512));
                g.DrawString("Previous design", font, Brushes.White, 16, 4);
                g.DrawString("New character", font, Brushes.White, 528, 4);
                g.DrawString("Equipment tint", font, Brushes.White, 1040, 4);
                preview.Save(Path.Combine(output, "comparison.png"), ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(output, "verification.txt"),
                "512x512 RGBA portrait; transparent exterior.\n"
                + "Visible alpha bounds: " + minX + "," + minY + " .. " + maxX + "," + maxY + "\n"
                + "Equipment mask selected pixels: " + selected + "\n"
                + "Mask is opaque black/white and selects only visible portrait pixels.\n");
        }
    }
}
'@
[NewValkyriePackage]::Build([IO.Path]::GetFullPath($SourceImage), $PSScriptRoot)
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt')
