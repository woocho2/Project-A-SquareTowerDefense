Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$assetDirectory = 'Assets/4. DotAsset/2. Tower/Parts/Tier'
$sourcePath = Join-Path $assetDirectory 'Bronze_Panel_512.png'
$outputPath = Join-Path $assetDirectory 'Bronze_Panel_512_InsetFit_V4.png'

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $sourcePath))
$output = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

function Fill-Rectangle($bitmap, [int]$left, [int]$top, [int]$width, [int]$height, $color) {
    for ($y = $top; $y -lt $top + $height; $y++) {
        for ($x = $left; $x -lt $left + $width; $x++) {
            $bitmap.SetPixel($x, $y, $color)
        }
    }
}

try {
    # Retain the original exterior, pedestal, and bronze palette.
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $output.SetPixel($x, $y, $source.GetPixel($x, $y))
        }
    }

    $darkBronze = [System.Drawing.Color]::FromArgb(255, 57, 24, 16)
    $midBronze = [System.Drawing.Color]::FromArgb(255, 113, 53, 31)
    $lightBronze = [System.Drawing.Color]::FromArgb(255, 183, 91, 48)

    # Rebuild the inset rim. Its central aperture is exactly 260×250px,
    # matching Black_PastelInset_V2 at a 5× integer scale.
    Fill-Rectangle $output 118 81 276 261 $darkBronze
    Fill-Rectangle $output 118 97 8 234 $midBronze
    Fill-Rectangle $output 386 97 8 234 $midBronze
    Fill-Rectangle $output 134 81 244 8 $midBronze
    Fill-Rectangle $output 134 334 244 8 $midBronze

    # Pixel-stepped, rounded corners keep the inset visually consistent with the outer frame.
    Fill-Rectangle $output 126 89 8 8 $midBronze
    Fill-Rectangle $output 378 89 8 8 $midBronze
    Fill-Rectangle $output 126 331 8 3 $midBronze
    Fill-Rectangle $output 378 331 8 3 $midBronze
    Fill-Rectangle $output 134 89 244 3 $lightBronze
    Fill-Rectangle $output 121 105 3 208 $lightBronze

    # 260×250px white aperture: 244px wide on the stepped corner rows,
    # 260px wide through the center. This accepts the 52×50px inset at 5×.
    Fill-Rectangle $output 134 89 244 8 ([System.Drawing.Color]::White)
    Fill-Rectangle $output 126 97 260 234 ([System.Drawing.Color]::White)
    Fill-Rectangle $output 134 331 244 8 ([System.Drawing.Color]::White)

    # Restore a thin dark inside edge so the white aperture sits cleanly inside the bronze bevel.
    Fill-Rectangle $output 122 101 4 226 $darkBronze
    Fill-Rectangle $output 386 101 4 226 $darkBronze
    $output.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $source.Dispose()
    $output.Dispose()
}
