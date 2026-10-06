$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source=[System.Drawing.Bitmap]::new((Join-Path $projectRoot 'Assets/4. Asset/2. Tower/2. Color/1. Red_Rune.png'))
$large=[System.Drawing.Bitmap]::new(256,256,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics=[System.Drawing.Graphics]::FromImage($large)
$graphics.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.DrawImage($source,[System.Drawing.Rectangle]::new(0,0,256,256))
$graphics.Dispose()
# Preserve the resized alpha of the existing face; store neutral white RGB only.
for($y=0;$y -lt 256;$y++){
 for($x=0;$x -lt 256;$x++){
  $alpha=$large.GetPixel($x,$y).A
  $large.SetPixel($x,$y,[System.Drawing.Color]::FromArgb($alpha,255,255,255))
 }
}
$large.Save((Join-Path $projectRoot 'Assets/4. Asset/2. Tower/2. Color/Tower_White_Background.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$large.Dispose()
$source.Dispose()
