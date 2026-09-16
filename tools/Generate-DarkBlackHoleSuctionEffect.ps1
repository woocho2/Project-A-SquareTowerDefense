Add-Type -AssemblyName System.Drawing

$projectRoot = "d:/MyGitHub/ProjectA/Project-A-SquareTowerDefense"
$emblemPath  = "$projectRoot/Assets/4. DotAsset/2. Tower/3. Emblem/Emblem/13. Dark.png"
$targetAsset = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Dark_Splash_BlackHole.png"
$artifactDir = "C:/Users/user/.gemini/antigravity/brain/dc024a25-2f90-4a0d-912c-bd52cd35c0ed"

if (-not (Test-Path $emblemPath)) {
    Write-Error "Emblem not found at $emblemPath"
    exit 1
}

$emblemSrc = [System.Drawing.Bitmap]::FromFile($emblemPath)

# Color helper
function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    return [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

# Tilted ellipse space: -22 deg tilt, aspect ratio 1.5 : 1
function Get-TiltedPoint([float]$cx, [float]$cy, [float]$r, [float]$ang, [float]$tiltRad = -0.38, [float]$aspectY = 0.65) {
    $ux = $r * [Math]::Cos($ang)
    $uy = ($r * [Math]::Sin($ang)) * $aspectY
    $cosT = [Math]::Cos($tiltRad)
    $sinT = [Math]::Sin($tiltRad)
    $rx = $ux * $cosT - $uy * $sinT
    $ry = $ux * $sinT + $uy * $cosT
    return @{ X = $cx + $rx; Y = $cy + $ry }
}

$sheetWidth = 1280
$sheetHeight = 128
$sheet = New-Object System.Drawing.Bitmap($sheetWidth, $sheetHeight)
$gfxSheet = [System.Drawing.Graphics]::FromImage($sheet)
$gfxSheet.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gfxSheet.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfxSheet.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

# 24 infalling particle streams
$particles = @()
for ($i = 0; $i -lt 24; $i++) {
    $birth = [int]($i % 4) # 0, 1, 2, 3
    $angBase = ($i * [Math]::PI * 2.0 / 24.0) + (($i % 3) * 0.25)
    $rStart = 48.0 + (($i * 7) % 12) # 48..59
    $rEnd = 6.0 + (($i * 3) % 6)     # 6..11
    $speed = 2.4 + (($i % 5) * 0.3)  # 2.4..3.6
    $particles += @{
        Birth = $birth
        StartAng = $angBase
        RStart = $rStart
        REnd = $rEnd
        Speed = $speed
        ColorType = ($i % 3) # 0: Bright Lavender, 1: Deep Violet, 2: Pure White
    }
}

for ($f = 0; $f -lt 10; $f++) {
    $cx = ($f * 128.0) + 64.0
    $cy = 64.0

    # 1. Outer Gravitational Distortion Halo (부드러운 방사 아우라)
    if ($f -le 5) {
        $haloProgress = $f / 5.0
        $haloAlpha = [int](70 * [Math]::Sin(($haloProgress + 0.1) * [Math]::PI * 0.9))
        $haloR = (50.0 - $f * 3.5)

        $pathHalo = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathHalo.AddEllipse(($cx - $haloR * 1.15), ($cy - $haloR * 0.72), ($haloR * 2.3), ($haloR * 1.44))
        $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathHalo)
        $pgb.CenterColor = Clr $haloAlpha 110 20 180
        $pgb.SurroundColors = @([System.Drawing.Color](Clr 0 60 5 110))
        $gfxSheet.FillPath($pgb, $pathHalo)
        $pgb.Dispose()
        $pathHalo.Dispose()
    }

    # 2. Infalling Suction Streams & Particles (부드러운 나선 궤적)
    foreach ($pt in $particles) {
        $age = $f - $pt.Birth
        if ($age -ge 0 -and $age -le 3) {
            $tau = $age / 3.0

            $ptsArray = @()
            for ($s = 0; $s -le 4; $s++) {
                $subTau = [Math]::Max(0.0, $tau - (0.45 * (1.0 - $s / 4.0)))
                $rSub = $pt.RStart * [Math]::Pow((1.0 - $subTau), 1.35) + $pt.REnd * $subTau
                $angSub = $pt.StartAng + ($pt.Speed * [Math]::Pow($subTau, 0.9) * 2.1)
                $pSub = Get-TiltedPoint $cx $cy $rSub $angSub
                $ptsArray += New-Object System.Drawing.PointF([float]$pSub.X, [float]$pSub.Y)
            }

            $alpha = [int](150 + $tau * 105)
            $trailColor = switch ($pt.ColorType) {
                0 { Clr $alpha 230 180 255 } # Lavender
                1 { Clr $alpha 170 80 240 }  # Deep Violet
                2 { Clr $alpha 255 255 255 } # Hot White
            }

            $penStreak = New-Object System.Drawing.Pen($trailColor, (1.2 + $tau * 1.4))
            $penStreak.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $penStreak.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $gfxSheet.DrawCurve($penStreak, $ptsArray)
            $penStreak.Dispose()

            $headPos = $ptsArray[4]
            $bHead = New-Object System.Drawing.SolidBrush((Clr $alpha 255 255 255))
            $headSize = 1.3 + ($tau * 1.5)
            Fill-EllipseCentered $gfxSheet $bHead $headPos.X $headPos.Y $headSize $headSize
            $bHead.Dispose()
        }
    }

    # F0 Subtle Starting Wisps
    if ($f -eq 0) {
        $pWisp = New-Object System.Drawing.Pen((Clr 90 200 140 255), 1.0)
        $pWisp.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pWisp.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        for ($w = 0; $w -lt 4; $w++) {
            $wAng = $w * [Math]::PI / 2.0 + 0.3
            $wpts = @()
            for ($ws = 0; $ws -le 3; $ws++) {
                $wr = 52.0 - ($ws * 6.0)
                $wa = $wAng + ($ws * 0.25)
                $wp = Get-TiltedPoint $cx $cy $wr $wa
                $wpts += New-Object System.Drawing.PointF([float]$wp.X, [float]$wp.Y)
            }
            $gfxSheet.DrawCurve($pWisp, $wpts)
        }
        $pWisp.Dispose()
    }

    # 3. Emblem Vortex Core (13. Dark.png 회전, 나선 수축, 스케일 변환)
    if ($f -le 6) {
        $state = $gfxSheet.Save()
        $gfxSheet.TranslateTransform($cx, $cy)

        $scale = 1.0
        $rotDeg = 0.0
        $alphaMul = 1.0

        switch ($f) {
            0 { $scale = 0.45; $rotDeg = -20.0; $alphaMul = 0.55 }
            1 { $scale = 0.80; $rotDeg = 15.0;  $alphaMul = 0.88 }
            2 { $scale = 0.98; $rotDeg = 65.0;  $alphaMul = 1.00 }
            3 { $scale = 1.05; $rotDeg = 120.0; $alphaMul = 1.00 }
            4 { $scale = 0.85; $rotDeg = 185.0; $alphaMul = 1.00 }
            5 { $scale = 0.58; $rotDeg = 260.0; $alphaMul = 0.92 }
            6 { $scale = 0.28; $rotDeg = 330.0; $alphaMul = 0.70 }
        }

        $gfxSheet.RotateTransform($rotDeg)
        $gfxSheet.ScaleTransform($scale, $scale)

        $destW = 82.0
        $destH = 54.0
        $destX = -$destW / 2.0
        $destY = -$destH / 2.0

        $colMatrix = New-Object System.Drawing.Imaging.ColorMatrix
        $colMatrix.Matrix33 = $alphaMul
        $imgAttr = New-Object System.Drawing.Imaging.ImageAttributes
        $imgAttr.SetColorMatrix($colMatrix, [System.Drawing.Imaging.ColorMatrixFlag]::Default, [System.Drawing.Imaging.ColorAdjustType]::Bitmap)

        $destRect = New-Object System.Drawing.Rectangle([int]$destX, [int]$destY, [int]$destW, [int]$destH)
        $gfxSheet.DrawImage($emblemSrc, $destRect, 185, 295, 645, 430, [System.Drawing.GraphicsUnit]::Pixel, $imgAttr)

        $imgAttr.Dispose()

        # Event horizon core inside rotated coords
        if ($f -ge 2 -and $f -le 4) {
            $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 8 2 16))
            Fill-EllipseCentered $gfxSheet $bVoid 0 0 (10.0 * $scale) (6.5 * $scale)
            $bVoid.Dispose()

            $pPhoton = New-Object System.Drawing.Pen((Clr 245 255 235 255), 1.6)
            Draw-EllipseCentered $gfxSheet $pPhoton 0 0 (10.0 * $scale) (6.5 * $scale)
            $pPhoton.Dispose()
        }

        $gfxSheet.Restore($state)
    }

    # 4. Singularity Flash (F6)
    if ($f -eq 6) {
        $bFlashGlow = New-Object System.Drawing.SolidBrush((Clr 160 210 120 255))
        Fill-EllipseCentered $gfxSheet $bFlashGlow $cx $cy 12.0 12.0
        $bFlashGlow.Dispose()

        $bFlash = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfxSheet $bFlash $cx $cy 4.5 4.5
        $bFlash.Dispose()

        $bSingDot = New-Object System.Drawing.SolidBrush((Clr 255 5 0 10))
        Fill-EllipseCentered $gfxSheet $bSingDot $cx $cy 2.0 2.0
        $bSingDot.Dispose()

        $pFlashRay = New-Object System.Drawing.Pen((Clr 240 240 200 255), 1.8)
        $gfxSheet.DrawLine($pFlashRay, ($cx - 20.0), $cy, ($cx + 20.0), $cy)
        $gfxSheet.DrawLine($pFlashRay, $cx, ($cy - 16.0), $cx, ($cy + 16.0))
        $pFlashRay.Dispose()
    }

    # 5. Gravitational Wave Recoil Pulse (F7..F9)
    if ($f -ge 7) {
        $waveProgress = ($f - 6) / 3.8
        $waveR = 24.0 + ($waveProgress * 30.0)
        $waveAlpha = [int]([Math]::Max(15.0, 210 * (1.0 - $waveProgress)))

        $stateWave = $gfxSheet.Save()
        $gfxSheet.TranslateTransform($cx, $cy)
        $gfxSheet.RotateTransform(-22.0)

        $pWave = New-Object System.Drawing.Pen((Clr $waveAlpha 210 120 255), (2.0 * (1.0 - $waveProgress * 0.4)))
        $gfxSheet.DrawEllipse($pWave, -$waveR, (-$waveR * 0.65), ($waveR * 2.0), ($waveR * 1.3))
        $pWave.Dispose()

        if ($f -eq 7 -or $f -eq 8) {
            $innerAlpha = [int]($waveAlpha * 0.6)
            $pInnerWave = New-Object System.Drawing.Pen((Clr $innerAlpha 255 220 255), 1.2)
            $innerR = $waveR * 0.7
            $gfxSheet.DrawEllipse($pInnerWave, -$innerR, (-$innerR * 0.65), ($innerR * 2.0), ($innerR * 1.3))
            $pInnerWave.Dispose()
        }

        $bSparkle = New-Object System.Drawing.SolidBrush((Clr $waveAlpha 245 220 255))
        $sparkCount = if ($f -eq 7) { 8 } elseif ($f -eq 8) { 6 } else { 4 }
        for ($k = 0; $k -lt $sparkCount; $k++) {
            $spAng = $k * [Math]::PI * 2.0 / $sparkCount + ($f * 0.5)
            $spDist = $waveR * (0.75 + (($k % 3) * 0.1))
            $sx = $spDist * [Math]::Cos($spAng)
            $sy = ($spDist * [Math]::Sin($spAng)) * 0.65
            $spSize = [Math]::Max(1.0, (2.2 * (1.0 - $waveProgress * 0.5)))
            $gfxSheet.FillEllipse($bSparkle, ($sx - $spSize/2), ($sy - $spSize/2), $spSize, $spSize)
        }
        $bSparkle.Dispose()

        $gfxSheet.Restore($stateWave)
    }
}

# Save directly to Unity asset
$sheet.Save($targetAsset, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Updated Unity Asset: $targetAsset"

# Generate Showcase Banner
$bannerW = 1200
$bannerH = 500
$banner = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$bgfx = [System.Drawing.Graphics]::FromImage($banner)
$bgfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$bgfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 20 28))
$bgfx.FillRectangle($bBg, 0, 0, $bannerW, $bannerH)
$bBg.Dispose()

$fontTitle = New-Object System.Drawing.Font("Malgun Gothic", 12, [System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font("Malgun Gothic", 9, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))

$bgfx.DrawString("E113: Dark Black Hole - Emblem Vortex & Suction Animation (10 Frames @ 16 FPS)", $fontTitle, $bWhite, 30, 20)
$bgfx.DrawString("13. Dark.png 문양 형태 + 나선형 물질 흡입 스트림(Infalling Accretion) + 특이점 붕괴 & 중력파 리플", $fontSub, $bGray, 30, 48)

for ($f = 0; $f -lt 10; $f++) {
    $x = 30 + ($f * 114)
    $y = 85
    $pBox = New-Object System.Drawing.Pen((Clr 255 45 50 68), 1.0)
    $bgfx.DrawRectangle($pBox, $x, $y, 96, 96)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.RectangleF(($f * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.RectangleF($x, $y, 96, 96)
    $bgfx.DrawImage($sheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $bgfx.DrawString("F$f", $fontSub, $bGray, ($x + 38), ($y + 102))
}

$zoomFrames = @(
    @{ F = 1; Label = "F1: 흡입 개시 (Accretion Start)" },
    @{ F = 2; Label = "F2: 문양 소용돌이 흡입 (Peak Suction)" },
    @{ F = 3; Label = "F3: 초강력 조석 가속 (Tidal Infall)" },
    @{ F = 4; Label = "F4: 중심 수축 (Core Winding)" },
    @{ F = 6; Label = "F6: 특이점 붕괴 섬광 (Singularity)" }
)

for ($zi = 0; $zi -lt $zoomFrames.Count; $zi++) {
    $zf = $zoomFrames[$zi]
    $zx = 30 + ($zi * 230)
    $zy = 240
    $zSize = 180

    $pBox = New-Object System.Drawing.Pen((Clr 255 65 70 95), 1.5)
    $bgfx.DrawRectangle($pBox, $zx, $zy, $zSize, $zSize)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.RectangleF(($zf.F * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.RectangleF($zx, $zy, $zSize, $zSize)
    $bgfx.DrawImage($sheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $bgfx.DrawString($zf.Label, $fontSub, $bWhite, $zx, ($zy + $zSize + 8))
}

$bannerPath = "$artifactDir/preview_dark_blackhole_suction.png"
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Updated Banner: $bannerPath"

$bgfx.Dispose()
$banner.Dispose()
$gfxSheet.Dispose()
$sheet.Dispose()
$emblemSrc.Dispose()

Write-Host "Dark Black Hole Suction Effect update completed successfully!"
