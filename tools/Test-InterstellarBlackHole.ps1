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

# Helper to draw jagged electric lightning
function Draw-Lightning($gfx, [float]$x1, [float]$y1, [float]$x2, [float]$y2, [float]$jitter, [int]$segments, $penGlow, $penCore) {
    $pts = @()
    $pts += New-Object System.Drawing.PointF($x1, $y1)
    
    $dx = ($x2 - $x1) / [float]$segments
    $dy = ($y2 - $y1) / [float]$segments
    $dist = [Math]::Sqrt(($x2 - $x1)*($x2 - $x1) + ($y2 - $y1)*($y2 - $y1))
    if ($dist -lt 1.0) { return }
    $nx = -($y2 - $y1) / $dist
    $ny = ($x2 - $x1) / $dist

    for ($i = 1; $i -lt $segments; $i++) {
        $midX = $x1 + $dx * $i
        $midY = $y1 + $dy * $i
        $offset = (([Math]::Sin($i * 4.3 + $dist) * 0.6) + ([Math]::Cos($i * 7.1) * 0.4)) * $jitter
        $pts += New-Object System.Drawing.PointF([float]($midX + $nx * $offset), [float]($midY + $ny * $offset))
    }
    $pts += New-Object System.Drawing.PointF($x2, $y2)

    $ptsArray = [System.Drawing.PointF[]]$pts
    if ($penGlow) { $gfx.DrawLines($penGlow, $ptsArray) }
    if ($penCore) { $gfx.DrawLines($penCore, $ptsArray) }
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
    $cy = 64.0

    # =========================================================================
    # FRAME 0: Spark & Gravitational Ignition (방전 & 블랙홀 태동)
    # =========================================================================
    if ($f -eq 0) {
        # Cosmic purple nebula haze
        $pathHaze = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathHaze.AddEllipse(($cx - 42), ($cy - 32), 84, 64)
        $pgbHaze = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathHaze)
        $pgbHaze.CenterColor = Clr 170 110 25 190
        $pgbHaze.SurroundColors = @([System.Drawing.Color](Clr 0 40 5 80))
        $gfx.FillPath($pgbHaze, $pathHaze)
        $pgbHaze.Dispose()
        $pathHaze.Dispose()

        # Radial lightning crackles from center
        $pGlow = New-Object System.Drawing.Pen((Clr 190 190 80 255), 2.2)
        $pCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.0)
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0 + 0.2
            $lx = $cx + [Math]::Cos($ang) * 34.0
            $ly = $cy + [Math]::Sin($ang) * 26.0
            Draw-Lightning $gfx $cx $cy $lx $ly 4.5 4 $pGlow $pCore
        }
        $pGlow.Dispose()
        $pCore.Dispose()

        # Small event horizon sphere
        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 3 1 6))
        Fill-EllipseCentered $gfx $bVoid $cx $cy 10.0 10.0
        $bVoid.Dispose()

        # Incandescent white photon ring
        $pRing = New-Object System.Drawing.Pen((Clr 255 255 255 255), 2.0)
        Draw-EllipseCentered $gfx $pRing $cx $cy 10.0 10.0
        $pRing.Dispose()

        # Infalling black dust motes
        $bDust = New-Object System.Drawing.SolidBrush((Clr 220 15 5 25))
        for ($d = 0; $d -lt 14; $d++) {
            $dAng = $d * [Math]::PI * 2.0 / 14.0 + 0.1
            $dx = $cx + [Math]::Cos($dAng) * 24.0
            $dy = $cy + [Math]::Sin($dAng) * 20.0
            Fill-EllipseCentered $gfx $bDust $dx $dy 1.6 1.6
        }
        $bDust.Dispose()
        continue
    }

    # =========================================================================
    # FRAMES 1..5: GARGANTUA INTERSTELLAR BLACK HOLE (메인 활성 & 흡입)
    # =========================================================================
    if ($f -ge 1 -and $f -le 5) {
        $tActive = ($f - 1) / 4.0 # 0.0, 0.25, 0.5, 0.75, 1.0

        # Core radius: 17px at peak (diameter 34px)
        $rCore = switch ($f) {
            1 { 14.5 }
            2 { 17.5 }
            3 { 18.0 }
            4 { 16.0 }
            5 { 12.5 }
        }

        # Tilt angle of the accretion plane (approx -10 to -14 degrees, like user video)
        $tiltDeg = -11.0
        $tiltRad = $tiltDeg * [Math]::PI / 180.0

        # Accretion disk full span
        $diskSpan = switch ($f) {
            1 { 90.0 }
            2 { 104.0 }
            3 { 106.0 }
            4 { 92.0 }
            5 { 72.0 }
        }

        # -------------------------------------------------------------
        # 1. Background Cosmic Nebula Haze (배경 자색 우주 가스)
        # -------------------------------------------------------------
        $pathNebula = New-Object System.Drawing.Drawing2D.GraphicsPath
        $nebW = $diskSpan * 1.15
        $nebH = $rCore * 3.6
        $pathNebula.AddEllipse(($cx - $nebW/2), ($cy - $nebH/2), $nebW, $nebH)
        $pgbNebula = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathNebula)
        $pgbNebula.CenterColor = Clr 120 120 20 180
        $pgbNebula.SurroundColors = @([System.Drawing.Color](Clr 0 35 5 70))
        $gfx.FillPath($pgbNebula, $pathNebula)
        $pgbNebula.Dispose()
        $pathNebula.Dispose()

        # -------------------------------------------------------------
        # 2. BEHIND-CORE: Gravitational Lensing Dome (후면 상단/하단 굴절광)
        # Bends over the top and beneath the bottom of the black hole
        # -------------------------------------------------------------
        $stateLens = $gfx.Save()
        $gfx.TranslateTransform($cx, $cy)
        $gfx.RotateTransform($tiltDeg)

        # Upper Lensing Dome (curves up over the top of the black hole)
        $domeRx = $rCore * 1.35
        $domeRy = $rCore * 1.45

        # Thick glowing gas layers
        $pDomeGlow = New-Object System.Drawing.Pen((Clr 140 160 40 240), 7.0)
        $pDomeMid  = New-Object System.Drawing.Pen((Clr 210 220 150 255), 3.5)
        $pDomeCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.4)

        # Draw upper curve (approx 175 deg to 365 deg in rotated frame)
        $gfx.DrawArc($pDomeGlow, -$domeRx, -$domeRy, ($domeRx * 2.0), ($domeRy * 2.0), 185, 170)
        $gfx.DrawArc($pDomeMid,  -$domeRx, -$domeRy, ($domeRx * 2.0), ($domeRy * 2.0), 185, 170)
        $gfx.DrawArc($pDomeCore, -$domeRx, -$domeRy, ($domeRx * 2.0), ($domeRy * 2.0), 185, 170)

        # Lower Lensing Arc (curves down under the black hole)
        $gfx.DrawArc($pDomeGlow, (-$domeRx * 0.95), (-$domeRy * 0.75), ($domeRx * 1.9), ($domeRy * 1.5), 10, 160)
        $gfx.DrawArc($pDomeMid,  (-$domeRx * 0.95), (-$domeRy * 0.75), ($domeRx * 1.9), ($domeRy * 1.5), 10, 160)
        $gfx.DrawArc($pDomeCore, (-$domeRx * 0.95), (-$domeRy * 0.75), ($domeRx * 1.9), ($domeRy * 1.5), 10, 160)

        $pDomeGlow.Dispose()
        $pDomeMid.Dispose()
        $pDomeCore.Dispose()

        $gfx.Restore($stateLens)

        # -------------------------------------------------------------
        # 3. CENTRAL SPHERICAL EVENT HORIZON (중심 칠흑의 사건의 지평선 구체)
        # Drawn over the rear dome, creating the iconic Interstellar occultation!
        # -------------------------------------------------------------
        $bEventHorizon = New-Object System.Drawing.SolidBrush((Clr 255 2 1 4))
        Fill-EllipseCentered $gfx $bEventHorizon $cx $cy $rCore $rCore
        $bEventHorizon.Dispose()

        # Razor-sharp incandescent photon ring
        $pPhotonGlow = New-Object System.Drawing.Pen((Clr 200 210 120 255), 2.2)
        $pPhotonCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.2)
        Draw-EllipseCentered $gfx $pPhotonGlow $cx $cy $rCore $rCore
        Draw-EllipseCentered $gfx $pPhotonCore $cx $cy $rCore $rCore
        $pPhotonGlow.Dispose()
        $pPhotonCore.Dispose()

        # -------------------------------------------------------------
        # 4. IN-FRONT: Horizontal Accretion Ribbon (전면 수평 강착 원반)
        # Slices across the front of the black hole equator!
        # -------------------------------------------------------------
        $stateFront = $gfx.Save()
        $gfx.TranslateTransform($cx, $cy)
        $gfx.RotateTransform($tiltDeg)

        $halfW = $diskSpan / 2.0
        $ribbonH = 8.0 # Flattened front ribbon

        # Concentric glowing ribbon lanes
        $pFrontGlow = New-Object System.Drawing.Pen((Clr 160 170 45 245), 6.5)
        $pFrontMid  = New-Object System.Drawing.Pen((Clr 230 235 170 255), 3.0)
        $pFrontCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.4)

        # Draw full horizontal ellipse (the bottom/front half is prominent)
        $gfx.DrawEllipse($pFrontGlow, -$halfW, (-$ribbonH / 2.0), $diskSpan, $ribbonH)
        $gfx.DrawEllipse($pFrontMid,  -$halfW, (-$ribbonH / 2.0), $diskSpan, $ribbonH)
        $gfx.DrawEllipse($pFrontCore, -$halfW, (-$ribbonH / 2.0), $diskSpan, $ribbonH)

        # Secondary outer delicate ring
        $pOuterThin = New-Object System.Drawing.Pen((Clr 140 190 110 250), 1.2)
        $gfx.DrawEllipse($pOuterThin, (-$halfW * 0.92), (-$ribbonH * 0.8), ($diskSpan * 0.92), ($ribbonH * 1.6))
        $pOuterThin.Dispose()

        $pFrontGlow.Dispose()
        $pFrontMid.Dispose()
        $pFrontCore.Dispose()

        $gfx.Restore($stateFront)

        # -------------------------------------------------------------
        # 5. 3 ORBITING DARK MATTER SPHERES (영상 참조: 3개의 공전 구체)
        # Orb 1: Upper-left (~10 o'clock)
        # Orb 2: Upper-right (~2 o'clock)
        # Orb 3: Lower-right (~4:30 o'clock)
        # -------------------------------------------------------------
        $orbitProgress = $tActive * 0.70

        $orbs = @(
            @{ BaseX = -38.0; BaseY = -18.0; R = 6.2; Inward = 9.0;  HasLightning = $true },
            @{ BaseX =  41.0; BaseY = -16.0; R = 5.0; Inward = 8.0;  HasLightning = $true },
            @{ BaseX =  28.0; BaseY =  25.0; R = 5.8; Inward = 7.0;  HasLightning = ($f -eq 2 -or $f -eq 3) }
        )

        foreach ($orb in $orbs) {
            $dist0 = [Math]::Sqrt($orb.BaseX * $orb.BaseX + $orb.BaseY * $orb.BaseY)
            $ang0  = [Math]::Atan2($orb.BaseY, $orb.BaseX) + $orbitProgress
            $curDist = [Math]::Max(($rCore + $orb.R + 2.5), ($dist0 - ($tActive * $orb.Inward)))

            # Tilted orbit coordinates
            $ox = $cx + [Math]::Cos($ang0) * $curDist
            $oy = $cy + ([Math]::Sin($ang0) * $curDist * 0.70)

            # Lightning Arc to Black Hole
            if ($orb.HasLightning) {
                $pLgGlow = New-Object System.Drawing.Pen((Clr 200 200 90 255), 2.4)
                $pLgCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.1)

                $hx = $cx + [Math]::Cos($ang0) * $rCore
                $hy = $cy + [Math]::Sin($ang0) * $rCore * 0.8
                Draw-Lightning $gfx $hx $hy $ox $oy (3.5 + $f * 0.4) 4 $pLgGlow $pLgCore
                $pLgGlow.Dispose()
                $pLgCore.Dispose()
            }

            # Purple Corona Glow around Orb
            $bCorona = New-Object System.Drawing.SolidBrush((Clr 210 180 80 255))
            Fill-EllipseCentered $gfx $bCorona $ox $oy ($orb.R + 2.0) ($orb.R + 2.0)
            $bCorona.Dispose()

            # Pitch-Black Orb Body
            $bOrbVoid = New-Object System.Drawing.SolidBrush((Clr 255 4 1 8))
            Fill-EllipseCentered $gfx $bOrbVoid $ox $oy $orb.R $orb.R
            $bOrbVoid.Dispose()

            # Bright Crescent Rim facing Black Hole
            $pRim = New-Object System.Drawing.Pen((Clr 250 255 240 255), 1.2)
            $dirToCenter = [Math]::Atan2(($cy - $oy), ($cx - $ox))
            $rimStartDeg = ($dirToCenter * 180.0 / [Math]::PI) - 75.0
            $gfx.DrawArc($pRim, ($ox - $orb.R), ($oy - $orb.R), ($orb.R * 2.0), ($orb.R * 2.0), [float]$rimStartDeg, 150.0)
            $pRim.Dispose()
        }

        # -------------------------------------------------------------
        # 6. INFALLING ACCRETION PARTICLES & VOID DUST (물질 흡입 잔해)
        # -------------------------------------------------------------
        $bInfallSpark = New-Object System.Drawing.SolidBrush((Clr 240 255 220 255))
        $bDarkMote    = New-Object System.Drawing.SolidBrush((Clr 255 12 2 18))
        for ($p = 0; $p -lt 16; $p++) {
            $pAng = ($p * [Math]::PI * 2.0 / 16.0) + ($tActive * 1.8)
            $pR = ($diskSpan * 0.46) - ($tActive * 16.0) + (($p % 3) * 3.5)

            # Projected to tilted accretion plane
            $ux = $pR * [Math]::Cos($pAng)
            $uy = $pR * [Math]::Sin($pAng) * 0.26
            $rotX = $ux * [Math]::Cos($tiltRad) - $uy * [Math]::Sin($tiltRad)
            $rotY = $ux * [Math]::Sin($tiltRad) + $uy * [Math]::Cos($tiltRad)

            $px = $cx + $rotX
            $py = $cy + $rotY

            Fill-EllipseCentered $gfx $bInfallSpark $px $py 1.3 1.3
            Fill-EllipseCentered $gfx $bDarkMote ($px + 3.0) ($py + 1.5) 1.6 1.6
        }
        $bInfallSpark.Dispose()
        $bDarkMote.Dispose()
    }

    # =========================================================================
    # FRAME 6: CRITICAL SINGULARITY COMPRESSION & FLASH (특이점 극한 압축 섬광)
    # =========================================================================
    if ($f -eq 6) {
        # Compressed intense violet cosmic flare
        $bFlare = New-Object System.Drawing.SolidBrush((Clr 160 180 60 255))
        Fill-EllipseCentered $gfx $bFlare $cx $cy 28.0 16.0
        $bFlare.Dispose()

        # Compressed event horizon dot
        $bSing = New-Object System.Drawing.SolidBrush((Clr 255 2 0 4))
        Fill-EllipseCentered $gfx $bSing $cx $cy 6.5 6.5
        $bSing.Dispose()

        # Blinding photon explosion ring
        $pSingRing = New-Object System.Drawing.Pen((Clr 255 255 255 255), 2.2)
        Draw-EllipseCentered $gfx $pSingRing $cx $cy 6.5 6.5
        $pSingRing.Dispose()

        # 4-way radiant cross beams
        $pBeam = New-Object System.Drawing.Pen((Clr 240 240 200 255), 1.8)
        $gfx.DrawLine($pBeam, ($cx - 36.0), $cy, ($cx + 36.0), $cy)
        $gfx.DrawLine($pBeam, $cx, ($cy - 24.0), $cx, ($cy + 24.0))
        $pBeam.Dispose()

        # Orbiting spheres collapsed into shattered micro-shards
        $bShard = New-Object System.Drawing.SolidBrush((Clr 240 220 120 255))
        for ($s = 0; $s -lt 6; $s++) {
            $sAng = $s * [Math]::PI / 3.0 + 0.3
            $sx = $cx + [Math]::Cos($sAng) * 18.0
            $sy = $cy + [Math]::Sin($sAng) * 11.0
            Fill-EllipseCentered $gfx $bShard $sx $sy 1.8 1.8
        }
        $bShard.Dispose()
    }

    # =========================================================================
    # FRAMES 7..9: GRAVITATIONAL DOME SHOCKWAVE & DISSOLVE (참조 영상 F04 우주 돔 충격파)
    # =========================================================================
    if ($f -ge 7) {
        $tFade = ($f - 6) / 3.5
        $domeAlpha = [int]([Math]::Max(10.0, 190 * (1.0 - $tFade)))
        $domeW = 60.0 + ($tFade * 48.0)
        $domeH = 34.0 + ($tFade * 26.0)

        # Cosmic purple shockwave dome (Matches 00:04 of user video)
        $pathDome = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathDome.AddEllipse(($cx - $domeW/2), ($cy - $domeH/2), $domeW, $domeH)
        $pgbDome = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathDome)
        $pgbDome.CenterColor = Clr $domeAlpha 140 40 210
        $pgbDome.SurroundColors = @([System.Drawing.Color](Clr 0 50 10 90))
        $gfx.FillPath($pgbDome, $pathDome)
        $pgbDome.Dispose()
        $pathDome.Dispose()

        # Expanding gravitational wave boundary ring
        $pDomeRing = New-Object System.Drawing.Pen((Clr $domeAlpha 220 140 255), (2.0 * (1.0 - $tFade * 0.5)))
        $gfx.DrawEllipse($pDomeRing, [float]($cx - $domeW/2), [float]($cy - $domeH/2), [float]$domeW, [float]$domeH)
        $pDomeRing.Dispose()

        # Residual central purple orb fading away
        $coreFadeAlpha = [int]([Math]::Max(0.0, $domeAlpha * 0.7))
        $bFadeCore = New-Object System.Drawing.SolidBrush((Clr $coreFadeAlpha 80 15 120))
        Fill-EllipseCentered $gfx $bFadeCore $cx $cy (14.0 * (1.0 - $tFade * 0.5)) (14.0 * (1.0 - $tFade * 0.5))
        $bFadeCore.Dispose()

        # Fading orbiting planet silhouettes (shadows of the consumed orbs)
        $bShadowOrb = New-Object System.Drawing.SolidBrush((Clr $coreFadeAlpha 20 5 30))
        Fill-EllipseCentered $gfx $bShadowOrb ($cx - $domeW * 0.35) ($cy - $domeH * 0.3) 4.0 4.0
        Fill-EllipseCentered $gfx $bShadowOrb ($cx + $domeW * 0.38) ($cy - $domeH * 0.25) 3.5 3.5
        Fill-EllipseCentered $gfx $bShadowOrb ($cx + $domeW * 0.26) ($cy + $domeH * 0.32) 3.8 3.8
        $bShadowOrb.Dispose()
    }
}

# Save spritesheet directly to asset
$targetAsset = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Dark_Splash_BlackHole.png"
$sheet.Save($targetAsset, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Updated Target Asset: $targetAsset"

# Generate Comparison Showcase Banner (1200x560)
$bannerW = 1200
$bannerH = 560
$banner = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$bgfx = [System.Drawing.Graphics]::FromImage($banner)
$bgfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$bgfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 16 18 24))
$bgfx.FillRectangle($bBg, 0, 0, $bannerW, $bannerH)
$bBg.Dispose()

$fontTitle = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$fontSub = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))

$bgfx.DrawString("E113: Interstellar Gargantua Black Hole - Video Reference Rebuild (10 Frames @ 16 FPS)", $fontTitle, $bWhite, 30, 20)
$bgfx.DrawString("Video Reference: Gravitational Lensing Top/Bottom Arcs + Horizontal Accretion Disk + 3 Orbiting Orbs + Lightning", $fontSub, $bGray, 30, 48)

# Row 1: All 10 Frames (1x scale, 96x96 each with border)
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
    @{ F = 0; Label = "F0: Discharge & Horizon Birth" },
    @{ F = 2; Label = "F2: Gravitational Lensing & Orbs" },
    @{ F = 3; Label = "F3: Peak Tidal Accretion" },
    @{ F = 4; Label = "F4: Core Infall & Disk Compression" },
    @{ F = 6; Label = "F6: Singularity Flash" },
    @{ F = 7; Label = "F7: Space-Time Dome Shockwave" }
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

# Save showcase banner
$bannerPath = "$artifactDir/preview_interstellar_blackhole.png"
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath "$projectRoot/preview_interstellar_blackhole.png" -Force

$bgfx.Dispose()
$banner.Dispose()
$gfx.Dispose()
$sheet.Dispose()

Write-Host "Interstellar Black Hole Generated Successfully!"
