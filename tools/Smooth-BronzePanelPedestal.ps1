Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$assetDirectory = 'Assets/4. DotAsset/2. Tower/Parts/Tier'
$sourcePath = Join-Path $assetDirectory 'Bronze_Panel_512_InsetFit_V5.png'
$outputPath = Join-Path $assetDirectory 'Bronze_Panel_512_InsetFit_V6.png'

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $sourcePath))
$output = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

function Fill-Band([int]$top, [int]$height, [int]$left, [int]$width, $color) {
    for ($y = $top; $y -lt $top + $height; $y++) {
        for ($x = $left; $x -lt $left + $width; $x++) {
            $output.SetPixel($x, $y, $color)
        }
    }
}

try {
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $output.SetPixel($x, $y, $source.GetPixel($x, $y))
        }
    }

    # Replace the fractured diagonal pattern with one continuous, symmetric bronze pedestal.
    Fill-Band 374 8 112 288 ([System.Drawing.Color]::FromArgb(255, 105, 48, 29))
    Fill-Band 382 14 116 280 ([System.Drawing.Color]::FromArgb(255, 86, 38, 23))
    Fill-Band 396 18 116 280 ([System.Drawing.Color]::FromArgb(255, 71, 30, 18))
    Fill-Band 414 10 120 272 ([System.Drawing.Color]::FromArgb(255, 60, 24, 14))
    Fill-Band 424 7 132 248 ([System.Drawing.Color]::FromArgb(255, 48, 18, 10))
    Fill-Band 431 3 148 216 ([System.Drawing.Color]::FromArgb(255, 37, 13, 8))
    Fill-Band 374 3 128 256 ([System.Drawing.Color]::FromArgb(255, 180, 88, 47))
    Fill-Band 434 3 160 192 ([System.Drawing.Color]::FromArgb(255, 42, 14, 9))

    $output.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $source.Dispose()
    $output.Dispose()
}
