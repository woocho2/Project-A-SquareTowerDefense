$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Get-Location).Path
$recordDir = $PSScriptRoot
$spec = Get-Content -LiteralPath (Join-Path $recordDir 'jobs-and-prompts.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$reference = Join-Path $root $spec.reference
$referenceHashes = @{}
foreach ($path in @($reference, ($reference + '.meta'))) {
    $referenceHashes[$path] = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}
foreach ($job in $spec.jobs) {
    $destination = Join-Path $root $job.destination
    if (Test-Path -LiteralPath $destination) { throw "Destination already exists: $destination" }
    if (-not (Test-Path -LiteralPath $job.source)) { throw "Missing imagegen output: $($job.source)" }
}

$records = @()
foreach ($job in $spec.jobs) {
    $destination = Join-Path $root $job.destination
    $source = New-Object Drawing.Bitmap($job.source)
    $output = New-Object Drawing.Bitmap(1920, 1080, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($output)
    $attributes = New-Object Drawing.Imaging.ImageAttributes
    try {
        $sourceWidth = $source.Width
        $sourceHeight = $source.Height
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [Drawing.Drawing2D.CompositingQuality]::HighQuality
        $attributes.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
        $rect = New-Object Drawing.Rectangle(0, 0, 1920, 1080)
        $graphics.DrawImage($source, $rect, 0, 0, $source.Width, $source.Height, [Drawing.GraphicsUnit]::Pixel, $attributes)
        $output.Save($destination, [Drawing.Imaging.ImageFormat]::Png)
    } finally {
        $attributes.Dispose()
        $graphics.Dispose()
        $output.Dispose()
        $source.Dispose()
    }
    $check = New-Object Drawing.Bitmap($destination)
    try {
        if ($check.Width -ne 1920 -or $check.Height -ne 1080) { throw "Wrong dimensions: $($job.world)" }
        $format = $check.PixelFormat.ToString()
        if ($format -ne 'Format24bppRgb') { throw "Unexpected output format: $format" }
    } finally { $check.Dispose() }
    $file = Get-Item -LiteralPath $destination
    $records += [pscustomobject]@{
        Stage = $job.id
        World = $job.world
        Korean = $job.korean
        Path = $job.destination
        GenerationSource = $job.source
        GeneratedWidth = $sourceWidth
        GeneratedHeight = $sourceHeight
        SavedWidth = 1920
        SavedHeight = 1080
        Opaque = $true
        PNGBytes = $file.Length
        SHA256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        ResizeMethod = 'GDI+ HighQualityBicubic; full image; no crop; no sharpening or new details'
    }
}

# Separate contact sheet: labels are not included in the game PNGs.
$sheet = New-Object Drawing.Bitmap(1440, 900, [Drawing.Imaging.PixelFormat]::Format24bppRgb)
$sg = [Drawing.Graphics]::FromImage($sheet)
$font = New-Object Drawing.Font('Malgun Gothic', 14)
try {
    $sg.Clear([Drawing.Color]::FromArgb(24, 26, 35))
    $sg.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    for ($i = 0; $i -lt $records.Count; $i++) {
        $column = $i % 3
        $row = [int][Math]::Floor($i / 3)
        $x = $column * 480
        $y = $row * 300
        $image = New-Object Drawing.Bitmap((Join-Path $root $records[$i].Path))
        try {
            $rect = New-Object Drawing.Rectangle($x, $y, 480, 270)
            $sg.DrawImage($image, $rect)
        } finally { $image.Dispose() }
        $label = '{0:00} {1} / {2}' -f $records[$i].Stage, $records[$i].Korean, $records[$i].World
        $sg.DrawString($label, $font, [Drawing.Brushes]::White, [single]($x + 10), [single]($y + 272))
    }
    $sheet.Save((Join-Path $recordDir 'Stage_Backdrops_ContactSheet.png'), [Drawing.Imaging.ImageFormat]::Png)
} finally { $font.Dispose(); $sg.Dispose(); $sheet.Dispose() }

foreach ($path in $referenceHashes.Keys) {
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $referenceHashes[$path]) { throw "Reference changed: $path" }
}
$report = [ordered]@{
    GenerationTool = 'Built-in image_gen'
    Count = $records.Count
    Size = @(1920, 1080)
    SourceDimensionsDifferFromSavedDimensions = $true
    ReferencePNGAndMetaUnchanged = $true
    Files = $records
    TotalPNGBytes = ($records | Measure-Object PNGBytes -Sum).Sum
    SceneReferencesChanged = $false
    GameScriptsChanged = $false
    ImportSettingsManuallyCreated = $false
    UnityImportTested = $false
    UnityPlayTested = $false
}
$report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $recordDir 'file-verification.json') -Encoding UTF8
$records | Select-Object World,GeneratedWidth,GeneratedHeight,SavedWidth,SavedHeight,PNGBytes | ConvertTo-Json
Write-Output ('Saved ' + $records.Count + ' PNGs. Total MiB: ' + [Math]::Round($report.TotalPNGBytes / 1MB, 2))
