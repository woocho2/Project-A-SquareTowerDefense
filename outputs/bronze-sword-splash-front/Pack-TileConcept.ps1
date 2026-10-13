param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$AssetName,[switch]$Refit)
$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskAsset=Join-Path $taskRoot ('Assets/4. Asset/2. Tower/CubeConcepts/'+$AssetName+'.png')
if((Test-Path -LiteralPath $taskAsset) -and !$Refit){throw 'Avoid overwriting an existing concept'}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class TileConceptPack
{
    static Rectangle Bounds(Bitmap image, bool artwork)
    {
        int minX=image.Width, minY=image.Height, maxX=-1, maxY=-1;
        int margin=artwork ? (int)(image.Width*.03f) : 0;
        for(int y=margin;y<image.Height-margin;y++) for(int x=margin;x<image.Width-margin;x++)
        {
            if(image.GetPixel(x,y).A <= (artwork ? 127 : 0)) continue;
            minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y);
        }
        if(maxX<minX) throw new Exception("Missing opaque artwork");
        return Rectangle.FromLTRB(minX,minY,maxX+1,maxY+1);
    }
    public static void Pack(string sourcePath,string tilePath,string destination)
    {
        using(var source=new Bitmap(sourcePath)) using(var tile=new Bitmap(tilePath))
        using(var resized=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var result=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        {
            if(tile.Width!=256 || tile.Height!=256) throw new Exception("Expected 256-square tile mask");
            var sourceRect=Bounds(source,true); var targetRect=Bounds(tile,false);
            using(var g=Graphics.FromImage(resized)) using(var attributes=new ImageAttributes())
            {
                g.Clear(Color.Transparent); g.CompositingMode=CompositingMode.SourceCopy;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source,targetRect,sourceRect.X,sourceRect.Y,sourceRect.Width,sourceRect.Height,GraphicsUnit.Pixel,attributes);
            }
            // Technical texture fitting: keep generated RGB, use the tile's exact outer alpha.
            for(int y=0;y<256;y++)
            {
                int left=256,right=-1,maskLeft=256,maskRight=-1;
                for(int x=0;x<256;x++) if(resized.GetPixel(x,y).A>=128) {left=Math.Min(left,x);right=Math.Max(right,x);}
                for(int x=0;x<256;x++) if(tile.GetPixel(x,y).A>0) {maskLeft=Math.Min(maskLeft,x);maskRight=Math.Max(maskRight,x);}
                for(int x=0;x<256;x++)
                {
                    var mask=tile.GetPixel(x,y);
                    if(mask.A==0){result.SetPixel(x,y,Color.Transparent);continue;}
                    if(right<left)throw new Exception("Artwork does not cover tile row");
                    // Fit each curved row to the tile span instead of extending edge pixels.
                    // This avoids stripes where the two corner curvatures differ.
                    double fraction=(x-maskLeft)/(double)Math.Max(1,maskRight-maskLeft);
                    double sample=left+fraction*(right-left);
                    int first=Math.Max(left,Math.Min(right,(int)Math.Floor(sample)));
                    int second=Math.Min(right,first+1);double blend=sample-first;
                    var a=resized.GetPixel(first,y);var b=resized.GetPixel(second,y);
                    var color=Color.FromArgb((int)Math.Round(a.R+(b.R-a.R)*blend),(int)Math.Round(a.G+(b.G-a.G)*blend),(int)Math.Round(a.B+(b.B-a.B)*blend));
                    result.SetPixel(x,y,Color.FromArgb(mask.A,color.R,color.G,color.B));
                }
            }
            result.Save(destination,ImageFormat.Png);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                if(result.GetPixel(x,y).A!=tile.GetPixel(x,y).A)throw new Exception("Tile silhouette mismatch");
            Console.WriteLine("PASS: 256x256 texture and all 65,536 alpha values match spawn tile. Source crop: "+sourceRect);
        }
    }
}
'@
[TileConceptPack]::Pack($Source,(Join-Path $PSScriptRoot 'spawn-tile-reference.png'),$taskAsset)
$taskHasMeta=Test-Path -LiteralPath ($taskAsset+'.meta')
$taskMetaPath=if($taskHasMeta){$taskAsset+'.meta'}else{Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Splash_Front_v2.png.meta'}
$taskMeta=[IO.File]::ReadAllText($taskMetaPath)
if(!$taskHasMeta){$taskMeta=[regex]::Replace($taskMeta,'(?m)^guid: \w+',('guid: '+[guid]::NewGuid().ToString('N')))}
$taskMeta=[regex]::Replace($taskMeta,'(?m)^  spritePixelsToUnits: [^\r\n]+','  spritePixelsToUnits: 192')
if(!$taskHasMeta){$taskMeta=[regex]::Replace($taskMeta,'(?m)^    spriteID: \w+',('    spriteID: '+[guid]::NewGuid().ToString('N')))}
[IO.File]::WriteAllText($taskAsset+'.meta',$taskMeta,(New-Object Text.UTF8Encoding($false)))
Write-Output ('Saved '+$taskAsset+'; PPU=192, sprite size=1.333333 world units.')
