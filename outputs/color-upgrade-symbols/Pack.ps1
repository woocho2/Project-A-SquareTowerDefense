param([string]$GeneratedSource)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class UpgradeSymbolPacking {
    public static Bitmap Cell(Bitmap source, int index) {
        int left=(index%2)*source.Width/2, top=(index/2)*source.Height/2;
        int w=source.Width/2,h=source.Height/2;
        using(var tile=source.Clone(new Rectangle(left,top,w,h),PixelFormat.Format32bppArgb)) {
            // Preserve the drawing; discard tiny disconnected generation fragments in the gutters.
            var labels=new int[w*h];var sizes=new List<int>();sizes.Add(0);int largest=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int at=y*w+x;if(labels[at]!=0||tile.GetPixel(x,y).A==0)continue;
                int id=sizes.Count,size=0;var queue=new Queue<int>();queue.Enqueue(at);labels[at]=id;
                while(queue.Count>0) {
                    int pos=queue.Dequeue(),px=pos%w,py=pos/w;size++;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                        int nx=px+dx,ny=py+dy;if(nx<0||ny<0||nx>=w||ny>=h)continue;
                        int ni=ny*w+nx;if(labels[ni]!=0||tile.GetPixel(nx,ny).A==0)continue;
                        labels[ni]=id;queue.Enqueue(ni);
                    }
                }
                sizes.Add(size);largest=Math.Max(largest,size);
            }
            int x0=w,y0=h,x1=-1,y1=-1;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int id=labels[y*w+x];
                if(id>0&&sizes[id]<largest*0.0075)tile.SetPixel(x,y,Color.Transparent);
                if(tile.GetPixel(x,y).A<8)continue;
                x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
            }
            if(x1<0)throw new Exception("Empty icon: "+index);
            var cell=new Bitmap(512,512,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(cell))using(var attributes=new ImageAttributes()) {
                g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(tile,new Rectangle(32,32,448,448),x0,y0,x1-x0+1,y1-y0+1,GraphicsUnit.Pixel,attributes);
            }
            return cell;
        }
    }
    public static int[] Inspect(Bitmap image) {
        int x0=image.Width,y0=image.Height,x1=-1,y1=-1,opaque=0,partial=0;
        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++) {
            int a=image.GetPixel(x,y).A;if(a==0)continue;
            if(a==255)opaque++;else partial++;
            x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
        }
        return new[]{x0,y0,x1-x0+1,y1-y0+1,opaque,partial};
    }
}
'@
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$assetDir=Join-Path $root 'Assets/4. Asset/5. UI'
$destination=Join-Path $assetDir 'UI_Color_Upgrade_Atlas.png'
if((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath "$destination.meta")) { throw 'Destination exists; refusing to replace an existing asset.' }
$sourcePath=Join-Path $PSScriptRoot 'source.png'
Copy-Item -LiteralPath $GeneratedSource -Destination $sourcePath
$names=@('ColorUpgrade_Splash','ColorUpgrade_Single','ColorUpgrade_Buff','ColorUpgrade_Debuff')
$source=[Drawing.Bitmap]::new($sourcePath)
$atlas=[Drawing.Bitmap]::new(1024,1024,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics=[Drawing.Graphics]::FromImage($atlas)
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$records=@()
try {
    for($i=0;$i -lt 4;$i++) {
        $cell=[UpgradeSymbolPacking]::Cell($source,$i)
        try {
            $stats=[UpgradeSymbolPacking]::Inspect($cell)
            if($stats[0] -lt 30 -or $stats[1] -lt 30 -or ($stats[0]+$stats[2]) -gt 482 -or ($stats[1]+$stats[3]) -gt 482) { throw "Invalid sprite padding: $($names[$i])" }
            $column=$i%2;$row=[int][Math]::Floor($i/2)
            $graphics.DrawImageUnscaled($cell,$column*512,$row*512)
            $records+=@{name=$names[$i];fileID=21300001+$i;spriteID=[Guid]::NewGuid().ToString('N');x=$column*512;y=(1-$row)*512;width=512;height=512;bounds=$stats[0..3];opaquePixels=$stats[4];antialiasedPixels=$stats[5]}
        } finally { $cell.Dispose() }
    }
    $graphics.Dispose()
    $atlas.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
} finally { $source.Dispose();$atlas.Dispose() }
$guid=[Guid]::NewGuid().ToString('N')
$template=Get-Content -LiteralPath (Join-Path $assetDir 'Tower_Card_Attack_Atlas.png.meta') -Raw
$header=($template -split '  spriteSheet:',2)[0] -replace '(?m)^guid: .*$',"guid: $guid"
$blocks=@()
foreach($entry in $records) {
    $blocks+=@"
    - serializedVersion: 2
      name: $($entry.name)
      rect:
        serializedVersion: 2
        x: $($entry.x)
        y: $($entry.y)
        width: 512
        height: 512
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $($entry.spriteID)
      internalID: $($entry.fileID)
      vertices: []
      indices:
      edges: []
      weights: []
"@
}
$table=($records | ForEach-Object { "      $($_.name): $($_.fileID)" }) -join "`n"
$metadata=$header+@"
  spriteSheet:
    serializedVersion: 2
    sprites:
$($blocks -join "`n")
    outline: []
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    secondaryTextures: []
    nameFileIdTable:
$table
  userData: Color upgrade badges - 512px cells, centered, transparent gutters
  assetBundleName:
  assetBundleVariant:
"@
$encoding=[Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText("$destination.meta",$metadata+"`n",$encoding)
$check=Get-Content -LiteralPath "$destination.meta" -Raw
if(([regex]::Matches($check,'(?m)^      name: ')).Count -ne 4) { throw 'Expected exactly four sprite slices.' }
$report=@{atlas=$destination;sha256=(Get-FileHash -LiteralPath $destination).Hash;guid=$guid;size=@(1024,1024);cellSize=@(512,512);pixelsPerUnit=100;pivot=@(0.5,0.5);sprites=$records;generationMode='built-in imagegen';source=$sourcePath}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report | ConvertTo-Json -Depth 7),$encoding)
$report | ConvertTo-Json -Depth 7
