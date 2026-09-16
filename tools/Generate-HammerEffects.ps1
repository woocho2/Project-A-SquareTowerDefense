Add-Type -AssemblyName System.Drawing

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

# ==============================================================================
# 1. MJOLNIR WARHAMMER MODEL (Vertical Y-Axis Alignment)
# Striking head at local +Y, Handle extending upwards (-Y)
# Perfectly centered and balanced to fit within 128x128
# ==============================================================================
function Draw-MjolnirHammer($gfx, [float]$cx, [float]$cy, [float]$angle, [float]$scale, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $state = $gfx.Save()
    $gfx.TranslateTransform($cx, $cy)
    if ($angle -ne 0) { $gfx.RotateTransform($angle) }

    $bGoldLight  = New-Object System.Drawing.SolidBrush((Clr $a 255 235 59))
    $bGoldMain   = New-Object System.Drawing.SolidBrush((Clr $a 255 193 7))
    $bGoldDark   = New-Object System.Drawing.SolidBrush((Clr $a 230 130 0))
    $bHandle     = New-Object System.Drawing.SolidBrush((Clr $a 130 70 20))
    $bCrack      = New-Object System.Drawing.SolidBrush((Clr $a 255 255 255))
    $pCrackGlow  = New-Object System.Drawing.Pen((Clr $a 255 245 157), (2.8 * $scale))
    $pDarkEdge   = New-Object System.Drawing.Pen((Clr $a 45 25 0), (1.8 * $scale))
    $pBevel      = New-Object System.Drawing.Pen((Clr $a 255 255 200), (2.0 * $scale))
    $pStrikeEdge = New-Object System.Drawing.Pen((Clr $a 255 255 255), (2.5 * $scale))

    # Balanced Dimensions
    $headHalfW = 26.0 * $scale
    $headHalfH = 12.0 * $scale
    $chamfer   = 6.0 * $scale

    # Handle extending straight UPWARDS (local -Y)
    $handleW = 6.5 * $scale
    $handleL = 24.0 * $scale
    $hTopY = -$headHalfH - $handleL
    $hRect = New-Object System.Drawing.RectangleF((-$handleW/2), $hTopY, $handleW, $handleL)
    $gfx.FillRectangle($bHandle, $hRect)
    $gfx.DrawRectangle($pDarkEdge, $hRect.X, $hRect.Y, $hRect.Width, $hRect.Height)

    # Leather grip wrap bands
    $pBand = New-Object System.Drawing.Pen((Clr $a 255 214 0), (1.8 * $scale))
    for ($y = $hTopY + 4.5 * $scale; $y -lt -$headHalfH - 2.5 * $scale; $y += 5.5 * $scale) {
        $gfx.DrawLine($pBand, (-$handleW/2), $y, ($handleW/2), $y)
    }
    $pBand.Dispose()

    # Pommel Ring at top of handle
    Fill-EllipseCentered $gfx $bGoldLight 0 $hTopY (4.5 * $scale) (4.5 * $scale)
    Draw-EllipseCentered $gfx $pDarkEdge 0 $hTopY (4.5 * $scale) (4.5 * $scale)
    Fill-EllipseCentered $gfx $bHandle 0 $hTopY (1.8 * $scale) (1.8 * $scale)

    # Socket / Collar connecting head and handle
    $collarW = 15.0 * $scale
    $collarH = 5.0 * $scale
    $nRect = New-Object System.Drawing.RectangleF((-$collarW/2), (-$headHalfH - $collarH), $collarW, $collarH)
    $gfx.FillRectangle($bGoldDark, $nRect)
    $gfx.DrawRectangle($pDarkEdge, $nRect.X, $nRect.Y, $nRect.Width, $nRect.Height)

    # Octagonal Hammer Head (Wide horizontal warhammer block)
    $headPts = [System.Drawing.PointF[]]@(
        (Pt (-$headHalfW + $chamfer) (-$headHalfH)),
        (Pt ($headHalfW - $chamfer) (-$headHalfH)),
        (Pt $headHalfW (-$headHalfH + $chamfer)),
        (Pt $headHalfW ($headHalfH - $chamfer)),
        (Pt ($headHalfW - $chamfer) $headHalfH),
        (Pt (-$headHalfW + $chamfer) $headHalfH),
        (Pt (-$headHalfW) ($headHalfH - $chamfer)),
        (Pt (-$headHalfW) (-$headHalfH + $chamfer))
    )
    $gfx.FillPolygon($bGoldMain, $headPts)
    $gfx.DrawPolygon($pDarkEdge, $headPts)

    # Top Bevel Highlight
    $bevelPts = [System.Drawing.PointF[]]@(
        (Pt (-$headHalfW + $chamfer) (-$headHalfH + 1.2 * $scale)),
        (Pt ($headHalfW - $chamfer) (-$headHalfH + 1.2 * $scale))
    )
    $gfx.DrawLines($pBevel, $bevelPts)

    # Striking Face at bottom (+headHalfH)
    $strikePts = [System.Drawing.PointF[]]@(
        (Pt (-$headHalfW + $chamfer) ($headHalfH - 0.8 * $scale)),
        (Pt ($headHalfW - $chamfer) ($headHalfH - 0.8 * $scale))
    )
    $gfx.DrawLines($pStrikeEdge, $strikePts)

    # Side impact facet plates (as in 6. Hammer.png)
    $pSide = New-Object System.Drawing.Pen((Clr $a 255 245 157), (2.0 * $scale))
    $gfx.DrawLine($pSide, (-$headHalfW + 4 * $scale), (-$headHalfH + 3 * $scale), (-$headHalfW + 3 * $scale), ($headHalfH - 3 * $scale))
    $gfx.DrawLine($pSide, ($headHalfW - 4 * $scale), (-$headHalfH + 3 * $scale), ($headHalfW - 3 * $scale), ($headHalfH - 3 * $scale))
    $pSide.Dispose()

    # Lightning Rune Crack down center
    $crackPts = [System.Drawing.PointF[]]@(
        (Pt (-1 * $scale) (-$headHalfH + 2.5 * $scale)),
        (Pt (2.5 * $scale) (-1.5 * $scale)),
        (Pt (-3.0 * $scale) (1.8 * $scale)),
        (Pt (1 * $scale) ($headHalfH - 2.5 * $scale))
    )
    $gfx.DrawCurve($pCrackGlow, $crackPts, 0.1)
    $pCrackWhite = New-Object System.Drawing.Pen($bCrack, (1.6 * $scale))
    $gfx.DrawCurve($pCrackWhite, $crackPts, 0.1)
    $pCrackWhite.Dispose()

    $gfx.Restore($state)

    $bGoldLight.Dispose(); $bGoldMain.Dispose(); $bGoldDark.Dispose(); $bHandle.Dispose(); $bCrack.Dispose()
    $pCrackGlow.Dispose(); $pDarkEdge.Dispose(); $pBevel.Dispose(); $pStrikeEdge.Dispose()
}

# ==============================================================================
# 2. SPLASH HAMMER: ORGANIC TOP-DOWN SEISMIC CRACKS & DUST
# ==============================================================================

# Jagged, organic earthquake cracks radiating naturally in 6 organic directions
function Draw-OrganicEarthquakeFissures($gfx, [float]$cx, [float]$cy, [float]$progress, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $pOuter = New-Object System.Drawing.Pen((Clr $a 160 75 30), 3.6)
    $pMid   = New-Object System.Drawing.Pen((Clr $a 255 130 0), 2.2)
    $pCore  = New-Object System.Drawing.Pen((Clr $a 255 245 160), 1.2)

    $p = [Math]::Min(1.0, [float]$progress)

    # Hand-crafted natural jagged fracture paths (uneven lengths, organic bends)
    $fissureList = @(
        # East-Southeast main crack
        @((Pt $cx $cy), (Pt ($cx + 14 * $p) ($cy + 4 * $p)), (Pt ($cx + 28 * $p) ($cy - 2 * $p)), (Pt ($cx + 42 * $p) ($cy + 6 * $p)), (Pt ($cx + 52 * $p) ($cy + 3 * $p))),
        # East-Southeast sub-fork
        @((Pt ($cx + 28 * $p) ($cy - 2 * $p)), (Pt ($cx + 38 * $p) ($cy - 12 * $p)), (Pt ($cx + 46 * $p) ($cy - 14 * $p))),
        # West-Southwest main crack
        @((Pt $cx $cy), (Pt ($cx - 16 * $p) ($cy - 3 * $p)), (Pt ($cx - 28 * $p) ($cy + 5 * $p)), (Pt ($cx - 44 * $p) ($cy + 2 * $p)), (Pt ($cx - 54 * $p) ($cy + 8 * $p))),
        # West-Southwest sub-fork
        @((Pt ($cx - 28 * $p) ($cy + 5 * $p)), (Pt ($cx - 36 * $p) ($cy + 16 * $p)), (Pt ($cx - 44 * $p) ($cy + 22 * $p))),
        # North-Northwest crack
        @((Pt $cx $cy), (Pt ($cx - 4 * $p) ($cy - 14 * $p)), (Pt ($cx - 14 * $p) ($cy - 26 * $p)), (Pt ($cx - 10 * $p) ($cy - 38 * $p))),
        # North-Northeast crack
        @((Pt $cx $cy), (Pt ($cx + 8 * $p) ($cy - 12 * $p)), (Pt ($cx + 18 * $p) ($cy - 24 * $p)), (Pt ($cx + 28 * $p) ($cy - 32 * $p))),
        # South-Southeast crack
        @((Pt $cx $cy), (Pt ($cx + 6 * $p) ($cy + 16 * $p)), (Pt ($cx + 14 * $p) ($cy + 28 * $p)), (Pt ($cx + 8 * $p) ($cy + 40 * $p))),
        # South-Southwest crack
        @((Pt $cx $cy), (Pt ($cx - 8 * $p) ($cy + 14 * $p)), (Pt ($cx - 18 * $p) ($cy + 26 * $p)), (Pt ($cx - 24 * $p) ($cy + 36 * $p)))
    )

    foreach ($branch in $fissureList) {
        $pts = [System.Drawing.PointF[]]$branch
        $gfx.DrawLines($pOuter, $pts)
        $gfx.DrawLines($pMid, $pts)
        $gfx.DrawLines($pCore, $pts)
    }

    $pOuter.Dispose(); $pMid.Dispose(); $pCore.Dispose()
}

# 360-Degree Radial Rubble & Golden Sparks
function Draw-RadialRubble($gfx, [float]$cx, [float]$cy, [float]$progress, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $bRock      = New-Object System.Drawing.SolidBrush((Clr $a 120 110 100))
    $bGoldSpark = New-Object System.Drawing.SolidBrush((Clr $a 255 214 0))
    $bElectric  = New-Object System.Drawing.SolidBrush((Clr $a 255 255 255))

    $items = @(
        @(15.0,  46.0, 4.0, 3.5, 20,  'rock'),
        @(65.0,  40.0, 3.5, 3.0, -35, 'rock'),
        @(120.0, 48.0, 4.5, 3.5, 45,  'rock'),
        @(175.0, 44.0, 3.8, 3.2, -15, 'rock'),
        @(220.0, 42.0, 3.5, 3.0, 30,  'rock'),
        @(280.0, 45.0, 4.0, 3.5, -40, 'rock'),
        @(335.0, 42.0, 3.5, 3.0, 15,  'rock'),
        @(40.0,  52.0, 2.2, 2.2, 0,   'gold'),
        @(95.0,  48.0, 2.0, 2.0, 0,   'elec'),
        @(150.0, 50.0, 2.2, 2.2, 0,   'gold'),
        @(205.0, 46.0, 2.0, 2.0, 0,   'elec'),
        @(260.0, 52.0, 2.2, 2.2, 0,   'gold'),
        @(310.0, 48.0, 2.0, 2.0, 0,   'elec')
    )

    foreach ($it in $items) {
        $rad = [float]$it[0] * [Math]::PI / 180.0
        $dist = [float]$it[1] * $progress
        $w = [float]$it[2]; $h = [float]$it[3]; $ang = [float]$it[4]; $type = [string]$it[5]

        $px = $cx + [Math]::Cos($rad) * $dist
        $py = $cy + [Math]::Sin($rad) * $dist

        if ($type -eq 'rock') {
            $st = $gfx.Save()
            $gfx.TranslateTransform($px, $py)
            $gfx.RotateTransform($ang)
            $gfx.FillRectangle($bRock, (-$w/2), (-$h/2), $w, $h)
            $gfx.Restore($st)
        } elseif ($type -eq 'gold') {
            $gfx.FillEllipse($bGoldSpark, ($px - $w/2), ($py - $h/2), $w, $h)
        } else {
            $gfx.FillEllipse($bElectric, ($px - $w/2), ($py - $h/2), $w, $h)
        }
    }

    $bRock.Dispose(); $bGoldSpark.Dispose(); $bElectric.Dispose()
}

# Organic Billowing Radial Dust Clouds
function Draw-OrganicDustCloud($gfx, [float]$cx, [float]$cy, [float]$radius, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)
    $bDark  = New-Object System.Drawing.SolidBrush((Clr ($a * 0.45) 165 175 185))
    $bLight = New-Object System.Drawing.SolidBrush((Clr ($a * 0.70) 235 240 245))

    # Organic clustered puff offsets (varied angles, distances, and sizes)
    $puffs = @(
        @(-0.75, -0.65, 17.0),
        @(0.70,  -0.70, 16.0),
        @(-0.90,  0.30, 18.0),
        @(0.85,   0.25, 17.5),
        @(-0.30, -0.95, 15.0),
        @(0.35,  -0.90, 15.5),
        @(-0.55,  0.75, 16.5),
        @(0.50,   0.80, 16.0),
        @(0.0,   -0.35, 14.0),
        @(0.0,    0.35, 14.0)
    )

    foreach ($p in $puffs) {
        $dx = [float]$p[0] * $radius
        $dy = [float]$p[1] * $radius
        $sz = [float]$p[2] * ($radius / 34.0)

        $px = $cx + $dx
        $py = $cy + $dy

        Fill-EllipseCentered $gfx $bDark  $px $py ($sz * 1.1) ($sz * 1.1)
        Fill-EllipseCentered $gfx $bLight $px ($py - 1) ($sz * 0.85) ($sz * 0.85)
    }

    $bDark.Dispose(); $bLight.Dispose()
}


# ==============================================================================
# 3. BUILD SPLASH HAMMER SPRITESHEET (1280x128, 10 frames)
# Vertical Y-Axis Plunge straight down into tile center (cx, 64)
# ==============================================================================
$bmpSplash = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gSplash = [System.Drawing.Graphics]::FromImage($bmpSplash)
$gSplash.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gSplash.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gSplash.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSplash.Clear([System.Drawing.Color]::Transparent)

$splashCenterY = 64.0
$hammerScale = 1.15
$headHalfH = 12.0 * $hammerScale # 13.8

# Frame 0: Sky Overhead Anticipation (수직 Y축 조준 & 지면 원형 룬)
$c0 = 0 * 128 + 64
# Ground circular targeting glyph at center (c0, 64)
$bWarn0 = New-Object System.Drawing.SolidBrush((Clr 45 255 193 7))
$pWarn0 = New-Object System.Drawing.Pen((Clr 190 255 235 59), 2.0)
Fill-EllipseCentered $gSplash $bWarn0 $c0 $splashCenterY 22 22
Draw-EllipseCentered $gSplash $pWarn0 $c0 $splashCenterY 22 22
# Reticle ticks
$pRet = New-Object System.Drawing.Pen((Clr 180 255 214 0), 1.5)
$gSplash.DrawLine($pRet, ($c0 - 28), $splashCenterY, ($c0 - 18), $splashCenterY)
$gSplash.DrawLine($pRet, ($c0 + 18), $splashCenterY, ($c0 + 28), $splashCenterY)
$gSplash.DrawLine($pRet, $c0, ($splashCenterY - 28), $c0, ($splashCenterY - 18))
$gSplash.DrawLine($pRet, $c0, ($splashCenterY + 18), $c0, ($splashCenterY + 28))
$pRet.Dispose(); $bWarn0.Dispose(); $pWarn0.Dispose()

# Vertical laser aim line from sky
$pLaser = New-Object System.Drawing.Pen((Clr 160 255 255 255), 1.8)
$gSplash.DrawLine($pLaser, $c0, 2, $c0, 64)
$pLaser.Dispose()

# Hammer held high overhead along Y-axis (cy = 40, entire hammer 100% visible!)
Draw-MjolnirHammer $gSplash $c0 40 0 $hammerScale 1.0
Fill-EllipseCentered $gSplash (New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))) $c0 52 4.5 4.5


# Frame 1: Rapid Vertical Plunge (수직 급강하 Y축 가속)
$c1 = 1 * 128 + 64
# Clean pulsing double targeting circle (no muddy fill!)
$pWarn1Outer = New-Object System.Drawing.Pen((Clr 240 255 214 0), 2.4)
$pWarn1Inner = New-Object System.Drawing.Pen((Clr 200 255 235 59), 1.6)
Draw-EllipseCentered $gSplash $pWarn1Outer $c1 $splashCenterY 26 26
Draw-EllipseCentered $gSplash $pWarn1Inner $c1 $splashCenterY 18 18
$pWarn1Outer.Dispose(); $pWarn1Inner.Dispose()

# Vertical downward plunge speed streaks along Y axis
$pSpdTrail = New-Object System.Drawing.Pen((Clr 180 255 214 0), 3.0)
$pSpdCore  = New-Object System.Drawing.Pen((Clr 240 255 255 255), 1.6)
$gSplash.DrawLine($pSpdTrail, ($c1 - 32), 2, ($c1 - 32), 44)
$gSplash.DrawLine($pSpdTrail, ($c1 + 32), 2, ($c1 + 32), 44)
$gSplash.DrawLine($pSpdCore,  ($c1 - 32), 8, ($c1 - 32), 42)
$gSplash.DrawLine($pSpdCore,  ($c1 + 32), 8, ($c1 + 32), 42)
$gSplash.DrawLine($pSpdTrail, ($c1 - 16), 4, ($c1 - 16), 34)
$gSplash.DrawLine($pSpdTrail, ($c1 + 16), 4, ($c1 + 16), 34)
$gSplash.DrawLine($pSpdCore,  $c1, 0, $c1, 28)
$pSpdTrail.Dispose(); $pSpdCore.Dispose()

# Hammer plunged downward along Y-axis (cy = 44)
Draw-MjolnirHammer $gSplash $c1 44 0 $hammerScale 1.0


# Frame 2: Ground Impact (수직 격돌 쾅!! 정중앙 타격)
$c2 = 2 * 128 + 64
# Initial organic cracks
Draw-OrganicEarthquakeFissures $gSplash $c2 $splashCenterY 0.45 1.0

# Inner circular shockwave
$pRing2 = New-Object System.Drawing.Pen((Clr 240 255 235 59), 2.8)
Draw-EllipseCentered $gSplash $pRing2 $c2 $splashCenterY 24 24
$pRing2.Dispose()

# Hammer striking face hits exactly at splashCenterY = 64 (cy = 64 - headHalfH = 50.2)
Draw-MjolnirHammer $gSplash $c2 ($splashCenterY - $headHalfH) 0 $hammerScale 1.0

# Blinding Radial Starburst Flash at impact center (c2, 64)
$bFlareW = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$bFlareY = New-Object System.Drawing.SolidBrush((Clr 220 255 214 0))
Fill-EllipseCentered $gSplash $bFlareY $c2 $splashCenterY 20 20
Fill-EllipseCentered $gSplash $bFlareW $c2 $splashCenterY 10 10

$pRay = New-Object System.Drawing.Pen((Clr 255 255 255 255), 2.4)
$gSplash.DrawLine($pRay, ($c2 - 42), $splashCenterY, ($c2 + 42), $splashCenterY)
$gSplash.DrawLine($pRay, $c2, ($splashCenterY - 42), $c2, ($splashCenterY + 42))
$gSplash.DrawLine($pRay, ($c2 - 28), ($splashCenterY - 28), ($c2 + 28), ($splashCenterY + 28))
$gSplash.DrawLine($pRay, ($c2 - 28), ($splashCenterY + 28), ($c2 + 28), ($splashCenterY - 28))
$pRay.Dispose(); $bFlareW.Dispose(); $bFlareY.Dispose()

Draw-RadialRubble $gSplash $c2 $splashCenterY 0.35 1.0


# Frame 3: Seismic Wave & Planted Hammer (수직으로 지면에 꽂힌 해머 & 원형 충격파)
$c3 = 3 * 128 + 64
# Full organic earthquake cracks
Draw-OrganicEarthquakeFissures $gSplash $c3 $splashCenterY 0.85 1.0

# Top-Down Circular Shockwave (radius = 38)
$pRing3 = New-Object System.Drawing.Pen((Clr 255 214 0), 3.2)
$pRing3Inner = New-Object System.Drawing.Pen((Clr 255 255 255), 1.8)
Draw-EllipseCentered $gSplash $pRing3 $c3 $splashCenterY 38 38
Draw-EllipseCentered $gSplash $pRing3Inner $c3 $splashCenterY 38 38
$pRing3.Dispose(); $pRing3Inner.Dispose()

# Hammer driven 3px deeper into crater (수직 꽂힘 연출!)
Draw-MjolnirHammer $gSplash $c3 ($splashCenterY - $headHalfH + 3) 0 $hammerScale 1.0

Draw-RadialRubble $gSplash $c3 $splashCenterY 0.75 1.0


# Frame 4: Peak Circular Shockwave & Rubble Blast (최대 원형 충격파)
$c4 = 4 * 128 + 64
Draw-OrganicEarthquakeFissures $gSplash $c4 $splashCenterY 1.0 1.0

# Giant Top-Down Circular Shockwave (radius = 52)
$pRing4 = New-Object System.Drawing.Pen((Clr 220 255 193 7), 2.6)
Draw-EllipseCentered $gSplash $pRing4 $c4 $splashCenterY 52 52
$pRing4.Dispose()

# Secondary inner ring
$pRing4Sub = New-Object System.Drawing.Pen((Clr 160 255 245 157), 1.6)
Draw-EllipseCentered $gSplash $pRing4Sub $c4 $splashCenterY 26 26
$pRing4Sub.Dispose()

# Hammer starting to dissolve (alpha = 0.8)
Draw-MjolnirHammer $gSplash $c4 ($splashCenterY - $headHalfH + 3) 0 $hammerScale 0.8

Draw-RadialRubble $gSplash $c4 $splashCenterY 1.0 1.0
Draw-OrganicDustCloud $gSplash $c4 $splashCenterY 22 0.5


# Frame 5: Hammer Dissolve & Runic Motes (해머가 황금 룬으로 승화)
$c5 = 5 * 128 + 64
Draw-OrganicEarthquakeFissures $gSplash $c5 $splashCenterY 1.0 0.8

# Runic sparkle motes
$bRuneMote = New-Object System.Drawing.SolidBrush((Clr 220 255 235 59))
for ($k = 0; $k -lt 24; $k++) {
    $rx = $c5 + [Math]::Sin($k * 1.3) * (20 + $k * 0.8)
    $ry = ($splashCenterY - $headHalfH) + [Math]::Cos($k * 1.3) * (18 + $k * 0.7)
    $gSplash.FillEllipse($bRuneMote, [float]$rx, [float]$ry, 2.5, 2.5)
}
$bRuneMote.Dispose()

Draw-RadialRubble $gSplash $c5 $splashCenterY 1.25 0.7
Draw-OrganicDustCloud $gSplash $c5 $splashCenterY 32 0.75


# Frame 6: Heavy Volumetric Radial Dust Ring (유기적 먼지 구름 확산)
$c6 = 6 * 128 + 64
Draw-OrganicEarthquakeFissures $gSplash $c6 $splashCenterY 0.95 0.55
Draw-OrganicDustCloud $gSplash $c6 $splashCenterY 42 0.85

$bSpark6 = New-Object System.Drawing.SolidBrush((Clr 170 255 214 0))
for ($k = 0; $k -lt 14; $k++) {
    $rad = ($k * 25.7) * [Math]::PI / 180.0
    $rx = $c6 + [Math]::Cos($rad) * (26 + $k * 1.2)
    $ry = $splashCenterY + [Math]::Sin($rad) * (26 + $k * 1.2)
    $gSplash.FillEllipse($bSpark6, [float]$rx, [float]$ry, 2.2, 2.2)
}
$bSpark6.Dispose()


# Frame 7: Expanding Dust Ring
$c7 = 7 * 128 + 64
Draw-OrganicEarthquakeFissures $gSplash $c7 $splashCenterY 0.85 0.3
Draw-OrganicDustCloud $gSplash $c7 $splashCenterY 48 0.6


# Frame 8: Dissolving Dust
$c8 = 8 * 128 + 64
Draw-OrganicDustCloud $gSplash $c8 $splashCenterY 54 0.3


# Frame 9: Clean Transparent Fadeout
$c9 = 9 * 128 + 64
Draw-OrganicDustCloud $gSplash $c9 $splashCenterY 58 0.1

$splashPath = "Assets/4. DotAsset/Effect_Hammer_Splash_Slam.png"
if (Test-Path $splashPath) { [System.IO.File]::Delete($splashPath) }
$bmpSplash.Save($splashPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Rendered Splash Hammer to $splashPath!"


# ==============================================================================
# 4. BUILD TARGET HAMMER SPRITESHEET (1280x128, 10 frames)
# DEDICATED ARMOR-BREAK (방어 파괴) CONCUSSIVE IMPACT - ZERO FALLING HAMMER!
# Instant blunt clank + shield fracture & shatter + circular shock ring
# ==============================================================================
$bmpTarget = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gTarget = [System.Drawing.Graphics]::FromImage($bmpTarget)
$gTarget.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gTarget.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gTarget.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gTarget.Clear([System.Drawing.Color]::Transparent)

$hitCenterY = 64.0

# Frame 0: High-Energy Bullet Impact Arrival (총알 착탄 섬광)
$t0 = 0 * 128 + 64
$pTrail0 = New-Object System.Drawing.Pen((Clr 220 255 214 0), 2.4)
$gTarget.DrawLine($pTrail0, ($t0 + 15), ($hitCenterY - 15), $t0, $hitCenterY)
$pTrail0.Dispose()

$bSpark0 = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
Fill-EllipseCentered $gTarget $bSpark0 $t0 $hitCenterY 4.5 4.5
$bSpark0.Dispose()


# Frame 1: "CLANK!!" Heavy Blunt Hit + Golden Shield Armor Target (정수리 깡! 직격 & 방패 출현)
$t1 = 1 * 128 + 64

# Sharp 8-point impact starburst (Blunt Clank Flare)
$bStarW = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$bStarY = New-Object System.Drawing.SolidBrush((Clr 230 255 214 0))
Fill-EllipseCentered $gTarget $bStarY $t1 $hitCenterY 11 11
Fill-EllipseCentered $gTarget $bStarW $t1 $hitCenterY 5.5 5.5

$pCross = New-Object System.Drawing.Pen((Clr 255 255 255 255), 2.2)
$gTarget.DrawLine($pCross, ($t1 - 18), $hitCenterY, ($t1 + 18), $hitCenterY)
$gTarget.DrawLine($pCross, $t1, ($hitCenterY - 18), $t1, ($hitCenterY + 18))
$gTarget.DrawLine($pCross, ($t1 - 12), ($hitCenterY - 12), ($t1 + 12), ($hitCenterY + 12))
$gTarget.DrawLine($pCross, ($t1 - 12), ($hitCenterY + 12), ($t1 + 12), ($hitCenterY - 12))
$pCross.Dispose(); $bStarW.Dispose(); $bStarY.Dispose()

# Iconic Shield Armor Silhouette (방패 실루엣)
$pShield = New-Object System.Drawing.Pen((Clr 255 255 193 7), 2.2)
$shieldPts = [System.Drawing.PointF[]]@(
    (Pt ($t1 - 12) ($hitCenterY - 10)),
    (Pt ($t1 + 12) ($hitCenterY - 10)),
    (Pt ($t1 + 12) ($hitCenterY + 2)),
    (Pt $t1 ($hitCenterY + 14)),
    (Pt ($t1 - 12) ($hitCenterY + 2))
)
$gTarget.DrawPolygon($pShield, $shieldPts)

# Red/white fracture crack splitting down the shield center
$pCrack1 = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.6)
$crackPts1 = [System.Drawing.PointF[]]@(
    (Pt $t1 ($hitCenterY - 9)),
    (Pt ($t1 + 2) ($hitCenterY - 2)),
    (Pt ($t1 - 2) ($hitCenterY + 4)),
    (Pt $t1 ($hitCenterY + 12))
)
$gTarget.DrawLines($pCrack1, $crackPts1)
$pShield.Dispose(); $pCrack1.Dispose()


# Frame 2: "쩍!!" SHIELD SHATTERS (방어 파괴! 방패가 4조각으로 박살나며 파편 비산)
$t2 = 2 * 128 + 64

# Left and Right halves splitting apart
$bShieldShard = New-Object System.Drawing.SolidBrush((Clr 240 255 214 0))
$pShardEdge   = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.2)

# Left Top Shard
$sLT = [System.Drawing.PointF[]]@((Pt ($t2 - 15) ($hitCenterY - 12)), (Pt ($t2 - 4) ($hitCenterY - 11)), (Pt ($t2 - 2) ($hitCenterY - 2)), (Pt ($t2 - 14) ($hitCenterY - 2)))
$gTarget.FillPolygon($bShieldShard, $sLT); $gTarget.DrawPolygon($pShardEdge, $sLT)

# Right Top Shard
$sRT = [System.Drawing.PointF[]]@((Pt ($t2 + 4) ($hitCenterY - 11)), (Pt ($t2 + 15) ($hitCenterY - 12)), (Pt ($t2 + 14) ($hitCenterY - 2)), (Pt ($t2 + 2) ($hitCenterY - 2)))
$gTarget.FillPolygon($bShieldShard, $sRT); $gTarget.DrawPolygon($pShardEdge, $sRT)

# Left Bottom Shard
$sLB = [System.Drawing.PointF[]]@((Pt ($t2 - 14) ($hitCenterY + 2)), (Pt ($t2 - 2) ($hitCenterY + 2)), (Pt ($t2 - 3) ($hitCenterY + 16)))
$gTarget.FillPolygon($bShieldShard, $sLB); $gTarget.DrawPolygon($pShardEdge, $sLB)

# Right Bottom Shard
$sRB = [System.Drawing.PointF[]]@((Pt ($t2 + 2) ($hitCenterY + 2)), (Pt ($t2 + 14) ($hitCenterY + 2)), (Pt ($t2 + 3) ($hitCenterY + 16)))
$gTarget.FillPolygon($bShieldShard, $sRB); $gTarget.DrawPolygon($pShardEdge, $sRB)

$bShieldShard.Dispose(); $pShardEdge.Dispose()

# Inner concussive shock ring
$pRingTgt2 = New-Object System.Drawing.Pen((Clr 220 255 235 59), 1.8)
Draw-EllipseCentered $gTarget $pRingTgt2 $t2 $hitCenterY 16 16
$pRingTgt2.Dispose()


# Frame 3: Expanding Shock Ring & Dispersing Armor Shards
$t3 = 3 * 128 + 64
$pRingTgt3 = New-Object System.Drawing.Pen((Clr 170 255 214 0), 1.6)
Draw-EllipseCentered $gTarget $pRingTgt3 $t3 $hitCenterY 22 22
$pRingTgt3.Dispose()

$bDot3 = New-Object System.Drawing.SolidBrush((Clr 180 255 235 59))
$gTarget.FillEllipse($bDot3, ($t3 - 22), ($hitCenterY - 10), 2.2, 2.2)
$gTarget.FillEllipse($bDot3, ($t3 + 20), ($hitCenterY - 12), 2.0, 2.0)
$gTarget.FillEllipse($bDot3, ($t3 - 12), ($hitCenterY + 18), 1.8, 1.8)
$gTarget.FillEllipse($bDot3, ($t3 + 16), ($hitCenterY + 16), 2.0, 2.0)
$bDot3.Dispose()


# Frame 4: Micro Spark Embers (미세 잔여 스파크)
$t4 = 4 * 128 + 64
$bDot4 = New-Object System.Drawing.SolidBrush((Clr 90 255 214 0))
$gTarget.FillEllipse($bDot4, ($t4 - 24), ($hitCenterY - 10), 1.4, 1.4)
$gTarget.FillEllipse($bDot4, ($t4 + 22), ($hitCenterY - 12), 1.4, 1.4)
$gTarget.FillEllipse($bDot4, ($t4 - 10), ($hitCenterY + 20), 1.3, 1.3)
$gTarget.FillEllipse($bDot4, ($t4 + 18), ($hitCenterY + 18), 1.4, 1.4)
$bDot4.Dispose()


# Frame 5~9: 100% Fully Transparent for Rapid 2-Hit Attacks!

$targetPath = "Assets/4. DotAsset/Effect_Hammer_Target_Hit.png"
if (Test-Path $targetPath) { [System.IO.File]::Delete($targetPath) }
$bmpTarget.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Rendered Target Hammer to $targetPath!"


# ==============================================================================
# 5. GENERATE PREVIEW IMAGE (1280x256: Top = Splash, Bottom = Target)
# ==============================================================================
$preview = New-Object System.Drawing.Bitmap(1280, 256)
$gPrev = [System.Drawing.Graphics]::FromImage($preview)
$gPrev.Clear([System.Drawing.Color]::FromArgb(30, 30, 35))

# Top Row: Splash Hammer Frames 0..9
for ($i = 0; $i -lt 10; $i++) {
    $srcRect = New-Object System.Drawing.Rectangle(($i * 128), 0, 128, 128)
    $dstRect = New-Object System.Drawing.Rectangle(($i * 128), 0, 128, 128)
    $gPrev.DrawImage($bmpSplash, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
}

# Bottom Row: Target Hammer Frames 0..9
for ($i = 0; $i -lt 10; $i++) {
    $srcRect = New-Object System.Drawing.Rectangle(($i * 128), 0, 128, 128)
    $dstRect = New-Object System.Drawing.Rectangle(($i * 128), 128, 128, 128)
    $gPrev.DrawImage($bmpTarget, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
}

$previewPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_hammer_v4.png"
if (Test-Path $previewPath) { [System.IO.File]::Delete($previewPath) }
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)

$gSplash.Dispose(); $bmpSplash.Dispose()
$gTarget.Dispose(); $bmpTarget.Dispose()
$gPrev.Dispose(); $preview.Dispose()
Write-Output "Saved Preview to $previewPath!"
