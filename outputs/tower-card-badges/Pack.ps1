param([string]$ProjectRoot = (Resolve-Path "$PSScriptRoot/../..").Path, [switch]$Rebuild)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class CardBadgePacking {
    // Extract a sprite, dropping only tiny disconnected fragments from adjoining cells.
    static Bitmap Extract(Bitmap full, int columns, int rows, int index) {
        int cx=index%columns,cy=index/columns;
        int l=cx*full.Width/columns,t=cy*full.Height/rows;
        int w=(cx+1)*full.Width/columns-l,h=(cy+1)*full.Height/rows-t;
        var tile=full.Clone(new Rectangle(l,t,w,h),PixelFormat.Format32bppArgb);
        var labels=new int[w*h]; var sizes=new List<int>(); sizes.Add(0);
        int largest=0;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            int at=y*w+x;
            if(labels[at]!=0||tile.GetPixel(x,y).A==0)continue;
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
        for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
            int id=labels[y*w+x];
            if(id>0&&sizes[id]<largest*0.0075)tile.SetPixel(x,y,Color.Transparent);
        }
        return tile;
    }
    public static Bitmap Cell(string path, int columns, int rows, int index, bool square) {
        using (var full = new Bitmap(path)) using(var source=Extract(full,columns,rows,index)) {
            int left=0,top=0,right=source.Width,bottom=source.Height;
            int x0=right,y0=bottom,x1=-1,y1=-1;
            for(int y=top;y<bottom;y++) for(int x=left;x<right;x++) {
                if(source.GetPixel(x,y).A < 8) continue;
                x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
            }
            if(x1<0) throw new Exception("Empty source sprite: "+path+" #"+index);
            int w=x1-x0+1,h=y1-y0+1;
            int dw=square?448:(int)Math.Round(w*448.0/Math.Max(w,h));
            int dh=square?448:(int)Math.Round(h*448.0/Math.Max(w,h));
            var result=new Bitmap(512,512,PixelFormat.Format32bppArgb);
            using(var g=Graphics.FromImage(result)) using(var attributes=new ImageAttributes()) {
                g.Clear(Color.Transparent);
                g.CompositingMode=CompositingMode.SourceCopy;
                g.CompositingQuality=CompositingQuality.HighQuality;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                g.DrawImage(source,new Rectangle((512-dw)/2,(512-dh)/2,dw,dh),x0,y0,w,h,GraphicsUnit.Pixel,attributes);
            }
            return result;
        }
    }
    public static int[] Bounds(Bitmap source) {
        int x0=source.Width,y0=source.Height,x1=-1,y1=-1;
        for(int y=0;y<source.Height;y++) for(int x=0;x<source.Width;x++) {
            if(source.GetPixel(x,y).A<8)continue;
            x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);
        }
        return new[]{x0,y0,x1-x0+1,y1-y0+1};
    }
}
'@
$sources = Get-Content -LiteralPath "$PSScriptRoot/sources-final.json" -Raw | ConvertFrom-Json
foreach($kind in 'Attack','Emblem') { $sources.$kind=Join-Path $PSScriptRoot $sources.$kind }
foreach($tier in 'Silver','Gold','Mithril','Diamond','Synergy') { $sources.Tiers.$tier=Join-Path $PSScriptRoot $sources.Tiers.$tier }
$original = Join-Path $ProjectRoot 'Assets/4. Asset/5. UI/Tower_Card_Icon_Atlas_v1.png'
$expected = @('AE0E59A315F76B80E3567919A12062E0E5E82807FE514F819D5581FEC0DB141D','490FD27C119E84BFCFC741656195A98C24B02940815BA9D6F6C05994D44CF379')
for($i=0;$i -lt 2;$i++) {
    $p = if($i -eq 0){$original}else{"$original.meta"}
    if((Get-FileHash -LiteralPath $p).Hash -ne $expected[$i]) { throw "Original CardStat atlas changed: $p" }
}
$groups = @(
    @{Kind='Attack';Columns=2;Rows=2;Names=@('CardBadge_AttackSplash','CardBadge_AttackSingle','CardBadge_AttackBuff','CardBadge_AttackDebuff')},
    @{Kind='Tier';Columns=3;Rows=2;Names=@('CardBadge_TierBronze','CardBadge_TierSilver','CardBadge_TierGold','CardBadge_TierMithril','CardBadge_TierDiamond','CardBadge_TierSynergy')},
    @{Kind='Emblem';Columns=4;Rows=4;Names=@('CardBadge_EmblemSword','CardBadge_EmblemAxe','CardBadge_EmblemBow','CardBadge_EmblemSpear','CardBadge_EmblemShield','CardBadge_EmblemHammer','CardBadge_EmblemFire','CardBadge_EmblemWater','CardBadge_EmblemEarth','CardBadge_EmblemWind','CardBadge_EmblemIce','CardBadge_EmblemElectricity','CardBadge_EmblemLight','CardBadge_EmblemDark')}
)
$assetDir = Join-Path $ProjectRoot 'Assets/4. Asset/5. UI'
$utf8 = [System.Text.UTF8Encoding]::new($false)
function Stable-Guid([string]$Value) {
    $md5 = [Security.Cryptography.MD5]::Create()
    try { return ([BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($Value)))).Replace('-','').ToLowerInvariant() } finally {$md5.Dispose()}
}
$manifest = @()
$preview = [Drawing.Bitmap]::new(1120,1050)
$pg = [Drawing.Graphics]::FromImage($preview)
$pg.Clear([Drawing.ColorTranslator]::FromHtml('#f6f0e5'))
$pg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font = [Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Bold)
$small = [Drawing.Font]::new('Segoe UI',10)
$brush = [Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml('#422a20'))
$rowY = 16
try {
 foreach($group in $groups) {
    $kind=$group.Kind; $cols=$group.Columns; $rows=$group.Rows
    $atlas = [Drawing.Bitmap]::new(($cols*512),($rows*512),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $ag = [Drawing.Graphics]::FromImage($atlas)
    $ag.Clear([Drawing.Color]::Transparent)
    $ag.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
    $pg.DrawString("$kind - 512 x 512 / PPU 100",$font,$brush,16,$rowY)
    $rowY += 30
    $records = @()
    for($i=0;$i -lt $group.Names.Count;$i++) {
       $name=$group.Names[$i]
       if($kind -eq 'Tier') {
           $tier=@('Bronze','Silver','Gold','Mithril','Diamond','Synergy')[$i]
           if($i -eq 0) {$src=$original;$sc=3;$sr=3;$si=8} else {$src=$sources.Tiers.$tier;$sc=1;$sr=1;$si=0}
       } elseif($i -eq 0) {
           $src=$original;$sc=3;$sr=3;$si=if($kind -eq 'Attack'){7}else{6}
       } else {
           $src=$sources.$kind;$sc=$cols;$sr=$rows;$si=$i
       }
       $cell=[CardBadgePacking]::Cell($src,$sc,$sr,$si,($kind -ne 'Emblem'))
       try {
         $bounds=[CardBadgePacking]::Bounds($cell)
         if($bounds[0] -lt 28 -or $bounds[1] -lt 28 -or ($bounds[0]+$bounds[2]) -gt 484 -or ($bounds[1]+$bounds[3]) -gt 484) {throw "Padding failed: $name"}
         $x=($i%$cols)*512;$top=[int][Math]::Floor($i/$cols)*512
         $ag.DrawImageUnscaled($cell,$x,$top)
         $px=16+($i%7)*156;$py=$rowY+[int][Math]::Floor($i/7)*178
         $pg.DrawImage($cell,[Drawing.Rectangle]::new($px,$py,140,140))
         $label=$name.Replace('CardBadge_','').Replace($kind,'')
         $pg.DrawString(('{0:00} {1}' -f ($i+1),$label),$small,$brush,$px,$py+143)
         $records+=@{name=$name;index=($i+1);fileID=(21300001+$i);x=$x;y=(($rows-1-[int][Math]::Floor($i/$cols))*512);width=512;height=512;pivot=@(0.5,0.5);visibleBounds=$bounds;spriteID=(Stable-Guid "TowerCard/$name")}
       } finally {$cell.Dispose()}
    }
    $ag.Dispose()
    $filename="Tower_Card_${kind}_Atlas.png"
    $dest=Join-Path $assetDir $filename
    $guid=Stable-Guid "SquareTowerDefense/$filename"
    if(Test-Path -LiteralPath $dest) {
       if(-not $Rebuild) {throw "New atlas already exists: $dest"}
       $previous=Get-Content -LiteralPath "$PSScriptRoot/verification.json" -Raw | ConvertFrom-Json
       $old=@($previous.atlases | Where-Object {$_.kind -eq $kind})[0]
       if((Get-FileHash -LiteralPath $dest).Hash -ne $old.sha256 -or (Get-Content -LiteralPath "$dest.meta" -Raw) -notmatch "guid: $guid") {throw "Atlas edited since packing: $dest"}
    }
    $atlas.Save($dest,[Drawing.Imaging.ImageFormat]::Png)
    $atlas.Dispose()
    $template=Get-Content -LiteralPath (Join-Path $assetDir 'Icon_Attack_Basic_Common.png.meta') -Raw
    $meta=$template -replace '(?m)^guid: .*$',"guid: $guid"
    $meta=$meta.Replace('  spriteMode: 1','  spriteMode: 2').Replace('maxTextureSize: 1024','maxTextureSize: 2048')
    $blocks=@()
    foreach($d in $records) {
       $blocks+=@"
    - serializedVersion: 2
      name: $($d.name)
      rect:
        serializedVersion: 2
        x: $($d.x)
        y: $($d.y)
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
      spriteID: $($d.spriteID)
      internalID: $($d.fileID)
      vertices: []
      indices:
      edges: []
      weights: []
"@
    }
    $meta=$meta.Replace('    sprites: []',("    sprites:`n"+($blocks -join "`n")))
    $table=($records | ForEach-Object {"      $($_.name): $($_.fileID)"}) -join "`n"
    $meta=$meta.Replace('    nameFileIdTable: {}',"    nameFileIdTable:`n$table")
    $meta=$meta -replace '(?m)^  userData: .*$',"  userData: Tower card $kind badges - 512px cells centered with 32px padding"
    [IO.File]::WriteAllText("$dest.meta",$meta,$utf8)
    $manifest+=@{kind=$kind;path="Assets/4. Asset/5. UI/$filename";guid=$guid;dimensions=@(($cols*512),($rows*512));sprites=$records;sha256=(Get-FileHash -LiteralPath $dest).Hash}
    $rowY += [int][Math]::Ceiling($group.Names.Count/7.0)*178+24
 }
 $preview.Save("$PSScriptRoot/preview.png",[Drawing.Imaging.ImageFormat]::Png)
} finally {$pg.Dispose();$preview.Dispose();$font.Dispose();$small.Dispose();$brush.Dispose()}
$result=@{originalAtlasUnchanged=$true;originalSha256=$expected;mode='built-in image_gen';cellSize=512;pixelsPerUnit=100;atlasCount=3;spriteCount=24;atlases=$manifest}
[IO.File]::WriteAllText("$PSScriptRoot/verification.json",($result|ConvertTo-Json -Depth 12),$utf8)
$manifest | ForEach-Object {"$($_.kind): $($_.sprites.Count) sprites, $($_.dimensions -join ' x ')"}
