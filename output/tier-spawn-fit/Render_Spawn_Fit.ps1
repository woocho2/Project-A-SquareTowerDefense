$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$atlas=[System.Drawing.Bitmap]::FromFile((Join-Path $project 'Assets/4. Asset/1. BackGround/Tile_TowerSpawn_Atlas.png'))
$out=[System.Drawing.Bitmap]::new(1400,620,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[System.Drawing.Graphics]::FromImage($out)
$font=[System.Drawing.Font]::new('Arial',12)
$g.Clear([System.Drawing.Color]::FromArgb(235,231,219))
$g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$names=@('Bronze','Silver','Gold','Mithril','Diamond')
for($i=0;$i -lt 5;$i++){
 $tier=[System.Drawing.Bitmap]::FromFile((Join-Path $project ('Assets/4. Asset/2. Tower/1. Tier/'+$names[$i]+'.png')))
 for($row=0;$row -lt 2;$row++){
  $originX=$i*280+12;$originY=$row*310+32
  $label=if($row -eq 0){'Before - '+$names[$i]}else{'After - '+$names[$i]}
  $g.DrawString($label,$font,[System.Drawing.Brushes]::Black,([single]($i*280+12)),([single]($row*310+8)))
  $tileRect=[System.Drawing.Rectangle]::new($originX,$originY,256,256)
  $g.DrawImage($atlas,$tileRect,0,0,256,256,[System.Drawing.GraphicsUnit]::Pixel)
  $scaleX=if($row -eq 0){1.0}else{1.06}
  $scaleY=if($row -eq 0){1.0}else{1.025}
  $rect=[System.Drawing.RectangleF]::new([single]($originX+128-128*$scaleX),[single]($originY+128-128*$scaleY),[single](256*$scaleX),[single](256*$scaleY))
  $g.DrawImage($tier,$rect,[System.Drawing.RectangleF]::new(0,0,256,256),[System.Drawing.GraphicsUnit]::Pixel)
 }
 $tier.Dispose()
}
$out.Save((Join-Path $PSScriptRoot 'Before_After_On_SpawnTile.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$g.Dispose();$out.Dispose();$atlas.Dispose()
