param([Parameter(Mandatory=$true)][string]$Manifest)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$items=Get-Content -Raw -LiteralPath $Manifest | ConvertFrom-Json
$target=Join-Path (Get-Location) 'Assets/4. DotAsset/2. Tower/Parts/Emblem'
$preview=[System.Drawing.Bitmap]::new(1024,1024)
$pg=[System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(252,242,221))
$font=[System.Drawing.Font]::new('Arial',12)
$ink=[System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(42,55,65))
$index=0
try {
 foreach($item in $items){
  $dest=Join-Path $target ($item.Name+'_WhitePattern_V10_2048.png')
  if(Test-Path -LiteralPath $dest){throw "Output already exists: $dest"}
  $src=[System.Drawing.Bitmap]::new([string]$item.Source)
  $mask=[System.Drawing.Bitmap]::new($src.Width,$src.Height,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $minX=$src.Width;$maxX=-1;$minY=$src.Height;$maxY=-1
  try {
   for($y=0;$y -lt $src.Height;$y++){for($x=0;$x -lt $src.Width;$x++){
    $p=$src.GetPixel($x,$y)
    if($item.Name -eq 'Fire'){$coverage=([double]$p.R-$p.G-15)/100.0}
    else{$coverage=(245.0-($p.R+$p.G+$p.B)/3.0)/200.0}
    $a=[int][Math]::Round(255*[Math]::Min(1,[Math]::Max(0,$coverage))*$p.A/255.0)
    $mask.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($a,255,255,255))
    if($a -gt 32){$minX=[Math]::Min($minX,$x);$maxX=[Math]::Max($maxX,$x);$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)}
   }}
   if($maxX -lt 0){throw "No symbol: $($item.Name)"}
   $crop=[System.Drawing.Rectangle]::FromLTRB([Math]::Max(0,$minX-2),[Math]::Max(0,$minY-2),[Math]::Min($src.Width,$maxX+3),[Math]::Min($src.Height,$maxY+3))
   $ratio=1720.0/[Math]::Max($crop.Width,$crop.Height)
   $w=[int][Math]::Round($crop.Width*$ratio);$h=[int][Math]::Round($crop.Height*$ratio)
   $out=[System.Drawing.Bitmap]::new(2048,2048,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
   $g=[System.Drawing.Graphics]::FromImage($out)
   try {
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.DrawImage($mask,[System.Drawing.Rectangle]::new([int][Math]::Floor((2048-$w)/2),[int][Math]::Floor((2048-$h)/2),$w,$h),$crop,[System.Drawing.GraphicsUnit]::Pixel)
    $out.Save($dest,[System.Drawing.Imaging.ImageFormat]::Png)
    $attributes=[System.Drawing.Imaging.ImageAttributes]::new()
    $matrix=[System.Drawing.Imaging.ColorMatrix]::new()
    $matrix.Matrix00=0.16;$matrix.Matrix11=0.21;$matrix.Matrix22=0.25
    $attributes.SetColorMatrix($matrix)
    try {$pg.DrawImage($out,[System.Drawing.Rectangle]::new(($index%4)*256+16,[int][Math]::Floor($index/4)*256+8,224,224),0,0,2048,2048,[System.Drawing.GraphicsUnit]::Pixel,$attributes)} finally {$attributes.Dispose()}
    $pg.DrawString([string]$item.Name,$font,$ink,($index%4)*256+20,[int][Math]::Floor($index/4)*256+232)
    Write-Output "$($item.Name): 2048x2048, centered, white with alpha"
   } finally {$g.Dispose();$out.Dispose()}
  } finally {$src.Dispose();$mask.Dispose()}
  $index++
 }
 $preview.Save((Join-Path (Get-Location) 'output/WhitePatterns_V10_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally {$pg.Dispose();$preview.Dispose();$font.Dispose();$ink.Dispose()}
