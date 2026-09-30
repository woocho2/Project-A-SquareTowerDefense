param(
    [string]$SourcePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'output/ui/Tile_TowerSpawn_CleanCartoon_NoCross_Pastel_AsgardTwoTone_Concept.png'),
    [string]$OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'output/ui/Tile_TowerSpawn_Cartoon_Atlas_1024x768_preview.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$sourceCellSize = 362
$targetCellSize = 256
$inset = 2
$source = [System.Drawing.Bitmap]::new($SourcePath)
$atlas = [System.Drawing.Bitmap]::new(1024, 768, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    if ($source.Width -ne 1448 -or $source.Height -ne 1086) {
        throw "Expected a 1448x1086 concept image, got $($source.Width)x$($source.Height)."
    }

    $graphics = [System.Drawing.Graphics]::FromImage($atlas)
    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

        # The concept has four tiles on row one and three on each later row.
        # Clone each source cell first so bicubic sampling cannot pull colors
        # from a neighboring realm. Two transparent pixels protect UV edges.
        for ($row = 0; $row -lt 3; $row++) {
            $columns = if ($row -eq 0) { 4 } else { 3 }
            for ($column = 0; $column -lt $columns; $column++) {
                $sourceRect = [System.Drawing.Rectangle]::new(
                    $column * $sourceCellSize,
                    $row * $sourceCellSize,
                    $sourceCellSize,
                    $sourceCellSize
                )
                $cell = $source.Clone($sourceRect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                try {
                    $targetRect = [System.Drawing.Rectangle]::new(
                        $column * $targetCellSize + $inset,
                        $row * $targetCellSize + $inset,
                        $targetCellSize - 2 * $inset,
                        $targetCellSize - 2 * $inset
                    )
                    $graphics.DrawImage($cell, $targetRect)
                }
                finally {
                    $cell.Dispose()
                }
            }
        }
    }
    finally {
        $graphics.Dispose()
    }

    # Image-generation output carries faint near-transparent color noise in
    # otherwise empty gutters. Remove it before Unity's bilinear sampling.
    for ($y = 0; $y -lt $atlas.Height; $y++) {
        for ($x = 0; $x -lt $atlas.Width; $x++) {
            if ($atlas.GetPixel($x, $y).A -lt 64) {
                $atlas.SetPixel($x, $y, [System.Drawing.Color]::Transparent)
            }
        }
    }

    $atlas.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "Saved 1024x768 cartoon atlas preview: $OutputPath"
}
finally {
    $atlas.Dispose()
    $source.Dispose()
}
