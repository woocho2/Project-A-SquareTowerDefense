Add-Type -AssemblyName System.Drawing

$width = 1280
$height = 128
$bmp = New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.Clear([System.Drawing.Color]::Transparent)

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }
function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $a = [int]([Math]::Min(255, [Math]::Max(0, $a)))
    $r = [int]([Math]::Min(255, [Math]::Max(0, $r)))
    $g = [int]([Math]::Min(255, [Math]::Max(0, $g)))
    $b = [int]([Math]::Min(255, [Math]::Max(0, $b)))
    [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2), ($ry * 2))
}
function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2), ($ry * 2))
}

# Darius War Axe Model
function Draw-ChopAxe($gfx, [float]$collarX, [float]$collarY, [float]$angle, [float]$scale, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $state = $gfx.Save()
    $gfx.TranslateTransform($collarX, $collarY)
    $gfx.RotateTransform($angle)

    $bShaft = New-Object System.Drawing.SolidBrush((Clr $a 45 28 22))
    $bIronDark = New-Object System.Drawing.SolidBrush((Clr $a 35 40 45))
    $bIronMid = New-Object System.Drawing.SolidBrush((Clr $a 75 85 95))
    $bGold = New-Object System.Drawing.SolidBrush((Clr $a 255 179 0))
    $bCrimson = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt (-46 * $scale) 0), (Pt 0 0),
        (Clr $a 225 35 35), (Clr $a 110 0 0)
    )
    $pDark = New-Object System.Drawing.Pen((Clr $a 20 10 10), (1.6 * $scale))
    $pEdge = New-Object System.Drawing.Pen((Clr $a 255 255 255), (2.2 * $scale))
    $pEdgeSoft = New-Object System.Drawing.Pen((Clr ($a * 0.9) 255 205 210), (3.8 * $scale))
    $pWrap = New-Object System.Drawing.Pen((Clr $a 180 40 40), (1.4 * $scale))

    # Shaft
    $sW = 5.2 * $scale
    $hL = 48.0 * $scale
    $rectShaft = New-Object System.Drawing.RectangleF((-$sW/2), -$hL, $sW, $hL)
    $gfx.FillRectangle($bShaft, $rectShaft)
    $gfx.DrawRectangle($pDark, $rectShaft.X, $rectShaft.Y, $rectShaft.Width, $rectShaft.Height)

    # Red wraps
    for ($y = -$hL + 5 * $scale; $y -lt -5 * $scale; $y += 6.0 * $scale) {
        $gfx.DrawLine($pWrap, (-$sW/2), $y, ($sW/2), ($y + 3.8 * $scale))
    }

    # Pommel ring
    Fill-EllipseCentered $gfx $bGold 0 (-$hL) (4.5 * $scale) (4.5 * $scale)
    Draw-EllipseCentered $gfx $pDark 0 (-$hL) (4.5 * $scale) (4.5 * $scale)
    Fill-EllipseCentered $gfx $bShaft 0 (-$hL) (2.0 * $scale) (2.0 * $scale)

    # Collar
    $cW = 12.0 * $scale
    $cH = 22.0 * $scale
    $rectCollar = New-Object System.Drawing.RectangleF((-$cW/2), (-$cH/2), $cW, $cH)
    $gfx.FillRectangle($bIronDark, $rectCollar)
    $gfx.DrawRectangle($pDark, $rectCollar.X, $rectCollar.Y, $rectCollar.Width, $rectCollar.Height)

    # Rune rivet
    $gfx.FillEllipse($bGold, (-3 * $scale), (-3 * $scale), (6 * $scale), (6 * $scale))
    $gfx.DrawEllipse($pDark, (-3 * $scale), (-3 * $scale), (6 * $scale), (6 * $scale))

    # Top Spike
    $ptsSpike = [System.Drawing.PointF[]]@(
        (Pt (-$sW/2) ($cH/2)),
        (Pt 0 ($cH/2 + 10 * $scale)),
        (Pt ($sW/2) ($cH/2))
    )
    $gfx.FillPolygon($bIronMid, $ptsSpike)
    $gfx.DrawPolygon($pDark, $ptsSpike)

    # Back Hook
    $ptsHook = [System.Drawing.PointF[]]@(
        (Pt ($cW/2) (-7 * $scale)),
        (Pt ($cW/2 + 13 * $scale) (-3 * $scale)),
        (Pt ($cW/2 + 9 * $scale) (3 * $scale)),
        (Pt ($cW/2) (7 * $scale))
    )
    $gfx.FillPolygon($bIronMid, $ptsHook)
    $gfx.DrawPolygon($pDark, $ptsHook)

    # Executioner Blade
    $pathBlade = New-Object System.Drawing.Drawing2D.GraphicsPath
    $ptsBlade = [System.Drawing.PointF[]]@(
        (Pt (-$cW/2) (-9 * $scale)),
        (Pt (-22 * $scale) (-22 * $scale)),
        (Pt (-39 * $scale) (-8 * $scale)),
        (Pt (-46 * $scale) (12 * $scale)),
        (Pt (-38 * $scale) (32 * $scale)),
        (Pt (-19 * $scale) (24 * $scale)),
        (Pt (-$cW/2) (9 * $scale))
    )
    $pathBlade.AddCurve($ptsBlade, 0.45)
    $pathBlade.CloseFigure()
    $gfx.FillPath($bCrimson, $pathBlade)
    $gfx.DrawPath($pDark, $pathBlade)

    # Cutting edge
    $edgePts = [System.Drawing.PointF[]]@(
        (Pt (-21 * $scale) (-21 * $scale)),
        (Pt (-38 * $scale) (-7 * $scale)),
        (Pt (-45 * $scale) (12 * $scale)),
        (Pt (-37 * $scale) (31 * $scale))
    )
    $gfx.DrawCurve($pEdgeSoft, $edgePts, 0.45)
    $gfx.DrawCurve($pEdge, $edgePts, 0.45)

    $gfx.Restore($state)

    $bShaft.Dispose(); $bIronDark.Dispose(); $bIronMid.Dispose(); $bGold.Dispose(); $bCrimson.Dispose()
    $pDark.Dispose(); $pEdge.Dispose(); $pEdgeSoft.Dispose(); $pWrap.Dispose(); $pathBlade.Dispose()
}

# Blade-following Vertical Guillotine Cleave Arc (날의 궤적을 완벽히 따라가는 수직 참격호)
function Draw-GuillotineCleaveArc($gfx, [float]$cx, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    # Outer sweep following the blade tip from top-left down through center
    $outerPts = [System.Drawing.PointF[]]@(
        (Pt ($cx - 20) 6),
        (Pt ($cx - 38) 28),
        (Pt ($cx - 44) 56),
        (Pt ($cx - 30) 84),
        (Pt ($cx + 4) 106)
    )
    # Inner sweep creating a wide, luminous curved blade trail
    $innerPts = [System.Drawing.PointF[]]@(
        (Pt ($cx + 4) 106),
        (Pt ($cx - 16) 82),
        (Pt ($cx - 24) 54),
        (Pt ($cx - 22) 28),
        (Pt ($cx - 20) 6)
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddCurve($outerPts, 0.45)
    $path.AddCurve($innerPts, 0.45)
    $path.CloseFigure()

    $bGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt ($cx - 35) 6), (Pt ($cx) 106),
        (Clr ($a * 0.95) 255 23 68), (Clr ($a * 0.4) 255 140 0)
    )
    $gfx.FillPath($bGrad, $path)

    $pAura = New-Object System.Drawing.Pen((Clr ($a * 0.6) 255 23 68), 7.0)
    $pMid = New-Object System.Drawing.Pen((Clr ($a * 0.9) 255 112 67), 4.0)
    $pYellow = New-Object System.Drawing.Pen((Clr $a 255 235 59), 2.2)
    $pCore = New-Object System.Drawing.Pen((Clr $a 255 255 255), 1.6)

    $gfx.DrawCurve($pAura, $outerPts, 0.45)
    $gfx.DrawCurve($pMid, $outerPts, 0.45)
    $gfx.DrawCurve($pYellow, $outerPts, 0.45)
    $gfx.DrawCurve($pCore, $outerPts, 0.45)

    # High speed blur streaks
    $pSpeed = New-Object System.Drawing.Pen((Clr ($a * 0.7) 255 214 0), 1.5)
    $gfx.DrawLine($pSpeed, ($cx - 14), 10, ($cx - 28), 50)
    $gfx.DrawLine($pSpeed, ($cx - 4), 16, ($cx - 14), 62)
    $pSpeed.Dispose()

    $path.Dispose(); $bGrad.Dispose(); $pAura.Dispose(); $pMid.Dispose(); $pCore.Dispose(); $pYellow.Dispose()
}

# Vertical Slice Seam
function Draw-VerticalCutSeam($gfx, [float]$cx, [float]$topY, [float]$botY, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $pOuter = New-Object System.Drawing.Pen((Clr ($a * 0.5) 255 23 68), 9.0)
    $pMid = New-Object System.Drawing.Pen((Clr ($a * 0.85) 255 87 34), 4.5)
    $pYellow = New-Object System.Drawing.Pen((Clr $a 255 235 59), 2.4)
    $pCore = New-Object System.Drawing.Pen((Clr $a 255 255 255), 1.6)

    $gfx.DrawLine($pOuter, $cx, $topY, $cx, $botY)
    $gfx.DrawLine($pMid, $cx, $topY, $cx, $botY)
    $gfx.DrawLine($pYellow, $cx, $topY, $cx, $botY)
    $gfx.DrawLine($pCore, $cx, $topY, $cx, $botY)

    $pOuter.Dispose(); $pMid.Dispose(); $pYellow.Dispose(); $pCore.Dispose()
}

# Lateral Split Shockwaves (장작이 좌우로 쪼개지는 연출)
function Draw-LateralSplinters($gfx, [float]$cx, [float]$cy, [float]$spread, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $pWaveRed = New-Object System.Drawing.Pen((Clr ($a * 0.75) 255 23 68), 5.0)
    $pWaveGold = New-Object System.Drawing.Pen((Clr $a 255 214 0), 2.6)
    $pWaveWhite = New-Object System.Drawing.Pen((Clr $a 255 255 255), 1.4)

    # Clamped spread to ensure points stay strictly within the frame
    $effSpread = [Math]::Min(1.25, [float]$spread)

    $ptsL = [System.Drawing.PointF[]]@(
        (Pt ($cx - 6) ($cy - 22)),
        (Pt ($cx - 44 * $effSpread) $cy),
        (Pt ($cx - 6) ($cy + 22))
    )
    $gfx.DrawCurve($pWaveRed, $ptsL, 0.45)
    $gfx.DrawCurve($pWaveGold, $ptsL, 0.45)
    $gfx.DrawCurve($pWaveWhite, $ptsL, 0.45)

    $ptsR = [System.Drawing.PointF[]]@(
        (Pt ($cx + 6) ($cy - 22)),
        (Pt ($cx + 44 * $effSpread) $cy),
        (Pt ($cx + 6) ($cy + 22))
    )
    $gfx.DrawCurve($pWaveRed, $ptsR, 0.45)
    $gfx.DrawCurve($pWaveGold, $ptsR, 0.45)
    $gfx.DrawCurve($pWaveWhite, $ptsR, 0.45)

    $pWaveRed.Dispose(); $pWaveGold.Dispose(); $pWaveWhite.Dispose()
}

# Ground Rupture
function Draw-GroundRupture($gfx, [float]$cx, [float]$cy, [float]$spread, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)
    $pOuter = New-Object System.Drawing.Pen((Clr $a 183 28 28), 3.5)
    $pInner = New-Object System.Drawing.Pen((Clr $a 255 214 0), 1.6)

    $effSpread = [Math]::Min(1.0, [float]$spread)

    $ptsL = [System.Drawing.PointF[]]@(
        (Pt $cx $cy),
        (Pt ($cx - 16.0 * $effSpread) ($cy - 1.5)),
        (Pt ($cx - 32.0 * $effSpread) ($cy + 3.0)),
        (Pt ($cx - 46.0 * $effSpread) ($cy - 2.0)),
        (Pt ($cx - 56.0 * $effSpread) ($cy + 2.0))
    )
    $gfx.DrawLines($pOuter, $ptsL)
    $gfx.DrawLines($pInner, $ptsL)

    $ptsR = [System.Drawing.PointF[]]@(
        (Pt $cx $cy),
        (Pt ($cx + 15.0 * $effSpread) ($cy + 2.0)),
        (Pt ($cx + 30.0 * $effSpread) ($cy - 2.0)),
        (Pt ($cx + 44.0 * $effSpread) ($cy + 3.0)),
        (Pt ($cx + 56.0 * $effSpread) ($cy - 1.5))
    )
    $gfx.DrawLines($pOuter, $ptsR)
    $gfx.DrawLines($pInner, $ptsR)

    $pOuter.Dispose(); $pInner.Dispose()
}

# Impact Flash Starburst
function Draw-ImpactFlash($gfx, [float]$cx, [float]$cy, [float]$size, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)
    $bWhite = New-Object System.Drawing.SolidBrush((Clr $a 255 255 255))
    $bYellow = New-Object System.Drawing.SolidBrush((Clr ($a * 0.85) 255 235 59))
    $bRed = New-Object System.Drawing.SolidBrush((Clr ($a * 0.5) 255 23 68))

    Fill-EllipseCentered $gfx $bRed $cx $cy ($size * 2.0) ($size * 2.0)
    Fill-EllipseCentered $gfx $bYellow $cx $cy ($size * 1.2) ($size * 1.2)
    Fill-EllipseCentered $gfx $bWhite $cx $cy ($size * 0.6) ($size * 0.6)

    $pRay = New-Object System.Drawing.Pen((Clr $a 255 255 255), 2.6)
    $rayLen = $size * 2.8
    $gfx.DrawLine($pRay, ($cx - $rayLen), $cy, ($cx + $rayLen), $cy)
    $gfx.DrawLine($pRay, $cx, ($cy - $rayLen), $cx, ($cy + $rayLen))

    $pRayDiag = New-Object System.Drawing.Pen((Clr ($a * 0.85) 255 235 59), 1.8)
    $dLen = $size * 1.8
    $gfx.DrawLine($pRayDiag, ($cx - $dLen), ($cy - $dLen), ($cx + $dLen), ($cy + $dLen))
    $gfx.DrawLine($pRayDiag, ($cx - $dLen), ($cy + $dLen), ($cx + $dLen), ($cy - $dLen))

    $bWhite.Dispose(); $bYellow.Dispose(); $bRed.Dispose(); $pRay.Dispose(); $pRayDiag.Dispose()
}

# Wood chips, splinters, embers
function Draw-Splinters($gfx, [float]$cx, [float]$cy, [float]$progress, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)
    $bWoodChip = New-Object System.Drawing.SolidBrush((Clr $a 161 136 127))
    $bEmber = New-Object System.Drawing.SolidBrush((Clr $a 255 112 67))
    $bGold = New-Object System.Drawing.SolidBrush((Clr $a 255 214 0))

    $items = @(
        @(-26.0, -20.0, 5.0, 1.8, -35, 'chip'), @(28.0, -22.0, 5.5, 1.8, 40, 'chip'),
        @(-42.0, -28.0, 4.0, 1.5, -50, 'chip'), @(44.0, -26.0, 4.5, 1.6, 55, 'chip'),
        @(-18.0, -44.0, 3.0, 3.0, 0, 'ember'),   @(20.0, -46.0, 3.0, 3.0, 0, 'ember'),
        @(-32.0, -52.0, 2.2, 2.2, 0, 'gold'),    @(34.0, -50.0, 2.2, 2.2, 0, 'gold'),
        @(-52.0, -14.0, 3.2, 3.2, 0, 'ember'),   @(54.0, -16.0, 3.2, 3.2, 0, 'ember'),
        @(-35.0, 6.0, 4.0, 1.5, 20, 'chip'),     @(37.0, 6.0, 4.0, 1.5, -20, 'chip')
    )

    foreach ($it in $items) {
        $dx = [float]$it[0] * $progress
        $dy = [float]$it[1] * $progress
        $w = [float]$it[2]; $h = [float]$it[3]; $ang = [float]$it[4]; $type = [string]$it[5]
        $px = $cx + $dx; $py = $cy + $dy

        if ($type -eq 'chip') {
            $st = $gfx.Save()
            $gfx.TranslateTransform($px, $py)
            $gfx.RotateTransform($ang)
            $gfx.FillRectangle($bWoodChip, (-$w/2), (-$h/2), $w, $h)
            $gfx.Restore($st)
        } elseif ($type -eq 'ember') {
            $gfx.FillEllipse($bEmber, ($px - $w/2), ($py - $h/2), $w, $h)
        } else {
            $gfx.FillEllipse($bGold, ($px - $w/2), ($py - $h/2), $w, $h)
        }
    }

    $bWoodChip.Dispose(); $bEmber.Dispose(); $bGold.Dispose()
}

# Stylized Smoke
function Draw-SmokePuffs($gfx, [float]$cx, [float]$cy, [float]$spread, [float]$rise, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)
    $bSmokeDark = New-Object System.Drawing.SolidBrush((Clr ($a * 0.45) 176 190 197))
    $bSmokeLight = New-Object System.Drawing.SolidBrush((Clr ($a * 0.65) 236 239 241))

    $puffs = @(
        @([float](-28.0 * $spread), [float](-8.0 - $rise), 18.0),
        @([float](28.0 * $spread), [float](-8.0 - $rise), 18.0),
        @([float](-12.0 * $spread), [float](-16.0 - $rise), 22.0),
        @([float](12.0 * $spread), [float](-16.0 - $rise), 22.0),
        @([float](-40.0 * $spread), [float](-4.0 - $rise), 14.0),
        @([float](40.0 * $spread), [float](-4.0 - $rise), 14.0),
        @(0.0, [float](-22.0 - $rise), 24.0)
    )

    foreach ($p in $puffs) {
        $px = $cx + [float]$p[0]; $py = $cy + [float]$p[1]; $r = [float]$p[2]
        Fill-EllipseCentered $gfx $bSmokeDark $px $py ($r * 1.1) ($r * 0.8)
        Fill-EllipseCentered $gfx $bSmokeLight $px ($py - 2) ($r * 0.85) ($r * 0.65)
    }

    $bSmokeDark.Dispose(); $bSmokeLight.Dispose()
}

$groundY = 90.0

# ==================== FRAME 0: HIGH OVERHEAD WINDUP ====================
$f0_cx = 0 * 128 + 64

# Ground target circle
$bWarn = New-Object System.Drawing.SolidBrush((Clr 75 244 67 54))
$pWarn = New-Object System.Drawing.Pen((Clr 150 255 82 82), 1.8)
Fill-EllipseCentered $g $bWarn $f0_cx $groundY 26 8
Draw-EllipseCentered $g $pWarn $f0_cx $groundY 26 8
$bWarn.Dispose(); $pWarn.Dispose()

# Vertical targeting streak
$pAnt = New-Object System.Drawing.Pen((Clr 160 255 214 0), 1.6)
$g.DrawLine($pAnt, $f0_cx, 6, $f0_cx, 86)
$pAnt.Dispose()

# Axe raised overhead ready to chop down: angle = +40 degrees
Draw-ChopAxe $g ($f0_cx + 8) 36 40 1.0 1.0

# Blade glint
Fill-EllipseCentered $g (New-Object System.Drawing.SolidBrush((Clr 240 255 235 59))) ($f0_cx - 24) 22 4.5 4.5
Fill-EllipseCentered $g (New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))) ($f0_cx - 24) 22 2.0 2.0


# ==================== FRAME 1: GIGANTIC DOWNWARD CLEAVE ====================
$f1_cx = 1 * 128 + 64

# Ground warning
$bWarn1 = New-Object System.Drawing.SolidBrush((Clr 150 255 23 68))
$pWarn1 = New-Object System.Drawing.Pen((Clr 250 255 214 0), 2.4)
Fill-EllipseCentered $g $bWarn1 $f1_cx $groundY 24 8
Draw-EllipseCentered $g $pWarn1 $f1_cx $groundY 24 8
$bWarn1.Dispose(); $pWarn1.Dispose()

# Guillotine cleave arc along the blade's swing path!
Draw-GuillotineCleaveArc $g $f1_cx 1.0

# Axe swinging downwards: angle = -20 degrees
Draw-ChopAxe $g ($f1_cx - 6) 42 -20 1.0 1.0


# ==================== FRAME 2: DEEP IMPACT & VERTICAL SEAM ====================
$f2_cx = 2 * 128 + 64

# Towering vertical slice seam
Draw-VerticalCutSeam $g $f2_cx 2 126 1.0

# Ground shockwave ring
$pRing2 = New-Object System.Drawing.Pen((Clr 240 255 235 59), 2.6)
Draw-EllipseCentered $g $pRing2 $f2_cx $groundY 32 11
$pRing2.Dispose()

# Ground rupture
Draw-GroundRupture $g $f2_cx $groundY 0.5 1.0

# Axe embedded into ground: angle = -75 degrees
Draw-ChopAxe $g ($f2_cx - 16) 54 -75 1.0 1.0

# Starburst flash
Draw-ImpactFlash $g $f2_cx $groundY 18.0 1.0
Draw-Splinters $g $f2_cx $groundY 0.4 1.0


# ==================== FRAME 3: LOG SPLITTING APART & LATERAL SHOCKWAVES ====================
$f3_cx = 3 * 128 + 64

# Vertical cut beam
Draw-VerticalCutSeam $g $f3_cx 6 120 0.75

# Shockwave rings
$pRing3A = New-Object System.Drawing.Pen((Clr 245 255 23 68), 3.4)
$pRing3B = New-Object System.Drawing.Pen((Clr 255 255 235 59), 2.0)
Draw-EllipseCentered $g $pRing3A $f3_cx $groundY 50 17
Draw-EllipseCentered $g $pRing3B $f3_cx $groundY 48 16
$pRing3A.Dispose(); $pRing3B.Dispose()

# Lateral split shockwaves
Draw-LateralSplinters $g $f3_cx $groundY 1.0 1.0

# Fissure
Draw-GroundRupture $g $f3_cx $groundY 0.85 1.0

# Axe firmly embedded
Draw-ChopAxe $g ($f3_cx - 16) 54 -75 1.0 1.0

Draw-ImpactFlash $g $f3_cx $groundY 12.0 0.85
Draw-Splinters $g $f3_cx $groundY 0.75 1.0


# ==================== FRAME 4: PEAK SPLASH AOE DETONATION ====================
$f4_cx = 4 * 128 + 64

$pRing4 = New-Object System.Drawing.Pen((Clr 225 255 61 0), 3.2)
$pRing4G = New-Object System.Drawing.Pen((Clr 245 255 214 0), 1.8)
Draw-EllipseCentered $g $pRing4 $f4_cx $groundY 58 20
Draw-EllipseCentered $g $pRing4G $f4_cx $groundY 56 19
$pRing4.Dispose(); $pRing4G.Dispose()

Draw-LateralSplinters $g $f4_cx $groundY 1.25 0.75

# Geysers
$bGeyser = New-Object System.Drawing.SolidBrush((Clr 190 255 171 0))
Fill-EllipseCentered $g $bGeyser ($f4_cx - 30) ($groundY - 8) 6 13
Fill-EllipseCentered $g $bGeyser ($f4_cx + 30) ($groundY - 8) 6 13
Fill-EllipseCentered $g $bGeyser ($f4_cx - 16) ($groundY - 7) 4 10
Fill-EllipseCentered $g $bGeyser ($f4_cx + 16) ($groundY - 7) 4 10
$bGeyser.Dispose()

Draw-VerticalCutSeam $g $f4_cx 18 108 0.40
Draw-GroundRupture $g $f4_cx $groundY 1.0 1.0
Draw-ChopAxe $g ($f4_cx - 16) 54 -75 1.0 0.95
Draw-Splinters $g $f4_cx $groundY 1.0 1.0
Draw-SmokePuffs $g $f4_cx $groundY 0.5 0.0 0.35


# ==================== FRAME 5: AXE DISSOLVE ====================
$f5_cx = 5 * 128 + 64
$pRing5 = New-Object System.Drawing.Pen((Clr 130 255 87 34), 2.2)
Draw-EllipseCentered $g $pRing5 $f5_cx $groundY 60 21
$pRing5.Dispose()

Draw-GroundRupture $g $f5_cx $groundY 1.0 0.85
Draw-ChopAxe $g ($f5_cx - 16) 54 -75 1.0 0.55

# Embers
$bDis = New-Object System.Drawing.SolidBrush((Clr 220 255 82 82))
for ($k = 0; $k -lt 18; $k++) {
    $rx = $f5_cx + [Math]::Sin($k * 1.5) * (22 + $k * 1.5)
    $ry = 65 + [Math]::Cos($k * 1.5) * (24 + $k)
    $g.FillEllipse($bDis, [float]$rx, [float]$ry, 2.5, 2.5)
}
$bDis.Dispose()

Draw-SmokePuffs $g $f5_cx $groundY 0.85 4.0 0.7
Draw-Splinters $g $f5_cx $groundY 1.25 0.75


# ==================== FRAME 6: DUST BILLOWS & FLOATING EMBERS ====================
$f6_cx = 6 * 128 + 64
Draw-GroundRupture $g $f6_cx $groundY 0.95 0.6
Draw-SmokePuffs $g $f6_cx $groundY 1.15 10.0 0.85

$bEmb = New-Object System.Drawing.SolidBrush((Clr 170 255 171 0))
for ($k = 0; $k -lt 14; $k++) {
    $rx = $f6_cx + [Math]::Sin($k * 2.1) * (28 + $k * 1.2)
    $ry = 50 - ($k * 3.5)
    $g.FillEllipse($bEmb, [float]$rx, [float]$ry, 2.2, 2.2)
}
$bEmb.Dispose()


# ==================== FRAME 7: EXPANDING DUST ====================
$f7_cx = 7 * 128 + 64
Draw-GroundRupture $g $f7_cx $groundY 0.85 0.35
Draw-SmokePuffs $g $f7_cx $groundY 1.35 18.0 0.58

$bEmb7 = New-Object System.Drawing.SolidBrush((Clr 110 255 171 0))
for ($k = 0; $k -lt 8; $k++) {
    $rx = $f7_cx + [Math]::Sin($k * 2.5) * (32 + $k * 1.5)
    $ry = 38 - ($k * 3.2)
    $g.FillEllipse($bEmb7, [float]$rx, [float]$ry, 1.8, 1.8)
}
$bEmb7.Dispose()


# ==================== FRAME 8: LINGERING SMOKE ====================
$f8_cx = 8 * 128 + 64
Draw-SmokePuffs $g $f8_cx $groundY 1.45 24.0 0.30


# ==================== FRAME 9: FINAL CLEAN FADEOUT ====================
$f9_cx = 9 * 128 + 64
Draw-SmokePuffs $g $f9_cx $groundY 1.5 28.0 0.10

$targetPath = "Assets/4. DotAsset/Effect_Axe_Splash_Cleave.png"
$bmp.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$g.Dispose()
Write-Output "Perfect Darius & firewood cleave rendered to $targetPath!"
