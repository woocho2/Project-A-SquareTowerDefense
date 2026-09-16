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
function Get-Utf8Str([byte[]]$bytes) { [System.Text.Encoding]::UTF8.GetString($bytes) }

# ==============================================================================
# REFINED TARGET HIT FRAME DRAWING FUNCTIONS (Radius strictly capped <= 35px at scale 1.0)
# Snappy, punchy impact: F0-F1 impact & burst, F2-F3 dispersal & fade, F4-F9 transparent
# ==============================================================================

# 1. Ice (E208) - 고드름 파편 비산 (Icicle Shatter)
function Draw-IceHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # Core diamond flash & frost glow
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 160 140 220 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bGlow.Dispose()

        $bDiamond = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        $diaPts = [System.Drawing.PointF[]]@(
            (Pt $cx ($cy - 10.0 * $scale)),
            (Pt ($cx + 4.0 * $scale) $cy),
            (Pt $cx ($cy + 10.0 * $scale)),
            (Pt ($cx - 4.0 * $scale) $cy)
        )
        $gfx.FillPolygon($bDiamond, $diaPts)
        $bDiamond.Dispose()

        # 4 mini icicle needle tips emerging
        $pNeedle = New-Object System.Drawing.Pen((Clr 240 200 245 255), (1.5 * $scale))
        $gfx.DrawLine($pNeedle, ($cx - 10.0 * $scale), $cy, ($cx + 10.0 * $scale), $cy)
        $pNeedle.Dispose()
    } elseif ($f -eq 1) {
        # 6 Sharp crystalline diamond icicle shards bursting outward
        $bCoreGlow = New-Object System.Drawing.SolidBrush((Clr 120 120 210 255))
        Fill-EllipseCentered $gfx $bCoreGlow $cx $cy (6.0 * $scale) (6.0 * $scale)
        $bCoreGlow.Dispose()

        $angles = @(0.3, 1.35, 2.4, 3.45, 4.5, 5.55)
        for ($i = 0; $i -lt 6; $i++) {
            $ang = $angles[$i]
            $dist = (16.0 + ($i % 2) * 3.0) * $scale
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $cy + [Math]::Sin($ang) * $dist
            $cosA = [Math]::Cos($ang); $sinA = [Math]::Sin($ang)

            $len = 8.5 * $scale
            $w = 3.2 * $scale
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($sx + $cosA * $len) ($sy + $sinA * $len)),
                (Pt ($sx - $sinA * $w) ($sy + $cosA * $w)),
                (Pt ($sx - $cosA * $len * 0.45) ($sy - $sinA * $len * 0.45)),
                (Pt ($sx + $sinA * $w) ($sy - $cosA * $w))
            )
            $bShard = New-Object System.Drawing.SolidBrush((Clr 240 120 220 255))
            $pShard = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.0 * $scale))
            $gfx.FillPolygon($bShard, $pts)
            $gfx.DrawPolygon($pShard, $pts)
            $bShard.Dispose(); $pShard.Dispose()
        }

        # Frost sparkle beads
        $bSpark = New-Object System.Drawing.SolidBrush((Clr 220 220 250 255))
        for ($m = 0; $m -lt 4; $m++) {
            $mAng = $m * 1.57 + 0.8
            $mx = $cx + [Math]::Cos($mAng) * (10.0 * $scale)
            $my = $cy + [Math]::Sin($mAng) * (10.0 * $scale)
            Fill-EllipseCentered $gfx $bSpark $mx $my (1.6 * $scale) (1.6 * $scale)
        }
        $bSpark.Dispose()
    } elseif ($f -eq 2) {
        # Shards traveling further and breaking down
        $angles = @(0.35, 1.4, 2.45, 3.5, 4.55, 5.6)
        for ($i = 0; $i -lt 6; $i++) {
            $ang = $angles[$i]
            $dist = (24.0 + ($i % 2) * 3.5) * $scale
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $cy + [Math]::Sin($ang) * $dist
            $cosA = [Math]::Cos($ang); $sinA = [Math]::Sin($ang)

            $len = 6.0 * $scale
            $w = 2.0 * $scale
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($sx + $cosA * $len) ($sy + $sinA * $len)),
                (Pt ($sx - $sinA * $w) ($sy + $cosA * $w)),
                (Pt ($sx - $cosA * $len * 0.4) ($sy - $sinA * $len * 0.4)),
                (Pt ($sx + $sinA * $w) ($sy - $cosA * $w))
            )
            $bShard = New-Object System.Drawing.SolidBrush((Clr 160 140 225 255))
            $pShard = New-Object System.Drawing.Pen((Clr 180 255 255 255), (0.8 * $scale))
            $gfx.FillPolygon($bShard, $pts)
            $gfx.DrawPolygon($pShard, $pts)
            $bShard.Dispose(); $pShard.Dispose()
        }

        # Sub-glitter motes
        $bMote = New-Object System.Drawing.SolidBrush((Clr 140 200 245 255))
        for ($m = 0; $m -lt 6; $m++) {
            $mAng = $m * 1.05 + 0.4
            $mx = $cx + [Math]::Cos($mAng) * (18.0 * $scale)
            $my = $cy + [Math]::Sin($mAng) * (18.0 * $scale)
            Fill-EllipseCentered $gfx $bMote $mx $my (1.3 * $scale) (1.3 * $scale)
        }
        $bMote.Dispose()
    } elseif ($f -eq 3) {
        # Fading fine diamond dust
        $bDust = New-Object System.Drawing.SolidBrush((Clr 85 180 235 255))
        for ($m = 0; $m -lt 8; $m++) {
            $mAng = $m * 0.785 + 0.2
            $dist = (28.0 + ($m % 2) * 4.0) * $scale
            $mx = $cx + [Math]::Cos($mAng) * $dist
            $my = $cy + [Math]::Sin($mAng) * $dist
            Fill-EllipseCentered $gfx $bDust $mx $my (1.2 * $scale) (1.2 * $scale)
        }
        $bDust.Dispose()
    }
}

# 2. Electric (E209) - 제우스 뇌전 방전 (Zeus Lightning Zap)
function Draw-ElectricHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # High voltage detonation flash
        $bFlash = New-Object System.Drawing.SolidBrush((Clr 180 255 225 40))
        Fill-EllipseCentered $gfx $bFlash $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy (5.0 * $scale) (5.0 * $scale)
        $bFlash.Dispose(); $bCore.Dispose()

        # 4 mini discharge prongs
        $pProng = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.6 * $scale))
        $gfx.DrawLine($pProng, ($cx - 10.0 * $scale), $cy, ($cx + 10.0 * $scale), $cy)
        $gfx.DrawLine($pProng, $cx, ($cy - 10.0 * $scale), $cx, ($cy + 10.0 * $scale))
        $pProng.Dispose()
    } elseif ($f -eq 1) {
        # 6 sharp jagged lightning arcs bursting outward
        $pOuter = New-Object System.Drawing.Pen((Clr 250 255 210 20), (2.4 * $scale))
        $pOuter.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
        $pInner = New-Object System.Drawing.Pen((Clr 255 255 255 240), (1.0 * $scale))
        $pInner.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
        $bNode = New-Object System.Drawing.SolidBrush((Clr 255 255 245 180))

        $angles = @(0.15, 1.20, 2.25, 3.30, 4.35, 5.40)
        for ($a = 0; $a -lt 6; $a++) {
            $ang = $angles[$a]
            $r1 = 5.0 * $scale
            $r2 = 13.0 * $scale
            $r3 = 20.0 * $scale

            $p1 = Pt ($cx + [Math]::Cos($ang) * $r1) ($cy + [Math]::Sin($ang) * $r1)
            $p2 = Pt ($cx + [Math]::Cos($ang + 0.28) * $r2) ($cy + [Math]::Sin($ang + 0.28) * $r2)
            $p3 = Pt ($cx + [Math]::Cos($ang - 0.15) * $r3) ($cy + [Math]::Sin($ang - 0.15) * $r3)

            $pts = @($p1, $p2, $p3)
            $gfx.DrawLines($pOuter, $pts)
            $gfx.DrawLines($pInner, $pts)
            Fill-EllipseCentered $gfx $bNode $p3.X $p3.Y (1.6 * $scale) (1.6 * $scale)
        }
        $pOuter.Dispose(); $pInner.Dispose(); $bNode.Dispose()
    } elseif ($f -eq 2) {
        # Snapping detached lightning segments
        $pArc = New-Object System.Drawing.Pen((Clr 165 255 215 30), (1.5 * $scale))
        $pArc.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
        $bSpk = New-Object System.Drawing.SolidBrush((Clr 180 255 245 150))

        $angles = @(0.25, 1.30, 2.35, 3.40, 4.45, 5.50)
        for ($a = 0; $a -lt 6; $a++) {
            $ang = $angles[$a]
            $r1 = 17.0 * $scale
            $r2 = 26.5 * $scale

            $p1 = Pt ($cx + [Math]::Cos($ang + 0.2) * $r1) ($cy + [Math]::Sin($ang + 0.2) * $r1)
            $p2 = Pt ($cx + [Math]::Cos($ang - 0.1) * $r2) ($cy + [Math]::Sin($ang - 0.1) * $r2)
            $gfx.DrawLine($pArc, $p1, $p2)
            Fill-EllipseCentered $gfx $bSpk $p2.X $p2.Y (1.3 * $scale) (1.3 * $scale)
        }
        $pArc.Dispose(); $bSpk.Dispose()
    } elseif ($f -eq 3) {
        # Fading electric ion sparks
        $bDust = New-Object System.Drawing.SolidBrush((Clr 80 255 225 60))
        for ($s = 0; $s -lt 8; $s++) {
            $ang = $s * 0.785 + 0.35
            $dist = (27.0 + ($s % 2) * 4.0) * $scale
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $cy + [Math]::Sin($ang) * $dist
            Fill-EllipseCentered $gfx $bDust $sx $sy (1.2 * $scale) (1.2 * $scale)
        }
        $bDust.Dispose()
    }
}

# 3. Wind (E210) - 돌풍 소용돌이 파열 (Gale Wind Burst)
function Draw-WindHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # Compressed vacuum detonation
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 150 40 240 210))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 220 255 250))
        Fill-EllipseCentered $gfx $bCore $cx $cy (4.5 * $scale) (4.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

        # 4 tight curled crescent hooks
        $pHook = New-Object System.Drawing.Pen((Clr 230 140 255 240), (1.6 * $scale))
        for ($w = 0; $w -lt 4; $w++) {
            $ang = $w * 1.57
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($cx + [Math]::Cos($ang) * 3.0 * $scale) ($cy + [Math]::Sin($ang) * 3.0 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.4) * 6.5 * $scale) ($cy + [Math]::Sin($ang + 0.4) * 6.5 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.8) * 9.5 * $scale) ($cy + [Math]::Sin($ang + 0.8) * 9.5 * $scale))
            )
            $gfx.DrawCurve($pHook, $pts)
        }
        $pHook.Dispose()
    } elseif ($f -eq 1) {
        # 4 expanding razor-sharp crescent wind blades
        $pRing = New-Object System.Drawing.Pen((Clr 90 30 220 190), (1.2 * $scale))
        Draw-EllipseCentered $gfx $pRing $cx $cy (14.0 * $scale) (14.0 * $scale)
        $pRing.Dispose()

        $pOuter = New-Object System.Drawing.Pen((Clr 240 50 245 220), (2.2 * $scale))
        $pInner = New-Object System.Drawing.Pen((Clr 255 210 255 250), (1.0 * $scale))
        for ($w = 0; $w -lt 4; $w++) {
            $ang = $w * 1.57 + 0.35
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($cx + [Math]::Cos($ang) * 6.0 * $scale) ($cy + [Math]::Sin($ang) * 6.0 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.45) * 13.5 * $scale) ($cy + [Math]::Sin($ang + 0.45) * 13.5 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.85) * 20.5 * $scale) ($cy + [Math]::Sin($ang + 0.85) * 20.5 * $scale))
            )
            $gfx.DrawCurve($pOuter, $pts)
            $gfx.DrawCurve($pInner, $pts)
        }
        $pOuter.Dispose(); $pInner.Dispose()
    } elseif ($f -eq 2) {
        # Elongated spiraling wind vortex trails
        $pRing = New-Object System.Drawing.Pen((Clr 60 30 220 190), (1.0 * $scale))
        Draw-EllipseCentered $gfx $pRing $cx $cy (22.0 * $scale) (22.0 * $scale)
        $pRing.Dispose()

        $pTrail = New-Object System.Drawing.Pen((Clr 150 80 250 230), (1.5 * $scale))
        for ($w = 0; $w -lt 4; $w++) {
            $ang = $w * 1.57 + 0.75
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($cx + [Math]::Cos($ang) * 13.0 * $scale) ($cy + [Math]::Sin($ang) * 13.0 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.35) * 20.5 * $scale) ($cy + [Math]::Sin($ang + 0.35) * 20.5 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.65) * 27.5 * $scale) ($cy + [Math]::Sin($ang + 0.65) * 27.5 * $scale))
            )
            $gfx.DrawCurve($pTrail, $pts)
        }
        $pTrail.Dispose()
    } elseif ($f -eq 3) {
        # Fading air vortex wisps
        $bWisp = New-Object System.Drawing.SolidBrush((Clr 75 80 250 230))
        for ($w = 0; $w -lt 6; $w++) {
            $ang = $w * 1.05 + 0.5
            $dist = (27.0 + ($w % 2) * 4.0) * $scale
            $wx = $cx + [Math]::Cos($ang) * $dist
            $wy = $cy + [Math]::Sin($ang) * $dist
            Fill-EllipseCentered $gfx $bWisp $wx $wy (1.2 * $scale) (1.2 * $scale)
        }
        $bWisp.Dispose()
    }
}

# 4. Earth (E211) - 흙/암석 파편 비산 (Earth & Soil Shatter)
function Draw-EarthHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # Earthen impact shock detonation
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 170 245 180 80))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 240 180))
        Fill-EllipseCentered $gfx $bCore $cx $cy (4.5 * $scale) (4.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

        # 4 initial stone chunks breaking free
        $bChunk = New-Object System.Drawing.SolidBrush((Clr 240 170 95 35))
        for ($i = 0; $i -lt 4; $i++) {
            $ang = $i * 1.57 + 0.4
            $rx = $cx + [Math]::Cos($ang) * 6.0 * $scale
            $ry = $cy + [Math]::Sin($ang) * 6.0 * $scale
            Fill-EllipseCentered $gfx $bChunk $rx $ry (2.5 * $scale) (2.0 * $scale)
        }
        $bChunk.Dispose()
    } elseif ($f -eq 1) {
        # 6 angular faceted stone shards bursting outward
        $pDust = New-Object System.Drawing.Pen((Clr 100 195 130 60), (1.4 * $scale))
        Draw-EllipseCentered $gfx $pDust $cx $cy (14.0 * $scale) (14.0 * $scale)
        $pDust.Dispose()

        $angles = @(0.35, 1.45, 2.50, 3.55, 4.65, 5.75)
        for ($i = 0; $i -lt 6; $i++) {
            $ang = $angles[$i]
            $dist = (15.5 + ($i % 2) * 3.0) * $scale
            $rx = $cx + [Math]::Cos($ang) * $dist
            $ry = $cy + [Math]::Sin($ang) * $dist
            $sz = 4.0 * $scale * (0.85 + ($i % 3) * 0.15)

            $pts = [System.Drawing.PointF[]]@(
                (Pt ($rx - $sz) ($ry - $sz * 0.4)),
                (Pt ($rx + $sz * 0.2) ($ry - $sz * 0.9)),
                (Pt ($rx + $sz) ($ry + $sz * 0.3)),
                (Pt ($rx - $sz * 0.3) ($ry + $sz * 0.8))
            )
            $bRock = New-Object System.Drawing.SolidBrush((Clr 245 145 80 30))
            $pRock = New-Object System.Drawing.Pen((Clr 255 225 165 95), (1.0 * $scale))
            $gfx.FillPolygon($bRock, $pts)
            $gfx.DrawPolygon($pRock, $pts)
            $bRock.Dispose(); $pRock.Dispose()
        }

        # Fine gravel crumbs
        $bGravel = New-Object System.Drawing.SolidBrush((Clr 200 210 145 70))
        for ($g = 0; $g -lt 4; $g++) {
            $gAng = $g * 1.57 + 0.9
            $gx = $cx + [Math]::Cos($gAng) * 9.5 * $scale
            $gy = $cy + [Math]::Sin($gAng) * 9.5 * $scale
            Fill-EllipseCentered $gfx $bGravel $gx $gy (1.5 * $scale) (1.5 * $scale)
        }
        $bGravel.Dispose()
    } elseif ($f -eq 2) {
        # Tumbling rock fragments and coarse soil gravel
        $angles = @(0.40, 1.50, 2.55, 3.60, 4.70, 5.80)
        for ($i = 0; $i -lt 6; $i++) {
            $ang = $angles[$i]
            $dist = (23.5 + ($i % 2) * 3.5) * $scale
            $rx = $cx + [Math]::Cos($ang) * $dist
            $ry = $cy + [Math]::Sin($ang) * $dist
            $sz = 2.8 * $scale

            $pts = [System.Drawing.PointF[]]@(
                (Pt ($rx - $sz) ($ry - $sz * 0.4)),
                (Pt ($rx + $sz * 0.2) ($ry - $sz * 0.8)),
                (Pt ($rx + $sz) ($ry + $sz * 0.3)),
                (Pt ($rx - $sz * 0.2) ($ry + $sz * 0.7))
            )
            $bRock = New-Object System.Drawing.SolidBrush((Clr 165 150 85 35))
            $pRock = New-Object System.Drawing.Pen((Clr 170 220 160 90), (0.8 * $scale))
            $gfx.FillPolygon($bRock, $pts)
            $gfx.DrawPolygon($pRock, $pts)
            $bRock.Dispose(); $pRock.Dispose()
        }

        # Gravel bits
        $bGravel = New-Object System.Drawing.SolidBrush((Clr 140 190 130 60))
        for ($g = 0; $g -lt 6; $g++) {
            $gAng = $g * 1.05 + 0.3
            $gx = $cx + [Math]::Cos($gAng) * 17.0 * $scale
            $gy = $cy + [Math]::Sin($gAng) * 17.0 * $scale
            Fill-EllipseCentered $gfx $bGravel $gx $gy (1.3 * $scale) (1.3 * $scale)
        }
        $bGravel.Dispose()
    } elseif ($f -eq 3) {
        # Fine sand & clay dust motes settling
        $bDust = New-Object System.Drawing.SolidBrush((Clr 85 190 135 65))
        for ($s = 0; $s -lt 8; $s++) {
            $ang = $s * 0.785 + 0.25
            $dist = (27.0 + ($s % 2) * 4.0) * $scale
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $cy + [Math]::Sin($ang) * $dist
            Fill-EllipseCentered $gfx $bDust $sx $sy (1.2 * $scale) (1.2 * $scale)
        }
        $bDust.Dispose()
    }
}

# 5. Light (E212) - 빛의 사방 방사 섬광 (Radiant Starburst Nova)
function Draw-LightHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # Solar nova detonation
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 170 255 240 130))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (12.0 * $scale) (12.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy (5.5 * $scale) (5.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

        # 4 sharp cardinal flare beams
        $pFlare = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.8 * $scale))
        $gfx.DrawLine($pFlare, ($cx - 12.0 * $scale), $cy, ($cx + 12.0 * $scale), $cy)
        $gfx.DrawLine($pFlare, $cx, ($cy - 12.0 * $scale), $cx, ($cy + 12.0 * $scale))
        $pFlare.Dispose()
    } elseif ($f -eq 1) {
        # 8-ray radiant light starburst spears + prism ring
        $pRing = New-Object System.Drawing.Pen((Clr 110 255 245 160), (1.2 * $scale))
        Draw-EllipseCentered $gfx $pRing $cx $cy (14.0 * $scale) (14.0 * $scale)
        $pRing.Dispose()

        $pOuter = New-Object System.Drawing.Pen((Clr 240 255 235 90), (2.2 * $scale))
        $pInner = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.0 * $scale))
        for ($r = 0; $r -lt 8; $r++) {
            $ang = $r * ([Math]::PI / 4.0)
            $len = if ($r % 2 -eq 0) { 21.0 * $scale } else { 15.0 * $scale }
            $x2 = $cx + [Math]::Cos($ang) * $len
            $y2 = $cy + [Math]::Sin($ang) * $len
            $gfx.DrawLine($pOuter, $cx, $cy, $x2, $y2)
            $gfx.DrawLine($pInner, $cx, $cy, $x2, $y2)
        }
        $pOuter.Dispose(); $pInner.Dispose()
    } elseif ($f -eq 2) {
        # Expanding holy light ring and detaching spear tips
        $pRing = New-Object System.Drawing.Pen((Clr 75 255 240 140), (1.0 * $scale))
        Draw-EllipseCentered $gfx $pRing $cx $cy (23.0 * $scale) (23.0 * $scale)
        $pRing.Dispose()

        $pOuter = New-Object System.Drawing.Pen((Clr 160 255 235 120), (1.6 * $scale))
        $pInner = New-Object System.Drawing.Pen((Clr 180 255 255 255), (0.8 * $scale))
        for ($r = 0; $r -lt 8; $r++) {
            $ang = $r * ([Math]::PI / 4.0)
            $dStart = 16.0 * $scale
            $dEnd = if ($r % 2 -eq 0) { 27.5 * $scale } else { 21.5 * $scale }
            $x1 = $cx + [Math]::Cos($ang) * $dStart
            $y1 = $cy + [Math]::Sin($ang) * $dStart
            $x2 = $cx + [Math]::Cos($ang) * $dEnd
            $y2 = $cy + [Math]::Sin($ang) * $dEnd
            $gfx.DrawLine($pOuter, $x1, $y1, $x2, $y2)
            $gfx.DrawLine($pInner, $x1, $y1, $x2, $y2)
        }
        $pOuter.Dispose(); $pInner.Dispose()
    } elseif ($f -eq 3) {
        # Radiant golden star glitter dissolving
        $bDust = New-Object System.Drawing.SolidBrush((Clr 80 255 245 160))
        for ($s = 0; $s -lt 8; $s++) {
            $ang = $s * 0.785 + 0.3
            $dist = (27.5 + ($s % 2) * 4.0) * $scale
            $sx = $cx + [Math]::Cos($ang) * $dist
            $sy = $cy + [Math]::Sin($ang) * $dist
            Fill-EllipseCentered $gfx $bDust $sx $sy (1.2 * $scale) (1.2 * $scale)
        }
        $bDust.Dispose()
    }
}

# 6. Darkness (E213) - 시공간 왜곡 보이드 (Spacetime Distortion Warp)
function Draw-DarkHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
        # Singularity collapse: purple glow + magenta accretion ring + void core
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 160 160 30 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (12.0 * $scale) (12.0 * $scale)
        $bGlow.Dispose()

        $pAccretion = New-Object System.Drawing.Pen((Clr 255 240 80 255), (2.0 * $scale))
        Draw-EllipseCentered $gfx $pAccretion $cx $cy (8.0 * $scale) (8.0 * $scale)
        $pAccretion.Dispose()

        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 12 5 22))
        Fill-EllipseCentered $gfx $bVoid $cx $cy (5.5 * $scale) (5.5 * $scale)
        $bVoid.Dispose()

        $pRim = New-Object System.Drawing.Pen((Clr 220 255 200 255), (1.0 * $scale))
        Draw-EllipseCentered $gfx $pRim $cx $cy (5.5 * $scale) (5.5 * $scale)
        $pRim.Dispose()
    } elseif ($f -eq 1) {
        # 4 curved spacetime gravity shear arcs + distorted oval reality ripple
        $pRipple = New-Object System.Drawing.Pen((Clr 110 170 40 240), (1.2 * $scale))
        Draw-EllipseCentered $gfx $pRipple $cx $cy (15.5 * $scale) (12.5 * $scale)
        $pRipple.Dispose()

        $pOuter = New-Object System.Drawing.Pen((Clr 245 200 65 255), (2.2 * $scale))
        $pInner = New-Object System.Drawing.Pen((Clr 255 250 190 255), (1.0 * $scale))
        for ($k = 0; $k -lt 4; $k++) {
            $ang = $k * 1.57 + 0.25
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($cx + [Math]::Cos($ang) * 5.5 * $scale) ($cy + [Math]::Sin($ang) * 5.5 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.4) * 13.0 * $scale) ($cy + [Math]::Sin($ang + 0.4) * 13.0 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.75) * 20.5 * $scale) ($cy + [Math]::Sin($ang + 0.75) * 20.5 * $scale))
            )
            $gfx.DrawCurve($pOuter, $pts)
            $gfx.DrawCurve($pInner, $pts)
        }
        $pOuter.Dispose(); $pInner.Dispose()

        # Mini void core
        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 12 5 22))
        Fill-EllipseCentered $gfx $bVoid $cx $cy (3.5 * $scale) (3.5 * $scale)
        $bVoid.Dispose()
    } elseif ($f -eq 2) {
        # Expanding void distortions
        $pRipple = New-Object System.Drawing.Pen((Clr 70 170 40 240), (1.0 * $scale))
        Draw-EllipseCentered $gfx $pRipple $cx $cy (23.5 * $scale) (18.5 * $scale)
        $pRipple.Dispose()

        $pTrail = New-Object System.Drawing.Pen((Clr 160 210 80 255), (1.5 * $scale))
        for ($k = 0; $k -lt 4; $k++) {
            $ang = $k * 1.57 + 0.65
            $pts = [System.Drawing.PointF[]]@(
                (Pt ($cx + [Math]::Cos($ang) * 13.0 * $scale) ($cy + [Math]::Sin($ang) * 13.0 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.3) * 20.5 * $scale) ($cy + [Math]::Sin($ang + 0.3) * 20.5 * $scale)),
                (Pt ($cx + [Math]::Cos($ang + 0.55) * 27.5 * $scale) ($cy + [Math]::Sin($ang + 0.55) * 27.5 * $scale))
            )
            $gfx.DrawCurve($pTrail, $pts)
        }
        $pTrail.Dispose()
    } elseif ($f -eq 3) {
        # Evaporating dark cosmic dust motes
        $bDust = New-Object System.Drawing.SolidBrush((Clr 80 210 90 255))
        for ($k = 0; $k -lt 6; $k++) {
            $ang = $k * 1.05 + 0.4
            $dist = (27.0 + ($k % 2) * 4.0) * $scale
            $kx = $cx + [Math]::Cos($ang) * $dist
            $ky = $cy + [Math]::Sin($ang) * $dist
            Fill-EllipseCentered $gfx $bDust $kx $ky (1.2 * $scale) (1.2 * $scale)
        }
        $bDust.Dispose()
    }
}

# ==============================================================================
# BUILD SHOWCASE BANNER (1200 x 680)
# ==============================================================================
$bmpShowcase = New-Object System.Drawing.Bitmap(1200, 680)
$gShow = [System.Drawing.Graphics]::FromImage($bmpShowcase)
$gShow.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gShow.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gShow.Clear([System.Drawing.Color]::FromArgb(18, 20, 26))

$title = Get-Utf8Str @(0xED, 0x83, 0x80, 0xEA, 0xB2, 0x8F, 0x28, 0xEB, 0x8B, 0xA8, 0xEC, 0x9D, 0xBC, 0x29, 0x20, 0xED, 0x83, 0x80, 0xEC, 0x9B, 0x8C, 0x20, 0xED, 0x94, 0xBC, 0xEA, 0xB2, 0xA9, 0x20, 0xEC, 0x9D, 0xB4, 0xED, 0x8E, 0x99, 0xED, 0x8A, 0xB8, 0x20, 0x36, 0xEC, 0x87, 0x85, 0x20, 0xEB, 0x94, 0x94, 0xEC, 0x9E, 0x90, 0xEC, 0x9D, 0xB8, 0x20, 0xEC, 0x8B, 0x9C, 0xEC, 0x95, 0x88)
$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
$fLabel = New-Object System.Drawing.Font('Malgun Gothic', 10, [System.Drawing.FontStyle]::Bold)
$fSub   = New-Object System.Drawing.Font('Malgun Gothic', 8, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$bSub   = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center

$gShow.DrawString($title, $fTitle, $bWhite, 600.0, 15.0, $sfCenter)

$effects = @(
    [pscustomobject]@{ Key = "Ice";      Name = (Get-Utf8Str @(0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C, 0x20, 0x28, 0x45, 0x32, 0x30, 0x38, 0x29, 0x20, 0x2D, 0x20, 0xEA, 0xB3, 0xA0, 0xEB, 0x93, 0x9C, 0xEB, 0xA6, 0x84, 0x20, 0xED, 0x8C, 0x8C, 0xED, 0x8E, 0xB8, 0x20, 0xEB, 0xB9, 0x84, 0xEC, 0x82, 0xB0));  Desc = (Get-Utf8Str @(0xEC, 0x99, 0x80, 0xEC, 0x9E, 0xA5, 0xEC, 0xB0, 0xBD, 0x20, 0xEA, 0xB9, 0xA8, 0xEC, 0xA7, 0x80, 0xEB, 0xA9, 0xB0, 0x20, 0xEC, 0x82, 0xAC, 0xEB, 0xB0, 0xA9, 0xEC, 0x9C, 0xBC, 0xEB, 0xA1, 0x9C, 0x20, 0xED, 0x8A, 0x80, 0xEB, 0x8A, 0x94, 0x20, 0xEB, 0x82, 0xA0, 0xEC, 0x89, 0x90, 0xED, 0x95, 0x9C, 0x20, 0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C, 0x20, 0xEA, 0xB2, 0xB0, 0xEC, 0xA0, 0x95, 0x20, 0xED, 0x8C, 0x8C, 0xED, 0x8E, 0xB8)); Color = (Clr 255 100 210 255); Draw = { param($g, $x, $y, $f) Draw-IceHitFrame $g $x $y $f 1.15 } },
    [pscustomobject]@{ Key = "Electric"; Name = (Get-Utf8Str @(0xEC, 0xA0, 0x84, 0xEA, 0xB8, 0xB0, 0x20, 0x28, 0x45, 0x32, 0x30, 0x39, 0x29, 0x20, 0x2D, 0x20, 0xEC, 0xA0, 0x9C, 0xEC, 0x9A, 0xB0, 0xEC, 0x8A, 0xA4, 0x20, 0xEB, 0x87, 0x8C, 0xEC, 0xA0, 0x84, 0x20, 0xEB, 0xB0, 0x89, 0xEC, 0xA0, 0x84));  Desc = (Get-Utf8Str @(0xED, 0x94, 0xBC, 0xEA, 0xB2, 0xA9, 0x20, 0xEB, 0xB6, 0x80, 0xEC, 0x9C, 0x84, 0xEC, 0x97, 0x90, 0xEC, 0x84, 0x9C, 0x20, 0x38, 0xEB, 0xB0, 0xA9, 0xED, 0x96, 0xA5, 0xEC, 0x9C, 0xBC, 0xEB, 0xA1, 0x9C, 0x20, 0xED, 0x8C, 0x8C, 0xEB, 0xB0, 0x94, 0xEB, 0xB0, 0x95, 0x20, 0xED, 0x8A, 0x80, 0xEB, 0x8A, 0x94, 0x20, 0xED, 0x99, 0xA9, 0xEA, 0xB8, 0x88, 0x20, 0xEB, 0xB2, 0x88, 0xEA, 0xB0, 0x9C, 0x20, 0xEC, 0x95, 0x84, 0xED, 0x81, 0xAC)); Color = (Clr 255 255 235 59);  Draw = { param($g, $x, $y, $f) Draw-ElectricHitFrame $g $x $y $f 1.15 } },
    [pscustomobject]@{ Key = "Wind";     Name = (Get-Utf8Str @(0xEB, 0xB0, 0x94, 0xEB, 0x9E, 0x8C, 0x20, 0x28, 0x45, 0x32, 0x31, 0x30, 0x29, 0x20, 0x2D, 0x20, 0xEB, 0x8F, 0x8C, 0xED, 0x92, 0x8D, 0x20, 0xEC, 0x86, 0x8C, 0xEC, 0x9A, 0xA9, 0xEB, 0x8F, 0x8C, 0xEC, 0x9D, 0xB4, 0x20, 0xED, 0x8C, 0x8C, 0xEC, 0x97, 0xB4)); Desc = (Get-Utf8Str @(0xEC, 0xA4, 0x91, 0xEC, 0x8B, 0xAC, 0x20, 0xEC, 0x95, 0x95, 0xEC, 0xB6, 0x95, 0x20, 0xED, 0x9B, 0x84, 0x20, 0xEC, 0x9B, 0x90, 0xED, 0x98, 0x95, 0xEC, 0x9C, 0xBC, 0xEB, 0xA1, 0x9C, 0x20, 0xEC, 0x86, 0x8C, 0xEC, 0x9A, 0xA9, 0xEB, 0x8F, 0x8C, 0xEC, 0x9D, 0xB4, 0xEC, 0xB9, 0x98, 0xEB, 0xA9, 0xB0, 0x20, 0xED, 0x84, 0xB0, 0xEC, 0xA7, 0x80, 0xEB, 0x8A, 0x94, 0x20, 0xEC, 0xB2, 0xAD, 0xEB, 0x87, 0x9D, 0x20, 0xEB, 0xB0, 0x94, 0xEB, 0x9E, 0x8C, 0x20, 0xEC, 0xBB, 0xAC, 0xEB, 0x82, 0xA0)); Color = (Clr 255 64 255 218);  Draw = { param($g, $x, $y, $f) Draw-WindHitFrame $g $x $y $f 1.15 } },
    [pscustomobject]@{ Key = "Earth";    Name = (Get-Utf8Str @(0xEB, 0x8C, 0x80, 0xEC, 0xA7, 0x80, 0x20, 0x28, 0x45, 0x32, 0x31, 0x31, 0x29, 0x20, 0x2D, 0x20, 0xED, 0x9D, 0x99, 0x2F, 0xEC, 0x95, 0x94, 0xEC, 0x84, 0x9D, 0x20, 0xED, 0x8C, 0x8C, 0xED, 0x8E, 0xB8, 0x20, 0xEB, 0xB9, 0x84, 0xEC, 0x82, 0xB0));   Desc = (Get-Utf8Str @(0xED, 0x8D, 0xBD, 0x20, 0xEB, 0xB6, 0x80, 0xEC, 0x84, 0x9C, 0xEC, 0xA7, 0x80, 0xEB, 0xA9, 0xB0, 0x20, 0xED, 0x8A, 0x80, 0xEB, 0x8A, 0x94, 0x20, 0xEB, 0xAC, 0xB5, 0xEC, 0x93, 0x81, 0xED, 0x95, 0x9C, 0x20, 0xEB, 0xB0, 0x94, 0xEC, 0x9C, 0x84, 0x20, 0xED, 0x8C, 0x8C, 0xED, 0x8E, 0xB8, 0xEA, 0xB3, 0xBC, 0x20, 0xED, 0x99, 0xA9, 0xED, 0x86, 0xA0, 0x20, 0xEB, 0x88, 0x88, 0xEC, 0xA7, 0x80, 0x20, 0xEC, 0xB6, 0x20, 0xEA, 0xB2, 0xA9, 0xED, 0x8C, 0x8C)); Color = (Clr 255 215 140 70);  Draw = { param($g, $x, $y, $f) Draw-EarthHitFrame $g $x $y $f 1.15 } },
    [pscustomobject]@{ Key = "Light";    Name = (Get-Utf8Str @(0xEB, 0xB9, 0x9B, 0x20, 0x28, 0x45, 0x32, 0x31, 0x32, 0x29, 0x20, 0x2D, 0x20, 0xEB, 0xB9, 0x9B, 0xEC, 0x9D, 0x98, 0x20, 0xEC, 0x82, 0xAC, 0xEB, 0xB0, 0xA9, 0x20, 0xEB, 0xB0, 0x89, 0xEC, 0x82, 0xAC, 0x20, 0xEC, 0x84, 0xAC, 0xEA, 0xB4, 0x91));   Desc = (Get-Utf8Str @(0xEB, 0x88, 0x88, 0xEB, 0xB6, 0x80, 0xEC, 0x8B, 0xA0, 0x20, 0x38, 0xEB, 0xB0, 0xA9, 0xED, 0x96, 0xA5, 0x20, 0xEC, 0x8A, 0xA4, 0xED, 0x83, 0x80, 0xEB, 0xB2, 0x84, 0xEC, 0x8A, 0xA4, 0xED, 0x8A, 0xB8, 0xEC, 0x99, 0x80, 0x20, 0xEC, 0x8D, 0xAD, 0xEC, 0x9E, 0x90, 0x20, 0xEB, 0xB9, 0x9B, 0xEC, 0x82, 0xB4, 0xEC, 0x9D, 0xB4, 0x20, 0xEC, 0x82, 0xAC, 0xEB, 0xB0, 0xA9, 0xEC, 0x9C, 0xBC, 0xEB, 0xA1, 0x9C, 0x20, 0xEB, 0xB0, 0x89, 0xEC, 0x82, 0xAC)); Color = (Clr 255 255 255 190); Draw = { param($g, $x, $y, $f) Draw-LightHitFrame $g $x $y $f 1.15 } },
    [pscustomobject]@{ Key = "Dark";     Name = (Get-Utf8Str @(0xEC, 0x96, 0xB4, 0xEB, 0x91, 0xA0, 0x20, 0x28, 0x45, 0x32, 0x31, 0x33, 0x29, 0x20, 0x2D, 0x20, 0xEC, 0x8B, 0x9C, 0xEA, 0xB3, 0xB5, 0xEA, 0xB0, 0x84, 0x20, 0xEC, 0x99, 0x9C, 0xEA, 0xB3, 0xA1, 0x20, 0xEB, 0xB3, 0xB4, 0xEC, 0x9D, 0xB4, 0xEB, 0x93, 0x99)); Desc = (Get-Utf8Str @(0xEB, 0xB9, 0x9B, 0xEC, 0x9D, 0x84, 0x20, 0xEB, 0xB9, 0xA8, 0xEC, 0x95, 0x84, 0xEB, 0x93, 0xA4, 0xEC, 0x9D, 0xB4, 0xEB, 0x8A, 0x94, 0x20, 0xEB, 0xB8, 0x94, 0xEB, 0x9E, 0x99, 0xED, 0x99, 0x80, 0x20, 0xEC, 0xBD, 0x94, 0xEC, 0x96, 0xB4, 0xEC, 0x99, 0x80, 0x20, 0xED, 0x8C, 0xBD, 0xEC, 0xB0, 0xBD, 0xED, 0x95, 0x98, 0xEB, 0x8A, 0x94, 0x20, 0xEC, 0x8B, 0x9C, 0xEA, 0xB3, 0xB5, 0xEA, 0xB0, 0x84, 0x20, 0xEA, 0xB7, 0xA0, 0xEC, 0x97, 0xB4, 0x20, 0xED, 0x8C, 0x8C, 0xEB, 0x8F, 0x99)); Color = (Clr 255 195 125 255); Draw = { param($g, $x, $y, $f) Draw-DarkHitFrame $g $x $y $f 1.15 } }
)

$rowH = 95
$startY = 55

for ($i = 0; $i -lt $effects.Count; $i++) {
    $eff = $effects[$i]
    $rowY = $startY + $i * $rowH

    $bRow = New-Object System.Drawing.SolidBrush((Clr 255 26 29 37))
    $pRow = New-Object System.Drawing.Pen((Clr 60 255 255 255), 1.0)
    $rect = New-Object System.Drawing.Rectangle(20, $rowY, 1160, ($rowH - 8))
    $gShow.FillRectangle($bRow, $rect)
    $gShow.DrawRectangle($pRow, $rect)
    $bRow.Dispose(); $pRow.Dispose()

    $bLbl = New-Object System.Drawing.SolidBrush($eff.Color)
    $sfLeft = New-Object System.Drawing.StringFormat
    $gShow.DrawString($eff.Name, $fLabel, $bLbl, 35.0, ($rowY + 18), $sfLeft)
    $gShow.DrawString($eff.Desc, $fSub, $bSub, 35.0, ($rowY + 45), $sfLeft)
    $bLbl.Dispose(); $sfLeft.Dispose()

    for ($f = 0; $f -lt 4; $f++) {
        $cellX = 480.0 + $f * 170.0
        $cellY = $rowY + ($rowH - 8) / 2.0

        # Frame background box
        $bCell = New-Object System.Drawing.SolidBrush((Clr 255 18 20 28))
        $pFrame = New-Object System.Drawing.Pen((Clr 50 255 255 255), 1.0)
        $cellBox = New-Object System.Drawing.Rectangle(($cellX - 42), ($cellY - 38), 84, 76)
        $gShow.FillRectangle($bCell, $cellBox)
        $gShow.DrawRectangle($pFrame, $cellBox)
        $bCell.Dispose(); $pFrame.Dispose()

        $sfTag = New-Object System.Drawing.StringFormat
        $sfTag.Alignment = [System.Drawing.StringAlignment]::Center
        $gShow.DrawString("F$f", $fSub, $bSub, $cellX, ($cellY - 36), $sfTag)
        $sfTag.Dispose()

        # Render effect safely clipped inside cell
        $gShow.SetClip($cellBox)
        & $eff.Draw $gShow $cellX ($cellY + 4) $f
        $gShow.ResetClip()
    }
}

$fTitle.Dispose(); $fLabel.Dispose(); $fSub.Dispose(); $bWhite.Dispose(); $bSub.Dispose(); $sfCenter.Dispose()

$previewPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_target_hit_effects.png"
if (Test-Path $previewPath) { [System.IO.File]::Delete($previewPath) }
$bmpShowcase.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)

$rootPath = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\preview_target_hit_effects.png"
Copy-Item $previewPath $rootPath -Force
$gShow.Dispose(); $bmpShowcase.Dispose()
Write-Output "Saved Hit Effects Preview to $previewPath and $rootPath"
