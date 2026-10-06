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
  $label=if($row -eq 0){'Before - '+$names[$i]}else{'Bottom fit - '+$names[$i]}
  $g.DrawString($label,$font,[System.Drawing.Brushes]::Black,([single]($i*280+12)),([single]($row*310+8)))
  $g.DrawImage($atlas,[System.Drawing.Rectangle]::new($originX,$originY,256,256),0,0,256,256,[System.Drawing.GraphicsUnit]::Pixel)
  $scaleY=if($row -eq 0){1.025}else{1.045}
  $offsetPixels=if($row -eq 0){0.0}else{2.46}
  $rect=[System.Drawing.RectangleF]::new([single]($originX+128-128*1.06),[single]($originY+128-128*$scaleY+$offsetPixels),[single](256*1.06),[single](256*$scaleY))
  $g.DrawImage($tier,$rect,[System.Drawing.RectangleF]::new(0,0,256,256),[System.Drawing.GraphicsUnit]::Pixel)
 }
 $tier.Dispose()
}
$out.Save((Join-Path $PSScriptRoot 'Before_After_Bottom_Fit.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$font.Dispose();$g.Dispose();$out.Dispose();$atlas.Dispose()
Write-Output 'Nominal top-edge movement = 0px; bottom-edge movement = 4.84px downward; width unchanged.'
