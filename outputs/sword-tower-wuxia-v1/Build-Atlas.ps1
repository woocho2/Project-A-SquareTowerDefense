$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$sourceDir = Join-Path $PSScriptRoot 'sources'
New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
$atlas = New-Object System.Drawing.Bitmap(1024, 1024, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
$graphics = [System.Drawing.Graphics]::FromImage($atlas)
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$report = New-Object System.Collections.Generic.List[string]

try {
    for ($i = 0; $i -lt 4; $i++) {
        $entry = $manifest[$i]
        $savedSource = Join-Path $sourceDir ($entry.name + '.png')
        Copy-Item -LiteralPath $entry.path -Destination $savedSource -Force
        $source = [System.Drawing.Bitmap]::FromFile($savedSource)
        try {
            if ($source.Width -ne $source.Height) { throw ('Expected square source: ' + $entry.name) }
            if ($source.GetPixel(0, 0).A -ne 0) { throw ('Source has an opaque background: ' + $entry.name) }
            $cellX = ($i % 2) * 512
            $cellY = [int][Math]::Floor($i / 2) * 512
            # All portraits use one common canvas scale and a flat lower-chest crop.
            # The crop excludes leftover background fragments below the generated busts.
            $cropHeight = [int][Math]::Floor($source.Height * 0.93)
            $height = 480.0 * $cropHeight / $source.Height
            $dest = New-Object System.Drawing.RectangleF(($cellX + 16), ($cellY + 36), 480, $height)
            $src = New-Object System.Drawing.RectangleF(0, 0, $source.Width, $cropHeight)
            $graphics.DrawImage($source, $dest, $src, [System.Drawing.GraphicsUnit]::Pixel)
            $report.Add(($entry.name + ': ' + $source.Width + 'x' + $source.Height + ', cell (' + $cellX + ',' + $cellY + '), sprite fileID ' + $entry.fileId))
        }
        finally { $source.Dispose() }
    }
}
finally { $graphics.Dispose() }

$atlasPath = Join-Path $PSScriptRoot 'Tower_Sword_Front_Atlas_v2.png'
try {
    $atlas.Save($atlasPath, [System.Drawing.Imaging.ImageFormat]::Png)
    # Transparent gutters prevent filtering from picking up a neighboring sprite.
    for ($i = 0; $i -lt 4; $i++) {
        $cellX = ($i % 2) * 512
        $cellY = [int][Math]::Floor($i / 2) * 512
        $hasArtwork = $false
        for ($y = 0; $y -lt 512; $y++) {
            for ($x = 0; $x -lt 512; $x++) {
                $alpha = $atlas.GetPixel($cellX + $x, $cellY + $y).A
                if (($x -lt 8 -or $x -ge 504 -or $y -lt 8 -or $y -ge 504) -and $alpha -ne 0) {
                    throw ('Sprite has no transparent gutter: ' + $manifest[$i].name)
                }
                if ($alpha -gt 0) { $hasArtwork = $true }
            }
        }
        if (-not $hasArtwork) { throw ('Empty sprite: ' + $manifest[$i].name) }
    }
    $preview = New-Object System.Drawing.Bitmap(1024, 1024)
    $previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
    $font = New-Object System.Drawing.Font('Malgun Gothic', 16, [System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245, 245, 245))
    $format = New-Object System.Drawing.StringFormat
    try {
        $previewGraphics.Clear([System.Drawing.Color]::FromArgb(47, 50, 59))
        $previewGraphics.DrawImageUnscaled($atlas, 0, 0)
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        for ($i = 0; $i -lt 4; $i++) {
            $cellX = ($i % 2) * 512
            $cellY = [int][Math]::Floor($i / 2) * 512
            $labelRect = New-Object System.Drawing.RectangleF($cellX, ($cellY + 482), 512, 30)
            $previewGraphics.DrawString([string]$manifest[$i].label, $font, $brush, $labelRect, $format)
        }
        $preview.Save((Join-Path $PSScriptRoot 'Atlas_Preview.png'), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $format.Dispose(); $brush.Dispose(); $font.Dispose(); $previewGraphics.Dispose(); $preview.Dispose()
    }
}
finally { $atlas.Dispose() }
$report.Add('Validation: RGBA 1024x1024; four nonempty 512x512 cells; transparent gutters on all cells.')
$report | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Validation.txt') -Encoding UTF8
Write-Output ($report -join "`n")
