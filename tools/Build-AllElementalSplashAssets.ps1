# ==============================================================================
# Build-AllElementalSplashAssets.ps1
# Full Pipeline for 5 Elemental Splash Towers:
#   109: Electricity (Electric)
#   110: Wind (Wind)
#   111: Earth (Earth)
#   112: Light (Light)
#   113: Darkness (Dark)
#
# Generates:
#   1. 5 Splash Projectiles (Spritesheet, Meta, Anim, Controller, Prefab)
#   2. 5 Splash Hit Effects (Spritesheet, Meta, Anim, Controller, Prefab, EffectLibrary)
#   3. High-Res Showcase Previews for both Projectiles and Hit Effects
#   4. Complete 100% GUID chain verification
# ==============================================================================

Add-Type -AssemblyName System.Drawing

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $a = [int]([Math]::Min(255, [Math]::Max(0, $a)))
    $r = [int]([Math]::Min(255, [Math]::Max(0, $r)))
    $g = [int]([Math]::Min(255, [Math]::Max(0, $g)))
    $b = [int]([Math]::Min(255, [Math]::Max(0, $b)))
    [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2), ($ry * 2))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2), ($ry * 2))
}

function Draw-Diamond($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $pts = [System.Drawing.PointF[]]@(
        (Pt $cx ($cy - $ry)),
        (Pt ($cx + $rx) $cy),
        (Pt $cx ($cy + $ry)),
        (Pt ($cx - $rx) $cy)
    )
    $gfx.FillPolygon($brush, $pts)
}

function Draw-RotatedDiamond($gfx, $brush, [float]$cx, [float]$cy, [float]$len, [float]$width, [float]$angle) {
    $cosA = [Math]::Cos($angle); $sinA = [Math]::Sin($angle)
    $cosP = [Math]::Cos($angle + [Math]::PI / 2.0); $sinP = [Math]::Sin($angle + [Math]::PI / 2.0)
    $pts = [System.Drawing.PointF[]]@(
        (Pt ($cx + $cosA * $len) ($cy + $sinA * $len)),
        (Pt ($cx + $cosP * $width) ($cy + $sinP * $width)),
        (Pt ($cx - $cosA * $len) ($cy - $sinA * $len)),
        (Pt ($cx - $cosP * $width) ($cy - $sinP * $width))
    )
    $gfx.FillPolygon($brush, $pts)
}

# ==============================================================================
# SECTION 1: PROJECTILE DRAWING FUNCTIONS (6 Frames, 768x128, Cell 128x128)
# ==============================================================================

function Draw-GenericComet($gfx, [float]$cellX, [float]$cellY, [int]$f, [int]$totalF, [hashtable]$p) {
    $phase = ($f / [float]$totalF) * 2.0 * [Math]::PI
    $cy = $cellY + 64.0
    $sphereX = $cellX + 76.0
    $radius = 15.0

    $tailLen1 = 58.0 + [Math]::Sin($phase * 2.0) * 5.0
    $tailLen2 = 36.0 + [Math]::Cos($phase * 2.0) * 4.0
    $wWave1 = [Math]::Sin($phase) * 2.2
    $wWave2 = [Math]::Cos($phase) * 2.2

    # 1. Outer Tail
    $tailPtsOuter = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 14.0)),
        (Pt ($sphereX - 22.0) ($cy - 12.0 + $wWave1)),
        (Pt ($sphereX - $tailLen2) ($cy - 7.0 + $wWave2)),
        (Pt ($sphereX - $tailLen1) ($cy + [Math]::Sin($phase) * 3.0)),
        (Pt ($sphereX - $tailLen2) ($cy + 7.0 + $wWave1)),
        (Pt ($sphereX - 22.0) ($cy + 12.0 + $wWave2)),
        (Pt ($sphereX - 2.0) ($cy + 14.0))
    )
    $bOuter = New-Object System.Drawing.SolidBrush($p.TailOuter)
    $gfx.FillPolygon($bOuter, $tailPtsOuter)
    $bOuter.Dispose()

    # 2. Mid Tail
    $oLen1 = $tailLen1 * 0.76
    $oLen2 = $tailLen2 * 0.72
    $tailPtsMid = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 9.5)),
        (Pt ($sphereX - 18.0) ($cy - 7.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - $oLen2) ($cy - 4.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - $oLen1) ($cy + [Math]::Sin($phase) * 2.0)),
        (Pt ($sphereX - $oLen2) ($cy + 4.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - 18.0) ($cy + 7.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - 2.0) ($cy + 9.5))
    )
    $bMid = New-Object System.Drawing.SolidBrush($p.TailMid)
    $gfx.FillPolygon($bMid, $tailPtsMid)
    $bMid.Dispose()

    # 3. Inner Core Streak
    $yLen = $tailLen1 * 0.48
    $tailPtsCore = [System.Drawing.PointF[]]@(
        (Pt ($sphereX) ($cy - 5.5)),
        (Pt ($sphereX - 14.0) ($cy - 3.5 + $wWave1 * 0.4)),
        (Pt ($sphereX - $yLen) ($cy + [Math]::Sin($phase) * 1.0)),
        (Pt ($sphereX - 14.0) ($cy + 3.5 + $wWave2 * 0.4)),
        (Pt ($sphereX) ($cy + 5.5))
    )
    $bCore = New-Object System.Drawing.SolidBrush($p.TailCore)
    $gfx.FillPolygon($bCore, $tailPtsCore)
    $bCore.Dispose()

    # 4. Trailing Filament Lines
    $pWhisp1 = New-Object System.Drawing.Pen($p.Whisp1, 1.8)
    $pWhisp2 = New-Object System.Drawing.Pen($p.Whisp2, 1.6)
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy - 4.0), ($sphereX - $tailLen1 - 6.0), ($cy - 3.0 + $wWave1))
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy + 4.0), ($sphereX - $tailLen1 - 6.0), ($cy + 3.0 + $wWave2))
    $gfx.DrawLine($pWhisp2, ($sphereX - 10.0), $cy, ($sphereX - $tailLen1 - 12.0), $cy)
    $pWhisp1.Dispose(); $pWhisp2.Dispose()

    # 5. Trailing Looping Sparkles
    $bSpk1 = New-Object System.Drawing.SolidBrush($p.Spark1)
    $bSpk2 = New-Object System.Drawing.SolidBrush($p.Spark2)
    $sparkData = @(
        @(0.15, -7.0, 2.3),
        @(0.35,  6.5, 2.0),
        @(0.55, -4.0, 2.5),
        @(0.75,  5.0, 1.9),
        @(0.92, -1.0, 1.7)
    )
    foreach ($spk in $sparkData) {
        $loopProgress = ($spk[0] + ($f / [float]$totalF)) % 1.0
        $spkX = $sphereX - 10.0 - ($loopProgress * 54.0)
        $spkY = $cy + [float]$spk[1] + [Math]::Sin($loopProgress * 4.0 + $phase) * 2.0
        $sz   = [float]$spk[2] * (1.1 - $loopProgress * 0.4)

        if ($p.SparkStyle -eq 'Diamond') {
            Draw-Diamond $gfx $bSpk2 $spkX $spkY ($sz * 1.4) ($sz * 1.4)
            Draw-Diamond $gfx $bSpk1 $spkX $spkY ($sz * 0.75) ($sz * 0.75)
        } else {
            Fill-EllipseCentered $gfx $bSpk2 $spkX $spkY ($sz * 1.4) ($sz * 1.4)
            Fill-EllipseCentered $gfx $bSpk1 $spkX $spkY ($sz * 0.75) ($sz * 0.75)
        }
    }
    $bSpk1.Dispose(); $bSpk2.Dispose()

    # 6. Sphere Head
    $bAura = New-Object System.Drawing.SolidBrush($p.Halo)
    Fill-EllipseCentered $gfx $bAura $sphereX $cy 19.5 19.5
    $bAura.Dispose()

    $pBowShock = New-Object System.Drawing.Pen($p.BowShock, 2.2)
    $gfx.DrawArc($pBowShock, ($sphereX - 19.5), ($cy - 19.5), 39.0, 39.0, -70.0, 140.0)
    $pBowShock.Dispose()

    $bSphereBody = New-Object System.Drawing.SolidBrush($p.Body)
    Fill-EllipseCentered $gfx $bSphereBody $sphereX $cy $radius $radius
    $bSphereBody.Dispose()

    $bSphereVol = New-Object System.Drawing.SolidBrush($p.BodyVol)
    Fill-EllipseCentered $gfx $bSphereVol ($sphereX + 2.5) $cy 11.0 11.0
    $bSphereVol.Dispose()

    $bCoreGlow = New-Object System.Drawing.SolidBrush($p.CenterCore)
    Fill-EllipseCentered $gfx $bCoreGlow ($sphereX + 4.5) $cy 6.5 6.5
    $bCoreGlow.Dispose()

    $bHotSpot = New-Object System.Drawing.SolidBrush($p.HotSpot)
    Fill-EllipseCentered $gfx $bHotSpot ($sphereX + 6.0) $cy 3.5 3.5
    $bHotSpot.Dispose()

    # Specular Glint
    if ($p.HasCross) {
        $pCross = New-Object System.Drawing.Pen($p.HotSpot, 1.2)
        $gfx.DrawLine($pCross, ($sphereX + 6.0 - 2.5), $cy, ($sphereX + 6.0 + 2.5), $cy)
        $gfx.DrawLine($pCross, ($sphereX + 6.0), ($cy - 2.5), ($sphereX + 6.0), ($cy + 2.5))
        $pCross.Dispose()
    }
}

function Get-ProjectilePalette([int]$id) {
    switch ($id) {
        109 { # Electricity
            return @{
                TailOuter  = Clr 180 210 160 0
                TailMid    = Clr 240 255 215 20
                TailCore   = Clr 255 255 255 180
                Whisp1     = Clr 180 255 235 50
                Whisp2     = Clr 150 220 180 0
                Spark1     = Clr 255 255 255 255
                Spark2     = Clr 220 255 210 0
                SparkStyle = 'Circle'
                Halo       = Clr 110 255 210 0
                BowShock   = Clr 210 255 250 160
                Body       = Clr 255 240 180 0
                BodyVol    = Clr 255 255 220 40
                CenterCore = Clr 255 255 255 200
                HotSpot    = Clr 255 255 255 255
                HasCross   = $true
            }
        }
        110 { # Wind
            return @{
                TailOuter  = Clr 170 0 160 130
                TailMid    = Clr 235 0 230 185
                TailCore   = Clr 255 180 255 235
                Whisp1     = Clr 170 40 240 190
                Whisp2     = Clr 140 0 170 140
                Spark1     = Clr 255 240 255 250
                Spark2     = Clr 200 0 210 170
                SparkStyle = 'Circle'
                Halo       = Clr 105 0 220 170
                BowShock   = Clr 200 180 255 240
                Body       = Clr 255 0 180 145
                BodyVol    = Clr 255 30 235 190
                CenterCore = Clr 255 190 255 240
                HotSpot    = Clr 255 255 255 255
                HasCross   = $false
            }
        }
        111 { # Earth
            return @{
                TailOuter  = Clr 180 130 65 20
                TailMid    = Clr 235 195 110 35
                TailCore   = Clr 255 250 205 130
                Whisp1     = Clr 170 190 110 40
                Whisp2     = Clr 140 140 70 20
                Spark1     = Clr 255 255 235 180
                Spark2     = Clr 220 190 95 30
                SparkStyle = 'Diamond'
                Halo       = Clr 110 160 85 25
                BowShock   = Clr 200 240 190 120
                Body       = Clr 255 140 68 20
                BodyVol    = Clr 255 195 110 40
                CenterCore = Clr 255 255 210 130
                HotSpot    = Clr 255 255 255 240
                HasCross   = $false
            }
        }
        112 { # Light
            return @{
                TailOuter  = Clr 170 230 200 80
                TailMid    = Clr 240 255 245 150
                TailCore   = Clr 255 255 255 255
                Whisp1     = Clr 180 255 245 140
                Whisp2     = Clr 150 235 200 80
                Spark1     = Clr 255 255 255 255
                Spark2     = Clr 220 255 230 120
                SparkStyle = 'Diamond'
                Halo       = Clr 115 255 235 100
                BowShock   = Clr 220 255 255 220
                Body       = Clr 255 245 210 90
                BodyVol    = Clr 255 255 245 160
                CenterCore = Clr 255 255 255 255
                HotSpot    = Clr 255 255 255 255
                HasCross   = $true
            }
        }
        113 { # Darkness (Void Black Hole)
            return @{
                TailOuter  = Clr 170 80 15 140
                TailMid    = Clr 235 155 35 215
                TailCore   = Clr 255 230 120 255
                Whisp1     = Clr 180 180 60 255
                Whisp2     = Clr 150 110 20 170
                Spark1     = Clr 255 245 190 255
                Spark2     = Clr 220 150 40 220
                SparkStyle = 'Diamond'
                Halo       = Clr 130 130 25 190
                BowShock   = Clr 210 230 140 255
                Body       = Clr 255 30 10 50
                BodyVol    = Clr 255 120 25 170
                CenterCore = Clr 255 220 90 255
                HotSpot    = Clr 255 15 5 25
                HasCross   = $false
            }
        }
    }
}


# ==============================================================================
# SECTION 2: HIT EFFECT DRAWING FUNCTIONS (10 Frames, 1280x128, Cell 128x128)
# ==============================================================================

# --- E109: Thunder Nova (대형 뇌전 폭뢰 방전) ---
function Draw-HitEffect-109($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }
    $axes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { $_ * [Math]::PI / 3.0 }

    if ($f -eq 0) {
        # Vertical lightning strike flash + core explosion
        $bFlash = New-Object System.Drawing.SolidBrush((Clr 160 255 240 80))
        Fill-EllipseCentered $gfx $bFlash $cx $cy 18.0 18.0
        $bFlash.Dispose()

        $pBolt = New-Object System.Drawing.Pen((Clr 255 255 255 255), 3.5)
        $gfx.DrawLine($pBolt, $cx, ($cy - 52), $cx, ($cy + 52))
        $pBolt.Dispose()
    } elseif ($f -eq 1) {
        # 6-way jagged lightning branches erupting
        $bAura = New-Object System.Drawing.SolidBrush((Clr 100 255 220 30))
        Fill-EllipseCentered $gfx $bAura $cx $cy 28.0 28.0
        $bAura.Dispose()

        $pFork = New-Object System.Drawing.Pen((Clr 255 255 250 180), 2.2)
        foreach ($a in $axes) {
            $x1 = $cx + [Math]::Cos($a) * 12.0; $y1 = $cy + [Math]::Sin($a) * 12.0
            $x2 = $cx + [Math]::Cos($a + 0.25) * 24.0; $y2 = $cy + [Math]::Sin($a + 0.25) * 24.0
            $x3 = $cx + [Math]::Cos($a - 0.15) * 36.0; $y3 = $cy + [Math]::Sin($a - 0.15) * 36.0
            $gfx.DrawLine($pFork, $cx, $cy, $x1, $y1)
            $gfx.DrawLine($pFork, $x1, $y1, $x2, $y2)
            $gfx.DrawLine($pFork, $x2, $y2, $x3, $y3)
        }
        $pFork.Dispose()

        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy 7.0 7.0
        $bCore.Dispose()
    } elseif ($f -eq 2) {
        # Maximum bloom: expanding electric shockwave ring + crackling arcs
        $pRing = New-Object System.Drawing.Pen((Clr 220 255 235 60), 2.0)
        $gfx.DrawEllipse($pRing, ($cx - 38.0), ($cy - 38.0), 76.0, 76.0)
        $pRing.Dispose()

        $pArc = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.6)
        foreach ($a in $axes) {
            $x1 = $cx + [Math]::Cos($a) * 20.0; $y1 = $cy + [Math]::Sin($a) * 20.0
            $x2 = $cx + [Math]::Cos($a - 0.3) * 38.0; $y2 = $cy + [Math]::Sin($a - 0.3) * 38.0
            $gfx.DrawLine($pArc, $x1, $y1, $x2, $y2)
        }
        $pArc.Dispose()
    } elseif ($f -eq 3) {
        # Shatter into radial electric spark nodes
        $bSpark = New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))
        foreach ($a in $axes) {
            for ($k = 0; $k -lt 2; $k++) {
                $ang = $a + ($k * 0.4 - 0.2)
                $sx = $cx + [Math]::Cos($ang) * 44.0
                $sy = $cy + [Math]::Sin($ang) * 44.0
                Draw-Diamond $gfx $bSpark $sx $sy 2.8 2.8
            }
        }
        $bSpark.Dispose()
    } elseif ($f -eq 4) {
        # Dispersing lightning sparks at 50px
        $bFade = New-Object System.Drawing.SolidBrush((Clr 190 255 230 60))
        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a + 0.1) * 50.0
            $sy = $cy + [Math]::Sin($a + 0.1) * 50.0
            Fill-EllipseCentered $gfx $bFade $sx $sy 2.0 2.0
        }
        $bFade.Dispose()
    } elseif ($f -ge 5) {
        $bDust = New-Object System.Drawing.SolidBrush((Clr 120 255 240 120))
        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a) * 52.0
            $sy = $cy + [Math]::Sin($a) * 52.0
            Fill-EllipseCentered $gfx $bDust $sx $sy 1.2 1.2
        }
        $bDust.Dispose()
    }
}

# --- E110: Typhoon Vortex (태풍 소용돌이 파열) ---
function Draw-HitEffect-110($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }
    $blades = @(0, 1, 2, 3) | ForEach-Object { $_ * [Math]::PI / 2.0 }

    if ($f -eq 0) {
        # Center vacuum implosion + teal flash
        $bFlash = New-Object System.Drawing.SolidBrush((Clr 160 0 240 190))
        Fill-EllipseCentered $gfx $bFlash $cx $cy 16.0 16.0
        $bFlash.Dispose()
    } elseif ($f -eq 1) {
        # 4 rotating curved cyclone blades
        $bBlade = New-Object System.Drawing.SolidBrush((Clr 240 0 230 185))
        foreach ($a in $blades) {
            $rot = $a + 0.4
            Draw-RotatedDiamond $gfx $bBlade ($cx + [Math]::Cos($rot) * 16.0) ($cy + [Math]::Sin($rot) * 16.0) 18.0 5.5 ($rot + 0.8)
        }
        $bBlade.Dispose()

        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 200 255 245))
        Fill-EllipseCentered $gfx $bCore $cx $cy 8.0 8.0
        $bCore.Dispose()
    } elseif ($f -eq 2) {
        # Full Bloom: giant spinning cyclone ring & shockwave
        $pRing = New-Object System.Drawing.Pen((Clr 200 120 255 230), 2.2)
        $gfx.DrawEllipse($pRing, ($cx - 36.0), ($cy - 36.0), 72.0, 72.0)
        $pRing.Dispose()

        $bBlade = New-Object System.Drawing.SolidBrush((Clr 245 0 240 200))
        foreach ($a in $blades) {
            $rot = $a + 0.9
            Draw-RotatedDiamond $gfx $bBlade ($cx + [Math]::Cos($rot) * 32.0) ($cy + [Math]::Sin($rot) * 32.0) 22.0 6.0 ($rot + 1.1)
        }
        $bBlade.Dispose()
    } elseif ($f -eq 3) {
        # Expanding slicing wind crescents
        $bCrescent = New-Object System.Drawing.SolidBrush((Clr 220 180 255 240))
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0 + 1.4
            Draw-RotatedDiamond $gfx $bCrescent ($cx + [Math]::Cos($ang) * 44.0) ($cy + [Math]::Sin($ang) * 44.0) 12.0 3.2 ($ang + 1.2)
        }
        $bCrescent.Dispose()
    } elseif ($f -eq 4) {
        # Dispersing breeze motes
        $bMote = New-Object System.Drawing.SolidBrush((Clr 180 80 245 220))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0
            Fill-EllipseCentered $gfx $bMote ($cx + [Math]::Cos($ang) * 50.0) ($cy + [Math]::Sin($ang) * 50.0) 2.0 2.0
        }
        $bMote.Dispose()
    } elseif ($f -ge 5) {
        $bDust = New-Object System.Drawing.SolidBrush((Clr 110 0 210 180))
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0
            Fill-EllipseCentered $gfx $bDust ($cx + [Math]::Cos($ang) * 52.0) ($cy + [Math]::Sin($ang) * 52.0) 1.2 1.2
        }
        $bDust.Dispose()
    }
}

# --- E111: Earth Quake (대지 진동 및 암반 파쇄) ---
function Draw-HitEffect-111($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }
    $axes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { $_ * [Math]::PI / 3.0 }

    if ($f -eq 0) {
        # Heavy impact shockwave
        $bShock = New-Object System.Drawing.SolidBrush((Clr 180 180 90 30))
        Fill-EllipseCentered $gfx $bShock $cx $cy 18.0 18.0
        $bShock.Dispose()
    } elseif ($f -eq 1) {
        # 6 sharp rising rock spikes
        $bRock = New-Object System.Drawing.SolidBrush((Clr 245 160 80 25))
        foreach ($a in $axes) {
            Draw-RotatedDiamond $gfx $bRock ($cx + [Math]::Cos($a) * 16.0) ($cy + [Math]::Sin($a) * 16.0) 16.0 6.5 $a
        }
        $bRock.Dispose()

        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 240 180 80))
        Fill-EllipseCentered $gfx $bCore $cx $cy 8.0 8.0
        $bCore.Dispose()
    } elseif ($f -eq 2) {
        # Full bloom: giant ground fracture ring & flying boulders
        $pRing = New-Object System.Drawing.Pen((Clr 210 210 120 40), 2.2)
        $gfx.DrawEllipse($pRing, ($cx - 36.0), ($cy - 36.0), 72.0, 72.0)
        $pRing.Dispose()

        $bBoulder = New-Object System.Drawing.SolidBrush((Clr 255 190 100 30))
        foreach ($a in $axes) {
            Draw-RotatedDiamond $gfx $bBoulder ($cx + [Math]::Cos($a) * 34.0) ($cy + [Math]::Sin($a) * 34.0) 14.0 6.0 ($a + 0.3)
        }
        $bBoulder.Dispose()
    } elseif ($f -eq 3) {
        # Shatter into tumbling jagged rock fragments
        $bFrag = New-Object System.Drawing.SolidBrush((Clr 220 220 140 60))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0
            Draw-Diamond $gfx $bFrag ($cx + [Math]::Cos($ang) * 44.0) ($cy + [Math]::Sin($ang) * 44.0) 3.5 3.0
        }
        $bFrag.Dispose()
    } elseif ($f -eq 4) {
        # Dispersing rock motes & dust
        $bDust = New-Object System.Drawing.SolidBrush((Clr 180 180 110 50))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0
            Fill-EllipseCentered $gfx $bDust ($cx + [Math]::Cos($ang) * 50.0) ($cy + [Math]::Sin($ang) * 50.0) 2.2 2.2
        }
        $bDust.Dispose()
    } elseif ($f -ge 5) {
        $bFaint = New-Object System.Drawing.SolidBrush((Clr 110 140 85 35))
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0
            Fill-EllipseCentered $gfx $bFaint ($cx + [Math]::Cos($ang) * 52.0) ($cy + [Math]::Sin($ang) * 52.0) 1.2 1.2
        }
        $bFaint.Dispose()
    }
}

# --- E112: Supernova Burst (초신성 섬광 및 성광 방사) ---
function Draw-HitEffect-112($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }
    $axes8 = @(0, 1, 2, 3, 4, 5, 6, 7) | ForEach-Object { $_ * [Math]::PI / 4.0 }

    if ($f -eq 0) {
        # Blinding solar seed flash
        $bFlash = New-Object System.Drawing.SolidBrush((Clr 180 255 245 140))
        Fill-EllipseCentered $gfx $bFlash $cx $cy 18.0 18.0
        $bFlash.Dispose()

        $pCross = New-Object System.Drawing.Pen((Clr 255 255 255 255), 2.8)
        $gfx.DrawLine($pCross, ($cx - 40), $cy, ($cx + 40), $cy)
        $gfx.DrawLine($pCross, $cx, ($cy - 40), $cx, ($cy + 40))
        $pCross.Dispose()
    } elseif ($f -eq 1) {
        # 8 radiant holy ray spears
        $bRay = New-Object System.Drawing.SolidBrush((Clr 255 255 250 180))
        foreach ($a in $axes8) {
            Draw-RotatedDiamond $gfx $bRay ($cx + [Math]::Cos($a) * 18.0) ($cy + [Math]::Sin($a) * 18.0) 18.0 5.0 $a
        }
        $bRay.Dispose()

        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy 9.0 9.0
        $bCore.Dispose()
    } elseif ($f -eq 2) {
        # Maximum bloom: prismatic shockwave ring & 8 giant star spears
        $pRing = New-Object System.Drawing.Pen((Clr 220 255 240 100), 2.2)
        $gfx.DrawEllipse($pRing, ($cx - 36.0), ($cy - 36.0), 72.0, 72.0)
        $pRing.Dispose()

        $bBigRay = New-Object System.Drawing.SolidBrush((Clr 255 255 255 220))
        foreach ($a in $axes8) {
            Draw-RotatedDiamond $gfx $bBigRay ($cx + [Math]::Cos($a) * 34.0) ($cy + [Math]::Sin($a) * 34.0) 16.0 5.5 $a
        }
        $bBigRay.Dispose()
    } elseif ($f -eq 3) {
        # Shatter into 16 sparkling solar crystals
        $bStar = New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))
        for ($k = 0; $k -lt 16; $k++) {
            $ang = $k * [Math]::PI / 8.0
            Draw-Diamond $gfx $bStar ($cx + [Math]::Cos($ang) * 44.0) ($cy + [Math]::Sin($ang) * 44.0) 3.2 3.2
        }
        $bStar.Dispose()
    } elseif ($f -eq 4) {
        # Dispersing holy glitter
        $bGlitter = New-Object System.Drawing.SolidBrush((Clr 200 255 245 160))
        for ($k = 0; $k -lt 16; $k++) {
            $ang = $k * [Math]::PI / 8.0
            Fill-EllipseCentered $gfx $bGlitter ($cx + [Math]::Cos($ang) * 50.0) ($cy + [Math]::Sin($ang) * 50.0) 2.0 2.0
        }
        $bGlitter.Dispose()
    } elseif ($f -ge 5) {
        $bFaint = New-Object System.Drawing.SolidBrush((Clr 120 255 240 120))
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0
            Fill-EllipseCentered $gfx $bFaint ($cx + [Math]::Cos($ang) * 52.0) ($cy + [Math]::Sin($ang) * 52.0) 1.2 1.2
        }
        $bFaint.Dispose()
    }
}

# --- E113: Black Hole Collapse (블랙홀 붕괴 & 중력장 폭발) ---
function Draw-HitEffect-113($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }

    if ($f -eq 0) {
        # F0: Gravitational distortion & purple space warp
        $bAura = New-Object System.Drawing.SolidBrush((Clr 160 140 20 210))
        Fill-EllipseCentered $gfx $bAura $cx $cy 18.0 18.0
        $bAura.Dispose()

        # Inward spiral distortion lines
        $pWarp = New-Object System.Drawing.Pen((Clr 240 220 90 255), 1.8)
        for ($i = 0; $i -lt 4; $i++) {
            $ang = $i * [Math]::PI / 2.0
            $gfx.DrawArc($pWarp, ($cx - 24.0), ($cy - 24.0), 48.0, 48.0, [float]($ang * 180 / [Math]::PI), 80.0)
        }
        $pWarp.Dispose()

        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 10 2 20))
        Fill-EllipseCentered $gfx $bCore $cx $cy 6.0 6.0
        $bCore.Dispose()
    } elseif ($f -eq 1) {
        # F1: Accretion Disk Swirl & Growing Event Horizon
        $bAccretion = New-Object System.Drawing.SolidBrush((Clr 220 190 40 255))
        for ($i = 0; $i -lt 4; $i++) {
            $ang = $i * [Math]::PI / 2.0 + 0.5
            Draw-RotatedDiamond $gfx $bAccretion ($cx + [Math]::Cos($ang) * 18.0) ($cy + [Math]::Sin($ang) * 18.0) 18.0 6.0 ($ang + 1.2)
        }
        $bAccretion.Dispose()

        # Obsidian Event Horizon Core
        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 5 0 12))
        Fill-EllipseCentered $gfx $bVoid $cx $cy 11.0 11.0
        $bVoid.Dispose()

        $pHorizon = New-Object System.Drawing.Pen((Clr 255 240 120 255), 1.6)
        Draw-EllipseCentered $gfx $pHorizon $cx $cy 11.0 11.0
        $pHorizon.Dispose()
    } elseif ($f -eq 2) {
        # F2: MAXIMUM EVENT HORIZON & GRAVITATIONAL LENSING
        # Outer purple cosmic halo
        $bHalo = New-Object System.Drawing.SolidBrush((Clr 90 120 10 190))
        Fill-EllipseCentered $gfx $bHalo $cx $cy 40.0 40.0
        $bHalo.Dispose()

        # Expanding Neon Purple Gravitational Shockwave Ring
        $pWave = New-Object System.Drawing.Pen((Clr 240 220 80 255), 2.4)
        $gfx.DrawEllipse($pWave, ($cx - 36.0), ($cy - 36.0), 72.0, 72.0)
        $pWave.Dispose()

        # Neon Accretion Arms
        $bArm = New-Object System.Drawing.SolidBrush((Clr 255 170 30 240))
        for ($i = 0; $i -lt 4; $i++) {
            $ang = $i * [Math]::PI / 2.0 + 1.0
            Draw-RotatedDiamond $gfx $bArm ($cx + [Math]::Cos($ang) * 30.0) ($cy + [Math]::Sin($ang) * 30.0) 20.0 6.5 ($ang + 1.3)
        }
        $bArm.Dispose()

        # Deep Pitch-Black Singularity
        $bSing = New-Object System.Drawing.SolidBrush((Clr 255 2 0 8))
        Fill-EllipseCentered $gfx $bSing $cx $cy 15.0 15.0
        $bSing.Dispose()

        # Incandescent Photon Ring (Edge of No Return)
        $pPhoton = New-Object System.Drawing.Pen((Clr 255 255 160 255), 2.0)
        Draw-EllipseCentered $gfx $pPhoton $cx $cy 15.0 15.0
        $pPhoton.Dispose()
    } elseif ($f -eq 3) {
        # F3: SINGULARITY COLLAPSE & TIDAL BURST
        $pShock = New-Object System.Drawing.Pen((Clr 220 180 40 255), 2.0)
        $gfx.DrawEllipse($pShock, ($cx - 42.0), ($cy - 42.0), 84.0, 84.0)
        $pShock.Dispose()

        # Outward blasted cosmic void shards
        $bShard = New-Object System.Drawing.SolidBrush((Clr 255 220 100 255))
        for ($i = 0; $i -lt 8; $i++) {
            $ang = $i * [Math]::PI / 4.0 + 0.3
            Draw-RotatedDiamond $gfx $bShard ($cx + [Math]::Cos($ang) * 40.0) ($cy + [Math]::Sin($ang) * 40.0) 12.0 4.2 ($ang + 0.8)
        }
        $bShard.Dispose()

        # Shrunken intense singularity dot
        $bDot = New-Object System.Drawing.SolidBrush((Clr 255 5 0 10))
        Fill-EllipseCentered $gfx $bDot $cx $cy 8.0 8.0
        $bDot.Dispose()
    } elseif ($f -eq 4) {
        # F4: Dispersing Void Shards & Spinning Gravity Motes
        $bMote = New-Object System.Drawing.SolidBrush((Clr 200 190 70 255))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0
            Draw-Diamond $gfx $bMote ($cx + [Math]::Cos($ang) * 48.0) ($cy + [Math]::Sin($ang) * 48.0) 3.0 3.0
        }
        $bMote.Dispose()
    } elseif ($f -eq 5) {
        # F5: Fading Ultraviolet Quantum Sparks
        $bSpark = New-Object System.Drawing.SolidBrush((Clr 160 160 50 230))
        for ($k = 0; $k -lt 12; $k++) {
            $ang = $k * [Math]::PI / 6.0
            Fill-EllipseCentered $gfx $bSpark ($cx + [Math]::Cos($ang) * 51.0) ($cy + [Math]::Sin($ang) * 51.0) 1.8 1.8
        }
        $bSpark.Dispose()
    } elseif ($f -ge 6) {
        # F6: Final evaporating void mist
        $bMist = New-Object System.Drawing.SolidBrush((Clr 90 120 20 180))
        for ($k = 0; $k -lt 8; $k++) {
            $ang = $k * [Math]::PI / 4.0
            Fill-EllipseCentered $gfx $bMist ($cx + [Math]::Cos($ang) * 52.0) ($cy + [Math]::Sin($ang) * 52.0) 1.2 1.2
        }
        $bMist.Dispose()
    }
}


# ==============================================================================
# SECTION 3: PIPELINE EXECUTION FOR ALL 5 ELEMENTS
# ==============================================================================

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"
$elements = @(
    [pscustomobject]@{ ID = 109; Name = "Electric"; TexProj = "Projectile_Meteor_Electric"; TexEff = "Effect_Electric_Splash_Thunder"; EffDraw = { param($g, $cx, $cy, $f) Draw-HitEffect-109 $g $cx $cy $f } },
    [pscustomobject]@{ ID = 110; Name = "Wind";     TexProj = "Projectile_Meteor_Wind";     TexEff = "Effect_Wind_Splash_Typhoon";     EffDraw = { param($g, $cx, $cy, $f) Draw-HitEffect-110 $g $cx $cy $f } },
    [pscustomobject]@{ ID = 111; Name = "Earth";    TexProj = "Projectile_Meteor_Earth";    TexEff = "Effect_Earth_Splash_Quake";       EffDraw = { param($g, $cx, $cy, $f) Draw-HitEffect-111 $g $cx $cy $f } },
    [pscustomobject]@{ ID = 112; Name = "Light";    TexProj = "Projectile_Meteor_Light";    TexEff = "Effect_Light_Splash_Supernova";   EffDraw = { param($g, $cx, $cy, $f) Draw-HitEffect-112 $g $cx $cy $f } },
    [pscustomobject]@{ ID = 113; Name = "Dark";     TexProj = "Projectile_Meteor_Dark";     TexEff = "Effect_Dark_Splash_BlackHole";   EffDraw = { param($g, $cx, $cy, $f) Draw-HitEffect-113 $g $cx $cy $f } }
)

Write-Output "=================================================="
Write-Output "Building 5 Elemental Splash Towers (109..113)..."
Write-Output "=================================================="

foreach ($elem in $elements) {
    $id = $elem.ID
    $eName = $elem.Name
    $projTexName = $elem.TexProj
    $effTexName = $elem.TexEff

    Write-Output "`n>>> Processing [$($id): $eName]..."

    # GUIDs (Strict 32-hex characters)
    $projTexGuid  = "7a0701" + $id + "0000000000000000000" + $id + "b"
    $projAnimGuid = "7a0701" + $id + "0000000000000000000" + $id + "a"
    $projCtrlGuid = "7a0701" + $id + "0000000000000000000" + $id + "c"

    $effTexGuid   = "8a0701" + $id + "0000000000000000000" + $id + "e"
    $effAnimGuid  = "7a0701" + $id + "0000000000000000000" + $id + "e"
    $effCtrlGuid  = "6a0701" + $id + "0000000000000000000" + $id + "e"
    $effPrefabGuid= if ($id -eq 109) { "440ddc97d929a1142a442dd23c76628e" } else { "520a01" + $id + "0000000000000000000" + $id + "f" }

    # --------------------------------------------------------------------------
    # 1. PROJECTILE SPRITESHEET (768 x 128, 6 Frames)
    # --------------------------------------------------------------------------
    $pBmp = New-Object System.Drawing.Bitmap(768, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pG = [System.Drawing.Graphics]::FromImage($pBmp)
    $pG.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $pG.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $pG.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $pG.Clear([System.Drawing.Color]::Transparent)

    $palette = Get-ProjectilePalette $id
    for ($f = 0; $f -lt 6; $f++) {
        Draw-GenericComet $pG ($f * 128) 0 $f 6 $palette
    }
    $pG.Dispose()

    $projPngPath = Join-Path $baseDir "Assets\4. DotAsset\3. Projectile\$projTexName.png"
    if (Test-Path $projPngPath) { [System.IO.File]::Delete($projPngPath) }
    $pBmp.Save($projPngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $pBmp.Dispose()
    Write-Output "  [1/8] Saved Projectile Spritesheet: $projPngPath"

    # Meta
    $pMeta = New-Object System.Collections.Generic.List[string]
    $pMeta.Add("fileFormatVersion: 2")
    $pMeta.Add("guid: $projTexGuid")
    $pMeta.Add("TextureImporter:")
    $pMeta.Add("  internalIDToNameTable:")
    for ($f = 0; $f -lt 6; $f++) {
        $fid = "${id}10${f}"
        $pMeta.Add("  - first:")
        $pMeta.Add("      213: $fid")
        $pMeta.Add("    second: ${projTexName}_${f}")
    }
    $pMeta.Add("  externalObjects: {}")
    $pMeta.Add("  serializedVersion: 13")
    $pMeta.Add("  mipmaps:")
    $pMeta.Add("    mipMapMode: 0")
    $pMeta.Add("    enableMipMap: 0")
    $pMeta.Add("    sRGBTexture: 1")
    $pMeta.Add("    linearTexture: 0")
    $pMeta.Add("    fadeOut: 0")
    $pMeta.Add("    borderMipMap: 0")
    $pMeta.Add("    mipMapsPreserveCoverage: 0")
    $pMeta.Add("    alphaTestReferenceValue: 0.5")
    $pMeta.Add("    mipMapFadeDistanceStart: 1")
    $pMeta.Add("    mipMapFadeDistanceEnd: 3")
    $pMeta.Add("  bumpmap:")
    $pMeta.Add("    convertToNormalMap: 0")
    $pMeta.Add("    externalNormalMap: 0")
    $pMeta.Add("    heightScale: 0.25")
    $pMeta.Add("    normalMapFilter: 0")
    $pMeta.Add("    flipGreenChannel: 0")
    $pMeta.Add("  isReadable: 0")
    $pMeta.Add("  streamingMipmaps: 0")
    $pMeta.Add("  streamingMipmapsPriority: 0")
    $pMeta.Add("  vTOnly: 0")
    $pMeta.Add("  ignoreMipmapLimit: 0")
    $pMeta.Add("  grayScaleToAlpha: 0")
    $pMeta.Add("  generateCubemap: 6")
    $pMeta.Add("  cubemapConvolution: 0")
    $pMeta.Add("  seamlessCubemap: 0")
    $pMeta.Add("  textureFormat: 1")
    $pMeta.Add("  maxTextureSize: 2048")
    $pMeta.Add("  textureSettings:")
    $pMeta.Add("    serializedVersion: 2")
    $pMeta.Add("    filterMode: 0")
    $pMeta.Add("    aniso: 1")
    $pMeta.Add("    mipBias: 0")
    $pMeta.Add("    wrapU: 1")
    $pMeta.Add("    wrapV: 1")
    $pMeta.Add("    wrapW: 1")
    $pMeta.Add("  nPOTScale: 0")
    $pMeta.Add("  lightmap: 0")
    $pMeta.Add("  compressionQuality: 50")
    $pMeta.Add("  spriteMode: 2")
    $pMeta.Add("  spriteExtrude: 1")
    $pMeta.Add("  spriteMeshType: 1")
    $pMeta.Add("  alignment: 0")
    $pMeta.Add("  spritePivot: {x: 0.5, y: 0.5}")
    $pMeta.Add("  spritePixelsToUnits: 64")
    $pMeta.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}")
    $pMeta.Add("  spriteGenerateFallbackPhysicsShape: 1")
    $pMeta.Add("  alphaUsage: 1")
    $pMeta.Add("  alphaIsTransparency: 1")
    $pMeta.Add("  spriteTessellationDetail: -1")
    $pMeta.Add("  textureType: 8")
    $pMeta.Add("  textureShape: 1")
    $pMeta.Add("  singleChannelComponent: 0")
    $pMeta.Add("  flipbookRows: 1")
    $pMeta.Add("  flipbookColumns: 1")
    $pMeta.Add("  maxTextureSizeSet: 0")
    $pMeta.Add("  compressionQualitySet: 0")
    $pMeta.Add("  textureFormatSet: 0")
    $pMeta.Add("  ignorePngGamma: 0")
    $pMeta.Add("  applyGammaDecoding: 0")
    $pMeta.Add("  swizzle: 50462976")
    $pMeta.Add("  cookieLightType: 0")
    $pMeta.Add("  platformSettings:")
    $pMeta.Add("  - serializedVersion: 4")
    $pMeta.Add("    buildTarget: DefaultTexturePlatform")
    $pMeta.Add("    maxTextureSize: 2048")
    $pMeta.Add("    resizeAlgorithm: 0")
    $pMeta.Add("    textureFormat: -1")
    $pMeta.Add("    textureCompression: 0")
    $pMeta.Add("    compressionQuality: 50")
    $pMeta.Add("    crunchedCompression: 0")
    $pMeta.Add("    allowsAlphaSplitting: 0")
    $pMeta.Add("    overridden: 0")
    $pMeta.Add("    ignorePlatformSupport: 0")
    $pMeta.Add("    androidETC2FallbackOverride: 0")
    $pMeta.Add("    forceMaximumCompressionQuality_BC6H_BC7: 0")
    $pMeta.Add("  spriteSheet:")
    $pMeta.Add("    serializedVersion: 2")
    $pMeta.Add("    sprites:")
    for ($f = 0; $f -lt 6; $f++) {
        $xPos = $f * 128
        $pMeta.Add("    - serializedVersion: 2")
        $pMeta.Add("      name: ${projTexName}_${f}")
        $pMeta.Add("      rect:")
        $pMeta.Add("        serializedVersion: 2")
        $pMeta.Add("        x: $xPos")
        $pMeta.Add("        y: 0")
        $pMeta.Add("        width: 128")
        $pMeta.Add("        height: 128")
        $pMeta.Add("      alignment: 0")
        $pMeta.Add("      pivot: {x: 0.5, y: 0.5}")
        $pMeta.Add("      border: {x: 0, y: 0, z: 0, w: 0}")
        $pMeta.Add("      customData: ")
        $pMeta.Add("      outline: []")
        $pMeta.Add("      physicsShape: []")
        $pMeta.Add("      tessellationDetail: 0")
        $pMeta.Add("      bones: []")
        $pMeta.Add("      spriteID: 7a0701${id}000${f}00000800000000000000")
        $pMeta.Add("      internalID: ${id}10${f}")
        $pMeta.Add("      vertices: []")
        $pMeta.Add("      indices: ")
        $pMeta.Add("      edges: []")
        $pMeta.Add("      weights: []")
    }
    $pMeta.Add("    outline: []")
    $pMeta.Add("    customData: ")
    $pMeta.Add("    physicsShape: []")
    $pMeta.Add("    bones: []")
    $pMeta.Add("    spriteID: ")
    $pMeta.Add("    internalID: 0")
    $pMeta.Add("    vertices: []")
    $pMeta.Add("    indices: ")
    $pMeta.Add("    edges: []")
    $pMeta.Add("    weights: []")
    $pMeta.Add("    secondaryTextures: []")
    $pMeta.Add("    spriteCustomMetadata:")
    $pMeta.Add("      entries: []")
    $pMeta.Add("    nameFileIdTable:")
    for ($f = 0; $f -lt 6; $f++) {
        $pMeta.Add("      ${projTexName}_${f}: ${id}10${f}")
    }
    $pMeta.Add("  mipmapLimitGroupName: ")
    $pMeta.Add("  pSDRemoveMatte: 0")
    $pMeta.Add("  userData: ")
    $pMeta.Add("  assetBundleName: ")
    $pMeta.Add("  assetBundleVariant: ")
    [System.IO.File]::WriteAllLines("$projPngPath.meta", $pMeta)
    Write-Output "  [2/8] Saved Projectile Meta: $projPngPath.meta"

    # --------------------------------------------------------------------------
    # 2. PROJECTILE ANIMATION (E_<ID>.anim) & CONTROLLER (E_<ID>.controller)
    # --------------------------------------------------------------------------
    $pAnim = New-Object System.Collections.Generic.List[string]
    $pAnim.Add("%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!74 &7400000`nAnimationClip:")
    $pAnim.Add("  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}")
    $pAnim.Add("  m_Name: E_$id`n  serializedVersion: 7`n  m_Legacy: 0`n  m_Compressed: 0`n  m_UseHighQualityCurve: 1`n  m_RotationCurves: []`n  m_CompressedRotationCurves: []`n  m_EulerCurves: []`n  m_PositionCurves: []`n  m_ScaleCurves: []`n  m_FloatCurves: []")
    $pAnim.Add("  m_PPtrCurves:`n  - serializedVersion: 2`n    curve:")
    for ($f = 0; $f -lt 6; $f++) {
        $t = ($f * 0.0625).ToString("0.0000", [System.Globalization.CultureInfo]::InvariantCulture)
        $fid = "${id}10${f}"
        $pAnim.Add("    - time: $t`n      value: {fileID: $fid, guid: $projTexGuid, type: 3}")
    }
    $pAnim.Add("    attribute: m_Sprite`n    path: `n    classID: 212`n    script: {fileID: 0}`n    flags: 2`n  m_SampleRate: 16`n  m_WrapMode: 0`n  m_Bounds:`n    m_Center: {x: 0, y: 0, z: 0}`n    m_Extent: {x: 0, y: 0, z: 0}`n  m_ClipBindingConstant:`n    genericBindings:`n    - serializedVersion: 2`n      path: 0`n      attribute: 0`n      script: {fileID: 0}`n      typeID: 212`n      customType: 23`n      isPPtrCurve: 1`n      isIntCurve: 0`n      isSerializeReferenceCurve: 0`n    pptrCurveMapping:")
    for ($f = 0; $f -lt 6; $f++) {
        $fid = "${id}10${f}"
        $pAnim.Add("    - {fileID: $fid, guid: $projTexGuid, type: 3}")
    }
    $pAnim.Add("  m_AnimationClipSettings:`n    serializedVersion: 2`n    m_AdditiveReferencePoseClip: {fileID: 0}`n    m_AdditiveReferencePoseTime: 0`n    m_StartTime: 0`n    m_StopTime: 0.375`n    m_OrientationOffsetY: 0`n    m_Level: 0`n    m_CycleOffset: 0`n    m_HasAdditiveReferencePose: 0`n    m_LoopTime: 1`n    m_LoopBlend: 0`n    m_LoopBlendOrientation: 0`n    m_LoopBlendPositionY: 0`n    m_LoopBlendPositionXZ: 0`n    m_KeepOriginalOrientation: 0`n    m_KeepOriginalPositionY: 1`n    m_KeepOriginalPositionXZ: 0`n    m_HeightFromFeet: 0`n    m_Mirror: 0`n  m_EditorCurves: []`n  m_EulerEditorCurves: []`n  m_HasGenericRootTransform: 0`n  m_HasMotionFloatCurves: 0`n  m_Events: []")
    
    $projAnimPath = Join-Path $baseDir "Assets\10.Animation\Projectile\E_$id.anim"
    [System.IO.File]::WriteAllLines($projAnimPath, $pAnim)
    [System.IO.File]::WriteAllLines("$projAnimPath.meta", @("fileFormatVersion: 2", "guid: $projAnimGuid", "AnimationClip:", "  serializedVersion: 2", "  defaultClipBindingConstant:", "    genericBindings: []", "    pptrCurveMapping: []", "  masterClip: {fileID: 0}"))

    # Controller
    $ctrlLines = @(
        "%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!91 &9100000", "AnimatorController:",
        "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
        "  m_Name: E_$id", "  serializedVersion: 5", "  m_AnimatorParameters: []", "  m_AnimatorLayers:",
        "  - serializedVersion: 5", "    m_Name: Base Layer", "    m_StateMachine: {fileID: 354852189424992894}", "    m_Mask: {fileID: 0}",
        "    m_Motions: []", "    m_Behaviours: []", "    m_BlendingMode: 0", "    m_SyncedLayerIndex: -1", "    m_DefaultWeight: 0",
        "    m_IKPass: 0", "    m_SyncedLayerAffectsTiming: 0", "    m_Controller: {fileID: 9100000}",
        "--- !u!1107 &354852189424992894", "AnimatorStateMachine:", "  serializedVersion: 6", "  m_ObjectHideFlags: 1",
        "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_Name: Base Layer",
        "  m_ChildStates:", "  - serializedVersion: 1", "    m_State: {fileID: 8833959328309836345}", "    m_Position: {x: 350, y: 120, z: 0}",
        "  m_ChildStateMachines: []", "  m_AnyStateTransitions: []", "  m_EntryTransitions: []", "  m_StateMachineTransitions: {}",
        "  m_StateMachineBehaviours: []", "  m_AnyStatePosition: {x: 50, y: 20, z: 0}", "  m_EntryPosition: {x: 50, y: 120, z: 0}",
        "  m_ExitPosition: {x: 800, y: 120, z: 0}", "  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}",
        "  m_DefaultState: {fileID: 8833959328309836345}",
        "--- !u!1102 &8833959328309836345", "AnimatorState:", "  serializedVersion: 6", "  m_ObjectHideFlags: 1",
        "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
        "  m_Name: E_$id", "  m_Speed: 1", "  m_CycleOffset: 0", "  m_Transitions: []", "  m_StateMachineBehaviours: []",
        "  m_Position: {x: 50, y: 50, z: 0}", "  m_IKOnFeet: 0", "  m_WriteDefaultValues: 1", "  m_Mirror: 0",
        "  m_SpeedParameterActive: 0", "  m_MirrorParameterActive: 0", "  m_CycleOffsetParameterActive: 0", "  m_TimeParameterActive: 0",
        "  m_Motion: {fileID: 7400000, guid: $projAnimGuid, type: 2}", "  m_Tag: ", "  m_SpeedParameter: ", "  m_MirrorParameter: ",
        "  m_CycleOffsetParameter: ", "  m_TimeParameter: "
    )
    $projCtrlPath = Join-Path $baseDir "Assets\10.Animation\Projectile\E_$id.controller"
    [System.IO.File]::WriteAllLines($projCtrlPath, $ctrlLines)
    [System.IO.File]::WriteAllLines("$projCtrlPath.meta", @("fileFormatVersion: 2", "guid: $projCtrlGuid", "NativeFormatImporter:", "  externalObjects: {}", "  mainObjectFileID: 0", "  userData: ", "  assetBundleName: ", "  assetBundleVariant: "))
    Write-Output "  [3/8] Saved Projectile Anim & Controller: $projAnimPath"

    # --------------------------------------------------------------------------
    # 3. PROJECTILE PREFAB UPDATE (109..113.prefab)
    # --------------------------------------------------------------------------
    $projPrefabPath = Join-Path $baseDir "Assets\Resources\Projectiles\$id.prefab"
    # Read template from 107.prefab to ensure absolute perfection
    $template107 = [System.IO.File]::ReadAllText((Join-Path $baseDir "Assets\Resources\Projectiles\107.prefab"))
    $pText = $template107
    $pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Name:\s*107", "m_Name: $id")
    $pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Sprite:\s*\{fileID:\s*\d+,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\}", "m_Sprite: {fileID: ${id}100, guid: $projTexGuid, type: 3}")
    $pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Controller:\s*\{fileID:\s*9100000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*2\}", "m_Controller: {fileID: 9100000, guid: $projCtrlGuid, type: 2}")
    [System.IO.File]::WriteAllText($projPrefabPath, $pText)
    Write-Output "  [4/8] Updated Projectile Prefab: $projPrefabPath"

    # --------------------------------------------------------------------------
    # 4. HIT EFFECT SPRITESHEET (1280 x 128, 10 Frames)
    # --------------------------------------------------------------------------
    $eBmp = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $eG = [System.Drawing.Graphics]::FromImage($eBmp)
    $eG.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $eG.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $eG.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $eG.Clear([System.Drawing.Color]::Transparent)

    for ($f = 0; $f -lt 10; $f++) {
        $cx = $f * 128.0 + 64.0
        $cy = 64.0
        & $elem.EffDraw $eG $cx $cy $f
    }
    $eG.Dispose()

    $effPngPath = Join-Path $baseDir "Assets\4. DotAsset\6. Effect\$effTexName.png"
    if (Test-Path $effPngPath) { [System.IO.File]::Delete($effPngPath) }
    $eBmp.Save($effPngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $eBmp.Dispose()
    Write-Output "  [5/8] Saved Effect Spritesheet: $effPngPath"

    # Meta
    $eMeta = New-Object System.Collections.Generic.List[string]
    $eMeta.Add("fileFormatVersion: 2")
    $eMeta.Add("guid: $effTexGuid")
    $eMeta.Add("TextureImporter:")
    $eMeta.Add("  internalIDToNameTable:")
    for ($f = 0; $f -lt 10; $f++) {
        $fid = "${id}00${f}"
        $eMeta.Add("  - first:")
        $eMeta.Add("      213: $fid")
        $eMeta.Add("    second: ${effTexName}_${f}")
    }
    $eMeta.Add("  externalObjects: {}")
    $eMeta.Add("  serializedVersion: 13")
    $eMeta.Add("  mipmaps:")
    $eMeta.Add("    mipMapMode: 0")
    $eMeta.Add("    enableMipMap: 0")
    $eMeta.Add("    sRGBTexture: 1")
    $eMeta.Add("    linearTexture: 0")
    $eMeta.Add("    fadeOut: 0")
    $eMeta.Add("    borderMipMap: 0")
    $eMeta.Add("    mipMapsPreserveCoverage: 0")
    $eMeta.Add("    alphaTestReferenceValue: 0.5")
    $eMeta.Add("    mipMapFadeDistanceStart: 1")
    $eMeta.Add("    mipMapFadeDistanceEnd: 3")
    $eMeta.Add("  bumpmap:")
    $eMeta.Add("    convertToNormalMap: 0")
    $eMeta.Add("    externalNormalMap: 0")
    $eMeta.Add("    heightScale: 0.25")
    $eMeta.Add("    normalMapFilter: 0")
    $eMeta.Add("    flipGreenChannel: 0")
    $eMeta.Add("  isReadable: 0")
    $eMeta.Add("  streamingMipmaps: 0")
    $eMeta.Add("  streamingMipmapsPriority: 0")
    $eMeta.Add("  vTOnly: 0")
    $eMeta.Add("  ignoreMipmapLimit: 0")
    $eMeta.Add("  grayScaleToAlpha: 0")
    $eMeta.Add("  generateCubemap: 6")
    $eMeta.Add("  cubemapConvolution: 0")
    $eMeta.Add("  seamlessCubemap: 0")
    $eMeta.Add("  textureFormat: 1")
    $eMeta.Add("  maxTextureSize: 2048")
    $eMeta.Add("  textureSettings:")
    $eMeta.Add("    serializedVersion: 2")
    $eMeta.Add("    filterMode: 0")
    $eMeta.Add("    aniso: 1")
    $eMeta.Add("    mipBias: 0")
    $eMeta.Add("    wrapU: 1")
    $eMeta.Add("    wrapV: 1")
    $eMeta.Add("    wrapW: 1")
    $eMeta.Add("  nPOTScale: 0")
    $eMeta.Add("  lightmap: 0")
    $eMeta.Add("  compressionQuality: 50")
    $eMeta.Add("  spriteMode: 2")
    $eMeta.Add("  spriteExtrude: 1")
    $eMeta.Add("  spriteMeshType: 1")
    $eMeta.Add("  alignment: 0")
    $eMeta.Add("  spritePivot: {x: 0.5, y: 0.5}")
    $eMeta.Add("  spritePixelsToUnits: 64")
    $eMeta.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}")
    $eMeta.Add("  spriteGenerateFallbackPhysicsShape: 1")
    $eMeta.Add("  alphaUsage: 1")
    $eMeta.Add("  alphaIsTransparency: 1")
    $eMeta.Add("  spriteTessellationDetail: -1")
    $eMeta.Add("  textureType: 8")
    $eMeta.Add("  textureShape: 1")
    $eMeta.Add("  singleChannelComponent: 0")
    $eMeta.Add("  flipbookRows: 1")
    $eMeta.Add("  flipbookColumns: 1")
    $eMeta.Add("  maxTextureSizeSet: 0")
    $eMeta.Add("  compressionQualitySet: 0")
    $eMeta.Add("  textureFormatSet: 0")
    $eMeta.Add("  ignorePngGamma: 0")
    $eMeta.Add("  applyGammaDecoding: 0")
    $eMeta.Add("  swizzle: 50462976")
    $eMeta.Add("  cookieLightType: 0")
    $eMeta.Add("  platformSettings:")
    $eMeta.Add("  - serializedVersion: 4")
    $eMeta.Add("    buildTarget: DefaultTexturePlatform")
    $eMeta.Add("    maxTextureSize: 2048")
    $eMeta.Add("    resizeAlgorithm: 0")
    $eMeta.Add("    textureFormat: -1")
    $eMeta.Add("    textureCompression: 0")
    $eMeta.Add("    compressionQuality: 50")
    $eMeta.Add("    crunchedCompression: 0")
    $eMeta.Add("    allowsAlphaSplitting: 0")
    $eMeta.Add("    overridden: 0")
    $eMeta.Add("    ignorePlatformSupport: 0")
    $eMeta.Add("    androidETC2FallbackOverride: 0")
    $eMeta.Add("    forceMaximumCompressionQuality_BC6H_BC7: 0")
    $eMeta.Add("  spriteSheet:")
    $eMeta.Add("    serializedVersion: 2")
    $eMeta.Add("    sprites:")
    for ($f = 0; $f -lt 10; $f++) {
        $xPos = $f * 128
        $eMeta.Add("    - serializedVersion: 2")
        $eMeta.Add("      name: ${effTexName}_${f}")
        $eMeta.Add("      rect:")
        $eMeta.Add("        serializedVersion: 2")
        $eMeta.Add("        x: $xPos")
        $eMeta.Add("        y: 0")
        $eMeta.Add("        width: 128")
        $eMeta.Add("        height: 128")
        $eMeta.Add("      alignment: 0")
        $eMeta.Add("      pivot: {x: 0.5, y: 0.5}")
        $eMeta.Add("      border: {x: 0, y: 0, z: 0, w: 0}")
        $eMeta.Add("      customData: ")
        $eMeta.Add("      outline: []")
        $eMeta.Add("      physicsShape: []")
        $eMeta.Add("      tessellationDetail: 0")
        $eMeta.Add("      bones: []")
        $eMeta.Add("      spriteID: 070${id}00000${f}00000800000000000000")
        $eMeta.Add("      internalID: ${id}00${f}")
        $eMeta.Add("      vertices: []")
        $eMeta.Add("      indices: ")
        $eMeta.Add("      edges: []")
        $eMeta.Add("      weights: []")
    }
    $eMeta.Add("    outline: []")
    $eMeta.Add("    customData: ")
    $eMeta.Add("    physicsShape: []")
    $eMeta.Add("    bones: []")
    $eMeta.Add("    spriteID: ")
    $eMeta.Add("    internalID: 0")
    $eMeta.Add("    vertices: []")
    $eMeta.Add("    indices: ")
    $eMeta.Add("    edges: []")
    $eMeta.Add("    weights: []")
    $eMeta.Add("    secondaryTextures: []")
    $eMeta.Add("    spriteCustomMetadata:")
    $eMeta.Add("      entries: []")
    $eMeta.Add("    nameFileIdTable:")
    for ($f = 0; $f -lt 10; $f++) {
        $eMeta.Add("      ${effTexName}_${f}: ${id}00${f}")
    }
    $eMeta.Add("  mipmapLimitGroupName: ")
    $eMeta.Add("  pSDRemoveMatte: 0")
    $eMeta.Add("  userData: ")
    $eMeta.Add("  assetBundleName: ")
    $eMeta.Add("  assetBundleVariant: ")
    [System.IO.File]::WriteAllLines("$effPngPath.meta", $eMeta)
    Write-Output "  [6/8] Saved Effect Meta: $effPngPath.meta"

    # --------------------------------------------------------------------------
    # 5. HIT EFFECT ANIMATION & CONTROLLER (E<ID>.anim, E<ID>.controller)
    # --------------------------------------------------------------------------
    $eAnim = New-Object System.Collections.Generic.List[string]
    $eAnim.Add("%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!74 &7400000`nAnimationClip:")
    $eAnim.Add("  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}")
    $eAnim.Add("  m_Name: E$id`n  serializedVersion: 7`n  m_Legacy: 0`n  m_Compressed: 0`n  m_UseHighQualityCurve: 1`n  m_RotationCurves: []`n  m_CompressedRotationCurves: []`n  m_EulerCurves: []`n  m_PositionCurves: []`n  m_ScaleCurves: []`n  m_FloatCurves: []")
    $eAnim.Add("  m_PPtrCurves:`n  - serializedVersion: 2`n    curve:")
    for ($f = 0; $f -lt 10; $f++) {
        $t = ($f * 0.0625).ToString("0.0000", [System.Globalization.CultureInfo]::InvariantCulture)
        $fid = "${id}00${f}"
        $eAnim.Add("    - time: $t`n      value: {fileID: $fid, guid: $effTexGuid, type: 3}")
    }
    $eAnim.Add("    attribute: m_Sprite`n    path: `n    classID: 212`n    script: {fileID: 0}`n    flags: 2`n  m_SampleRate: 16`n  m_WrapMode: 0`n  m_Bounds:`n    m_Center: {x: 0, y: 0, z: 0}`n    m_Extent: {x: 0, y: 0, z: 0}`n  m_ClipBindingConstant:`n    genericBindings:`n    - serializedVersion: 2`n      path: 0`n      attribute: 0`n      script: {fileID: 0}`n      typeID: 212`n      customType: 23`n      isPPtrCurve: 1`n      isIntCurve: 0`n      isSerializeReferenceCurve: 0`n    pptrCurveMapping:")
    for ($f = 0; $f -lt 10; $f++) {
        $fid = "${id}00${f}"
        $eAnim.Add("    - {fileID: $fid, guid: $effTexGuid, type: 3}")
    }
    $eAnim.Add("  m_AnimationClipSettings:`n    serializedVersion: 2`n    m_AdditiveReferencePoseClip: {fileID: 0}`n    m_AdditiveReferencePoseTime: 0`n    m_StartTime: 0`n    m_StopTime: 0.625`n    m_OrientationOffsetY: 0`n    m_Level: 0`n    m_CycleOffset: 0`n    m_HasAdditiveReferencePose: 0`n    m_LoopTime: 0`n    m_LoopBlend: 0`n    m_LoopBlendOrientation: 0`n    m_LoopBlendPositionY: 0`n    m_LoopBlendPositionXZ: 0`n    m_KeepOriginalOrientation: 0`n    m_KeepOriginalPositionY: 1`n    m_KeepOriginalPositionXZ: 0`n    m_HeightFromFeet: 0`n    m_Mirror: 0`n  m_EditorCurves: []`n  m_EulerEditorCurves: []`n  m_HasGenericRootTransform: 0`n  m_HasMotionFloatCurves: 0`n  m_Events:`n  - time: 0.625`n    functionName: DestroyEffect`n    data: `n    objectReferenceParameter: {fileID: 0}`n    floatParameter: 0`n    intParameter: 0`n    messageOptions: 0")

    $effAnimPath = Join-Path $baseDir "Assets\10.Animation\Effect\E$id.anim"
    [System.IO.File]::WriteAllLines($effAnimPath, $eAnim)
    [System.IO.File]::WriteAllLines("$effAnimPath.meta", @("fileFormatVersion: 2", "guid: $effAnimGuid", "AnimationClip:", "  serializedVersion: 2", "  defaultClipBindingConstant:", "    genericBindings: []", "    pptrCurveMapping: []", "  masterClip: {fileID: 0}"))

    # Controller
    $eCtrlLines = @(
        "%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!91 &9100000", "AnimatorController:",
        "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
        "  m_Name: E$id", "  serializedVersion: 5", "  m_AnimatorParameters: []", "  m_AnimatorLayers:",
        "  - serializedVersion: 5", "    m_Name: Base Layer", "    m_StateMachine: {fileID: 354852189424992894}", "    m_Mask: {fileID: 0}",
        "    m_Motions: []", "    m_Behaviours: []", "    m_BlendingMode: 0", "    m_SyncedLayerIndex: -1", "    m_DefaultWeight: 0",
        "    m_IKPass: 0", "    m_SyncedLayerAffectsTiming: 0", "    m_Controller: {fileID: 9100000}",
        "--- !u!1107 &354852189424992894", "AnimatorStateMachine:", "  serializedVersion: 6", "  m_ObjectHideFlags: 1",
        "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}", "  m_Name: Base Layer",
        "  m_ChildStates:", "  - serializedVersion: 1", "    m_State: {fileID: 8833959328309836345}", "    m_Position: {x: 350, y: 120, z: 0}",
        "  m_ChildStateMachines: []", "  m_AnyStateTransitions: []", "  m_EntryTransitions: []", "  m_StateMachineTransitions: {}",
        "  m_StateMachineBehaviours: []", "  m_AnyStatePosition: {x: 50, y: 20, z: 0}", "  m_EntryPosition: {x: 50, y: 120, z: 0}",
        "  m_ExitPosition: {x: 800, y: 120, z: 0}", "  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}",
        "  m_DefaultState: {fileID: 8833959328309836345}",
        "--- !u!1102 &8833959328309836345", "AnimatorState:", "  serializedVersion: 6", "  m_ObjectHideFlags: 1",
        "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}", "  m_PrefabAsset: {fileID: 0}",
        "  m_Name: E$id", "  m_Speed: 1", "  m_CycleOffset: 0", "  m_Transitions: []", "  m_StateMachineBehaviours: []",
        "  m_Position: {x: 50, y: 50, z: 0}", "  m_IKOnFeet: 0", "  m_WriteDefaultValues: 1", "  m_Mirror: 0",
        "  m_SpeedParameterActive: 0", "  m_MirrorParameterActive: 0", "  m_CycleOffsetParameterActive: 0", "  m_TimeParameterActive: 0",
        "  m_Motion: {fileID: 7400000, guid: $effAnimGuid, type: 2}", "  m_Tag: ", "  m_SpeedParameter: ", "  m_MirrorParameter: ",
        "  m_CycleOffsetParameter: ", "  m_TimeParameter: "
    )
    $effCtrlPath = Join-Path $baseDir "Assets\10.Animation\Effect\E$id.controller"
    [System.IO.File]::WriteAllLines($effCtrlPath, $eCtrlLines)
    [System.IO.File]::WriteAllLines("$effCtrlPath.meta", @("fileFormatVersion: 2", "guid: $effCtrlGuid", "NativeFormatImporter:", "  externalObjects: {}", "  mainObjectFileID: 0", "  userData: ", "  assetBundleName: ", "  assetBundleVariant: "))
    Write-Output "  [7/8] Saved Effect Anim & Controller: $effAnimPath"

    # --------------------------------------------------------------------------
    # 6. HIT EFFECT PREFAB (E<ID>.prefab) & EFFECT LIBRARY
    # --------------------------------------------------------------------------
    $effPrefabPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\E$id.prefab"
    # Take template from E108.prefab
    $templateE108 = [System.IO.File]::ReadAllText((Join-Path $baseDir "Assets\2. Prefab\4. Effect\E108.prefab"))
    $eText = $templateE108
    $eText = [System.Text.RegularExpressions.Regex]::Replace($eText, "m_Name:\s*E108", "m_Name: E$id")
    $eText = [System.Text.RegularExpressions.Regex]::Replace($eText, "m_Sprite:\s*\{fileID:\s*\d+,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\}", "m_Sprite: {fileID: ${id}000, guid: $effTexGuid, type: 3}")
    $eText = [System.Text.RegularExpressions.Regex]::Replace($eText, "m_Controller:\s*\{fileID:\s*9100000,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*2\}", "m_Controller: {fileID: 9100000, guid: $effCtrlGuid, type: 2}")
    [System.IO.File]::WriteAllText($effPrefabPath, $eText)

    [System.IO.File]::WriteAllLines("$effPrefabPath.meta", @("fileFormatVersion: 2", "guid: $effPrefabGuid", "PrefabImporter:", "  externalObjects: {}", "  userData: ", "  assetBundleName: ", "  assetBundleVariant: "))
    Write-Output "  [8/8] Created Effect Prefab: $effPrefabPath"
}

# ==============================================================================
# SECTION 4: UPDATE EFFECT LIBRARY (EffectLibrary.asset)
# ==============================================================================
Write-Output "`n>>> Updating EffectLibrary.asset for 109..113..."
$libPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\EffectLibrary.asset"
$libText = [System.IO.File]::ReadAllText($libPath)

foreach ($elem in $elements) {
    $id = $elem.ID
    $guid = if ($id -eq 109) { "440ddc97d929a1142a442dd23c76628e" } else { "520a01" + $id + "0000000000000000000" + $id + "f" }

    if ($libText -match "- effectID: $id\s+effectPrefab:") {
        $libText = [System.Text.RegularExpressions.Regex]::Replace($libText, "- effectID: $id\s+effectPrefab:\s*\{fileID:\s*\d+,\s*guid:\s*[0-9a-fA-F]+,\s*type:\s*3\}", "- effectID: $id`n    effectPrefab: {fileID: 5220447335354414950, guid: $guid, type: 3}")
    } else {
        # Append before EOF or at end of m_Effects
        $newEntry = "  - effectID: $id`n    effectPrefab: {fileID: 5220447335354414950, guid: $guid, type: 3}"
        $libText = $libText.TrimEnd() + "`n" + $newEntry + "`n"
    }
}
[System.IO.File]::WriteAllText($libPath, $libText)
Write-Output "EffectLibrary.asset updated successfully!"


# ==============================================================================
# SECTION 5: GENERATE SHOWCASE PREVIEW BANNERS
# ==============================================================================
Write-Output "`n>>> Generating Comprehensive Showcase Preview Banners..."

# --- 1. Projectiles Showcase (1200 x 680) ---
$prevProj = New-Object System.Drawing.Bitmap(1200, 680)
$gPP = [System.Drawing.Graphics]::FromImage($prevProj)
$gPP.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gPP.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gPP.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
$fSub   = New-Object System.Drawing.Font('Malgun Gothic', 9, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$bSub   = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))
$sf     = New-Object System.Drawing.StringFormat; $sf.Alignment = [System.Drawing.StringAlignment]::Center

$pTitleBytes = @(236, 138, 164, 237, 148, 140, 235, 158, 152, 236, 139, 156, 32, 236, 155, 144, 236, 134, 140, 32, 237, 131, 128, 236, 155, 140, 32, 236, 180, 157, 236, 149, 140, 32, 53, 236, 162, 133, 32, 40, 49, 48, 57, 32, 126, 32, 49, 49, 51, 41, 32, 236, 149, 160, 235, 139, 136, 235, 169, 148, 236, 157, 180, 236, 133, 152)
$gPP.DrawString([System.Text.Encoding]::UTF8.GetString($pTitleBytes), $fTitle, $bWhite, 600.0, 15.0, $sf)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $rowY = 55 + $i * 122
    $pBox = New-Object System.Drawing.Pen((Clr 60 255 255 255), 1.0)
    $gPP.DrawRectangle($pBox, 15, $rowY, 1170, 116)
    $pBox.Dispose()

    # Load its sheet
    $sheetImg = [System.Drawing.Image]::FromFile((Join-Path $baseDir "Assets\4. DotAsset\3. Projectile\$($elem.TexProj).png"))
    
    # Label on left
    $lblBytes = [System.Text.Encoding]::UTF8.GetBytes("$($elem.ID): $($elem.Name)")
    $gPP.DrawString([System.Text.Encoding]::UTF8.GetString($lblBytes), $fTitle, $bWhite, 80.0, [float]($rowY + 45), $sf)

    # 6 frames
    for ($f = 0; $f -lt 6; $f++) {
        $dx = 160 + $f * 105
        $srcR = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
        $dstR = New-Object System.Drawing.Rectangle($dx, ($rowY + 8), 100, 100)
        $gPP.DrawImage($sheetImg, $dstR, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
    }

    # 2x Zoom on right
    $srcZoom = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
    $dstZoom = New-Object System.Drawing.Rectangle(820, ($rowY + 8), 200, 100)
    $gPP.DrawImage($sheetImg, $dstZoom, $srcZoom, [System.Drawing.GraphicsUnit]::Pixel)

    $sheetImg.Dispose()
}

$prevProjPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_elemental_splash_projectiles.png"
if (Test-Path $prevProjPath) { [System.IO.File]::Delete($prevProjPath) }
$prevProj.Save($prevProjPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $prevProjPath (Join-Path $baseDir "preview_elemental_splash_projectiles.png") -Force
$gPP.Dispose(); $prevProj.Dispose()
Write-Output "Saved Projectiles Preview Banner to $prevProjPath"

# --- 2. Hit Effects Showcase (1320 x 780) ---
$prevEff = New-Object System.Drawing.Bitmap(1320, 780)
$gPE = [System.Drawing.Graphics]::FromImage($prevEff)
$gPE.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gPE.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gPE.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

$eTitleBytes = @(236, 138, 164, 237, 148, 140, 235, 158, 152, 236, 139, 156, 32, 237, 148, 188, 234, 178, 169, 32, 234, 180, 145, 236, 151, 173, 32, 236, 157, 180, 237, 142, 153, 237, 138, 184, 32, 53, 236, 162, 133, 32, 40, 69, 49, 48, 57, 32, 126, 32, 69, 49, 49, 51, 41)
$gPE.DrawString([System.Text.Encoding]::UTF8.GetString($eTitleBytes), $fTitle, $bWhite, 660.0, 15.0, $sf)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $rowY = 55 + $i * 142
    $pBox = New-Object System.Drawing.Pen((Clr 60 255 255 255), 1.0)
    $gPE.DrawRectangle($pBox, 15, $rowY, 1290, 136)
    $pBox.Dispose()

    $sheetImg = [System.Drawing.Image]::FromFile((Join-Path $baseDir "Assets\4. DotAsset\6. Effect\$($elem.TexEff).png"))
    
    $lblBytes = [System.Text.Encoding]::UTF8.GetBytes("E$($elem.ID): $($elem.Name)")
    $gPE.DrawString([System.Text.Encoding]::UTF8.GetString($lblBytes), $fTitle, $bWhite, 80.0, [float]($rowY + 55), $sf)

    # 7 key frames
    for ($f = 0; $f -le 6; $f++) {
        $dx = 160 + $f * 115
        $srcR = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
        $dstR = New-Object System.Drawing.Rectangle($dx, ($rowY + 14), 108, 108)
        $gPE.DrawImage($sheetImg, $dstR, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
    }

    # 2x Zoom of F2 (Bloom/Collapse)
    $srcZoom = New-Object System.Drawing.Rectangle((2 * 128), 0, 128, 128)
    $dstZoom = New-Object System.Drawing.Rectangle(1000, ($rowY + 8), 240, 120)
    $gPE.DrawImage($sheetImg, $dstZoom, $srcZoom, [System.Drawing.GraphicsUnit]::Pixel)

    $sheetImg.Dispose()
}

$prevEffPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_elemental_splash_effects.png"
if (Test-Path $prevEffPath) { [System.IO.File]::Delete($prevEffPath) }
$prevEff.Save($prevEffPath, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $prevEffPath (Join-Path $baseDir "preview_elemental_splash_effects.png") -Force
$gPE.Dispose(); $prevEff.Dispose()
Write-Output "Saved Effects Preview Banner to $prevEffPath"


# ==============================================================================
# SECTION 6: COMPLETE VERIFICATION
# ==============================================================================
Write-Output "`n=================================================="
Write-Output "VERIFICATION REPORT (109..113)"
Write-Output "=================================================="
$allPass = $true

foreach ($elem in $elements) {
    $id = $elem.ID
    $projTexGuid  = "7a0701" + $id + "0000000000000000000" + $id + "b"
    $projAnimGuid = "7a0701" + $id + "0000000000000000000" + $id + "a"
    $projCtrlGuid = "7a0701" + $id + "0000000000000000000" + $id + "c"

    $effTexGuid   = "8a0701" + $id + "0000000000000000000" + $id + "e"
    $effAnimGuid  = "7a0701" + $id + "0000000000000000000" + $id + "e"
    $effCtrlGuid  = "6a0701" + $id + "0000000000000000000" + $id + "e"
    $effPrefabGuid= if ($id -eq 109) { "440ddc97d929a1142a442dd23c76628e" } else { "520a01" + $id + "0000000000000000000" + $id + "f" }

    $pP = (Get-Content (Join-Path $baseDir "Assets\Resources\Projectiles\$id.prefab") | Select-String "m_Controller:.*$projCtrlGuid").Matches.Count -gt 0
    $pS = (Get-Content (Join-Path $baseDir "Assets\Resources\Projectiles\$id.prefab") | Select-String "m_Sprite:.*$projTexGuid").Matches.Count -gt 0
    $pA = (Get-Content (Join-Path $baseDir "Assets\10.Animation\Projectile\E_$id.controller") | Select-String "m_Motion:.*$projAnimGuid").Matches.Count -gt 0

    $libContent = [System.IO.File]::ReadAllText($libPath)
    $eL = $libContent -match "- effectID: $id\s+effectPrefab:\s*\{fileID:\s*5220447335354414950,\s*guid:\s*$effPrefabGuid"
    $eC = (Get-Content (Join-Path $baseDir "Assets\2. Prefab\4. Effect\E$id.prefab") | Select-String "m_Controller:.*$effCtrlGuid").Matches.Count -gt 0
    $eS = (Get-Content (Join-Path $baseDir "Assets\2. Prefab\4. Effect\E$id.prefab") | Select-String "m_Sprite:.*$effTexGuid").Matches.Count -gt 0
    $eA = (Get-Content (Join-Path $baseDir "Assets\10.Animation\Effect\E$id.controller") | Select-String "m_Motion:.*$effAnimGuid").Matches.Count -gt 0

    $status = if ($pP -and $pS -and $pA -and $eL -and $eC -and $eS -and $eA) { "PASS" } else { "FAIL"; $allPass = $false }
    Write-Output "[$status] Element $id ($($elem.Name)): Proj(P=$pP, S=$pS, A=$pA) | Eff(Lib=$eL, C=$eC, S=$eS, A=$eA)"
}

if ($allPass) {
    Write-Output "`n>>> ALL 5 ELEMENTAL SPLASH TOWERS FULLY BUILT, WIRED, AND VERIFIED! <<<"
} else {
    Write-Warning "Some assets failed verification. Please check the logs."
}
