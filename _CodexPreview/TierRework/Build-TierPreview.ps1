$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$candidateDirectory = Join-Path $PSScriptRoot 'Candidates'
New-Item -ItemType Directory -Path $candidateDirectory -Force | Out-Null

$tiers = @(
    @{ Name = 'Bronze'; WingSource = 'Bronze.png'; FeatherCount = 2; Shadow = '#8A4A15'; Mid = '#D7852D'; Light = '#FFD08A'; Highlight = '#FFF0BE' },
    @{ Name = 'Silver'; WingSource = 'Bronze.png'; FeatherCount = 2; Shadow = '#6E7F96'; Mid = '#B8C7D8'; Light = '#E7F1FA'; Highlight = '#FFFFFF' },
    @{ Name = 'Gold'; WingSource = 'Gold.png'; FeatherCount = 3; Shadow = '#A96C00'; Mid = '#E3A400'; Light = '#FFD95B'; Highlight = '#FFF3B0' },
    @{ Name = 'Mithril'; WingSource = 'Gold.png'; FeatherCount = 3; Shadow = '#4D9799'; Mid = '#9DDED9'; Light = '#D9FFF7'; Highlight = '#E9FFFC' },
    @{ Name = 'Diamond'; WingSource = 'Diamond.png'; FeatherCount = 4; Shadow = '#3F6FA8'; Mid = '#68AEDA'; Light = '#CBEFFF'; Highlight = '#E8FAFF' }
)

$sourceTierDirectory = Join-Path $projectRoot 'Assets\4. DotAsset\2. Tower\1. Tier'

function Convert-HexColor([string]$hex) {
    $value = $hex.TrimStart('#')
    return [System.Drawing.Color]::FromArgb(
        255,
        [Convert]::ToInt32($value.Substring(0, 2), 16),
        [Convert]::ToInt32($value.Substring(2, 2), 16),
        [Convert]::ToInt32($value.Substring(4, 2), 16)
    )
}

function New-Brush([string]$hex) {
    return [System.Drawing.SolidBrush]::new((Convert-HexColor $hex))
}

function Get-TierColor([System.Drawing.Color]$sourceColor, [hashtable]$tier) {
    if ($sourceColor.R -lt 35 -and $sourceColor.G -lt 35 -and $sourceColor.B -lt 35) {
        return [System.Drawing.Color]::Black
    }
    $luminance = (0.2126 * $sourceColor.R) + (0.7152 * $sourceColor.G) + (0.0722 * $sourceColor.B)
    if ($luminance -lt 95) { return Convert-HexColor $tier.Shadow }
    if ($luminance -lt 175) { return Convert-HexColor $tier.Mid }
    if ($luminance -lt 225) { return Convert-HexColor $tier.Light }
    return Convert-HexColor $tier.Highlight
}

function Copy-WingRegion(
    [System.Drawing.Bitmap]$destination,
    [System.Drawing.Bitmap]$source,
    [hashtable]$tier,
    [int]$sourceLeft,
    [int]$sourceTop,
    [int]$sourceRight,
    [int]$sourceBottom,
    [int]$offsetX,
    [int]$offsetY
) {
    for ($sourceY = $sourceTop; $sourceY -le $sourceBottom; $sourceY++) {
        for ($sourceX = $sourceLeft; $sourceX -le $sourceRight; $sourceX++) {
            $sourceColor = $source.GetPixel($sourceX, $sourceY)
            if ($sourceColor.A -lt 128) { continue }
            $destinationX = $sourceX + $offsetX
            $destinationY = $sourceY + $offsetY
            if ($destinationX -lt 0 -or $destinationX -ge 128 -or $destinationY -lt 0 -or $destinationY -ge 256) { continue }
            if ($destinationX -ge 64 -and $destinationX -le 191 -and $destinationY -ge 64 -and $destinationY -le 191) { continue }

            $mappedColor = Get-TierColor $sourceColor $tier
            $destination.SetPixel($destinationX, $destinationY, $mappedColor)
            $destination.SetPixel(255 - $destinationX, $destinationY, $mappedColor)
        }
    }
}

function New-FeatherPolygon(
    [double]$baseX,
    [double]$baseY,
    [double]$tipX,
    [double]$tipY,
    [double]$width,
    [double]$startT,
    [double]$endT
) {
    $vectorX = $baseX - $tipX
    $vectorY = $baseY - $tipY
    $length = [Math]::Sqrt(($vectorX * $vectorX) + ($vectorY * $vectorY))
    $perpendicularX = -$vectorY / $length
    $perpendicularY = $vectorX / $length

    $pointAt = {
        param([double]$t, [double]$side)
        $centerX = $tipX + ($vectorX * $t)
        $centerY = $tipY + ($vectorY * $t)
        return [System.Drawing.Point]::new(
            [int][Math]::Round($centerX + ($perpendicularX * $side)),
            [int][Math]::Round($centerY + ($perpendicularY * $side))
        )
    }

    return [System.Drawing.Point[]]@(
        (& $pointAt $startT 0),
        (& $pointAt ([Math]::Min(0.32, $endT)) ($width * 0.58)),
        (& $pointAt ([Math]::Min(0.68, $endT)) $width),
        (& $pointAt $endT ($width * 0.58)),
        (& $pointAt $endT (-$width * 0.58)),
        (& $pointAt ([Math]::Min(0.68, $endT)) (-$width * 0.78)),
        (& $pointAt ([Math]::Min(0.32, $endT)) (-$width * 0.48))
    )
}

function Draw-RuleFeather(
    [System.Drawing.Graphics]$graphics,
    [hashtable]$tier,
    [double]$baseX,
    [double]$baseY,
    [double]$tipX,
    [double]$tipY
) {
    $black = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
    $shadow = New-Brush $tier.Shadow
    $mid = New-Brush $tier.Mid
    $lightPen = [System.Drawing.Pen]::new((Convert-HexColor $tier.Light), 1)
    $highlightPen = [System.Drawing.Pen]::new((Convert-HexColor $tier.Highlight), 1)
    try {
        $outer = New-FeatherPolygon $baseX $baseY $tipX $tipY 5.2 0 1
        $inner = New-FeatherPolygon $baseX $baseY $tipX $tipY 4.0 0.07 0.94
        $center = New-FeatherPolygon $baseX $baseY $tipX $tipY 2.65 0.14 0.88
        $graphics.FillPolygon($black, $outer)
        $graphics.FillPolygon($shadow, $inner)
        $graphics.FillPolygon($lightPen.Brush, $center)

        # 좌상단 광원 방향을 모든 깃털에 동일 적용한다.
        $vectorX = $baseX - $tipX
        $vectorY = $baseY - $tipY
        $graphics.DrawLine(
            $lightPen,
            [int][Math]::Round($tipX + ($vectorX * 0.22)),
            [int][Math]::Round($tipY + ($vectorY * 0.22)),
            [int][Math]::Round($tipX + ($vectorX * 0.72)),
            [int][Math]::Round($tipY + ($vectorY * 0.72))
        )
        $graphics.DrawLine(
            $highlightPen,
            [int][Math]::Round($tipX + ($vectorX * 0.28)),
            [int][Math]::Round($tipY + ($vectorY * 0.28)),
            [int][Math]::Round($tipX + ($vectorX * 0.43)),
            [int][Math]::Round($tipY + ($vectorY * 0.43))
        )
    }
    finally {
        $black.Dispose()
        $shadow.Dispose()
        $mid.Dispose()
        $lightPen.Dispose()
        $highlightPen.Dispose()
    }
}

function Draw-RuleBasedWings([System.Drawing.Bitmap]$destination, [hashtable]$tier) {
    # 128 논리 픽셀에서 제작한 뒤 2배 복사해 모든 날개 픽셀을 정확한 2x2 블록으로 만든다.
    $logical = [System.Drawing.Bitmap]::new(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($logical)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy

        # 모든 슬롯의 길이는 약 20 논리 픽셀로 같고 각도만 바깥쪽으로 펼쳐진다.
        # 최외곽 깃털도 수평으로 눕히지 않아 가시가 아닌 날개 실루엣을 유지한다.
        $slots = @(
            @{ BaseX = 31; BaseY = 100; TipX = 27; TipY = 80 },
            @{ BaseX = 30; BaseY = 101; TipX = 21; TipY = 83 },
            @{ BaseX = 29; BaseY = 102; TipX = 15; TipY = 88 },
            @{ BaseX = 28; BaseY = 103; TipX = 12; TipY = 91 }
        )

        # 깃털과 하단 소용돌이를 하나의 날개로 묶는 공통 어깨/받침 실루엣.
        $shoulderBlack = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
        $shoulderShadow = New-Brush $tier.Shadow
        $shoulderMid = New-Brush $tier.Mid
        try {
            $shoulderOuter = [System.Drawing.Point[]]@(
                [System.Drawing.Point]::new(31, 94),
                [System.Drawing.Point]::new(28, 91),
                [System.Drawing.Point]::new(24, 94),
                [System.Drawing.Point]::new(22, 99),
                [System.Drawing.Point]::new(25, 104),
                [System.Drawing.Point]::new(31, 103)
            )
            $shoulderInner = [System.Drawing.Point[]]@(
                [System.Drawing.Point]::new(30, 95),
                [System.Drawing.Point]::new(28, 93),
                [System.Drawing.Point]::new(25, 95),
                [System.Drawing.Point]::new(24, 99),
                [System.Drawing.Point]::new(26, 102),
                [System.Drawing.Point]::new(30, 101)
            )
            $graphics.FillPolygon($shoulderBlack, $shoulderOuter)
            $graphics.FillPolygon($shoulderShadow, $shoulderInner)
            $graphics.FillRectangle($shoulderMid, 27, 95, 3, 6)
        }
        finally {
            $shoulderBlack.Dispose()
            $shoulderShadow.Dispose()
            $shoulderMid.Dispose()
        }

        # 바깥 깃털부터 그려 안쪽 깃털이 뿌리에서 자연스럽게 위에 겹치게 한다.
        for ($slotIndex = $tier.FeatherCount - 1; $slotIndex -ge 0; $slotIndex--) {
            $slot = $slots[$slotIndex]
            Draw-RuleFeather $graphics $tier $slot.BaseX $slot.BaseY $slot.TipX $slot.TipY
        }

        # 모든 티어가 공유하는 고정 날개 뿌리와 하단 소용돌이.
        $black = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
        $shadow = New-Brush $tier.Shadow
        $mid = New-Brush $tier.Mid
        $light = New-Brush $tier.Light
        $highlight = New-Brush $tier.Highlight
        try {
            $graphics.FillEllipse($black, 25, 95, 13, 13)
            $graphics.FillEllipse($shadow, 27, 97, 9, 9)
            $graphics.FillEllipse($mid, 28, 98, 7, 7)
            $graphics.FillEllipse($black, 29, 99, 5, 5)
            $graphics.FillEllipse($light, 30, 100, 3, 3)
            $graphics.FillRectangle($highlight, 30, 99, 2, 1)
            $graphics.FillRectangle($black, 32, 103, 7, 2)
            $graphics.FillRectangle($mid, 32, 102, 6, 1)
        }
        finally {
            $black.Dispose()
            $shadow.Dispose()
            $mid.Dispose()
            $light.Dispose()
            $highlight.Dispose()
        }
    }
    finally { $graphics.Dispose() }

    try {
        for ($logicalY = 0; $logicalY -lt 128; $logicalY++) {
            for ($logicalX = 0; $logicalX -lt 64; $logicalX++) {
                $color = $logical.GetPixel($logicalX, $logicalY)
                if ($color.A -eq 0) { continue }
                $pixelX = $logicalX * 2
                $pixelY = $logicalY * 2
                for ($offsetY = 0; $offsetY -lt 2; $offsetY++) {
                    for ($offsetX = 0; $offsetX -lt 2; $offsetX++) {
                        $leftX = $pixelX + $offsetX
                        $targetY = $pixelY + $offsetY
                        if (-not ($leftX -ge 64 -and $leftX -le 191 -and $targetY -ge 64 -and $targetY -le 191)) {
                            $destination.SetPixel($leftX, $targetY, $color)
                            $destination.SetPixel(255 - $leftX, $targetY, $color)
                        }
                    }
                }
            }
        }
    }
    finally { $logical.Dispose() }
}

function Draw-TopCornerNode(
    [System.Drawing.Graphics]$graphics,
    [int]$left,
    [string]$side,
    [System.Drawing.Brush]$black,
    [System.Drawing.Brush]$shadow,
    [System.Drawing.Brush]$mid,
    [System.Drawing.Brush]$light,
    [System.Drawing.Brush]$highlight
) {
    # Bronze 원본의 둥근 결절. 코어 프레임을 키우지 않고 바깥 장식으로만 돌출한다.
    $graphics.FillRectangle($black, $left + 4, 51, 8, 2)
    $graphics.FillRectangle($black, $left + 2, 53, 12, 2)
    $graphics.FillRectangle($black, $left, 55, 16, 8)
    $graphics.FillRectangle($black, $left + 2, 63, 12, 2)
    $graphics.FillRectangle($black, $left + 4, 65, 8, 2)

    $graphics.FillRectangle($shadow, $left + 4, 53, 8, 12)
    $graphics.FillRectangle($shadow, $left + 2, 55, 12, 8)
    $graphics.FillRectangle($mid, $left + 4, 55, 8, 8)
    $graphics.FillRectangle($light, $left + 4, 55, 6, 6)
    $graphics.FillRectangle($highlight, $left + 4, 55, 2, 2)

    # 원본 결절 중앙의 작은 마름모/나사 디테일.
    $graphics.FillRectangle($black, $left + 7, 57, 2, 6)
    $graphics.FillRectangle($black, $left + 5, 59, 6, 2)
    $graphics.FillRectangle($highlight, $left + 7, 59, 2, 2)
}

function Draw-Frame([System.Drawing.Graphics]$graphics, [hashtable]$tier) {
    $black = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Black)
    $shadow = New-Brush $tier.Shadow
    $mid = New-Brush $tier.Mid
    $light = New-Brush $tier.Light
    $highlight = New-Brush $tier.Highlight
    try {
        # 공통 150x150 프레임: x/y 53..202. 모든 티어가 이 마스크를 공유한다.
        $graphics.FillRectangle($black, 53, 53, 150, 150)

        # 6px 주 레일. Bronze 원본처럼 밝은 중심선과 바깥 그림자를 분리한다.
        $graphics.FillRectangle($shadow, 55, 55, 146, 7)
        $graphics.FillRectangle($shadow, 55, 55, 7, 146)
        $graphics.FillRectangle($shadow, 194, 55, 7, 146)
        $graphics.FillRectangle($shadow, 55, 194, 146, 7)

        $graphics.FillRectangle($mid, 57, 55, 142, 6)
        $graphics.FillRectangle($mid, 55, 57, 6, 142)
        $graphics.FillRectangle($mid, 195, 57, 4, 142)
        $graphics.FillRectangle($mid, 57, 195, 142, 4)

        $graphics.FillRectangle($highlight, 59, 55, 138, 2)
        $graphics.FillRectangle($highlight, 55, 59, 2, 138)
        $graphics.FillRectangle($light, 59, 57, 138, 2)
        $graphics.FillRectangle($light, 57, 59, 2, 138)
        $graphics.FillRectangle($shadow, 197, 59, 2, 138)
        $graphics.FillRectangle($shadow, 59, 197, 138, 2)

        # 순수 검정 2px 내곽선. 투명 창은 정확히 64..191이다.
        $graphics.FillRectangle($black, 62, 62, 132, 2)
        $graphics.FillRectangle($black, 62, 192, 132, 2)
        $graphics.FillRectangle($black, 62, 62, 2, 132)
        $graphics.FillRectangle($black, 192, 62, 2, 132)

        # 상단 결절은 Bronze의 둥근 모서리 인상을 공통 마스터로 사용한다.
        Draw-TopCornerNode $graphics 51 'Left' $black $shadow $mid $light $highlight
        Draw-TopCornerNode $graphics 189 'Right' $black $shadow $mid $light $highlight

        # 내부 창 128x128을 마지막에 완전 투명으로 비운다: x/y 64..191
        $previousMode = $graphics.CompositingMode
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $transparent = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::Transparent)
        try { $graphics.FillRectangle($transparent, 64, 64, 128, 128) }
        finally { $transparent.Dispose() }
        $graphics.CompositingMode = $previousMode
    }
    finally {
        $black.Dispose()
        $shadow.Dispose()
        $mid.Dispose()
        $light.Dispose()
        $highlight.Dispose()
    }
}

function Assert-Candidate([string]$path) {
    $bitmap = [System.Drawing.Bitmap]::FromFile($path)
    try {
        if ($bitmap.Width -ne 256 -or $bitmap.Height -ne 256) { throw "Invalid canvas: $path" }
        for ($y = 64; $y -le 191; $y++) {
            for ($x = 64; $x -le 191; $x++) {
                if ($bitmap.GetPixel($x, $y).A -ne 0) { throw "Interior intrusion at $x,$y in $path" }
            }
        }
        for ($y = 0; $y -lt 256; $y++) {
            for ($x = 0; $x -lt 128; $x++) {
                $leftOpaque = $bitmap.GetPixel($x, $y).A -gt 0
                $rightOpaque = $bitmap.GetPixel(255 - $x, $y).A -gt 0
                if ($leftOpaque -ne $rightOpaque) { throw "Asymmetric alpha at $x,$y in $path" }
                $alpha = $bitmap.GetPixel($x, $y).A
                if ($alpha -ne 0 -and $alpha -ne 255) { throw "Semitransparent pixel at $x,$y in $path" }
            }
        }
    }
    finally { $bitmap.Dispose() }
}

function Enforce-AlphaSymmetry([System.Drawing.Bitmap]$bitmap) {
    for ($y = 0; $y -lt 256; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $mirrorX = 255 - $x
            $left = $bitmap.GetPixel($x, $y)
            $right = $bitmap.GetPixel($mirrorX, $y)
            if ($left.A -gt 0 -and $right.A -eq 0) {
                $bitmap.SetPixel($mirrorX, $y, $left)
            }
            elseif ($right.A -gt 0 -and $left.A -eq 0) {
                $bitmap.SetPixel($x, $y, $right)
            }
        }
    }
}

$candidatePaths = @()
foreach ($tier in $tiers) {
    $bitmap = [System.Drawing.Bitmap]::new(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy

        Draw-Frame $graphics $tier
    }
    finally { $graphics.Dispose() }

    # 규칙 기반 날개와 소용돌이를 공통 하단 프레임 위에 마지막으로 합성한다.
    Draw-RuleBasedWings $bitmap $tier
    Enforce-AlphaSymmetry $bitmap
    $path = Join-Path $candidateDirectory "$($tier.Name)_Preview.png"
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    Assert-Candidate $path
    $candidatePaths += $path
}

# 공통 코어 프레임 알파가 다섯 후보에서 동일한지 검증한다.
$reference = [System.Drawing.Bitmap]::FromFile($candidatePaths[0])
try {
    for ($candidateIndex = 1; $candidateIndex -lt $candidatePaths.Count; $candidateIndex++) {
        $candidate = [System.Drawing.Bitmap]::FromFile($candidatePaths[$candidateIndex])
        try {
            for ($y = 53; $y -le 202; $y++) {
                for ($x = 53; $x -le 202; $x++) {
                    if (($reference.GetPixel($x, $y).A -gt 0) -ne ($candidate.GetPixel($x, $y).A -gt 0)) {
                        throw "Core frame alpha differs at ${x},${y}: $($candidatePaths[$candidateIndex])"
                    }
                }
            }
        }
        finally { $candidate.Dispose() }
    }
}
finally { $reference.Dispose() }

$preview = [System.Drawing.Bitmap]::new(1420, 650, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
try {
    $previewGraphics.Clear([System.Drawing.Color]::FromArgb(255, 35, 52, 73))
    $previewGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $previewGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $previewGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

    $font = [System.Drawing.Font]::new('Segoe UI', 16, [System.Drawing.FontStyle]::Bold)
    $textBrush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $colorAsset = [System.Drawing.Bitmap]::FromFile((Join-Path $projectRoot 'Assets\4. DotAsset\2. Tower\2. Color\2. Blue.png'))
    $emblemAsset = [System.Drawing.Bitmap]::FromFile((Join-Path $projectRoot 'Assets\4. DotAsset\2. Tower\3. Emblem\Emblem\13. Dark.png'))
    try {
        for ($index = 0; $index -lt $candidatePaths.Count; $index++) {
            $cellX = 14 + ($index * 282)
            $frame = [System.Drawing.Bitmap]::FromFile($candidatePaths[$index])
            try {
                $previewGraphics.DrawString($tiers[$index].Name, $font, $textBrush, $cellX + 86, 6)
                $previewGraphics.DrawImage($frame, $cellX, 38, 256, 256)

                $previewGraphics.DrawImage($colorAsset, [System.Drawing.Rectangle]::new($cellX + 64, 390, 128, 128))
                $previewGraphics.DrawImage($emblemAsset, [System.Drawing.Rectangle]::new($cellX + 64, 390, 128, 128))
                $previewGraphics.DrawImage($frame, $cellX, 326, 256, 256)
            }
            finally { $frame.Dispose() }
        }
    }
    finally {
        $font.Dispose()
        $textBrush.Dispose()
        $colorAsset.Dispose()
        $emblemAsset.Dispose()
    }
}
finally { $previewGraphics.Dispose() }

$previewPath = Join-Path $PSScriptRoot 'TierFramePreview.png'
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()

$candidatePaths
$previewPath
Write-Output 'Validated: 256 canvas, 150 core frame, 128 transparent window, shared core alpha, symmetric opaque alpha.'
