Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$assetDirectory = 'Assets/4. DotAsset/2. Tower/Parts/Tier'
$sourcePath = Join-Path $assetDirectory 'Bronze_Panel_512.png'
$outputPath = Join-Path $assetDirectory 'Bronze_Panel_512_Refined_V2.png'

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $sourcePath))
$output = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

function Blend-Color($base, $overlay, [double]$amount) {
    return [System.Drawing.Color]::FromArgb(
        $base.A,
        [Math]::Round($base.R * (1 - $amount) + $overlay.R * $amount),
        [Math]::Round($base.G * (1 - $amount) + $overlay.G * $amount),
        [Math]::Round($base.B * (1 - $amount) + $overlay.B * $amount)
    )
}

try {
    # Preserve every existing pixel, then darken only the outer contour.
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $pixel = $source.GetPixel($x, $y)
            if ($pixel.A -eq 0) {
                $output.SetPixel($x, $y, $pixel)
                continue
            }

            $isWhitePanel = $x -ge 120 -and $x -le 391 -and $y -ge 90 -and $y -le 339 -and $pixel.R -gt 238 -and $pixel.G -gt 238 -and $pixel.B -gt 238
            if ($isWhitePanel) {
                $output.SetPixel($x, $y, $pixel)
                continue
            }

            $touchesTransparency = $false
            foreach ($offset in @(@(-1, 0), @(1, 0), @(0, -1), @(0, 1))) {
                $neighborX = $x + $offset[0]
                $neighborY = $y + $offset[1]
                if ($neighborX -lt 0 -or $neighborX -ge 512 -or $neighborY -lt 0 -or $neighborY -ge 512 -or $source.GetPixel($neighborX, $neighborY).A -eq 0) {
                    $touchesTransparency = $true
                    break
                }
            }

            if ($touchesTransparency) {
                $pixel = [System.Drawing.Color]::FromArgb($pixel.A, [Math]::Round($pixel.R * 0.77), [Math]::Round($pixel.G * 0.77), [Math]::Round($pixel.B * 0.77))
            }
            $output.SetPixel($x, $y, $pixel)
        }
    }

    $highlight = [System.Drawing.Color]::FromArgb(255, 255, 190, 112)
    $shadow = [System.Drawing.Color]::FromArgb(255, 71, 27, 14)

    # Small, deliberate square-pixel highlights on the top, corners, and lower bevel.
    $highlightRects = @(@(136, 65, 224, 2), @(92, 115, 2, 176), @(108, 84, 8, 4), @(116, 76, 12, 4), @(392, 76, 12, 4), @(404, 84, 8, 4), @(122, 340, 10, 3), @(378, 340, 10, 3))
    foreach ($rect in $highlightRects) {
        for ($y = $rect[1]; $y -lt $rect[1] + $rect[3]; $y++) {
            for ($x = $rect[0]; $x -lt $rect[0] + $rect[2]; $x++) {
                $pixel = $output.GetPixel($x, $y)
                if ($pixel.A -gt 0) { $output.SetPixel($x, $y, (Blend-Color $pixel $highlight 0.44)) }
            }
        }
    }

    $shadowRects = @(@(82, 230, 2, 70), @(426, 230, 2, 70), @(150, 430, 210, 2), @(112, 397, 6, 18), @(394, 397, 6, 18))
    foreach ($rect in $shadowRects) {
        for ($y = $rect[1]; $y -lt $rect[1] + $rect[3]; $y++) {
            for ($x = $rect[0]; $x -lt $rect[0] + $rect[2]; $x++) {
                $pixel = $output.GetPixel($x, $y)
                if ($pixel.A -gt 0) { $output.SetPixel($x, $y, (Blend-Color $pixel $shadow 0.38)) }
            }
        }
    }

    $output.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $source.Dispose()
    $output.Dispose()
}
