$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class RagnarokPaletteReview {
    public static string Check(string oldPath, string newPath, bool floor) {
        using (Bitmap before = new Bitmap(oldPath))
        using (Bitmap after = new Bitmap(newPath)) {
            if (before.Size != after.Size) throw new Exception("Atlas dimensions changed.");
            int changed = 0;
            for (int y = 0; y < before.Height; y++) for (int x = 0; x < before.Width; x++) {
                Color a = before.GetPixel(x,y), b = after.GetPixel(x,y);
                if (a.A != b.A) throw new Exception("Transparency changed at " + x + "," + y);
                if (a.ToArgb() == b.ToArgb()) continue;
                if (floor && !(y < 256 && ((x >= 768 && x < 1024) || (x >= 1792 && x < 2048))))
                    throw new Exception("A different realm's floor changed.");
                if (!floor && a.R == 255 && a.G == 255 && a.B == 255)
                    throw new Exception("White label pixels changed.");
                changed++;
            }
            if (changed == 0) throw new Exception("No palette change found.");
            return (floor ? "Floor" : "Ragnarok overlay") + ": " + changed + " changed pixels; dimensions and alpha preserved.";
        }
    }
    public static void Preview(string buildRoot, string destination) {
        using (Bitmap floors = new Bitmap(System.IO.Path.Combine(buildRoot, "Tile_Floor_Atlas.png")))
        using (Bitmap canvas = new Bitmap(720,590,PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(canvas))
        using (Font title = new Font("Malgun Gothic",16,FontStyle.Bold))
        using (Font small = new Font("Malgun Gothic",11)) {
            g.Clear(ColorTranslator.FromHtml("#19212C"));
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            string[] realms = { "Asgard", "Ragnarok" };
            string[] names = { "아스가르드", "라그나로크 · 빛바랜 아스가르드" };
            string[] labels = { "타워 스폰", "패스", "START" };
            for (int row=0;row<2;row++) {
                int top=15+row*290, cell=row==0 ? 0 : 3;
                g.DrawString(names[row],title,Brushes.White,20,top);
                using (Bitmap overlay = new Bitmap(System.IO.Path.Combine(buildRoot,"Tile_Path_Overlay_"+realms[row]+"_Atlas.png"))) {
                    for (int col=0;col<3;col++) {
                        int left=20+col*235;
                        g.DrawString(labels[col],small,Brushes.LightGray,left,top+36);
                        Rectangle target = new Rectangle(left,top+65,200,200);
                        Rectangle floor = new Rectangle(cell*256+(col==0 ? 1024:0),0,256,256);
                        Rectangle ornament = new Rectangle(col==2 ? 0:512,512,256,256);
                        g.DrawImage(floors,target,floor,GraphicsUnit.Pixel);
                        g.DrawImage(overlay,target,ornament,GraphicsUnit.Pixel);
                    }
                }
            }
            canvas.Save(destination,ImageFormat.Png);
        }
    }
}
'@
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$runtime = Join-Path $repo 'Assets/4. Asset/1. BackGround'
$build = Join-Path $PSScriptRoot 'build'
$floorName = 'Tile_Floor_Atlas.png'
$overlayName = 'Tile_Path_Overlay_Ragnarok_Atlas.png'
$floorReport = [RagnarokPaletteReview]::Check((Join-Path $runtime $floorName),(Join-Path $build $floorName),$true)
$overlayReport = [RagnarokPaletteReview]::Check((Join-Path $runtime $overlayName),(Join-Path $build $overlayName),$false)
$otherRealms = @('Asgard','Alfheim','Vanaheim','Midgard','Jotunheim','Nidavellir','Niflheim','Muspelheim','Hel')
foreach ($realm in $otherRealms) {
    $name = 'Tile_Path_Overlay_' + $realm + '_Atlas.png'
    $beforeHash = (Get-FileHash -LiteralPath (Join-Path $runtime $name)).Hash
    $afterHash = (Get-FileHash -LiteralPath (Join-Path $build $name)).Hash
    if ($beforeHash -ne $afterHash) { throw "Unexpected overlay change: $realm" }
}
$metaBefore = @{}
foreach ($name in @($floorName,$overlayName)) {
    $metaBefore[$name] = (Get-FileHash -LiteralPath (Join-Path $runtime ($name + '.meta'))).Hash
    $backupName = $name.Replace('.png','.before-ragnarok.png')
    $backupPath = Join-Path $PSScriptRoot $backupName
    if (-not (Test-Path -LiteralPath $backupPath)) { Copy-Item -LiteralPath (Join-Path $runtime $name) -Destination $backupPath }
}
$preview = Join-Path $PSScriptRoot 'preview/asgard-ragnarok.png'
[RagnarokPaletteReview]::Preview($build,$preview)
foreach ($name in @($floorName,$overlayName)) {
    Copy-Item -LiteralPath (Join-Path $build $name) -Destination (Join-Path $runtime $name) -Force
    if ((Get-FileHash -LiteralPath (Join-Path $runtime ($name + '.meta'))).Hash -ne $metaBefore[$name]) {
        throw 'Sprite metadata changed.'
    }
}
$report = @($floorReport,$overlayReport,'Other nine realm overlays unchanged.','Both sprite metadata files unchanged.','Applied only floor atlas and Ragnarok overlay PNGs.')
$report | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'ragnarok-validation.txt') -Encoding UTF8
$report
Write-Output "Preview: $preview"
