Add-Type -AssemblyName System.Drawing

$projectRoot = "d:/MyGitHub/ProjectA/Project-A-SquareTowerDefense"
$artifactDir = "C:/Users/user/.gemini/antigravity/brain/dc024a25-2f90-4a0d-912c-bd52cd35c0ed"

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $ca = [Math]::Max(0, [Math]::Min(255, $a))
    $cr = [Math]::Max(0, [Math]::Min(255, $r))
    $cg = [Math]::Max(0, [Math]::Min(255, $g))
    $cb = [Math]::Max(0, [Math]::Min(255, $b))
    return [System.Drawing.Color]::FromArgb($ca, $cr, $cg, $cb)
}

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

# Helper to draw volumetric storm clouds at the top
# cx is center X, topY is cloud base Y, alpha is opacity, isLit is whether lightning illuminates from within
function Draw-StormCloud($gfx, [float]$cx, [float]$cy, [float]$w, [float]$h, [float]$alpha, [bool]$isLit, [float]$flashGlow = 0.0) {
    $a = [int]$alpha
    if ($a -le 0) { return }

    # 1. Back/Deep Cloud Puffs (Dark charcoal slate)
    $bDarkPuff = New-Object System.Drawing.SolidBrush((Clr $a 35 38 48))
    Fill-EllipseCentered $gfx $bDarkPuff ($cx - $w * 0.32) ($cy + 2.0) ($w * 0.28) ($h * 0.42)
    Fill-EllipseCentered $gfx $bDarkPuff ($cx + $w * 0.30) ($cy + 1.0) ($w * 0.26) ($h * 0.40)
    Fill-EllipseCentered $gfx $bDarkPuff ($cx - $w * 0.12) ($cy - 3.0) ($w * 0.34) ($h * 0.48)
    Fill-EllipseCentered $gfx $bDarkPuff ($cx + $w * 0.14) ($cy - 2.0) ($w * 0.30) ($h * 0.46)
    $bDarkPuff.Dispose()

    # 2. Mid Cloud Body (Slate grey with purple/blue undertone)
    $bMidPuff = New-Object System.Drawing.SolidBrush((Clr $a 52 56 70))
    Fill-EllipseCentered $gfx $bMidPuff ($cx - $w * 0.22) $cy ($w * 0.24) ($h * 0.38)
    Fill-EllipseCentered $gfx $bMidPuff ($cx + $w * 0.20) $cy ($w * 0.22) ($h * 0.36)
    Fill-EllipseCentered $gfx $bMidPuff $cx ($cy - 4.0) ($w * 0.28) ($h * 0.44)
    $bMidPuff.Dispose()

    # 3. Internal Lightning Illumination (먹구름 내부에서 번쩍이는 황금빛/순백 번개 광휘)
    if ($isLit) {
        $litAlpha = [int]([Math]::Min(255.0, $a * 1.2))
        $pathLit = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathLit.AddEllipse(($cx - $w * 0.26), ($cy - $h * 0.35), ($w * 0.52), ($h * 0.70))
        $pgbLit = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathLit)
        $pgbLit.CenterColor = Clr $litAlpha 255 240 100
        $pgbLit.SurroundColors = @([System.Drawing.Color](Clr 0 180 140 30))
        $gfx.FillPath($pgbLit, $pathLit)
        $pgbLit.Dispose()
        $pathLit.Dispose()

        # Cloud rim highlight (Top and bottom silver-gold rims)
        $rimA = [int]($a * 0.85)
        $pRim = New-Object System.Drawing.Pen((Clr $rimA 255 235 120), 1.2)
        $gfx.DrawArc($pRim, ($cx - $w * 0.38), ($cy - $h * 0.45), ($w * 0.40), ($h * 0.65), 180, 150)
        $gfx.DrawArc($pRim, ($cx - $w * 0.15), ($cy - $h * 0.52), ($w * 0.45), ($h * 0.70), 190, 160)
        $pRim.Dispose()
    }

    # 4. Extra Flash Bloom (F1/F2 strike moments)
    if ($flashGlow -gt 0.0) {
        $glowA = [int]([Math]::Min(255.0, $flashGlow * 180))
        $pathBloom = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathBloom.AddEllipse(($cx - $w * 0.6), ($cy - $h * 0.6), ($w * 1.2), ($h * 1.2))
        $pgbBloom = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathBloom)
        $pgbBloom.CenterColor = Clr $glowA 255 250 160
        $pgbBloom.SurroundColors = @([System.Drawing.Color](Clr 0 255 220 50))
        $gfx.FillPath($pgbBloom, $pathBloom)
        $pgbBloom.Dispose()
        $pathBloom.Dispose()
    }
}

# Helper to draw multi-segment jagged lightning bolts
function Draw-Bolt($gfx, [System.Drawing.PointF[]]$nodes, $glowColor, [float]$glowW, $coreColor, [float]$coreW) {
    if ($nodes.Count -lt 2) { return }

    # Glow pen
    if ($glowW -gt 0.0 -and $glowColor) {
        $pGlow = New-Object System.Drawing.Pen($glowColor, $glowW)
        $pGlow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pGlow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pGlow.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
        $gfx.DrawLines($pGlow, $nodes)
        $pGlow.Dispose()
    }

    # Core pen
    if ($coreW -gt 0.0 -and $coreColor) {
        $pCore = New-Object System.Drawing.Pen($coreColor, $coreW)
        $pCore.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pCore.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pCore.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
        $gfx.DrawLines($pCore, $nodes)
        $pCore.Dispose()
    }
}

$sheetW = 1280
$sheetH = 128
$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$gfx = [System.Drawing.Graphics]::FromImage($sheet)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

for ($f = 0; $f -lt 10; $f++) {
    $cx = ($f * 128.0) + 64.0
    $cloudY = 24.0   # Cloud center Y
    $groundY = 100.0 # Ground impact Y

    # =========================================================================
    # F0: CLOUD ACCUMULATION & INTERNAL LIGHTNING FLASH (먹구름 집결 & 내부 번쩍임)
    # =========================================================================
    if ($f -eq 0) {
        # Cloud forming and flashing brightly inside
        Draw-StormCloud $gfx $cx $cloudY 84.0 32.0 230 $true 0.4

        # Stepped leader spark darting down halfway
        $nodesLeader = @(
            [System.Drawing.PointF]::new($cx, ($cloudY + 6.0)),
            [System.Drawing.PointF]::new(($cx - 6.0), ($cloudY + 20.0)),
            [System.Drawing.PointF]::new(($cx + 4.0), ($cloudY + 36.0)),
            [System.Drawing.PointF]::new(($cx - 2.0), ($cloudY + 50.0))
        )
        Draw-Bolt $gfx $nodesLeader (Clr 160 255 220 30) 2.5 (Clr 255 255 255 255) 1.2

        # Faint ground static charge glow
        $bCharge = New-Object System.Drawing.SolidBrush((Clr 90 255 210 20))
        Fill-EllipseCentered $gfx $bCharge $cx $groundY 18.0 6.0
        $bCharge.Dispose()
        continue
    }

    # =========================================================================
    # F1: MASSIVE LIGHTNING STRIKE (거대 벼락 강타! 구름 -> 지면 수직 폭뢰)
    # =========================================================================
    if ($f -eq 1) {
        # Blinding cloud internal illumination & top bloom
        Draw-StormCloud $gfx $cx $cloudY 90.0 34.0 255 $true 1.0

        # 1. Main Trunk Lightning Bolt (Center massive zig-zag)
        $nodesMain = @(
            [System.Drawing.PointF]::new($cx, ($cloudY + 4.0)),
            [System.Drawing.PointF]::new(($cx - 8.0), 40.0),
            [System.Drawing.PointF]::new(($cx + 7.0), 56.0),
            [System.Drawing.PointF]::new(($cx - 5.0), 72.0),
            [System.Drawing.PointF]::new(($cx + 6.0), 86.0),
            [System.Drawing.PointF]::new($cx, $groundY)
        )
        # Giant golden glow (width 7.0) + pure white incandescent channel (width 2.8)
        Draw-Bolt $gfx $nodesMain (Clr 210 255 210 20) 7.0 (Clr 255 255 255 255) 2.8

        # 2. Left Flanking Branch Bolt (Secondary fork like 9. Electricity.png)
        $nodesLeft = @(
            [System.Drawing.PointF]::new(($cx - 8.0), 40.0),
            [System.Drawing.PointF]::new(($cx - 22.0), 52.0),
            [System.Drawing.PointF]::new(($cx - 18.0), 68.0),
            [System.Drawing.PointF]::new(($cx - 28.0), 84.0)
        )
        Draw-Bolt $gfx $nodesLeft (Clr 180 255 200 20) 3.8 (Clr 255 255 250 220) 1.5

        # 3. Right Flanking Branch Bolt (Secondary fork like 9. Electricity.png)
        $nodesRight = @(
            [System.Drawing.PointF]::new(($cx + 7.0), 56.0),
            [System.Drawing.PointF]::new(($cx + 24.0), 68.0),
            [System.Drawing.PointF]::new(($cx + 19.0), 80.0),
            [System.Drawing.PointF]::new(($cx + 26.0), 92.0)
        )
        Draw-Bolt $gfx $nodesRight (Clr 180 255 200 20) 3.5 (Clr 255 255 250 220) 1.4

        # 4. Ground Impact Explosion Flash
        $pathGround = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathGround.AddEllipse(($cx - 36), ($groundY - 14), 72, 28)
        $pgbGround = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathGround)
        $pgbGround.CenterColor = Clr 255 255 255 255
        $pgbGround.SurroundColors = @([System.Drawing.Color](Clr 0 255 200 20))
        $gfx.FillPath($pgbGround, $pathGround)
        $pgbGround.Dispose()
        $pathGround.Dispose()

        # Core impact dot
        $bImp = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bImp $cx $groundY 12.0 5.5
        $bImp.Dispose()
        continue
    }

    # =========================================================================
    # F2: RETURN STROKE & GROUND SHOCKWAVE (정점 방전 & 지면 방전 파동)
    # =========================================================================
    if ($f -eq 2) {
        # Clouds heavily charged with lightning
        Draw-StormCloud $gfx $cx $cloudY 88.0 34.0 250 $true 0.75

        # Main channel roaring with intense energy
        $nodesMain = @(
            [System.Drawing.PointF]::new($cx, ($cloudY + 4.0)),
            [System.Drawing.PointF]::new(($cx + 6.0), 38.0),
            [System.Drawing.PointF]::new(($cx - 9.0), 55.0),
            [System.Drawing.PointF]::new(($cx + 5.0), 73.0),
            [System.Drawing.PointF]::new(($cx - 4.0), 87.0),
            [System.Drawing.PointF]::new($cx, $groundY)
        )
        Draw-Bolt $gfx $nodesMain (Clr 220 255 220 30) 6.0 (Clr 255 255 255 255) 2.4

        # Left/right plasma arcs
        $nodesLeft = @(
            [System.Drawing.PointF]::new(($cx - 9.0), 55.0),
            [System.Drawing.PointF]::new(($cx - 24.0), 66.0),
            [System.Drawing.PointF]::new(($cx - 20.0), 80.0)
        )
        $nodesRight = @(
            [System.Drawing.PointF]::new(($cx + 5.0), 73.0),
            [System.Drawing.PointF]::new(($cx + 22.0), 82.0),
            [System.Drawing.PointF]::new(($cx + 30.0), 96.0)
        )
        Draw-Bolt $gfx $nodesLeft  (Clr 180 255 210 20) 3.0 (Clr 255 255 255 240) 1.2
        Draw-Bolt $gfx $nodesRight (Clr 180 255 210 20) 3.0 (Clr 255 255 255 240) 1.2

        # Expanding Ground Electric Shockwave Ring (수평 전격 충격파 링)
        $pRing = New-Object System.Drawing.Pen((Clr 240 255 235 60), 2.4)
        $gfx.DrawEllipse($pRing, ($cx - 40.0), ($groundY - 12.0), 80.0, 24.0)
        $pRing.Dispose()

        # Ground plasma creepers crawling along the earth
        $creepLeft = @(
            [System.Drawing.PointF]::new($cx, $groundY),
            [System.Drawing.PointF]::new(($cx - 18.0), ($groundY - 4.0)),
            [System.Drawing.PointF]::new(($cx - 36.0), ($groundY + 3.0)),
            [System.Drawing.PointF]::new(($cx - 48.0), $groundY)
        )
        $creepRight = @(
            [System.Drawing.PointF]::new($cx, $groundY),
            [System.Drawing.PointF]::new(($cx + 16.0), ($groundY + 3.0)),
            [System.Drawing.PointF]::new(($cx + 34.0), ($groundY - 3.0)),
            [System.Drawing.PointF]::new(($cx + 46.0), $groundY)
        )
        Draw-Bolt $gfx $creepLeft  (Clr 180 255 220 40) 2.2 (Clr 255 255 255 255) 1.0
        Draw-Bolt $gfx $creepRight (Clr 180 255 220 40) 2.2 (Clr 255 255 255 255) 1.0

        # Ground impact center core
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $groundY 8.0 4.0
        $bCore.Dispose()
        continue
    }

    # =========================================================================
    # F3: RESIDUAL LIGHTNING & EXPANDING WAVE (잔류 벼락 & 전격 파동 확장)
    # =========================================================================
    if ($f -eq 3) {
        # Clouds still discharging
        Draw-StormCloud $gfx $cx $cloudY 84.0 32.0 230 $true 0.35

        # Thin crackling residual bolt
        $nodesThin = @(
            [System.Drawing.PointF]::new($cx, ($cloudY + 6.0)),
            [System.Drawing.PointF]::new(($cx - 5.0), 42.0),
            [System.Drawing.PointF]::new(($cx + 4.0), 62.0),
            [System.Drawing.PointF]::new(($cx - 3.0), 80.0),
            [System.Drawing.PointF]::new($cx, $groundY)
        )
        Draw-Bolt $gfx $nodesThin (Clr 160 255 210 30) 2.8 (Clr 255 255 255 240) 1.2

        # Ground Shockwave expands to 96px
        $pRing = New-Object System.Drawing.Pen((Clr 200 255 225 50), 2.0)
        $gfx.DrawEllipse($pRing, ($cx - 48.0), ($groundY - 14.0), 96.0, 28.0)
        $pRing.Dispose()

        # Discharging spark nodes around the ring
        $bSpark = New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))
        for ($k = 0; $k -lt 10; $k++) {
            $ang = $k * [Math]::PI / 5.0
            $sx = $cx + [Math]::Cos($ang) * 48.0
            $sy = $groundY + [Math]::Sin($ang) * 14.0
            Fill-EllipseCentered $gfx $bSpark $sx $sy 2.0 2.0
        }
        $bSpark.Dispose()
        continue
    }

    # =========================================================================
    # F4: BEAD LIGHTNING & DUST SHOCKWAVE (플라즈마 비드 & 지면 충격파)
    # =========================================================================
    if ($f -eq 4) {
        # Clouds starting to calm
        Draw-StormCloud $gfx $cx $cloudY 80.0 30.0 200 $false 0.0

        # Bead lightning: segmented glowing spark beads along the strike path
        $bBead = New-Object System.Drawing.SolidBrush((Clr 240 255 245 140))
        $beadsY = @(36.0, 48.0, 60.0, 72.0, 84.0)
        foreach ($by in $beadsY) {
            $bx = $cx + ([Math]::Sin($by * 0.4) * 5.0)
            Fill-EllipseCentered $gfx $bBead $bx $by 2.2 2.2
        }
        $bBead.Dispose()

        # Large fading ground ring
        $pRing = New-Object System.Drawing.Pen((Clr 150 255 220 40), 1.6)
        $gfx.DrawEllipse($pRing, ($cx - 52.0), ($groundY - 15.0), 104.0, 30.0)
        $pRing.Dispose()

        # Ground electric embers
        $bEmber = New-Object System.Drawing.SolidBrush((Clr 200 255 230 60))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0 + 0.1
            $sx = $cx + [Math]::Cos($ang) * 52.0
            $sy = $groundY + [Math]::Sin($ang) * 15.0
            Fill-EllipseCentered $gfx $bEmber $sx $sy 1.8 1.8
        }
        $bEmber.Dispose()
        continue
    }

    # =========================================================================
    # F5..F6: DISPERSING STATIC SPARKS & CALMING CLOUDS (정전기 방전 & 구름 진정)
    # =========================================================================
    if ($f -eq 5 -or $f -eq 6) {
        $calmA = if ($f -eq 5) { 150 } else { 100 }
        Draw-StormCloud $gfx $cx $cloudY 76.0 28.0 $calmA $false 0.0

        # Faint sparks floating across the ground
        $sparkAlpha = [int]($calmA * 1.3)
        $bSpark = New-Object System.Drawing.SolidBrush((Clr $sparkAlpha 255 240 100))
        $spkCount = if ($f -eq 5) { 8 } else { 5 }
        for ($k = 0; $k -lt $spkCount; $k++) {
            $ang = $k * [Math]::PI * 2.0 / $spkCount + ($f * 0.4)
            $dist = 36.0 + (($k % 3) * 6.0)
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $groundY + [Math]::Sin($ang) * ($dist * 0.3)
            Fill-EllipseCentered $gfx $bSpark $sx $sy 1.5 1.5
        }
        $bSpark.Dispose()
        continue
    }

    # =========================================================================
    # F7..F9: CLOUD DISSIPATION & CLEAR SKY (먹구름 소멸 & 잔류 스파클)
    # =========================================================================
    if ($f -ge 7) {
        $tDissolve = ($f - 6) / 3.0 # 0.33, 0.66, 1.0
        $cloudAlpha = [int]([Math]::Max(10.0, 90 * (1.0 - $tDissolve)))

        Draw-StormCloud $gfx $cx $cloudY 68.0 24.0 $cloudAlpha $false 0.0

        # Tiny static sparks fading out
        if ($f -eq 7 -or $f -eq 8) {
            $bTiny = New-Object System.Drawing.SolidBrush((Clr $cloudAlpha 255 250 160))
            for ($s = 0; $s -lt 4; $s++) {
                $sx = $cx + (($s * 22) - 33)
                $sy = $groundY + (($s % 2) * 4.0 - 2.0)
                Fill-EllipseCentered $gfx $bTiny $sx $sy 1.2 1.2
            }
            $bTiny.Dispose()
        }
    }
}

# Save directly to Unity asset
$targetAsset = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Electric_Splash_Thunder.png"
$sheet.Save($targetAsset, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Updated Target Asset: $targetAsset"

# Generate Showcase Banner (1200x560)
$bannerW = 1200
$bannerH = 560
$banner = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$bgfx = [System.Drawing.Graphics]::FromImage($banner)
$bgfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$bgfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 20 28))
$bgfx.FillRectangle($bBg, 0, 0, $bannerW, $bannerH)
$bBg.Dispose()

$fontTitle = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))

$bgfx.DrawString("E109: Thunder Storm - Lightning Striking from Storm Clouds (10 Frames @ 16 FPS)", $fontTitle, $bWhite, 30, 20)
$bgfx.DrawString("Dark Storm Clouds with Internal Flash + Massive Vertical Lightning Strike + Flanking Branches + Ground Shockwave", $fontSub, $bGray, 30, 48)

# Row 1: All 10 Frames
for ($f = 0; $f -lt 10; $f++) {
    $x = 30 + ($f * 114)
    $y = 85
    $pBox = New-Object System.Drawing.Pen((Clr 255 45 50 68), 1.0)
    $bgfx.DrawRectangle($pBox, $x, $y, 96, 96)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.Rectangle($x, $y, 96, 96)
    $bgfx.DrawImage($sheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $bgfx.DrawString("F$f", $fontSub, $bGray, ($x + 38), ($y + 102))
}

# Row 2: 2.2x Zoom Comparison of Key Stages
$zoomFrames = @(
    @{ F = 0; Label = "F0: Storm Cloud & Internal Flash" },
    @{ F = 1; Label = "F1: Massive Vertical Strike" },
    @{ F = 2; Label = "F2: Return Stroke & Ground Waves" },
    @{ F = 3; Label = "F3: Expanding Electric Shockwave" },
    @{ F = 4; Label = "F4: Bead Lightning & Sparks" },
    @{ F = 7; Label = "F7: Cloud Dissolve & Fade" }
)

for ($zi = 0; $zi -lt $zoomFrames.Count; $zi++) {
    $zf = $zoomFrames[$zi]
    $zx = 30 + ($zi * 190)
    $zy = 240
    $zSize = 160

    $pBox = New-Object System.Drawing.Pen((Clr 255 65 70 95), 1.5)
    $bgfx.DrawRectangle($pBox, $zx, $zy, $zSize, $zSize)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.Rectangle(($zf.F * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.Rectangle($zx, $zy, $zSize, $zSize)
    $bgfx.DrawImage($sheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $bgfx.DrawString($zf.Label, $fontSub, $bWhite, $zx, ($zy + $zSize + 8))
}

$bannerPath = "$artifactDir/preview_electric_storm_lightning.png"
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath "$projectRoot/preview_electric_storm_lightning.png" -Force

$bgfx.Dispose()
$banner.Dispose()
$gfx.Dispose()
$sheet.Dispose()

Write-Host "Electric Storm Lightning Effect generated successfully!"
