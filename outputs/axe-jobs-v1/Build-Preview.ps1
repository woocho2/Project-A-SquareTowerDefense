$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'Compare-Faces.cs') -ReferencedAssemblies 'System.Drawing'
$entries = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$referenceDir = Join-Path (Split-Path $PSScriptRoot -Parent) 'axe-base-approved'
$masterPath = Join-Path $referenceDir 'Axe_Base_Approved.png'
$reference = Get-Content -LiteralPath (Join-Path $referenceDir 'Reference.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$rawDir = Join-Path $PSScriptRoot 'raw'
$cellDir = Join-Path $PSScriptRoot 'cells'
New-Item -ItemType Directory -Path $rawDir,$cellDir -Force | Out-Null
$report = New-Object System.Collections.Generic.List[string]
$report.Add('variant,landmark,source_dx,source_dy,edge_correlation,error_at_512_px')
$atlas = New-Object System.Drawing.Bitmap(1024,1024,([System.Drawing.Imaging.PixelFormat]::Format32bppArgb))
$graphics = [System.Drawing.Graphics]::FromImage($atlas)
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
try {
    for ($i=0; $i -lt 4; $i++) {
        $entry = $entries[$i]
        $rawPath = Join-Path $rawDir ($entry.id + '.png')
        Copy-Item -LiteralPath $entry.path -Destination $rawPath -Force
        $report.Add([AxeFaceComparison]::Compare($masterPath,$rawPath,[string]$entry.id))
        $image = [System.Drawing.Bitmap]::FromFile($rawPath)
        try {
            if ($image.Width -ne 1254 -or $image.Height -ne 1254) { throw 'Unexpected source dimensions.' }
            if ($image.GetPixel(0,0).A -gt 8) { throw 'Source background is not transparent.' }
            $cellX = ($i % 2) * 512
            $cellY = [int][Math]::Floor($i / 2) * 512
            $scale = [double]$reference.sprite_mapping.uniform_scale
            $dest = New-Object System.Drawing.RectangleF(($cellX + $reference.sprite_mapping.offset_x),($cellY + $reference.sprite_mapping.offset_y),($image.Width * $scale),($reference.source_landmarks.bust_baseline_y * $scale))
            $src = New-Object System.Drawing.RectangleF(0,0,$image.Width,$reference.source_landmarks.bust_baseline_y)
            $graphics.DrawImage($image,$dest,$src,[System.Drawing.GraphicsUnit]::Pixel)
        } finally { $image.Dispose() }
    }
} finally { $graphics.Dispose() }
$report | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Face_Landmarks.csv') -Encoding UTF8

try {
    $atlas.Save((Join-Path $PSScriptRoot 'Axe_Classes_Atlas_Draft.png'),[System.Drawing.Imaging.ImageFormat]::Png)
    $preview = New-Object System.Drawing.Bitmap(1024,1024)
    $g = [System.Drawing.Graphics]::FromImage($preview)
    $font = New-Object System.Drawing.Font('Malgun Gothic',16,[System.Drawing.FontStyle]::Bold)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $format = New-Object System.Drawing.StringFormat
    try {
        $g.Clear([System.Drawing.Color]::FromArgb(47,50,59))
        $g.DrawImageUnscaled($atlas,0,0)
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        for ($i=0; $i -lt 4; $i++) {
            $cellX = ($i % 2) * 512
            $cellY = [int][Math]::Floor($i / 2) * 512
            $rect = New-Object System.Drawing.RectangleF($cellX,($cellY + 485),512,27)
            $g.DrawString([string]$entries[$i].name,$font,$brush,$rect,$format)
            $cell = $atlas.Clone((New-Object System.Drawing.Rectangle($cellX,$cellY,512,512)),[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            try { $cell.Save((Join-Path $cellDir ($entries[$i].id + '.png')),[System.Drawing.Imaging.ImageFormat]::Png) } finally { $cell.Dispose() }
        }
        $preview.Save((Join-Path $PSScriptRoot 'Axe_Classes_Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $format.Dispose(); $brush.Dispose(); $font.Dispose(); $g.Dispose(); $preview.Dispose() }
} finally { $atlas.Dispose() }

# Diagnostic overlays use the exact same source rectangle for every face.
$overlay = New-Object System.Drawing.Bitmap(2160,630)
$g = [System.Drawing.Graphics]::FromImage($overlay)
$master = [System.Drawing.Bitmap]::FromFile($masterPath)
$attributes = New-Object System.Drawing.Imaging.ImageAttributes
$matrix = New-Object System.Drawing.Imaging.ColorMatrix
$matrix.Matrix33 = 0.5
$attributes.SetColorMatrix($matrix)
$font = New-Object System.Drawing.Font('Malgun Gothic',14)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
try {
    $g.Clear([System.Drawing.Color]::FromArgb(47,50,59))
    for ($i=0; $i -lt 4; $i++) {
        $rect = New-Object System.Drawing.Rectangle(($i * 540),0,540,590)
        $g.DrawImage($master,$rect,390,390,540,590,[System.Drawing.GraphicsUnit]::Pixel)
        $image = [System.Drawing.Bitmap]::FromFile((Join-Path $rawDir ($entries[$i].id + '.png')))
        try { $g.DrawImage($image,$rect,390,390,540,590,[System.Drawing.GraphicsUnit]::Pixel,$attributes) } finally { $image.Dispose() }
        $g.DrawString(([string]$entries[$i].name + ' / master 50% overlay'),$font,$brush,($i * 540 + 10),595)
    }
    $overlay.Save((Join-Path $PSScriptRoot 'Face_Overlay.png'),[System.Drawing.Imaging.ImageFormat]::Png)
} finally { $brush.Dispose(); $font.Dispose(); $attributes.Dispose(); $master.Dispose(); $g.Dispose(); $overlay.Dispose() }

$metrics = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Face_Landmarks.csv') -Encoding UTF8 | ConvertFrom-Csv
$maxError = ($metrics | ForEach-Object { [double]$_.error_at_512_px } | Measure-Object -Maximum).Maximum
$minCorrelation = ($metrics | ForEach-Object { [double]$_.edge_correlation } | Measure-Object -Minimum).Minimum
Write-Output ('Four variants saved; common scale and coordinates used. Max landmark shift at 512px: ' + $maxError + '; minimum edge correlation: ' + $minCorrelation)
