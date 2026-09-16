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

# Helper to draw a 3D faceted mountain / rock spire
# Base at (bx, by), peak at (px, py), width at base is bw
function Draw-RockSpire($gfx, [float]$bx, [float]$by, [float]$px, [float]$py, [float]$bw, [float]$alpha, [float]$scale = 1.0) {
    $w = [float](($bw * $scale) / 2.0)
    $h = [float](($by - $py) * $scale)
    $topY = [float]($by - $h)
    $aInt = [int]$alpha

    # Facet 1: Left Light Face (Lit by amber sunlight)
    $ptsLeft = @(
        [System.Drawing.PointF]::new($px, $topY),
        [System.Drawing.PointF]::new(($bx - $w), $by),
        [System.Drawing.PointF]::new($bx, ($by + 2.0))
    )
    $bLeft = New-Object System.Drawing.SolidBrush((Clr $aInt 185 105 35))
    $gfx.FillPolygon($bLeft, [System.Drawing.PointF[]]$ptsLeft)
    $bLeft.Dispose()

    # Facet 2: Right Shadow Face (Dark earth shadow)
    $ptsRight = @(
        [System.Drawing.PointF]::new($px, $topY),
        [System.Drawing.PointF]::new($bx, ($by + 2.0)),
        [System.Drawing.PointF]::new(($bx + $w), $by)
    )
    $bRight = New-Object System.Drawing.SolidBrush((Clr $aInt 95 45 15))
    $gfx.FillPolygon($bRight, [System.Drawing.PointF[]]$ptsRight)
    $bRight.Dispose()

    # Lit Edge Ridge (Central spine from peak to base)
    $pRidge = New-Object System.Drawing.Pen((Clr ([int]($alpha * 0.95)) 255 210 120), [float](1.4 * $scale))
    $gfx.DrawLine($pRidge, [float]$px, [float]$topY, [float]$bx, [float]($by + 2.0))
    $pRidge.Dispose()

    # Highlight contour line along left side
    $pLeftEdge = New-Object System.Drawing.Pen((Clr ([int]($alpha * 0.8)) 230 160 70), [float](1.1 * $scale))
    $gfx.DrawLine($pLeftEdge, [float]$px, [float]$topY, [float]($bx - $w), [float]$by)
    $pLeftEdge.Dispose()

    # Dark contour line along right side
    $pRightEdge = New-Object System.Drawing.Pen((Clr ([int]($alpha * 0.9)) 55 25 8), [float](1.2 * $scale))
    $gfx.DrawLine($pRightEdge, [float]$px, [float]$topY, [float]($bx + $w), [float]$by)
    $pRightEdge.Dispose()
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
    $baseY = 82.0 # Ground baseline for rock eruption

    # =========================================================================
    # F0: Impact & Ground Rupture (운석 착탄 & 지면 균열 발광)
    # =========================================================================
    if ($f -eq 0) {
        # Seismic golden impact glow
        $pathGlow = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathGlow.AddEllipse(($cx - 36), ($baseY - 20), 72, 40)
        $pgbGlow = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathGlow)
        $pgbGlow.CenterColor = Clr 220 255 190 60
        $pgbGlow.SurroundColors = @([System.Drawing.Color](Clr 0 160 80 15))
        $gfx.FillPath($pgbGlow, $pathGlow)
        $pgbGlow.Dispose()
        $pathGlow.Dispose()

        # Cracked ground base
        $ptsBase = @(
            [System.Drawing.PointF]::new(($cx - 30), ($baseY - 4)),
            [System.Drawing.PointF]::new(($cx + 30), ($baseY - 4)),
            [System.Drawing.PointF]::new(($cx + 24), ($baseY + 10)),
            [System.Drawing.PointF]::new(($cx - 24), ($baseY + 10))
        )
        $bBase = New-Object System.Drawing.SolidBrush((Clr 240 85 40 12))
        $gfx.FillPolygon($bBase, [System.Drawing.PointF[]]$ptsBase)
        $bBase.Dispose()

        # Fissures radiating with golden magma energy
        $pFissure = New-Object System.Drawing.Pen((Clr 255 255 220 100), 1.8)
        $gfx.DrawLine($pFissure, ($cx - 24), $baseY, ($cx - 6), ($baseY + 2))
        $gfx.DrawLine($pFissure, ($cx - 6), ($baseY + 2), $cx, ($baseY - 4))
        $gfx.DrawLine($pFissure, $cx, ($baseY - 4), ($cx + 14), ($baseY + 4))
        $gfx.DrawLine($pFissure, ($cx + 14), ($baseY + 4), ($cx + 26), ($baseY - 2))
        $pFissure.Dispose()

        # Emerging tiny rock tips breaking through
        Draw-RockSpire $gfx $cx $baseY $cx ($baseY - 14.0) 14.0 255 0.5
        Draw-RockSpire $gfx ($cx - 16) $baseY ($cx - 16) ($baseY - 9.0) 10.0 240 0.5
        Draw-RockSpire $gfx ($cx + 16) $baseY ($cx + 16) ($baseY - 10.0) 10.0 240 0.5
        continue
    }

    # =========================================================================
    # F1..F4: EARTH RUPTURE & ROCK SPIRES ERUPTION (솟구쳐 오르는 대지 첨탑)
    # =========================================================================
    if ($f -ge 1 -and $f -le 4) {
        # Eruption progress: 0.35 (F1) -> 0.75 (F2) -> 1.0 (F3, Peak) -> 0.95 (F4)
        $eruptT = switch ($f) {
            1 { 0.42 }
            2 { 0.82 }
            3 { 1.00 }
            4 { 0.95 }
        }

        # 1. Ground Dust Shockwave & Seismic Flare
        $shockW = 44.0 + ($f * 16.0) # Expands 60 -> 108
        $shockH = $shockW * 0.42
        $pathShock = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathShock.AddEllipse(($cx - $shockW/2), ($baseY - $shockH/2 + 4), $shockW, $shockH)
        $pgbShock = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathShock)
        $pgbShock.CenterColor = Clr 140 220 140 40
        $pgbShock.SurroundColors = @([System.Drawing.Color](Clr 0 120 60 10))
        $gfx.FillPath($pgbShock, $pathShock)
        $pgbShock.Dispose()
        $pathShock.Dispose()

        # Shockwave dust ring
        $pDustRing = New-Object System.Drawing.Pen((Clr 190 230 170 80), (1.8 * (1.0 - $f * 0.15)))
        $gfx.DrawEllipse($pDustRing, [float]($cx - $shockW/2), [float]($baseY - $shockH/2 + 4), [float]$shockW, [float]$shockH)
        $pDustRing.Dispose()

        # 2. Uplifted Earth Ground Slabs (Ruptured trapezoidal base)
        $ptsEarthBase = @(
            [System.Drawing.PointF]::new(($cx - 42), ($baseY + 2)),
            [System.Drawing.PointF]::new(($cx + 42), ($baseY + 2)),
            [System.Drawing.PointF]::new(($cx + 34), ($baseY + 16)),
            [System.Drawing.PointF]::new(($cx - 34), ($baseY + 16))
        )
        $bEarthBase = New-Object System.Drawing.SolidBrush((Clr 255 75 35 10))
        $gfx.FillPolygon($bEarthBase, [System.Drawing.PointF[]]$ptsEarthBase)
        $bEarthBase.Dispose()

        # Golden magma fault fissure inside ruptured earth
        $pFissure = New-Object System.Drawing.Pen((Clr 240 255 210 80), 2.2)
        $gfx.DrawLine($pFissure, ($cx - 30), ($baseY + 4), ($cx - 10), ($baseY + 8))
        $gfx.DrawLine($pFissure, ($cx - 10), ($baseY + 8), ($cx + 8), ($baseY + 3))
        $gfx.DrawLine($pFissure, ($cx + 8), ($baseY + 3), ($cx + 32), ($baseY + 7))
        $pFissure.Dispose()

        # 3. THE 5 RISING ROCK SPIRES (솟아오르는 5개의 거대 암석 기둥)
        # Peak heights at full eruption (F3):
        # Central spire: Peak at y=23 (Height ~ 60px)
        # Left main peak: Peak at y=36 (Height ~ 47px)
        # Right main peak: Peak at y=38 (Height ~ 45px)
        # Far-left spike: Peak at y=52 (Height ~ 31px)
        # Far-right spike: Peak at y=54 (Height ~ 29px)

        $curHCenter = 60.0 * $eruptT
        $curHLeft   = 47.0 * $eruptT
        $curHRight  = 45.0 * $eruptT
        $curHFarL   = 31.0 * $eruptT
        $curHFarR   = 29.0 * $eruptT

        # Far Left Spire (angled outward -10 deg)
        Draw-RockSpire $gfx ($cx - 32) ($baseY + 2) ($cx - 34) ($baseY + 2 - $curHFarL) 12.0 255 1.0
        # Far Right Spire (angled outward +10 deg)
        Draw-RockSpire $gfx ($cx + 32) ($baseY + 2) ($cx + 34) ($baseY + 2 - $curHFarR) 12.0 255 1.0

        # Left Main Mountain Peak (Like 11. Earth.png)
        Draw-RockSpire $gfx ($cx - 17) ($baseY + 1) ($cx - 18) ($baseY + 1 - $curHLeft) 18.0 255 1.0
        # Right Main Mountain Peak (Like 11. Earth.png)
        Draw-RockSpire $gfx ($cx + 17) ($baseY + 1) ($cx + 18) ($baseY + 1 - $curHRight) 18.0 255 1.0

        # Central Giant Monolith (Tallest sharp apex with white groove lines!)
        Draw-RockSpire $gfx $cx $baseY $cx ($baseY - $curHCenter) 22.0 255 1.0

        # White vertical power groove line in the center spire (Iconic to 11. Earth.png!)
        $pWhiteGroove = New-Object System.Drawing.Pen((Clr 245 255 255 255), 1.6)
        $gfx.DrawLine($pWhiteGroove, $cx, ($baseY - $curHCenter + 6.0), $cx, ($baseY - 4.0))
        $pWhiteGroove.Dispose()

        # 4. Flying Fractured Stone Boulders & Dust Puffs
        $bRockShard = New-Object System.Drawing.SolidBrush((Clr 255 160 85 25))
        $pRockBorder = New-Object System.Drawing.Pen((Clr 255 240 180 80), 1.0)
        
        $shards = @(
            @{ X = -38.0; Y = -22.0; R = 4.0 },
            @{ X = -24.0; Y = -42.0; R = 3.5 },
            @{ X =  25.0; Y = -40.0; R = 3.8 },
            @{ X =  40.0; Y = -24.0; R = 3.2 },
            @{ X =  -8.0; Y = -54.0; R = 2.8 },
            @{ X =  12.0; Y = -52.0; R = 3.0 }
        )

        foreach ($sh in $shards) {
            $sx = $cx + ($sh.X * $eruptT)
            $sy = $baseY + ($sh.Y * $eruptT)
            Fill-EllipseCentered $gfx $bRockShard $sx $sy $sh.R ($sh.R * 0.8)
            Draw-EllipseCentered $gfx $pRockBorder $sx $sy $sh.R ($sh.R * 0.8)
        }
        $bRockShard.Dispose()
        $pRockBorder.Dispose()

        # Dust motes along the base
        $bDustMote = New-Object System.Drawing.SolidBrush((Clr 180 210 150 70))
        for ($d = 0; $d -lt 12; $d++) {
            $dAng = $d * [Math]::PI / 6.0
            $distD = ($shockW * 0.44) + (($d % 3) * 3.0)
            $dx = $cx + [Math]::Cos($dAng) * $distD
            $dy = ($baseY + 4) + [Math]::Sin($dAng) * ($distD * 0.4)
            Fill-EllipseCentered $gfx $bDustMote $dx $dy 2.0 2.0
        }
        $bDustMote.Dispose()
        continue
    }

    # =========================================================================
    # F5..F6: PEAK COLLAPSE & ROCK SHATTERING (암석 파쇄 & 붕괴 분진)
    # =========================================================================
    if ($f -eq 5 -or $f -eq 6) {
        $collapseT = ($f - 4) / 2.0 # 0.5, 1.0
        $curH = 50.0 * (1.0 - $collapseT * 0.45) # 38, 26

        # Expanding heavy dust cloud
        $dustW = 85.0 + ($collapseT * 25.0)
        $dustH = 42.0 + ($collapseT * 12.0)
        $pathDust = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathDust.AddEllipse(($cx - $dustW/2), ($baseY - $dustH/2 + 2), $dustW, $dustH)
        $pgbDust = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathDust)
        $pgbDust.CenterColor = Clr 180 190 120 40
        $pgbDust.SurroundColors = @([System.Drawing.Color](Clr 0 100 50 10))
        $gfx.FillPath($pgbDust, $pathDust)
        $pgbDust.Dispose()
        $pathDust.Dispose()

        # Crumbling rock spires (cracked, fractured, sinking)
        Draw-RockSpire $gfx ($cx - 16) ($baseY + 2) ($cx - 16) ($baseY + 2 - $curH * 0.7) 16.0 220 0.85
        Draw-RockSpire $gfx ($cx + 16) ($baseY + 2) ($cx + 16) ($baseY + 2 - $curH * 0.65) 16.0 220 0.85
        Draw-RockSpire $gfx $cx $baseY $cx ($baseY - $curH) 20.0 230 0.9

        # Massive flying stone chunks bursting outward
        $bChunk = New-Object System.Drawing.SolidBrush((Clr 240 160 80 20))
        for ($c = 0; $c -lt 10; $c++) {
            $cAng = $c * [Math]::PI * 2.0 / 10.0 + 0.3
            $cDist = 32.0 + ($collapseT * 24.0) + (($c % 3) * 5.0)
            $cx_pos = $cx + [Math]::Cos($cAng) * $cDist
            $cy_pos = ($baseY - 15) + [Math]::Sin($cAng) * ($cDist * 0.65)
            Fill-EllipseCentered $gfx $bChunk $cx_pos $cy_pos 3.2 2.6
        }
        $bChunk.Dispose()
        continue
    }

    # =========================================================================
    # F7..F9: DISSOLVING RUBBLE & DUST DISPERSION (침강된 잔해 & 먼지 소멸)
    # =========================================================================
    if ($f -ge 7) {
        $tEnd = ($f - 6) / 3.0 # 0.33, 0.66, 1.0
        $dustAlpha = [int]([Math]::Max(10.0, 180 * (1.0 - $tEnd)))
        $cloudW = 90.0 + ($tEnd * 25.0)
        $cloudH = 34.0 + ($tEnd * 10.0)

        # Dispersing dust veil
        $pathCloud = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathCloud.AddEllipse(($cx - $cloudW/2), ($baseY - $cloudH/2 + 4), $cloudW, $cloudH)
        $pgbCloud = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathCloud)
        $pgbCloud.CenterColor = Clr $dustAlpha 170 100 35
        $pgbCloud.SurroundColors = @([System.Drawing.Color](Clr 0 80 35 10))
        $gfx.FillPath($pgbCloud, $pathCloud)
        $pgbCloud.Dispose()
        $pathCloud.Dispose()

        # Sunk rubble mounds on ground
        if ($f -eq 7 -or $f -eq 8) {
            $bRubble = New-Object System.Drawing.SolidBrush((Clr $dustAlpha 110 55 18))
            Fill-EllipseCentered $gfx $bRubble ($cx - 14) ($baseY + 4) 10.0 4.5
            Fill-EllipseCentered $gfx $bRubble ($cx + 12) ($baseY + 4) 12.0 5.0
            Fill-EllipseCentered $gfx $bRubble $cx ($baseY + 3) 14.0 6.0
            $bRubble.Dispose()
        }

        # Fine dust sparks floating away
        $bSpeck = New-Object System.Drawing.SolidBrush((Clr $dustAlpha 220 160 80))
        for ($s = 0; $s -lt 8; $s++) {
            $sAng = $s * [Math]::PI / 4.0 + ($f * 0.4)
            $sDist = ($cloudW * 0.42) + (($s % 3) * 3.0)
            $sx = $cx + [Math]::Cos($sAng) * $sDist
            $sy = ($baseY + 2) + [Math]::Sin($sAng) * ($sDist * 0.35)
            $gfx.FillEllipse($bSpeck, ($sx - 1.2), ($sy - 1.2), 2.4, 2.4)
        }
        $bSpeck.Dispose()
    }
}

# Save directly to Unity asset
$targetAsset = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Earth_Splash_Quake.png"
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

$bgfx.DrawString("E111: Earth Quake - Rising Earth Spires Animation (10 Frames @ 16 FPS)", $fontTitle, $bWhite, 30, 20)
$bgfx.DrawString("11. Earth.png Emblem Silhouette: Ruptured Earth Base + 5 Rising Monolith Spires + Seismic Shockwave + Boulder Shatter", $fontSub, $bGray, 30, 48)

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

# Row 2: 2.2x Zoom Comparison of Key Eruption Stages
$zoomFrames = @(
    @{ F = 0; Label = "F0: Impact & Fissures" },
    @{ F = 1; Label = "F1: Rapid Ground Uplift" },
    @{ F = 2; Label = "F2: 5 Spires Erupting" },
    @{ F = 3; Label = "F3: Peak Eruption & Shockwave" },
    @{ F = 5; Label = "F5: Peak Shatter & Dust" },
    @{ F = 7; Label = "F7: Sunk Rubble & Dissipate" }
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

$bannerPath = "$artifactDir/preview_earth_rising_spikes.png"
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath "$projectRoot/preview_earth_rising_spikes.png" -Force

$bgfx.Dispose()
$banner.Dispose()
$gfx.Dispose()
$sheet.Dispose()

Write-Host "Earth Rising Spikes Effect generated successfully!"
