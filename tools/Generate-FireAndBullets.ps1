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
# 1. ELEMENTAL BULLET GENERATOR (7 Elements: Same unified model, distinct colors)
# Comet / Magic Energy Orb with directional plasma tail and orbiting aura
# ==============================================================================
function Draw-ElementalBullet($gfx, [float]$cx, [float]$cy, [string]$element, [float]$scale) {
    # Color Palettes for each element
    switch ($element) {
        'Fire' {
            $cAura  = Clr 90 255 30 0
            $cBody  = Clr 235 255 65 0
            $cCore  = Clr 255 255 245 180
            $cTrail = Clr 160 255 120 0
            $cSpark = Clr 240 255 215 30
        }
        'Ice' {
            $cAura  = Clr 85 0 170 255
            $cBody  = Clr 230 0 210 255
            $cCore  = Clr 255 240 255 255
            $cTrail = Clr 150 100 230 255
            $cSpark = Clr 240 200 245 255
        }
        'Electricity' {
            $cAura  = Clr 90 255 200 0
            $cBody  = Clr 240 255 235 59
            $cCore  = Clr 255 255 255 255
            $cTrail = Clr 160 255 214 0
            $cSpark = Clr 255 255 255 200
        }
        'Wind' {
            $cAura  = Clr 85 0 190 165
            $cBody  = Clr 230 0 230 195
            $cCore  = Clr 255 230 255 250
            $cTrail = Clr 150 30 235 185
            $cSpark = Clr 240 170 255 235
        }
        'Earth' {
            $cAura  = Clr 95 130 65 15
            $cBody  = Clr 230 205 115 35
            $cCore  = Clr 255 255 235 180
            $cTrail = Clr 160 175 90 25
            $cSpark = Clr 240 255 185 80
        }
        'Light' {
            $cAura  = Clr 90 255 235 110
            $cBody  = Clr 240 255 248 185
            $cCore  = Clr 255 255 255 255
            $cTrail = Clr 170 255 245 160
            $cSpark = Clr 255 255 255 255
        }
        'Darkness' {
            $cAura  = Clr 130 110 30 200
            $cBody  = Clr 240 30 12 45
            $cCore  = Clr 255 190 100 255
            $cTrail = Clr 160 85 20 150
            $cSpark = Clr 220 200 130 255
        }
    }

    $bAura  = New-Object System.Drawing.SolidBrush($cAura)
    $bBody  = New-Object System.Drawing.SolidBrush($cBody)
    $bCore  = New-Object System.Drawing.SolidBrush($cCore)
    $bTrail = New-Object System.Drawing.SolidBrush($cTrail)
    $bSpark = New-Object System.Drawing.SolidBrush($cSpark)
    $pTrail = New-Object System.Drawing.Pen($cTrail, (1.8 * $scale))
    $pAura  = New-Object System.Drawing.Pen($cAura, (2.2 * $scale))

    # 1. Trailing Plasma Tails (comet tail)
    $tailPts = [System.Drawing.PointF[]]@(
        (Pt ($cx - 24 * $scale) ($cy)),
        (Pt ($cx - 6 * $scale) ($cy - 7 * $scale)),
        (Pt ($cx + 4 * $scale) ($cy - 5 * $scale)),
        (Pt ($cx + 8 * $scale) ($cy)),
        (Pt ($cx + 4 * $scale) ($cy + 5 * $scale)),
        (Pt ($cx - 6 * $scale) ($cy + 7 * $scale))
    )
    $gfx.FillPolygon($bTrail, $tailPts)

    # Thin whisps trailing further back
    $gfx.DrawLine($pTrail, ($cx - 6 * $scale), ($cy - 3 * $scale), ($cx - 30 * $scale), ($cy - 5 * $scale))
    $gfx.DrawLine($pTrail, ($cx - 6 * $scale), ($cy + 3 * $scale), ($cx - 30 * $scale), ($cy + 5 * $scale))
    $gfx.DrawLine($pTrail, ($cx - 8 * $scale), $cy, ($cx - 36 * $scale), $cy)

    # 2. Outer Glow Halo
    Fill-EllipseCentered $gfx $bAura $cx $cy (13.0 * $scale) (10.5 * $scale)
    Draw-EllipseCentered $gfx $pAura $cx $cy (13.0 * $scale) (10.5 * $scale)

    # 3. Vibrant Elemental Orb Body
    Fill-EllipseCentered $gfx $bBody ($cx + 1 * $scale) $cy (9.0 * $scale) (7.5 * $scale)

    # 4. Superheated Core Glow
    Fill-EllipseCentered $gfx $bCore ($cx + 3 * $scale) $cy (5.0 * $scale) (4.0 * $scale)

    # 5. Orbiting Energy Spark Motes
    Fill-EllipseCentered $gfx $bSpark ($cx - 10 * $scale) ($cy - 8 * $scale) (2.0 * $scale) (2.0 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx - 12 * $scale) ($cy + 7 * $scale) (1.8 * $scale) (1.8 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx + 8 * $scale) ($cy - 6 * $scale) (1.6 * $scale) (1.6 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx - 2 * $scale) ($cy + 9 * $scale) (1.8 * $scale) (1.8 * $scale)

    $bAura.Dispose(); $bBody.Dispose(); $bCore.Dispose(); $bTrail.Dispose(); $bSpark.Dispose()
    $pTrail.Dispose(); $pAura.Dispose()
}


# ==============================================================================
# 2. FIRE SPLASH VFX (E107): ORGANIC RADIAL FLAME NOVA
# Real licking, swirling flame waves spreading outwards in 360 degrees
# ==============================================================================

function PolarPt([float]$cx, [float]$cy, [float]$r, [float]$angRad) {
    New-Object System.Drawing.PointF(($cx + [Math]::Cos($angRad) * $r), ($cy + [Math]::Sin($angRad) * $r))
}

# Helper to draw an organic licking flame tongue with swirling arc and flame teeth
function Draw-LickingFlameTongue($gfx, [float]$cx, [float]$cy, [float]$angleDeg, [float]$innerR, [float]$length, [float]$widthDeg, [float]$twistDeg, $brush) {
    if ($length -le 1.0) { return }
    $rad = $angleDeg * [Math]::PI / 180.0
    $wRad = ($widthDeg * 0.5) * [Math]::PI / 180.0
    $twRad = $twistDeg * [Math]::PI / 180.0

    # Organic 9-point flame silhouette
    $pts = [System.Drawing.PointF[]]@(
        (PolarPt $cx $cy $innerR ($rad - $wRad)),
        (PolarPt $cx $cy ($innerR + $length * 0.35) ($rad - $wRad * 0.90 + $twRad * 0.30)),
        (PolarPt $cx $cy ($innerR + $length * 0.65) ($rad - $wRad * 0.95 + $twRad * 0.60)),
        (PolarPt $cx $cy ($innerR + $length * 0.60) ($rad - $wRad * 0.45 + $twRad * 0.65)),
        (PolarPt $cx $cy ($innerR + $length) ($rad + $twRad)),
        (PolarPt $cx $cy ($innerR + $length * 0.68) ($rad + $wRad * 0.45 + $twRad * 0.70)),
        (PolarPt $cx $cy ($innerR + $length * 0.72) ($rad + $wRad * 0.90 + $twRad * 0.55)),
        (PolarPt $cx $cy ($innerR + $length * 0.38) ($rad + $wRad * 0.85 + $twRad * 0.25)),
        (PolarPt $cx $cy $innerR ($rad + $wRad))
    )
    $gfx.FillPolygon($brush, $pts)
}

function Draw-RadialInfernoNova($gfx, [float]$cx, [float]$cy, [float]$radius, [float]$innerR, [float]$intensity, [float]$alpha) {
    if ($alpha -le 0.01 -or $radius -le 2.0) { return }
    $a = [int]($alpha * 255)

    $bRed    = New-Object System.Drawing.SolidBrush((Clr ($a * 0.85) 230 40 0))
    $bOrange = New-Object System.Drawing.SolidBrush((Clr ($a * 0.95) 255 140 0))
    $bYellow = New-Object System.Drawing.SolidBrush((Clr $a 255 230 50))
    $bWhite  = New-Object System.Drawing.SolidBrush((Clr $a 255 255 220))

    $flameSpan = $radius - $innerR
    if ($flameSpan -lt 4.0) { $flameSpan = 4.0 }

    # 14 primary and secondary swirling flame tongues
    $numFlames = 14
    for ($i = 0; $i -lt $numFlames; $i++) {
        $ang = $i * (360.0 / $numFlames)
        $isPrimary = ($i % 2 -eq 0)
        $lenMult = if ($isPrimary) { 1.0 } else { 0.74 }
        $fLen = $flameSpan * $lenMult
        $wDeg = if ($isPrimary) { 26.0 } else { 18.0 }
        $twist = 16.0 # Dynamic swirling inferno twist

        # 1. Outer Fierce Red Flame Tongue
        Draw-LickingFlameTongue $gfx $cx $cy $ang $innerR $fLen $wDeg $twist $bRed

        # 2. Mid Hot Orange Tongue (75% length, 70% width)
        Draw-LickingFlameTongue $gfx $cx $cy $ang ($innerR + $flameSpan * 0.08) ($fLen * 0.75) ($wDeg * 0.70) ($twist * 0.9) $bOrange

        # 3. Inner Golden Yellow Tongue (48% length, 45% width)
        if ($intensity -gt 0.25) {
            Draw-LickingFlameTongue $gfx $cx $cy $ang ($innerR + $flameSpan * 0.12) ($fLen * 0.48) ($wDeg * 0.45) ($twist * 0.8) $bYellow
        }

        # 4. Core White-Hot Core Streak
        if ($intensity -gt 0.5 -and $isPrimary) {
            Draw-LickingFlameTongue $gfx $cx $cy $ang ($innerR + $flameSpan * 0.15) ($fLen * 0.28) ($wDeg * 0.25) ($twist * 0.7) $bWhite
        }
    }

    # Center Radiant Flare (during ignition / expansion)
    if ($innerR -le 8.0 -and $intensity -gt 0.3) {
        $coreR = [Math]::Min(16.0, ($radius * 0.35))
        Fill-EllipseCentered $gfx $bOrange $cx $cy ($coreR * 1.3) ($coreR * 1.3)
        Fill-EllipseCentered $gfx $bYellow $cx $cy ($coreR * 0.85) ($coreR * 0.85)
        Fill-EllipseCentered $gfx $bWhite  $cx $cy ($coreR * 0.45) ($coreR * 0.45)
    } elseif ($innerR -gt 8.0) {
        # Subtle warm radiant glow in center (soft semi-transparent fiery halo)
        $bHoleGlow = New-Object System.Drawing.SolidBrush((Clr ($a * 0.15) 255 180 60))
        Fill-EllipseCentered $gfx $bHoleGlow $cx $cy ($innerR * 0.7) ($innerR * 0.7)
        $bHoleGlow.Dispose()
    }

    $bRed.Dispose(); $bOrange.Dispose(); $bYellow.Dispose(); $bWhite.Dispose()
}

# 360-Degree Fiery Shockwave Ring with High-Heat Arcs
function Draw-FireShockRing($gfx, [float]$cx, [float]$cy, [float]$radius, [float]$thickness, [float]$alpha) {
    if ($alpha -le 0.01 -or $radius -le 1.0) { return }
    $a = [int]($alpha * 255)

    $pOuter = New-Object System.Drawing.Pen((Clr ($a * 0.6) 255 45 0), ($thickness * 1.8))
    $pMid   = New-Object System.Drawing.Pen((Clr ($a * 0.85) 255 145 0), ($thickness * 1.1))
    $pCore  = New-Object System.Drawing.Pen((Clr $a 255 240 120), ($thickness * 0.55))

    Draw-EllipseCentered $gfx $pOuter $cx $cy $radius $radius
    Draw-EllipseCentered $gfx $pMid   $cx $cy $radius $radius
    Draw-EllipseCentered $gfx $pCore  $cx $cy $radius $radius

    $pOuter.Dispose(); $pMid.Dispose(); $pCore.Dispose()
}

# Flying Embers & Fire Sparks scattering in 360 degrees
function Draw-FireEmbers($gfx, [float]$cx, [float]$cy, [float]$radius, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $bGold = New-Object System.Drawing.SolidBrush((Clr $a 255 235 70))
    $bRed  = New-Object System.Drawing.SolidBrush((Clr ($a * 0.9) 255 70 0))

    $angles = @(10, 32, 58, 84, 108, 134, 160, 185, 212, 236, 262, 286, 310, 334, 352, 22)
    $offsets = @(1.02, 0.9, 1.12, 0.95, 1.05, 0.88, 1.14, 0.92, 1.08, 0.96, 1.15, 0.89, 1.07, 0.93, 1.1, 0.86)
    $sizes   = @(2.5, 1.8, 2.6, 2.0, 2.2, 1.8, 2.5, 2.1, 2.4, 1.9, 2.5, 2.0, 2.3, 1.8, 2.6, 1.9)

    for ($i = 0; $i -lt $angles.Count; $i++) {
        $rad = [float]$angles[$i] * [Math]::PI / 180.0
        $dist = $radius * [float]$offsets[$i]
        $sz = [float]$sizes[$i]

        $px = $cx + [Math]::Cos($rad) * $dist
        $py = $cy + [Math]::Sin($rad) * $dist

        Fill-EllipseCentered $gfx $bRed  $px $py ($sz * 1.3) ($sz * 1.3)
        Fill-EllipseCentered $gfx $bGold $px $py ($sz * 0.7) ($sz * 0.7)
    }

    $bGold.Dispose(); $bRed.Dispose()
}

# Soft Fading Heat Wisps (Soft glowing warm embers dissolving gracefully)
function Draw-HeatWisps($gfx, [float]$cx, [float]$cy, [float]$radius, [float]$alpha) {
    if ($alpha -le 0.01) { return }
    $a = [int]($alpha * 255)

    $bHaze = New-Object System.Drawing.SolidBrush((Clr ($a * 0.45) 170 50 15))
    $bDark = New-Object System.Drawing.SolidBrush((Clr ($a * 0.3) 70 30 20))

    $wisps = @(
        @(0.0, -0.65, 14.0),
        @(0.6, -0.4, 13.0),
        @(-0.6, -0.45, 13.5),
        @(0.7, 0.35, 14.0),
        @(-0.65, 0.4, 14.0),
        @(0.0, 0.7, 13.0),
        @(0.35, 0.1, 12.0),
        @(-0.35, 0.05, 12.0)
    )

    foreach ($w in $wisps) {
        $px = $cx + [float]$w[0] * $radius
        $py = $cy + [float]$w[1] * $radius
        $sz = [float]$w[2]

        Fill-EllipseCentered $gfx $bDark $px $py ($sz * 1.1) ($sz * 1.1)
        Fill-EllipseCentered $gfx $bHaze $px $py ($sz * 0.75) ($sz * 0.75)
    }

    $bHaze.Dispose(); $bDark.Dispose()
}


# ==============================================================================
# 3. BUILD FIRE SPLASH SPRITESHEET (1280x128, 10 frames)
# E107: Massive Radial Fire Nova spreading out in all directions (사방으로 퍼지는 불)
# ==============================================================================
$bmpSplash = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gSplash = [System.Drawing.Graphics]::FromImage($bmpSplash)
$gSplash.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gSplash.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gSplash.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSplash.Clear([System.Drawing.Color]::Transparent)

$centerY = 64.0

# Frame 0: Heat Detonation Point (중심점 고온 응축 & 점화)
$s0 = 0 * 128 + 64
Draw-RadialInfernoNova $gSplash $s0 $centerY 18 0 1.0 1.0
Draw-FireShockRing $gSplash $s0 $centerY 14 2.2 1.0

# Frame 1: Core Eruption & Outward Flame Surge (화염 폭발 분출)
$s1 = 1 * 128 + 64
Draw-RadialInfernoNova $gSplash $s1 $centerY 30 0 1.0 1.0
Draw-FireShockRing $gSplash $s1 $centerY 26 2.8 1.0
Draw-FireEmbers $gSplash $s1 $centerY 24 0.9

# Frame 2: Fierce Radial Blast Wave (360도 화염 파도 급팽창)
$s2 = 2 * 128 + 64
Draw-RadialInfernoNova $gSplash $s2 $centerY 45 0 1.0 1.0
Draw-FireShockRing $gSplash $s2 $centerY 38 3.2 1.0
Draw-FireEmbers $gSplash $s2 $centerY 38 1.0

# Frame 3: Peak Inferno Expansion (최대 화염 노바 분출 - 사방으로 뻗어가는 화염 날개)
$s3 = 3 * 128 + 64
Draw-RadialInfernoNova $gSplash $s3 $centerY 56 0 0.95 1.0
Draw-FireShockRing $gSplash $s3 $centerY 50 3.0 1.0
Draw-FireEmbers $gSplash $s3 $centerY 52 1.0

# Frame 4: Radial Flame Blossom (사방으로 완전히 퍼져나간 거대한 불꽃)
$s4 = 4 * 128 + 64
Draw-RadialInfernoNova $gSplash $s4 $centerY 62 0 0.8 1.0
Draw-FireShockRing $gSplash $s4 $centerY 58 2.4 0.9
Draw-FireEmbers $gSplash $s4 $centerY 60 1.0

# Frame 5: Dispersing Fiery Tendrils & Glowing Embers (불길 해체 & 화염 파편 비산)
$s5 = 5 * 128 + 64
Draw-RadialInfernoNova $gSplash $s5 $centerY 64 0 0.55 0.8
Draw-FireShockRing $gSplash $s5 $centerY 62 1.8 0.65
Draw-FireEmbers $gSplash $s5 $centerY 63 1.0

# Frame 6: Expanding Ember Halo (타오르는 잿불 & 불꽃 비산)
$s6 = 6 * 128 + 64
Draw-RadialInfernoNova $gSplash $s6 $centerY 64 0 0.25 0.45
Draw-FireEmbers $gSplash $s6 $centerY 64 0.9

# Frame 7: Fading Outer Embers (잔여 잿불 소멸 진행)
$s7 = 7 * 128 + 64
Draw-FireEmbers $gSplash $s7 $centerY 65 0.55

# Frame 8: Final Micro Ember Dust (미세 잔열 페이드아웃)
$s8 = 8 * 128 + 64
Draw-FireEmbers $gSplash $s8 $centerY 65 0.22

# Frame 9: Clean Transparent (완전 소멸)
# 100% clean transparent

$splashPath = "Assets/4. DotAsset/Effect_Fire_Splash_Nova.png"
if (Test-Path $splashPath) { [System.IO.File]::Delete($splashPath) }
$bmpSplash.Save($splashPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Rendered Fire Splash to $splashPath!"


# ==============================================================================
# 4. BUILD FIRE TARGET SPRITESHEET (1280x128, 10 frames)
# E207: Small, snappy flame spark / burst on hit (작게 불꽃 튀는 느낌)
# Dynamic tapered spark streaks flying out, finishing in ~4 frames
# ==============================================================================
$bmpTarget = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gTarget = [System.Drawing.Graphics]::FromImage($bmpTarget)
$gTarget.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gTarget.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gTarget.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gTarget.Clear([System.Drawing.Color]::Transparent)

# Helper to draw a tapered flying spark shard
function Draw-TaperedSpark($gfx, [float]$cx, [float]$cy, [float]$angleDeg, [float]$distStart, [float]$distEnd, [float]$tipWidth, $bOuter, $bCore) {
    $rad = $angleDeg * [Math]::PI / 180.0
    $perpRad = ($angleDeg + 90.0) * [Math]::PI / 180.0

    $tipX = $cx + [Math]::Cos($rad) * $distEnd
    $tipY = $cy + [Math]::Sin($rad) * $distEnd
    $tailX = $cx + [Math]::Cos($rad) * $distStart
    $tailY = $cy + [Math]::Sin($rad) * $distStart

    $headLX = $tipX + [Math]::Cos($perpRad) * ($tipWidth * 0.5)
    $headLY = $tipY + [Math]::Sin($perpRad) * ($tipWidth * 0.5)
    $headRX = $tipX - [Math]::Cos($perpRad) * ($tipWidth * 0.5)
    $headRY = $tipY - [Math]::Sin($perpRad) * ($tipWidth * 0.5)

    $pts = [System.Drawing.PointF[]]@(
        (Pt $tailX $tailY), (Pt $headLX $headLY), (Pt ($tipX + [Math]::Cos($rad) * 1.5) ($tipY + [Math]::Sin($rad) * 1.5)), (Pt $headRX $headRY)
    )
    $gfx.FillPolygon($bOuter, $pts)

    if ($bCore -ne $null) {
        $cTipX = $cx + [Math]::Cos($rad) * ($distEnd - 0.5)
        $cTipY = $cy + [Math]::Sin($rad) * ($distEnd - 0.5)
        $cTailX = $cx + [Math]::Cos($rad) * ($distStart + ($distEnd - $distStart) * 0.4)
        $cTailY = $cy + [Math]::Sin($rad) * ($distStart + ($distEnd - $distStart) * 0.4)
        $cPts = [System.Drawing.PointF[]]@(
            (Pt $cTailX $cTailY), (Pt $cTipX $cTipY)
        )
        Fill-EllipseCentered $gfx $bCore $cTipX $cTipY ($tipWidth * 0.45) ($tipWidth * 0.45)
    }
}

# Frame 0: Instant Impact Star Flare (착탄 순간 강렬한 백열 스타 플레어)
$t0 = 0 * 128 + 64
$bStarW0 = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$bStarY0 = New-Object System.Drawing.SolidBrush((Clr 240 255 230 60))
$bStarR0 = New-Object System.Drawing.SolidBrush((Clr 180 255 60 0))

Fill-EllipseCentered $gTarget $bStarR0 $t0 $centerY 12 12
Fill-EllipseCentered $gTarget $bStarY0 $t0 $centerY 7 7
Fill-EllipseCentered $gTarget $bStarW0 $t0 $centerY 3.5 3.5

$pStar0 = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.8)
$gTarget.DrawLine($pStar0, ($t0 - 13), $centerY, ($t0 + 13), $centerY)
$gTarget.DrawLine($pStar0, $t0, ($centerY - 13), $t0, ($centerY + 13))
$pStar0.Dispose(); $bStarW0.Dispose(); $bStarY0.Dispose(); $bStarR0.Dispose()

# Frame 1: Dynamic Tapered Sparks Bursting Outward (팍! 사방으로 튀는 날카로운 불꽃 파편)
$t1 = 1 * 128 + 64
$bCore1  = New-Object System.Drawing.SolidBrush((Clr 255 255 255 220))
$bSpkY1  = New-Object System.Drawing.SolidBrush((Clr 255 255 215 30))
$bSpkR1  = New-Object System.Drawing.SolidBrush((Clr 220 255 70 0))

# Mini core flash
Fill-EllipseCentered $gTarget $bSpkR1 $t1 $centerY 9 9
Fill-EllipseCentered $gTarget $bSpkY1 $t1 $centerY 5 5
Fill-EllipseCentered $gTarget $bCore1 $t1 $centerY 2.5 2.5

# 7 asymmetric dynamic flying sparks
$sparks1 = @(
    @(15.0,  5.0, 18.0, 3.2),
    @(68.0,  4.0, 16.0, 2.8),
    @(120.0, 5.0, 19.0, 3.0),
    @(172.0, 4.5, 17.0, 2.7),
    @(225.0, 5.5, 20.0, 3.2),
    @(280.0, 4.0, 16.5, 2.9),
    @(330.0, 5.0, 18.5, 3.0)
)
foreach ($sp in $sparks1) {
    Draw-TaperedSpark $gTarget $t1 $centerY $sp[0] $sp[1] $sp[2] $sp[3] $bSpkR1 $bSpkY1
}
$bCore1.Dispose(); $bSpkY1.Dispose(); $bSpkR1.Dispose()

# Frame 2: Dispersing Flying Embers (흩어지며 비산하는 작은 불똥들)
$t2 = 2 * 128 + 64
$bSpkY2 = New-Object System.Drawing.SolidBrush((Clr 210 255 200 40))
$bSpkR2 = New-Object System.Drawing.SolidBrush((Clr 170 255 80 0))

$sparks2 = @(
    @(15.0,  18.0, 25.0, 2.2),
    @(68.0,  16.0, 23.0, 1.9),
    @(120.0, 19.0, 26.0, 2.1),
    @(172.0, 17.0, 24.0, 1.8),
    @(225.0, 20.0, 27.0, 2.2),
    @(280.0, 16.5, 23.5, 1.9),
    @(330.0, 18.5, 25.5, 2.0)
)
foreach ($sp in $sparks2) {
    Draw-TaperedSpark $gTarget $t2 $centerY $sp[0] $sp[1] $sp[2] $sp[3] $bSpkR2 $bSpkY2
}
$bSpkY2.Dispose(); $bSpkR2.Dispose()

# Frame 3: Micro Spark Dust Fading (미세 잔열 스파크 소멸)
$t3 = 3 * 128 + 64
$bSpk3 = New-Object System.Drawing.SolidBrush((Clr 95 255 110 0))
$dots3 = @(
    @(15.0, 28.0, 1.4),
    @(68.0, 26.0, 1.2),
    @(120.0, 29.0, 1.3),
    @(172.0, 27.0, 1.1),
    @(225.0, 30.0, 1.4),
    @(280.0, 26.5, 1.2),
    @(330.0, 28.5, 1.3)
)
foreach ($d in $dots3) {
    $rad = [float]$d[0] * [Math]::PI / 180.0
    $dist = [float]$d[1]
    $sz = [float]$d[2]
    $px = $t3 + [Math]::Cos($rad) * $dist
    $py = $centerY + [Math]::Sin($rad) * $dist
    Fill-EllipseCentered $gTarget $bSpk3 $px $py $sz $sz
}
$bSpk3.Dispose()

# Frames 4~9: 100% Fully Transparent

$targetPath = "Assets/4. DotAsset/Effect_Fire_Target_Hit.png"
if (Test-Path $targetPath) { [System.IO.File]::Delete($targetPath) }
$bmpTarget.Save($targetPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Rendered Fire Target to $targetPath!"


# ==============================================================================
# 5. GENERATE PREVIEWS
# 1) preview_elemental_bullets.png (All 7 element bullets draft preview banner)
# 2) preview_fire_effects.png (10-frame preview of Splash & Target Fire)
# ==============================================================================

# --- Preview 1: Elemental Bullets (840x160) ---
$prevBullets = New-Object System.Drawing.Bitmap(840, 160)
$gB = [System.Drawing.Graphics]::FromImage($prevBullets)
$gB.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gB.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gB.Clear([System.Drawing.Color]::FromArgb(25, 25, 30))

function Get-Utf8Str([byte[]]$bytes) { [System.Text.Encoding]::UTF8.GetString($bytes) }

$strFire  = (Get-Utf8Str @(0xEB, 0xB6, 0x88)) + " (Fire)"
$strIce   = (Get-Utf8Str @(0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C)) + " (Ice)"
$strElec  = (Get-Utf8Str @(0xEC, 0xA0, 0x84, 0xEA, 0xB8, 0xB0)) + " (Electric)"
$strWind  = (Get-Utf8Str @(0xEB, 0xB0, 0x94, 0xEB, 0x9E, 0x8C)) + " (Wind)"
$strEarth = (Get-Utf8Str @(0xEB, 0x8C, 0x80, 0xEC, 0xA7, 0x80)) + " (Earth)"
$strLight = (Get-Utf8Str @(0xEB, 0xB9, 0x9B)) + " (Light)"
$strDark  = (Get-Utf8Str @(0xEC, 0x96, 0xB4, 0xEB, 0x91, 0xA0)) + " (Dark)"

$elements = @(
    @('Fire',        $strFire,  (Clr 255 255 85 85)),
    @('Ice',         $strIce,   (Clr 255 100 210 255)),
    @('Electricity', $strElec,  (Clr 255 255 235 59)),
    @('Wind',        $strWind,  (Clr 255 64 255 218)),
    @('Earth',       $strEarth, (Clr 255 215 140 70)),
    @('Light',       $strLight, (Clr 255 255 255 190)),
    @('Darkness',    $strDark,  (Clr 255 195 125 255))
)

$fontName = New-Object System.Drawing.Font('Malgun Gothic', 10.5, [System.Drawing.FontStyle]::Bold)
$pCardBorder = New-Object System.Drawing.Pen((Clr 80 255 255 255), 1.0)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elemName = $elements[$i][0]
    $label    = $elements[$i][1]
    $lblColor = $elements[$i][2]

    $slotX = $i * 120
    $cardRect = New-Object System.Drawing.Rectangle(($slotX + 6), 8, 108, 144)
    $bCard = New-Object System.Drawing.SolidBrush((Clr 255 35 36 42))
    $gB.FillRectangle($bCard, $cardRect)
    $gB.DrawRectangle($pCardBorder, $cardRect)
    $bCard.Dispose()

    # Draw Bullet centered at (slotX + 60, 60)
    Draw-ElementalBullet $gB ($slotX + 60) 60 $elemName 1.25

    # Label text
    $bLabel = New-Object System.Drawing.SolidBrush($lblColor)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $gB.DrawString($label, $fontName, $bLabel, [float]($slotX + 60), 114.0, $sf)
    $bLabel.Dispose(); $sf.Dispose()
}

$fontName.Dispose(); $pCardBorder.Dispose()

$previewBulletsPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_elemental_bullets.png"
if (Test-Path $previewBulletsPath) { [System.IO.File]::Delete($previewBulletsPath) }
$prevBullets.Save($previewBulletsPath, [System.Drawing.Imaging.ImageFormat]::Png)
$gB.Dispose(); $prevBullets.Dispose()
Write-Output "Saved Elemental Bullets preview to $previewBulletsPath!"


# --- Preview 2: Fire Effects 10-Frame (1280x256) ---
$previewFire = New-Object System.Drawing.Bitmap(1280, 256)
$gPrev = [System.Drawing.Graphics]::FromImage($previewFire)
$gPrev.Clear([System.Drawing.Color]::FromArgb(25, 25, 30))

for ($i = 0; $i -lt 10; $i++) {
    $srcRect = New-Object System.Drawing.Rectangle(($i * 128), 0, 128, 128)
    $dstRectTop = New-Object System.Drawing.Rectangle(($i * 128), 0, 128, 128)
    $dstRectBot = New-Object System.Drawing.Rectangle(($i * 128), 128, 128, 128)
    $gPrev.DrawImage($bmpSplash, $dstRectTop, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $gPrev.DrawImage($bmpTarget, $dstRectBot, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
}

$previewFirePath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_fire_effects.png"
if (Test-Path $previewFirePath) { [System.IO.File]::Delete($previewFirePath) }
$previewFire.Save($previewFirePath, [System.Drawing.Imaging.ImageFormat]::Png)

$gSplash.Dispose(); $bmpSplash.Dispose()
$gTarget.Dispose(); $bmpTarget.Dispose()
$gPrev.Dispose(); $previewFire.Dispose()
Write-Output "Saved Fire Effects preview to $previewFirePath!"
