param([Parameter(Mandatory=$true)][string]$Source)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$names = @('Sword','Bow','Shield','Spear','Axe','Hammer','Fire','Ice','Lightning','Wind','Earth','Light','Dark')
$windows = @(@(75,45,315,340),@(385,60,630,340),@(690,70,910,340),@(965,45,1210,340),@(1220,70,1490,340),@(60,370,290,650),@(388,390,595,650),@(670,390,885,645),@(990,390,1150,650),@(1235,400,1490,650),@(50,700,320,950),@(365,700,610,950),@(645,700,920,960))
$folder = Join-Path (Get-Location) 'Assets/4. DotAsset/2. Tower/Parts/Emblem'
$sheet = [System.Drawing.Bitmap]::new($Source)
$preview = [System.Drawing.Bitmap]::new(640,384)
$pg = [System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(252,242,221))
try {
  for($i=0; $i -lt $names.Count; $i++) {
    $col=$i%5; $row=[int][Math]::Floor($i/5)
    $x0=[int][Math]::Floor($col*$sheet.Width/5); $x1=[int][Math]::Floor(($col+1)*$sheet.Width/5)
    $y0=[int][Math]::Floor($row*$sheet.Height/3); $y1=[int][Math]::Floor(($row+1)*$sheet.Height/3)
    $window=$windows[$i]
    $x0=$window[0];$y0=$window[1];$x1=$window[2];$y1=$window[3]
    $minX=$x1; $maxX=-1; $minY=$y1; $maxY=-1
    for($y=$y0; $y -lt $y1; $y++){for($x=$x0; $x -lt $x1; $x++){
      if($sheet.GetPixel($x,$y).A -gt 32){$minX=[Math]::Min($minX,$x);$maxX=[Math]::Max($maxX,$x);$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)}
    }}
    if($maxX -lt 0){throw "Empty cell: $($names[$i])"}
    if(($maxX-$minX) -ge ($x1-$x0-2) -and ($maxY-$minY) -ge ($y1-$y0-2)){throw 'Sheet background is not transparent'}
    $crop=[System.Drawing.Rectangle]::FromLTRB($minX,$minY,$maxX+1,$maxY+1)
    $scale=90.0/[Math]::Max($crop.Width,$crop.Height)
    $w=[int][Math]::Round($crop.Width*$scale);$h=[int][Math]::Round($crop.Height*$scale)
    $dest=Join-Path $folder ($names[$i]+'_ShadowSymbol_V9_128.png')
    $sprite=[System.Drawing.Bitmap]::new(128,128,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g=[System.Drawing.Graphics]::FromImage($sprite)
    try {
      $g.Clear([System.Drawing.Color]::Transparent)
      $g.CompositingMode=[System.Drawing.Drawing2D.CompositingMode]::SourceCopy
      $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
      $g.PixelOffsetMode=[System.Drawing.Drawing2D.PixelOffsetMode]::Half
      $g.DrawImage($sheet,[System.Drawing.Rectangle]::new([int][Math]::Floor((128-$w)/2),[int][Math]::Floor((128-$h)/2),$w,$h),$crop,[System.Drawing.GraphicsUnit]::Pixel)
      $sprite.Save($dest,[System.Drawing.Imaging.ImageFormat]::Png)
      $pg.DrawImageUnscaled($sprite,$col*128,$row*128)
      Write-Output "$($names[$i]): 128x128, visible bounds ${w}x${h}"
    } finally {$g.Dispose();$sprite.Dispose()}
  }
  $preview.Save((Join-Path (Get-Location) 'output/Emblems_Shadow_V9_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally {$pg.Dispose();$preview.Dispose();$sheet.Dispose()}
