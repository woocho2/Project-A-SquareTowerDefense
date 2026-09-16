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

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

# Draw aerodynamic wind blade matching 10. Wind.png
function Draw-WindBlade($gfx, [float]$cx, [float]$cy, [float]$scale, [float]$angle, [float]$alpha) {
    $a = [int]$alpha
    if ($a -le 5) { return }

    $cosA = [Math]::Cos($angle); $sinA = [Math]::Sin($angle)

    # Local blade coordinates
    # Main curved blade (Emerald green #059669 -> #10b981)
    $localMain = @(
        (Pt (0.0 * $scale) (-15.0 * $scale)),
        (Pt (4.5 * $scale) (-11.0 * $scale)),
        (Pt (8.5 * $scale) (-3.0 * $scale)),
        (Pt (9.5 * $scale) (6.0 * $scale)),
        (Pt (5.5 * $scale) (13.0 * $scale)),
        (Pt (0.8 * $scale) (15.0 * $scale)),
        (Pt (2.5 * $scale) (7.0 * $scale)),
        (Pt (3.0 * $scale) (-2.0 * $scale)),
        (Pt (1.2 * $scale) (-9.0 * $scale))
    )

    # Secondary outer feather (Bright mint cyan #34d399 -> #6ee7b7)
    $localFeather = @(
        (Pt (5.5 * $scale) (-13.0 * $scale)),
        (Pt (10.0 * $scale) (-6.0 * $scale)),
        (Pt (12.5 * $scale) (2.0 * $scale)),
        (Pt (9.0 * $scale) (10.0 * $scale)),
        (Pt (8.0 * $scale) (4.5 * $scale)),
        (Pt (6.8 * $scale) (-3.0 * $scale))
    )

    $worldMain = New-Object System.Drawing.PointF[] $localMain.Count
    for ($i = 0; $i -lt $localMain.Count; $i++) {
        $lx = $localMain[$i].X; $ly = $localMain[$i].Y
        $worldMain[$i] = Pt ($cx + ($lx * $cosA - $ly * $sinA)) ($cy + ($lx * $sinA + $ly * $cosA))
    }

    $worldFeather = New-Object System.Drawing.PointF[] $localFeather.Count
    for ($i = 0; $i -lt $localFeather.Count; $i++) {
        $lx = $localFeather[$i].X; $ly = $localFeather[$i].Y
        $worldFeather[$i] = Pt ($cx + ($lx * $cosA - $ly * $sinA)) ($cy + ($lx * $sinA + $ly * $cosA))
    }

    $bMain = New-Object System.Drawing.SolidBrush((Clr $a 16 185 129))
    $gfx.FillPolygon($bMain, $worldMain)
    $bMain.Dispose()

    $featherA = [int]($a * 0.9)
    $bFeather = New-Object System.Drawing.SolidBrush((Clr $featherA 52 211 153))
    $gfx.FillPolygon($bFeather, $worldFeather)
    $bFeather.Dispose()

    # Inner bright spine
    $pSpine = New-Object System.Drawing.Pen((Clr $a 210 255 240), (1.1 * $scale))
    $sx1 = $cx + ($localMain[0].X * $cosA - $localMain[0].Y * $sinA)
    $sy1 = $cy + ($localMain[0].X * $sinA + $localMain[0].Y * $cosA)
    $sx2 = $cx + ($localMain[5].X * $cosA - $localMain[5].Y * $sinA)
    $sy2 = $cy + ($localMain[5].X * $sinA + $localMain[5].Y * $cosA)
    $gfx.DrawLine($pSpine, $sx1, $sy1, $sx2, $sy2)
    $pSpine.Dispose()
}

# 3D Swirling Tornado Funnel
function Draw-TornadoFunnel($gfx, [float]$cx, [float]$yTop, [float]$yBase, [float]$wTop, [float]$wWaist, [float]$wBase, [float]$alpha, [float]$spinPhase) {
    $a = [int]$alpha
    if ($a -le 5) { return }

    $hTotal = $yBase - $yTop
    if ($hTotal -le 5.0) { return }

    # 1. Base Funnel Body Fill (Translucent gradient polygon)
    $numSteps = 16
    $leftPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $rightPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]

    for ($s = 0; $s -le $numSteps; $s++) {
        $t = $s / [float]$numSteps # 0 (top) to 1 (base)
        $y = $yTop + ($t * $hTotal)
        $w = if ($t -le 0.6) {
            $subT = $t / 0.6
            $wTop + ($wWaist - $wTop) * [Math]::Sin($subT * [Math]::PI * 0.5)
        } else {
            $subT = ($t - 0.6) / 0.4
            $wWaist + ($wBase - $wWaist) * [Math]::Sin($subT * [Math]::PI * 0.5)
        }
        $hw = $w * 0.5
        $leftPts.Add((Pt ($cx - $hw) $y))
        $rightPts.Add((Pt ($cx + $hw) $y))
    }

    $polyPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    foreach ($pt in $leftPts) { $polyPts.Add($pt) }
    for ($i = $rightPts.Count - 1; $i -ge 0; $i--) { $polyPts.Add($rightPts[$i]) }

    $bodyPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $bodyPath.AddPolygon($polyPts.ToArray())
    $pgbBody = New-Object System.Drawing.Drawing2D.PathGradientBrush($bodyPath)
    $bodyCoreA = [int]($a * 0.7)
    $pgbBody.CenterColor = Clr $bodyCoreA 45 212 191 # vibrant cyan mint
    $pgbBody.SurroundColors = @([System.Drawing.Color](Clr 0 4 120 87)) # deep emerald fade
    $gfx.FillPath($pgbBody, $bodyPath)
    $pgbBody.Dispose()
    $bodyPath.Dispose()

    # 2. Helical Wind Ribbons (3D Spiral Wind Bands)
    $numStrands = 4
    for ($strand = 0; $strand -lt $numStrands; $strand++) {
        $strandPhase = $spinPhase + ($strand * [Math]::PI * 0.5)
        $strandPoints = New-Object System.Collections.Generic.List[System.Drawing.PointF]
        $strandDepth = New-Object System.Collections.Generic.List[float]

        for ($s = 0; $s -le 24; $s++) {
            $t = $s / 24.0
            $y = $yTop + ($t * $hTotal)
            $w = if ($t -le 0.6) {
                $subT = $t / 0.6
                $wTop + ($wWaist - $wTop) * [Math]::Sin($subT * [Math]::PI * 0.5)
            } else {
                $subT = ($t - 0.6) / 0.4
                $wWaist + ($wBase - $wWaist) * [Math]::Sin($subT * [Math]::PI * 0.5)
            }
            $hw = $w * 0.55

            $angle = $strandPhase + ((1.0 - $t) * 4.2 * [Math]::PI)
            $x = $cx + ($hw * [Math]::Cos($angle))
            $z = [Math]::Sin($angle)

            $strandPoints.Add((Pt $x $y))
            $strandDepth.Add($z)
        }

        for ($i = 0; $i -lt ($strandPoints.Count - 1); $i++) {
            $p1 = $strandPoints[$i]
            $p2 = $strandPoints[$i + 1]
            $avgZ = ($strandDepth[$i] + $strandDepth[$i + 1]) * 0.5

            if ($avgZ -ge -0.15) {
                $frontA = [int]([Math]::Min(255.0, $a * (0.65 + $avgZ * 0.35)))
                $penW = 1.4 + ($avgZ * 1.0)
                $col = if ($avgZ -gt 0.5) { Clr $frontA 225 255 245 } else { Clr $frontA 45 212 191 }
                $pRibbon = New-Object System.Drawing.Pen($col, $penW)
                $gfx.DrawLine($pRibbon, $p1, $p2)
                $pRibbon.Dispose()
            } else {
                $backA = [int]($a * 0.35)
                $pBack = New-Object System.Drawing.Pen((Clr $backA 6 95 70), 1.2)
                $gfx.DrawLine($pBack, $p1, $p2)
                $pBack.Dispose()
            }
        }
    }
}

# Top Storm Canopy (태풍 상단 소용돌이 팔 & 태풍의 눈)
function Draw-TopCanopy($gfx, [float]$cx, [float]$cy, [float]$rx, [float]$ry, [float]$alpha, [float]$spinPhase) {
    $a = [int]$alpha
    if ($a -le 5 -or $rx -le 4.0) { return }

    # 1. Base canopy glow
    $pathCanopy = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pathCanopy.AddEllipse(($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
    $pgbCanopy = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathCanopy)
    $pgbCanopy.CenterColor = Clr ([int]($a * 0.6)) 52 211 153
    $pgbCanopy.SurroundColors = @([System.Drawing.Color](Clr 0 4 120 87))
    $gfx.FillPath($pgbCanopy, $pathCanopy)
    $pgbCanopy.Dispose()
    $pathCanopy.Dispose()

    # 2. 3 Curved Spiral Arms
    for ($arm = 0; $arm -lt 3; $arm++) {
        $armPhase = $spinPhase + ($arm * [Math]::PI * 2.0 / 3.0)
        $armPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
        for ($s = 0; $s -le 16; $s++) {
            $t = $s / 16.0
            $dist = 4.0 + ($t * ($rx - 4.0))
            $ang = $armPhase + ($t * 2.4)
            $ax = $cx + ($dist * [Math]::Cos($ang))
            $ay = $cy + ($dist * ($ry / $rx) * [Math]::Sin($ang))
            $armPts.Add((Pt $ax $ay))
        }
        $pArm = New-Object System.Drawing.Pen((Clr ([int]($a * 0.85)) 167 243 208), 2.0)
        $gfx.DrawCurve($pArm, $armPts.ToArray())
        $pArm.Dispose()
    }

    # 3. Eye of the Storm (순백/민트 백열 링 및 중심 진공)
    $eyeRx = [Math]::Max(2.5, $rx * 0.22)
    $eyeRy = [Math]::Max(1.5, $ry * 0.22)
    $bEyeRing = New-Object System.Drawing.SolidBrush((Clr $a 255 255 255))
    Fill-EllipseCentered $gfx $bEyeRing $cx $cy $eyeRx $eyeRy
    $bEyeRing.Dispose()

    $bEyeHole = New-Object System.Drawing.SolidBrush((Clr ([int]($a * 0.9)) 6 78 59))
    Fill-EllipseCentered $gfx $bEyeHole $cx $cy ($eyeRx * 0.55) ($eyeRy * 0.55)
    $bEyeHole.Dispose()
}

# Ground Gale Ring & Shockwave (지면 회전 와류 링 및 충격파)
function Draw-GroundGaleRing($gfx, [float]$cx, [float]$cy, [float]$rx, [float]$ry, [float]$alpha, [float]$spinPhase, [float]$shockwaveRx = 0.0) {
    $a = [int]$alpha
    if ($a -le 5) { return }

    # 1. Rotating ground vortex ring
    $pRing = New-Object System.Drawing.Pen((Clr ([int]($a * 0.8)) 52 211 153), 1.6)
    Draw-EllipseCentered $gfx $pRing $cx $cy $rx $ry
    $pRing.Dispose()

    # Dust/Wind streaks orbiting around
    for ($k = 0; $k -lt 6; $k++) {
        $ang = $spinPhase + ($k * [Math]::PI / 3.0)
        $sx = $cx + ($rx * [Math]::Cos($ang))
        $sy = $cy + ($ry * [Math]::Sin($ang))
        $bStreak = New-Object System.Drawing.SolidBrush((Clr $a 200 255 240))
        Fill-EllipseCentered $gfx $bStreak $sx $sy 2.0 1.2
        $bStreak.Dispose()
    }

    # 2. Expanding shockwave ring
    if ($shockwaveRx -gt 0.0) {
        $swRy = $shockwaveRx * ($ry / $rx)
        $pSw = New-Object System.Drawing.Pen((Clr ([int]($a * 0.8)) 110 231 183), 2.2)
        Draw-EllipseCentered $gfx $pSw $cx $cy $shockwaveRx $swRy
        $pSw.Dispose()

        # Wind dart particles on the shockwave
        for ($k = 0; $k -lt 8; $k++) {
            $ang = ($k * [Math]::PI / 4.0) + ($spinPhase * 0.5)
            $px = $cx + ($shockwaveRx * [Math]::Cos($ang))
            $py = $cy + ($swRy * [Math]::Sin($ang))
            $bDart = New-Object System.Drawing.SolidBrush((Clr $a 255 255 255))
            Fill-EllipseCentered $gfx $bDart $px $py 2.2 1.4
            $bDart.Dispose()
        }
    }
}

$sheetW = 1280
$sheetH = 128
$sheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH)
$gfx = [System.Drawing.Graphics]::FromImage($sheet)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gfx.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$groundY = 102.0

for ($f = 0; $f -lt 10; $f++) {
    $cx = ($f * 128.0) + 64.0

    # =========================================================================
    # F0: CYCLONE INCEPTION (기류 응집 & 회오리 발아)
    # =========================================================================
    if ($f -eq 0) {
        $alpha = 200.0
        Draw-GroundGaleRing $gfx $cx $groundY 20.0 6.5 $alpha 0.0 0.0
        Draw-TornadoFunnel $gfx $cx 72.0 $groundY 28.0 18.0 16.0 $alpha 0.0
        Draw-TopCanopy $gfx $cx 72.0 14.0 5.5 $alpha 0.0

        # In-rushing wind streaks from outer ground
        for ($s = 0; $s -lt 5; $s++) {
            $ang = $s * [Math]::PI * 2.0 / 5.0
            $x1 = $cx + (36.0 * [Math]::Cos($ang))
            $y1 = $groundY + (12.0 * [Math]::Sin($ang))
            $x2 = $cx + (18.0 * [Math]::Cos($ang + 0.3))
            $y2 = $groundY + (6.0 * [Math]::Sin($ang + 0.3))
            $pIn = New-Object System.Drawing.Pen((Clr 160 52 211 153), 1.2)
            $gfx.DrawLine($pIn, $x1, $y1, $x2, $y2)
            $pIn.Dispose()
        }
        continue
    }

    # =========================================================================
    # F1: RAPID UPDRAFT (급속 융기 & 소용돌이 신장)
    # =========================================================================
    if ($f -eq 1) {
        $alpha = 240.0
        Draw-GroundGaleRing $gfx $cx $groundY 30.0 9.0 $alpha 1.6 0.0
        Draw-TornadoFunnel $gfx $cx 40.0 $groundY 48.0 20.0 22.0 $alpha 1.6
        Draw-TopCanopy $gfx $cx 40.0 24.0 9.0 $alpha 1.6

        # 2 Orbiting Wind Blades
        Draw-WindBlade $gfx ($cx - 16.0) 65.0 0.80 -0.5 $alpha
        Draw-WindBlade $gfx ($cx + 15.0) 76.0 0.75 2.2 $alpha
        continue
    }

    # =========================================================================
    # F2: FULL TYPHOON (태풍 회오리 완성 & 고속 회전)
    # =========================================================================
    if ($f -eq 2) {
        $alpha = 255.0
        Draw-GroundGaleRing $gfx $cx $groundY 38.0 11.0 $alpha 3.4 0.0
        Draw-TornadoFunnel $gfx $cx 22.0 $groundY 64.0 24.0 26.0 $alpha 3.4
        Draw-TopCanopy $gfx $cx 22.0 32.0 12.0 $alpha 3.4

        # 3 Orbiting Wind Blades at varying heights
        Draw-WindBlade $gfx ($cx + 25.0) 38.0 0.95 1.2 $alpha
        Draw-WindBlade $gfx ($cx - 20.0) 62.0 0.88 -1.8 $alpha
        Draw-WindBlade $gfx ($cx + 18.0) 84.0 0.80 3.0 $alpha

        # Bloom aura around the upper funnel
        $pathBloom = New-Object System.Drawing.Drawing2D.GraphicsPath
        $pathBloom.AddEllipse(($cx - 40), 10, 80, 50)
        $pgbBloom = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathBloom)
        $pgbBloom.CenterColor = Clr 120 200 255 240
        $pgbBloom.SurroundColors = @([System.Drawing.Color](Clr 0 16 185 129))
        $gfx.FillPath($pgbBloom, $pathBloom)
        $pgbBloom.Dispose()
        $pathBloom.Dispose()
        continue
    }

    # =========================================================================
    # F3: PEAK STORM INTENSITY & GALE SHOCKWAVE (정점 폭풍 & 외곽 충격파 방출)
    # =========================================================================
    if ($f -eq 3) {
        $alpha = 255.0
        # Expanding shockwave ring at 52px
        Draw-GroundGaleRing $gfx $cx $groundY 44.0 12.5 $alpha 5.4 52.0
        Draw-TornadoFunnel $gfx $cx 20.0 $groundY 72.0 28.0 30.0 $alpha 5.4
        Draw-TopCanopy $gfx $cx 20.0 36.0 13.0 $alpha 5.4

        # 4 Furious Wind Blades
        Draw-WindBlade $gfx ($cx - 30.0) 32.0 1.05 -0.8 $alpha
        Draw-WindBlade $gfx ($cx + 32.0) 45.0 1.00 1.9 $alpha
        Draw-WindBlade $gfx ($cx - 24.0) 70.0 0.92 3.6 $alpha
        Draw-WindBlade $gfx ($cx + 22.0) 88.0 0.85 -2.2 $alpha

        # Centrifugal Wind Wisps slinging outwards
        for ($w = 0; $w -lt 6; $w++) {
            $ang = $w * [Math]::PI / 3.0 + 0.4
            $wx = $cx + (48.0 * [Math]::Cos($ang))
            $wy = 45.0 + (22.0 * [Math]::Sin($ang))
            $bWisp = New-Object System.Drawing.SolidBrush((Clr 200 255 255 255))
            Fill-EllipseCentered $gfx $bWisp $wx $wy 2.4 1.4
            $bWisp.Dispose()
        }
        continue
    }

    # =========================================================================
    # F4: CENTRIFUGAL DISCHARGE & FUNNEL EXPANSION (원심력 팽창 & 칼날 비산)
    # =========================================================================
    if ($f -eq 4) {
        $alpha = 220.0
        # Shockwave expands to 60px
        Draw-GroundGaleRing $gfx $cx $groundY 46.0 13.0 $alpha 7.2 60.0
        # Funnel widens and thins out
        Draw-TornadoFunnel $gfx $cx 26.0 $groundY 76.0 36.0 36.0 $alpha 7.2
        Draw-TopCanopy $gfx $cx 26.0 38.0 13.5 $alpha 7.2

        # Wind blades flung outward to 42~48px
        Draw-WindBlade $gfx ($cx - 42.0) 32.0 0.90 -1.2 190
        Draw-WindBlade $gfx ($cx + 44.0) 48.0 0.85 2.4 190
        Draw-WindBlade $gfx ($cx - 36.0) 76.0 0.80 4.0 180
        Draw-WindBlade $gfx ($cx + 34.0) 90.0 0.75 -1.8 170
        continue
    }

    # =========================================================================
    # F5: VORTEX DECONSTRUCTION (소용돌이 붕괴 & 분산 링)
    # =========================================================================
    if ($f -eq 5) {
        $alpha = 165.0
        # 3 Detached horizontal swirling rings
        $pRing1 = New-Object System.Drawing.Pen((Clr $alpha 52 211 153), 2.0)
        Draw-EllipseCentered $gfx $pRing1 $cx 36.0 34.0 11.0
        Draw-EllipseCentered $gfx $pRing1 $cx 66.0 26.0 8.5
        Draw-EllipseCentered $gfx $pRing1 $cx 96.0 32.0 9.5
        $pRing1.Dispose()

        # Expanding ground shockwave fading
        $pSw = New-Object System.Drawing.Pen((Clr 120 110 231 183), 1.8)
        Draw-EllipseCentered $gfx $pSw $cx $groundY 62.0 16.0
        $pSw.Dispose()

        # Dispersing wind blade shards
        for ($s = 0; $s -lt 6; $s++) {
            $ang = $s * [Math]::PI / 3.0 + 0.2
            $sx = $cx + (46.0 * [Math]::Cos($ang))
            $sy = 65.0 + (30.0 * [Math]::Sin($ang))
            $bShard = New-Object System.Drawing.SolidBrush((Clr $alpha 167 243 208))
            Fill-EllipseCentered $gfx $bShard $sx $sy 2.6 1.6
            $bShard.Dispose()
        }
        continue
    }

    # =========================================================================
    # F6: CALMING SWIRLS (기류 분산 & 잔류 회전)
    # =========================================================================
    if ($f -eq 6) {
        $alpha = 115.0
        $pRing = New-Object System.Drawing.Pen((Clr $alpha 45 212 191), 1.4)
        Draw-EllipseCentered $gfx $pRing $cx 48.0 28.0 9.0
        Draw-EllipseCentered $gfx $pRing $cx 82.0 30.0 9.0
        $pRing.Dispose()

        # Floating mint glitter motes
        for ($s = 0; $s -lt 8; $s++) {
            $ang = $s * [Math]::PI / 4.0 + 0.5
            $sx = $cx + (38.0 * [Math]::Cos($ang))
            $sy = 65.0 + (22.0 * [Math]::Sin($ang))
            $bMote = New-Object System.Drawing.SolidBrush((Clr $alpha 200 255 240))
            Fill-EllipseCentered $gfx $bMote $sx $sy 1.8 1.2
            $bMote.Dispose()
        }
        continue
    }

    # =========================================================================
    # F7: FADING GUSTS (잔류 미풍)
    # =========================================================================
    if ($f -eq 7) {
        $alpha = 70.0
        $pRing = New-Object System.Drawing.Pen((Clr $alpha 52 211 153), 1.1)
        Draw-EllipseCentered $gfx $pRing $cx 68.0 24.0 7.5
        $pRing.Dispose()

        for ($s = 0; $s -lt 5; $s++) {
            $ang = $s * [Math]::PI * 2.0 / 5.0 + 0.8
            $sx = $cx + (28.0 * [Math]::Cos($ang))
            $sy = 68.0 + (14.0 * [Math]::Sin($ang))
            $bMote = New-Object System.Drawing.SolidBrush((Clr $alpha 220 255 245))
            Fill-EllipseCentered $gfx $bMote $sx $sy 1.4 1.0
            $bMote.Dispose()
        }
        continue
    }

    # =========================================================================
    # F8: DISSOLVING MOTES (글리터 소멸)
    # =========================================================================
    if ($f -eq 8) {
        $alpha = 30.0
        for ($s = 0; $s -lt 3; $s++) {
            $sx = $cx + (($s * 20) - 20)
            $sy = 68.0 + (($s % 2) * 6 - 3)
            $bMote = New-Object System.Drawing.SolidBrush((Clr $alpha 220 255 245))
            Fill-EllipseCentered $gfx $bMote $sx $sy 1.2 1.2
            $bMote.Dispose()
        }
        continue
    }

    # F9: Clear / completely transparent for clean loop termination
}

# Save spritesheet to Unity target asset
$targetAsset = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Wind_Splash_Typhoon.png"
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

$bgfx.DrawString("E110: Cyan Typhoon Whirlwind - Emerald Vortex & Spinning Wind Blades (10 Frames @ 16 FPS)", $fontTitle, $bWhite, 30, 20)
$bgfx.DrawString("Ground Cyclone Inception + 3D Helical Spiral Funnel + Wind Emblem Blades + Gale Shockwave & Dispersal", $fontSub, $bGray, 30, 48)

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
    @{ F = 0; Label = "F0: Cyclone Inception" },
    @{ F = 1; Label = "F1: Rapid Updraft & Funnel" },
    @{ F = 2; Label = "F2: Full Typhoon Whirlwind" },
    @{ F = 3; Label = "F3: Peak Storm & Shockwave" },
    @{ F = 4; Label = "F4: Centrifugal Blade Slingshot" },
    @{ F = 5; Label = "F5: Vortex Deconstruction" }
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

$bannerPath = "$artifactDir/preview_cyan_typhoon_whirlwind.png"
$banner.Save($bannerPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath "$projectRoot/preview_cyan_typhoon_whirlwind.png" -Force

$bgfx.Dispose()
$banner.Dispose()
$gfx.Dispose()
$sheet.Dispose()

Write-Host "Cyan Typhoon Whirlwind Effect generated successfully!"
