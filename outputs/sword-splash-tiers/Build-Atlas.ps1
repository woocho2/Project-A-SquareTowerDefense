$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskAsset=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Splash_Tiers_Layer_Atlas_v1.png'
$taskSources=@(
    (Join-Path $PSScriptRoot 'Plain_Silver.png'),
    (Join-Path $PSScriptRoot 'Plain_Gold.png'),
    (Join-Path $PSScriptRoot 'Plain_Mithril.png'),
    (Join-Path $PSScriptRoot 'Plain_Diamond.png')
)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class TierAtlasPack
{
    static bool OriginalMetal(Color p)
    {
        bool skinEdge=p.R>180&&p.G>105&&p.B>p.G*.80f;
        return p.A>0&&!skinEdge&&p.GetHue()>=8&&p.GetHue()<=52&&p.GetSaturation()>.15f;
    }
    static bool GeneratedMetal(Color p)
    {
        float hue=p.GetHue();
        return p.A>127&&(p.GetSaturation()<.15f||(hue>12&&hue<260));
    }
    static Rectangle Bounds(Bitmap image)
    {
        int l=image.Width,t=image.Height,r=-1,b=-1,margin=Math.Max(1,image.Width/100);
        for(int y=margin;y<image.Height-margin;y++)for(int x=margin;x<image.Width-margin;x++)
        {if(image.GetPixel(x,y).A<=220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
        if(r<l)throw new Exception("Missing generated helmet");return Rectangle.FromLTRB(l,t,r+1,b+1);
    }
    static Color SampleMetal(Bitmap image,int x,int y,Color fallback)
    {
        var p=image.GetPixel(x,y);if(GeneratedMetal(p))return p;
        for(int radius=1;radius<22;radius++)
            foreach(var offset in new[]{new Point(radius,0),new Point(-radius,0),new Point(0,radius),new Point(0,-radius)})
            {int nx=x+offset.X,ny=y+offset.Y;if(nx<0||nx>=256||ny<0||ny>=256)continue;p=image.GetPixel(nx,ny);if(GeneratedMetal(p))return p;}
        return fallback;
    }
    public static void Build(string originalPath,string facePath,string bronzePath,string[] sources,string atlasPath,string outputDir)
    {
        using(var original=new Bitmap(originalPath))using(var face=new Bitmap(facePath))using(var bronze=new Bitmap(bronzePath))
        using(var atlas=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        {
            var layers=new Bitmap[6];layers[0]=(Bitmap)face.Clone();layers[1]=(Bitmap)bronze.Clone();
            Color[] defaults={Color.FromArgb(174,184,198),Color.FromArgb(217,173,76),Color.FromArgb(120,186,198),Color.FromArgb(208,234,243)};
            for(int i=0;i<4;i++)
            using(var generated=new Bitmap(sources[i]))using(var resized=new Bitmap(256,256,PixelFormat.Format32bppArgb))
            {
                var rect=Bounds(generated);
                using(var g=Graphics.FromImage(resized))using(var a=new ImageAttributes())
                {g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;a.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(generated,new Rectangle(6,5,244,243),rect.X,rect.Y,rect.Width,rect.Height,GraphicsUnit.Pixel,a);}
                var overlay=new Bitmap(256,256,PixelFormat.Format32bppArgb);
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    var old=bronze.GetPixel(x,y);
                    if(old.A==0){overlay.SetPixel(x,y,Color.Transparent);continue;}
                    var rgb=OriginalMetal(old)?SampleMetal(resized,x,y,defaults[i]):old;
                    overlay.SetPixel(x,y,Color.FromArgb(old.A,rgb.R,rgb.G,rgb.B));
                }
                layers[i+2]=overlay;
            }
            // Direct RGBA copying avoids premultiplied-alpha rounding at the tile edge.
            for(int i=0;i<6;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                atlas.SetPixel((i%3)*256+x,(i/3)*256+y,layers[i].GetPixel(x,y));
            atlas.Save(atlasPath,ImageFormat.Png);
            // Verify all equipment swaps retain the same exact face, alpha, red and sword pixels.
            for(int i=1;i<6;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                var f=face.GetPixel(x,y);var h=layers[i].GetPixel(x,y);var old=original.GetPixel(x,y);var b=bronze.GetPixel(x,y);
                if(f.A>0&&h.A>0)throw new Exception("Helmet overlaps common face");
                var composed=f.A>0?f:h;
                if(composed.A!=old.A)throw new Exception("Tier silhouette mismatch");
                if(f.A>0&&composed.ToArgb()!=old.ToArgb())throw new Exception("Face pixel changed");
                if(i==1&&composed.ToArgb()!=old.ToArgb())throw new Exception("Bronze reconstruction mismatch");
                if(b.A>0&&!OriginalMetal(b)&&h.ToArgb()!=b.ToArgb())throw new Exception("Non-metal detail changed");
            }
            using(var preview=new Bitmap(1280,640,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var format=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,30,34,44));format.Alignment=StringAlignment.Center;
                string[] labels={"브론즈","실버","골드","미스릴","다이아"};
                g.DrawString("공통 얼굴 + 티어별 투구",font,Brushes.Ivory,new RectangleF(0,0,1280,35),format);
                for(int i=1;i<6;i++)
                {
                    g.DrawImageUnscaled(face,(i-1)*256,35);g.DrawImageUnscaled(layers[i],(i-1)*256,35);
                    g.DrawString(labels[i-1],font,Brushes.Ivory,new RectangleF((i-1)*256,297,256,35),format);
                    g.DrawImageUnscaled(layers[i],(i-1)*256,344);
                }
                g.DrawString("투구 레이어 · 얼굴 부분은 투명",font,Brushes.LightSkyBlue,new RectangleF(0,604,1280,35),format);
                preview.Save(Path.Combine(outputDir,"preview.png"),ImageFormat.Png);
            }
            foreach(var layer in layers)layer.Dispose();
            Console.WriteLine("PASS: six atlas layers, exact original bronze reconstruction, identical face and silhouettes across all five tiers, red/sword/recess detail preservation.");
        }
    }
}
'@
[TierAtlasPack]::Build((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Splash_Front_v3.png'),(Join-Path $PSScriptRoot 'Face_Common.png'),(Join-Path $PSScriptRoot 'Helmet_Bronze.png'),$taskSources,$taskAsset,$PSScriptRoot)
$taskNames=@('Tower_Sword_Splash_Face_Common','Tower_Sword_Splash_Helmet_Bronze','Tower_Sword_Splash_Helmet_Silver','Tower_Sword_Splash_Helmet_Gold','Tower_Sword_Splash_Helmet_Mithril','Tower_Sword_Splash_Helmet_Diamond')
$taskIds=@(31001,31011,31012,31013,31014,31015)
$taskManifest=@();for($i=0;$i -lt 6;$i++){$taskManifest += [pscustomobject]@{name=$taskNames[$i];fileID=$taskIds[$i];x=(($i%3)*256);y=((1-[int][Math]::Floor($i/3))*256);width=256;height=256}}
$taskOldMeta=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Splash_Front_v3.png.meta'))
$taskGuid=if(Test-Path -LiteralPath ($taskAsset+'.meta')){[regex]::Match([IO.File]::ReadAllText($taskAsset+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
$taskPrefix=$taskOldMeta.Substring(0,$taskOldMeta.IndexOf('  spriteSheet:'))
$taskPrefix=[regex]::Replace($taskPrefix,'(?m)^guid: \w+',"guid: $taskGuid")
$taskPrefix=$taskPrefix.Replace('spriteMode: 1','spriteMode: 2')
$taskSheet=New-Object Text.StringBuilder
[void]$taskSheet.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
foreach($entry in $taskManifest){$spriteGuid=[guid]::NewGuid().ToString('N');[void]$taskSheet.AppendLine(@"
    - serializedVersion: 2
      name: $($entry.name)
      rect:
        serializedVersion: 2
        x: $($entry.x)
        y: $($entry.y)
        width: 256
        height: 256
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteGuid
      internalID: $($entry.fileID)
      vertices: []
      indices: 
      edges: []
      weights: []
"@)}
[void]$taskSheet.AppendLine(@"
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
"@)
foreach($entry in $taskManifest){[void]$taskSheet.AppendLine("      $($entry.name): $($entry.fileID)")}
$taskUtf8=New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($taskAsset+'.meta',$taskPrefix+$taskSheet.ToString()+$taskOldMeta.Substring($taskOldMeta.IndexOf('  mipmapLimitGroupName:')),$taskUtf8)
$taskManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Encoding UTF8
Write-Output ('Saved layer atlas; GUID='+$taskGuid+'; 768x512, six 256x256 sprites, PPU=192.')
