$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
$specs=@(
    @{kind='Background';file='HPBar_Background.png';source='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-425f24f2-9010-497d-81e2-57e4340ebeb1.png';rect=@(2,2,252,28);border='{x: 14, y: 8, z: 14, w: 8}'},
    @{kind='Fill';file='HPBar_Fill.png';source='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-709f62b1-6dc2-4924-a2ea-13d3b6f89ace.png';rect=@(7,7,242,18);border='{x: 14, y: 8, z: 14, w: 8}'}
)
foreach($spec in $specs) {if((Test-Path -LiteralPath (Join-Path $ui $spec.file)) -or (Test-Path -LiteralPath (Join-Path $ui ($spec.file+'.meta')))) {throw "Destination already exists: $($spec.file)"}}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
public static class HpBarPacking {
    public static Bitmap Pack(Bitmap src,Rectangle destination) {
        int w=src.Width,h=src.Height;
        using(var tile=src.Clone(new Rectangle(0,0,w,h),PixelFormat.Format32bppArgb)) {
            int[] labels=new int[w*h];var sizes=new List<int>();sizes.Add(0);int largest=0;
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
                int id=labels[y*w+x];if(id>0&&sizes[id]<largest*0.0075)tile.SetPixel(x,y,Color.Transparent);
                if(tile.GetPixel(x,y).A<8)continue;
                x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
            }
            if(x1<0)throw new Exception("Empty generated health bar.");
            var result=new Bitmap(256,32,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes()) {
                g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(tile,destination,x0,y0,x1-x0+1,y1-y0+1,GraphicsUnit.Pixel,attributes);
            }
            return result;
        }
    }
    public static int[] Inspect(Bitmap image) {
        int x0=image.Width,y0=image.Height,x1=-1,y1=-1,maxChroma=0;
        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++) {
            var c=image.GetPixel(x,y);if(c.A==0)continue;
            x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
            if(c.A>240)maxChroma=Math.Max(maxChroma,Math.Max(c.R,Math.Max(c.G,c.B))-Math.Min(c.R,Math.Min(c.G,c.B)));
        }
        return new[]{x0,y0,x1-x0+1,y1-y0+1,maxChroma};
    }
}
'@
$template=[IO.File]::ReadAllText((Join-Path $ui 'UI_Action_Atlas.png.meta'))
$header=($template -split '  spriteSheet:',2)[0]
$encoding=[Text.UTF8Encoding]::new($false)
$report=@()
foreach($spec in $specs) {
    $localSource=Join-Path $PSScriptRoot ("source-$($spec.kind).png")
    Copy-Item -LiteralPath $spec.source -Destination $localSource
    $source=[Drawing.Bitmap]::new($localSource)
    try {$result=[HpBarPacking]::Pack($source,[Drawing.Rectangle]::new($spec.rect[0],$spec.rect[1],$spec.rect[2],$spec.rect[3]))} finally {$source.Dispose()}
    try {
        $stats=[HpBarPacking]::Inspect($result)
        if($stats[0] -lt 1 -or $stats[1] -lt 1 -or ($stats[0]+$stats[2]) -gt 255 -or ($stats[1]+$stats[3]) -gt 31) {throw 'Missing transparent outer gutter.'}
        if($spec.kind -eq 'Fill' -and $stats[4] -gt 8) {throw 'Fill must be neutral to support health tint colors.'}
        $destination=Join-Path $ui $spec.file
        $result.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$result.Dispose()}
    $guid=[Guid]::NewGuid().ToString('N')
    $meta=($header -replace '(?m)^guid: .*$',"guid: $guid").Replace('  spriteMode: 2','  spriteMode: 1').Replace('  spriteBorder: {x: 0, y: 0, z: 0, w: 0}',('  spriteBorder: '+$spec.border))
    $meta+=@"
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    secondaryTextures: []
    nameFileIdTable: {}
  userData: Pastel cartoon HP bar $($spec.kind) - aligned 256x32 transparent sprite
  assetBundleName:
  assetBundleVariant:
"@
    [IO.File]::WriteAllText("$destination.meta",$meta+"`n",$encoding)
    $report+=@{file=$spec.file;size=@(256,32);bounds=$stats[0..3];maxOpaqueColorChannelDifference=$stats[4];guid=$guid;sha256=(Get-FileHash -LiteralPath $destination).Hash}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report | ConvertTo-Json -Depth 5),$encoding)
$report | ConvertTo-Json -Depth 5
