$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class RubyTierPalette {
    public static void Apply(string nativePath,string input,string output,bool decoration) {
        using(var native=new Bitmap(nativePath))using(var normalized=new Bitmap(768,256,PixelFormat.Format32bppArgb))
        using(var current=new Bitmap(input))using(var result=current.Clone(new Rectangle(0,0,current.Width,current.Height),PixelFormat.Format32bppArgb)) {
            if(current.Width!=768||current.Height!=512)throw new Exception("Unexpected tier atlas size.");
            using(var g=Graphics.FromImage(normalized)) {
                g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(native,new Rectangle(0,0,768,256));
            }
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                var original=current.GetPixel(512+x,256+y);
                var generated=decoration?normalized.GetPixel(256+x,y):normalized.GetPixel(128,16+(int)Math.Round(Math.Max(0,Math.Min(242,y-7))*223.0/242));
                result.SetPixel(512+x,256+y,original.A==0?Color.Transparent:Color.FromArgb(original.A,generated.R,generated.G,generated.B));
            }
            int changed=0;
            for(int y=0;y<512;y++)for(int x=0;x<768;x++) {
                var before=current.GetPixel(x,y);var after=result.GetPixel(x,y);
                if(before.A!=after.A)throw new Exception("Alpha geometry changed.");
                if(x<512||y<256){if(before.ToArgb()!=after.ToArgb())throw new Exception("Another tier changed.");}
                else if(before.ToArgb()!=after.ToArgb())changed++;
            }
            if(decoration&&result.GetPixel(640,384).A!=0)throw new Exception("Decoration center filled.");
            result.Save(output,ImageFormat.Png);
            Console.WriteLine(Path.GetFileName(output)+": "+changed+" synergy pixels changed; other 5 tiers and all alpha retained exactly.");
        }
    }
    public static void Preview(string backgroundPath,string decorationPath,string characterPath,string overviewPath,string folder) {
        using(var backgrounds=new Bitmap(backgroundPath))using(var decorations=new Bitmap(decorationPath))using(var characters=new Bitmap(characterPath))
        using(var bg=backgrounds.Clone(new Rectangle(512,256,256,256),PixelFormat.Format32bppArgb))
        using(var deco=decorations.Clone(new Rectangle(512,256,256,256),PixelFormat.Format32bppArgb))
        using(var odin=characters.Clone(new Rectangle(0,0,256,256),PixelFormat.Format32bppArgb))
        using(var trio=new Bitmap(768,256,PixelFormat.Format32bppArgb)) {
            using(var g=Graphics.FromImage(trio)) {
                g.Clear(Color.FromArgb(255,232,235,240));
                g.DrawImageUnscaled(bg,0,0);g.DrawImageUnscaled(deco,256,0);
                g.DrawImageUnscaled(bg,512,0);g.DrawImageUnscaled(odin,512,0);g.DrawImageUnscaled(deco,512,0);
            }
            trio.Save(Path.Combine(folder,"preview.png"),ImageFormat.Png);
            using(var old=new Bitmap(overviewPath))using(var overview=old.Clone(new Rectangle(0,0,old.Width,old.Height),PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(overview)) {
                using(var brush=new SolidBrush(Color.FromArgb(255,232,235,240)))g.FillRectangle(brush,820,655,780,256);
                g.DrawImage(trio,new Rectangle(820,655,256,256),0,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(trio,new Rectangle(1082,655,256,256),256,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(trio,new Rectangle(1344,655,256,256),512,0,256,256,GraphicsUnit.Pixel);
                overview.Save(Path.Combine(folder,"overview-updated.png"),ImageFormat.Png);
            }
        }
    }
}
'@
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$taskFolder=Join-Path $projectRoot 'outputs/tower-tier-layers-v1'
$assetFolder=Join-Path $projectRoot 'Assets/4. Asset/2. Tower/TierLayers'
$report=[Collections.Generic.List[string]]::new()
foreach($entry in @(@('Tower_Backgrounds_Atlas_v1.png',$false),@('Tower_Decorations_Atlas_v1.png',$true))) {
    $target=Join-Path $assetFolder $entry[0]
    $backup=Join-Path $PSScriptRoot ('before-'+$entry[0])
    if(-not(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup}
    $metaHash=(Get-FileHash -LiteralPath ($target+'.meta') -Algorithm SHA256).Hash
    $result=Join-Path $PSScriptRoot $entry[0]
    [RubyTierPalette]::Apply((Join-Path $PSScriptRoot 'native-approved.png'),$target,$result,[bool]$entry[1])
    Copy-Item -LiteralPath $result -Destination $target -Force
    Copy-Item -LiteralPath $result -Destination (Join-Path $taskFolder $entry[0]) -Force
    if((Get-FileHash -LiteralPath ($target+'.meta') -Algorithm SHA256).Hash -ne $metaHash){throw 'Sprite metadata changed.'}
    if((Get-FileHash -LiteralPath $result -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash){throw 'Asset copy mismatch.'}
    $report.Add($entry[0]+': synergy palette applied; other five tiers RGBA identical; all alpha unchanged; sprite metadata SHA256='+$metaHash)
}
[RubyTierPalette]::Preview((Join-Path $assetFolder 'Tower_Backgrounds_Atlas_v1.png'),(Join-Path $assetFolder 'Tower_Decorations_Atlas_v1.png'),(Join-Path $projectRoot 'Assets/4. Asset/2. Tower/Portraits_SD_Front_v2/Synergy_All14_Front_Atlas_v2.png'),(Join-Path $taskFolder 'preview.png'),$PSScriptRoot)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'overview-updated.png') -Destination (Join-Path $taskFolder 'preview.png') -Force
$report.Add('Approved ruby palette from native imagegen preview. No character, prefab, runtime or source spawn-tile edits. Unity was not launched.')
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),($report -join "`n"),[Text.UTF8Encoding]::new($false))
Write-Output 'Saved ruby Synergy background + decoration; metadata and geometry retained.'
