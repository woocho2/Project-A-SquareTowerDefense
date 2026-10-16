$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$atlasPath = Join-Path $repoRoot 'Assets/4. Asset/2. Tower/Tower_Decorations_Atlas_v1.png'
$originalPath = Join-Path $PSScriptRoot 'before/Tower_Decorations_Atlas_v1.png'
$nativePath = Join-Path $PSScriptRoot 'generated-source.png'
$outputPath = Join-Path $PSScriptRoot 'Tower_Decorations_Atlas_v1.png'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class PastelTowerFrames {
 // Broad matte shadow / main paint / light paint. Never pure black or chrome white.
 private static readonly string[][] Palette = {
  new[]{"#B77856","#D89C73","#E8B187"},
  new[]{"#889EB4","#AFBED0","#C8D5E3"},
  new[]{"#C79B35","#E6BD54","#F1D071"},
  new[]{"#4C9E92","#78C6AF","#99D9BF"},
  new[]{"#5B94C2","#83B6DD","#9AC9EA"},
  new[]{"#AD5E7C","#CD7F9D","#DF9EB7"}
 };
 public static void Build(string originalPath,string nativePath,string resultPath) {
  using(var original=new Bitmap(originalPath))using(var native=new Bitmap(nativePath))
  using(var art=new Bitmap(768,512,PixelFormat.Format32bppArgb))
  using(var output=new Bitmap(768,512,PixelFormat.Format32bppArgb)) {
   if(original.Width!=768||original.Height!=512)throw new Exception("Unexpected original atlas size");
   using(var g=Graphics.FromImage(art)) {
    g.CompositingMode=CompositingMode.SourceCopy;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(native,new Rectangle(0,0,768,512),new Rectangle(0,0,native.Width,native.Height),GraphicsUnit.Pixel);
   }
   for(int cell=0;cell<6;cell++) {
    int ox=(cell%3)*256,oy=(cell/3)*256;
    var palette=new Color[3];
    for(int i=0;i<3;i++)palette[i]=ColorTranslator.FromHtml(Palette[cell][i]);
    for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
     int alpha=original.GetPixel(ox+x,oy+y).A;
     if(alpha==0) { output.SetPixel(ox+x,oy+y,Color.FromArgb(0,0,0,0)); continue; }
     Color generated=art.GetPixel(ox+x,oy+y);
     double brightness=(generated.R*0.2126+generated.G*0.7152+generated.B*0.0722)/255.0;
     // Keep the generated painted shading, compressed into three restrained pastel tones.
     int tone=brightness<0.55?0:brightness<0.82?1:2;
     Color color=palette[tone];
     output.SetPixel(ox+x,oy+y,Color.FromArgb(alpha,color.R,color.G,color.B));
    }
   }
   output.Save(resultPath,ImageFormat.Png);
  }
 }
 public static string Verify(string originalPath,string resultPath) {
  using(var original=new Bitmap(originalPath))using(var result=new Bitmap(resultPath)) {
   int changed=0;
   for(int cell=0;cell<6;cell++) {
    int ox=(cell%3)*256,oy=(cell/3)*256,l=256,t=256,r=-1,b=-1,colors=0;
    var used=new System.Collections.Generic.HashSet<int>();
    for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
     Color before=original.GetPixel(ox+x,oy+y),after=result.GetPixel(ox+x,oy+y);
     if(before.A!=after.A)throw new Exception("Alpha/silhouette changed");
     if(after.A==0)continue;
     l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
     used.Add(after.ToArgb()&0x00FFFFFF);
     if((before.ToArgb()&0x00FFFFFF)!=(after.ToArgb()&0x00FFFFFF))colors++;
    }
    if(l!=7||t!=7||r!=248||b!=248)throw new Exception("Frame placement/size changed");
    if(used.Count>3||colors==0)throw new Exception("Expected recolored matte three-tone paint");
    changed+=colors;
   }
   return "PASS: six pastel frames, 768x512 atlas, original alpha at every pixel, 242x242 centred bounds, up to three tones per frame. Recolored pixels: "+changed;
  }
 }
 public static void Comparison(string originalPath,string resultPath,string outputPath,bool whiteBackground=false) {
  using(var canvas=new Bitmap(768,1084,PixelFormat.Format32bppArgb))
  using(var before=new Bitmap(originalPath))using(var after=new Bitmap(resultPath))
  using(var g=Graphics.FromImage(canvas))using(var font=new Font("Segoe UI",13,FontStyle.Bold)) {
   g.Clear(Color.FromArgb(248,246,242));
   if(whiteBackground)g.Clear(Color.White);
   for(int y=30;!whiteBackground&&y<1084;y+=16)for(int x=0;x<768;x+=16) {
    using(var brush=new SolidBrush(((x/16+y/16)%2)==0?Color.FromArgb(237,234,229):Color.FromArgb(248,246,242)))g.FillRectangle(brush,x,y,16,16);
   }
   g.DrawString("BEFORE",font,Brushes.DimGray,10,4);
   g.DrawImageUnscaled(before,0,30);
   g.FillRectangle(Brushes.WhiteSmoke,0,542,768,30);
   g.DrawString("PASTEL / MATTE",font,Brushes.DimGray,10,546);
   g.DrawImageUnscaled(after,0,572);
   canvas.Save(outputPath,ImageFormat.Png);
  }
 }
}
'@
[PastelTowerFrames]::Build($originalPath,$nativePath,$outputPath)
$result = [PastelTowerFrames]::Verify($originalPath,$outputPath)
Copy-Item -LiteralPath $outputPath -Destination $atlasPath -Force
if ((Get-FileHash -LiteralPath ($atlasPath + '.meta')).Hash -ne
    (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'before/Tower_Decorations_Atlas_v1.png.meta')).Hash) {
    throw 'Sprite metadata changed'
}
$protected = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'protected-hashes.json') -Raw | ConvertFrom-Json
foreach ($entry in $protected) {
    if ((Get-FileHash -LiteralPath $entry.path).Hash -ne $entry.hash) { throw ('Protected file changed: ' + $entry.path) }
}
[PastelTowerFrames]::Comparison($originalPath,$outputPath,(Join-Path $PSScriptRoot 'comparison.png'))
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'before-saturation.png')) {
    [PastelTowerFrames]::Comparison((Join-Path $PSScriptRoot 'before-saturation.png'),$outputPath,(Join-Path $PSScriptRoot 'saturation-comparison.png'),$true)
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),$result + "`nPASS: metadata, tower backgrounds, all character assets, prefab and runtime code hashes unchanged.",[Text.UTF8Encoding]::new($false))
Write-Output $result
