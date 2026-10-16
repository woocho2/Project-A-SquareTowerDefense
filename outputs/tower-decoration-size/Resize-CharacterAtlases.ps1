$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$atlasRoot = Join-Path $repoRoot 'Assets/4. Asset/2. Tower/TowerCharacter'
$backupRoot = Join-Path $PSScriptRoot 'character-atlas-before'
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class TowerPortraitPixels {
 public static Rectangle Bounds(Bitmap b, int x, int y) {
  int l=256,t=256,r=-1,d=-1;
  for(int py=0;py<256;py++)for(int px=0;px<256;px++) {
   if(b.GetPixel(x+px,y+py).A<128)continue;
   l=Math.Min(l,px);t=Math.Min(t,py);r=Math.Max(r,px);d=Math.Max(d,py);
  }
  return r<0 ? Rectangle.Empty : Rectangle.FromLTRB(l,t,r+1,d+1);
 }
 public static void Resize(string source,string destination) {
  using(var input=new Bitmap(source))using(var output=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
   if(input.Width!=512||input.Height!=512)throw new Exception("Expected four 256x256 cells: "+source);
   using(var graphics=Graphics.FromImage(output)) {
    graphics.Clear(Color.Transparent);
    graphics.CompositingMode=CompositingMode.SourceCopy;
    graphics.CompositingQuality=CompositingQuality.HighQuality;
    graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
    graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
    for(int row=0;row<2;row++)for(int col=0;col<2;col++) {
     using(var cell=input.Clone(new Rectangle(col*256,row*256,256,256),PixelFormat.Format32bppArgb)) {
      // Scale inside the unchanged sprite cell, about the existing centre pivot.
      graphics.DrawImage(cell,new RectangleF(col*256+6.4f,row*256+6.4f,243.2f,243.2f),
       new RectangleF(0,0,256,256),GraphicsUnit.Pixel);
     }
    }
   }
   output.Save(destination,ImageFormat.Png);
  }
 }
 public static string Verify(string before,string after) {
  using(var a=new Bitmap(before))using(var b=new Bitmap(after)) {
   if(b.Width!=512||b.Height!=512)throw new Exception("Atlas dimensions changed");
   for(int row=0;row<2;row++)for(int col=0;col<2;col++) {
    int x=col*256,y=row*256;
    Rectangle oldBounds=Bounds(a,x,y),newBounds=Bounds(b,x,y);
    if(oldBounds.IsEmpty||newBounds.IsEmpty)throw new Exception("Empty character cell");
    if(Math.Abs(newBounds.Width-oldBounds.Width*0.95)>2||Math.Abs(newBounds.Height-oldBounds.Height*0.95)>2)
     throw new Exception("Unexpected resized bounds in "+after);
    double expectedX=128+(oldBounds.Left+oldBounds.Width*0.5-128)*0.95;
    double expectedY=128+(oldBounds.Top+oldBounds.Height*0.5-128)*0.95;
    if(Math.Abs(newBounds.Left+newBounds.Width*0.5-expectedX)>1.5||Math.Abs(newBounds.Top+newBounds.Height*0.5-expectedY)>1.5)
     throw new Exception("Character alignment shifted in "+after);
    for(int p=0;p<256;p++)for(int edge=0;edge<5;edge++) {
     if(b.GetPixel(x+p,y+edge).A!=0||b.GetPixel(x+p,y+255-edge).A!=0||
        b.GetPixel(x+edge,y+p).A!=0||b.GetPixel(x+255-edge,y+p).A!=0)
      throw new Exception("Unexpected pixels outside inset in "+after);
    }
   }
   return "four cells: artwork scaled 95%, dimensions/pivots preserved, transparent inset verified";
  }
 }
}
'@
$catalog = [IO.File]::ReadAllText((Join-Path $repoRoot 'Assets/Resources/TowerCharacterCatalog.asset'))
$atlases = @(Get-ChildItem -LiteralPath $atlasRoot -Filter 'Tower_*_Front_Atlas_v2.png')
if ($atlases.Count -ne 14) { throw "Expected 14 regular character atlases, found $($atlases.Count)" }
$protectedPaths = @(
    (Join-Path $atlasRoot 'Synergy_All14_Front_Atlas_v2.png'),
    (Join-Path $repoRoot 'Assets/4. Asset/2. Tower/Tower_Decorations_Atlas_v1.png'),
    (Join-Path $repoRoot 'Assets/4. Asset/2. Tower/Tower_Backgrounds_Atlas_v1.png'),
    (Join-Path $repoRoot 'Assets/Resources/TowerCharacterCatalog.asset')
)
$protectedHashes = @{}
foreach ($protected in $protectedPaths) { $protectedHashes[$protected]=(Get-FileHash -LiteralPath $protected).Hash }
$report = [Collections.Generic.List[string]]::new()
foreach ($atlas in $atlases) {
    $metaPath = $atlas.FullName + '.meta'
    $metaText = [IO.File]::ReadAllText($metaPath)
    $guid = [regex]::Match($metaText,'(?m)^guid: (\w+)').Groups[1].Value
    if (!$catalog.Contains($guid)) { throw ('Atlas not referenced by regular tower catalog: ' + $atlas.Name) }
    $backupFile = Join-Path $backupRoot $atlas.Name
    if (!(Test-Path -LiteralPath $backupFile)) {
        Copy-Item -LiteralPath $atlas.FullName -Destination $backupFile
        Copy-Item -LiteralPath $metaPath -Destination ($backupFile + '.meta')
    }
    # Always use the original backup, so repeating this script cannot shrink twice.
    $stagedPath = Join-Path $PSScriptRoot ('scaled-' + $atlas.Name)
    [TowerPortraitPixels]::Resize($backupFile,$stagedPath)
    $result = [TowerPortraitPixels]::Verify($backupFile,$stagedPath)
    Copy-Item -LiteralPath $stagedPath -Destination $atlas.FullName -Force
    if ((Get-FileHash -LiteralPath $metaPath).Hash -ne (Get-FileHash -LiteralPath ($backupFile + '.meta')).Hash) {
        throw ('Sprite metadata changed: ' + $atlas.Name)
    }
    $report.Add($atlas.Name + ': ' + $result)
}
foreach ($protected in $protectedPaths) {
    if ((Get-FileHash -LiteralPath $protected).Hash -ne $protectedHashes[$protected]) { throw ('Protected asset changed: ' + $protected) }
}
$report.Add('PASS: 14 PNG atlases / 56 portraits; atlas sizes, all metadata, catalog and protected artwork unchanged.')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'character-atlas-verification.txt'),$report,[Text.UTF8Encoding]::new($false))
Write-Output $report[$report.Count-1]
