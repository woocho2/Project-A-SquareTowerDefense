param(
    [string]$SourceImage = 'C:/Users/user/.codex/generated_images/01a064d2-c8d9-7780-93e6-a51a25095313/exec-760499ea-20e1-43e5-a46d-a904b4911e6c.png'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = (Resolve-Path '.').Path
$previewDirectory = Join-Path $projectRoot '_CodexPreview/ColorRune'
$assetDirectory = Join-Path $projectRoot 'Assets/4. DotAsset/2. Tower/2. Color'
$maskPath = Join-Path $previewDirectory 'RuneMask.png'

function New-Bitmap([int]$width, [int]$height) {
    return [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function Is-SourceRune([System.Drawing.Color]$pixel) {
    return $pixel.A -ge 128 -and $pixel.R -ge 155 -and
        ($pixel.R - $pixel.G) -ge 65 -and ($pixel.R - $pixel.B) -ge 55
}

if (-not (Test-Path -LiteralPath $maskPath)) {
    if (-not (Test-Path -LiteralPath $SourceImage)) {
        throw "Rune source image missing: $SourceImage"
    }

    $source = [System.Drawing.Bitmap]::FromFile($SourceImage)
    $mask = New-Bitmap 108 108
    try {
        # The AI draft supplies ornament shapes only. Extract its red pixels;
        # the gray remnant in its center and all outside artifacts are discarded.
        $cropX = 159
        $cropY = 159
        $cropWidth = 937
        $cropHeight = 926
        for ($y = 0; $y -lt 108; $y++) {
            for ($x = 0; $x -lt 108; $x++) {
                $sourceX = [int]($cropX + ($x + 0.5) * $cropWidth / 108)
                $sourceY = [int]($cropY + ($y + 0.5) * $cropHeight / 108)
                $redSamples = 0
                foreach ($dy in @(-2, 0, 2)) {
                    foreach ($dx in @(-2, 0, 2)) {
                        if (Is-SourceRune ($source.GetPixel($sourceX + $dx, $sourceY + $dy))) {
                            $redSamples++
                        }
                    }
                }
                $pixel = if ($redSamples -ge 3) {
                    [System.Drawing.Color]::Black
                } else {
                    [System.Drawing.Color]::White
                }
                $mask.SetPixel($x, $y, $pixel)
            }
        }
        $mask.Save($maskPath, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $mask.Dispose()
        $source.Dispose()
    }
}

$palette = @(
    @{ Name = '1. Red_Rune.png'; Color = [System.Drawing.Color]::FromArgb(255, 217, 77, 77) },
    @{ Name = '2. Blue_Rune.png'; Color = [System.Drawing.Color]::FromArgb(255, 43, 125, 211) },
    @{ Name = '3. Yellow_Rune.png'; Color = [System.Drawing.Color]::FromArgb(255, 217, 151, 30) },
    @{ Name = '4. Black_Rune.png'; Color = [System.Drawing.Color]::FromArgb(255, 45, 48, 52) }
)

$white = [System.Drawing.Color]::White
$transparent = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
$mask = [System.Drawing.Bitmap]::FromFile($maskPath)
try {
    foreach ($entry in $palette) {
        $sprite = New-Bitmap 128 128
        try {
            for ($y = 0; $y -lt 128; $y++) {
                for ($x = 0; $x -lt 128; $x++) {
                    if ($x -lt 10 -or $x -gt 117 -or $y -lt 10 -or $y -gt 117) {
                        $sprite.SetPixel($x, $y, $transparent)
                    } elseif ($mask.GetPixel($x - 10, $y - 10).R -lt 128) {
                        $sprite.SetPixel($x, $y, $entry.Color)
                    } else {
                        $sprite.SetPixel($x, $y, $white)
                    }
                }
            }
            $sprite.Save((Join-Path $assetDirectory $entry.Name), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $sprite.Dispose()
        }
    }
} finally {
    $mask.Dispose()
}

$tier = [System.Drawing.Bitmap]::FromFile((Join-Path $projectRoot 'Assets/4. DotAsset/2. Tower/1. Tier/Diamond.png'))
$emblem = [System.Drawing.Bitmap]::FromFile((Join-Path $projectRoot 'Assets/4. DotAsset/2. Tower/3. Emblem/Emblem/13. Dark.png'))
$preview = New-Bitmap 1120 1120
try {
    $graphics = [System.Drawing.Graphics]::FromImage($preview)
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(29, 45, 62))
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        for ($index = 0; $index -lt $palette.Count; $index++) {
            $color = [System.Drawing.Bitmap]::FromFile((Join-Path $assetDirectory $palette[$index].Name))
            try {
                $left = 20 + ($index % 2) * 560
                $top = 20 + [int][Math]::Floor($index / 2) * 560
                $graphics.DrawImage($color, [System.Drawing.Rectangle]::new($left + 128, $top + 128, 256, 256))
                $graphics.DrawImage($tier, [System.Drawing.Rectangle]::new($left, $top, 512, 512))
                # Emblem textures use PPU 256; at world scale 1 they occupy
                # half the 256px tier canvas (128px, doubled here for preview).
                $graphics.DrawImage($emblem, [System.Drawing.Rectangle]::new($left + 128, $top + 128, 256, 256))
            } finally {
                $color.Dispose()
            }
        }
    } finally {
        $graphics.Dispose()
    }
    $preview.Save((Join-Path $previewDirectory 'RuneColors_InGamePreview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $preview.Dispose()
    $emblem.Dispose()
    $tier.Dispose()
}

Write-Output "Created four 128x128 sprites with centered 108x108 solid-white panels and flat single-color runes."
