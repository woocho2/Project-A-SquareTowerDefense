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
# DRAWING FUNCTIONS FOR THE 6 TARGET HIT EFFECTS
# ==============================================================================

# 1. Ice (E208) - 고드름 파편 비산 (Icicle Shatter)
function Draw-IceHitFrame($gfx, [float]$cx, [float]$cy, [int]$f, [float]$scale) {
    if ($f -gt 3) { return }

    if ($f -eq 0) {
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

        $pNeedle = New-Object System.Drawing.Pen((Clr 240 200 245 255), (1.5 * $scale))
        $gfx.DrawLine($pNeedle, ($cx - 10.0 * $scale), $cy, ($cx + 10.0 * $scale), $cy)
        $pNeedle.Dispose()
    } elseif ($f -eq 1) {
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

        $bSpark = New-Object System.Drawing.SolidBrush((Clr 220 220 250 255))
        for ($m = 0; $m -lt 4; $m++) {
            $mAng = $m * 1.57 + 0.8
            $mx = $cx + [Math]::Cos($mAng) * (10.0 * $scale)
            $my = $cy + [Math]::Sin($mAng) * (10.0 * $scale)
            Fill-EllipseCentered $gfx $bSpark $mx $my (1.6 * $scale) (1.6 * $scale)
        }
        $bSpark.Dispose()
    } elseif ($f -eq 2) {
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

        $bMote = New-Object System.Drawing.SolidBrush((Clr 140 200 245 255))
        for ($m = 0; $m -lt 6; $m++) {
            $mAng = $m * 1.05 + 0.4
            $mx = $cx + [Math]::Cos($mAng) * (18.0 * $scale)
            $my = $cy + [Math]::Sin($mAng) * (18.0 * $scale)
            Fill-EllipseCentered $gfx $bMote $mx $my (1.3 * $scale) (1.3 * $scale)
        }
        $bMote.Dispose()
    } elseif ($f -eq 3) {
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
        $bFlash = New-Object System.Drawing.SolidBrush((Clr 180 255 225 40))
        Fill-EllipseCentered $gfx $bFlash $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy (5.0 * $scale) (5.0 * $scale)
        $bFlash.Dispose(); $bCore.Dispose()

        $pProng = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.6 * $scale))
        $gfx.DrawLine($pProng, ($cx - 10.0 * $scale), $cy, ($cx + 10.0 * $scale), $cy)
        $gfx.DrawLine($pProng, $cx, ($cy - 10.0 * $scale), $cx, ($cy + 10.0 * $scale))
        $pProng.Dispose()
    } elseif ($f -eq 1) {
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
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 150 40 240 210))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 220 255 250))
        Fill-EllipseCentered $gfx $bCore $cx $cy (4.5 * $scale) (4.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

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
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 170 245 180 80))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (11.0 * $scale) (11.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 240 180))
        Fill-EllipseCentered $gfx $bCore $cx $cy (4.5 * $scale) (4.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

        $bChunk = New-Object System.Drawing.SolidBrush((Clr 240 170 95 35))
        for ($i = 0; $i -lt 4; $i++) {
            $ang = $i * 1.57 + 0.4
            $rx = $cx + [Math]::Cos($ang) * 6.0 * $scale
            $ry = $cy + [Math]::Sin($ang) * 6.0 * $scale
            Fill-EllipseCentered $gfx $bChunk $rx $ry (2.5 * $scale) (2.0 * $scale)
        }
        $bChunk.Dispose()
    } elseif ($f -eq 1) {
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

        $bGravel = New-Object System.Drawing.SolidBrush((Clr 200 210 145 70))
        for ($g = 0; $g -lt 4; $g++) {
            $gAng = $g * 1.57 + 0.9
            $gx = $cx + [Math]::Cos($gAng) * 9.5 * $scale
            $gy = $cy + [Math]::Sin($gAng) * 9.5 * $scale
            Fill-EllipseCentered $gfx $bGravel $gx $gy (1.5 * $scale) (1.5 * $scale)
        }
        $bGravel.Dispose()
    } elseif ($f -eq 2) {
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

        $bGravel = New-Object System.Drawing.SolidBrush((Clr 140 190 130 60))
        for ($g = 0; $g -lt 6; $g++) {
            $gAng = $g * 1.05 + 0.3
            $gx = $cx + [Math]::Cos($gAng) * 17.0 * $scale
            $gy = $cy + [Math]::Sin($gAng) * 17.0 * $scale
            Fill-EllipseCentered $gfx $bGravel $gx $gy (1.3 * $scale) (1.3 * $scale)
        }
        $bGravel.Dispose()
    } elseif ($f -eq 3) {
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
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 170 255 240 130))
        Fill-EllipseCentered $gfx $bGlow $cx $cy (12.0 * $scale) (12.0 * $scale)
        $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
        Fill-EllipseCentered $gfx $bCore $cx $cy (5.5 * $scale) (5.5 * $scale)
        $bGlow.Dispose(); $bCore.Dispose()

        $pFlare = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.8 * $scale))
        $gfx.DrawLine($pFlare, ($cx - 12.0 * $scale), $cy, ($cx + 12.0 * $scale), $cy)
        $gfx.DrawLine($pFlare, $cx, ($cy - 12.0 * $scale), $cx, ($cy + 12.0 * $scale))
        $pFlare.Dispose()
    } elseif ($f -eq 1) {
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

        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 12 5 22))
        Fill-EllipseCentered $gfx $bVoid $cx $cy (3.5 * $scale) (3.5 * $scale)
        $bVoid.Dispose()
    } elseif ($f -eq 2) {
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
# CONFIGURATION FOR 6 ELEMENTS
# ==============================================================================
$elements = @(
    @{
        ID = 208
        Name = "Ice"
        FilePrefix = "Effect_Ice_Target_Hit"
        TexGuid = "8a07020800000000000000000000e208"
        AnimGuid = "2b84c45dddf0edc44a92428497808ece"
        CtrlGuid = "034d8d062f2f93d42a585cb287d6c031"
        PrefabGuid = "59b6a148bbdf8f842b44321ea98fe03a"
        DrawFunc = { param($g, $x, $y, $f) Draw-IceHitFrame $g $x $y $f 1.0 }
    },
    @{
        ID = 209
        Name = "Electric"
        FilePrefix = "Effect_Electric_Target_Hit"
        TexGuid = "8a07020900000000000000000000e209"
        AnimGuid = "6d2062f0d27bf5640ad74160703d747d"
        CtrlGuid = "66577208108b5f3488aec957d27af1ed"
        PrefabGuid = "da4fc7619d48d544690a0ec6f90927ae"
        DrawFunc = { param($g, $x, $y, $f) Draw-ElectricHitFrame $g $x $y $f 1.0 }
    },
    @{
        ID = 210
        Name = "Wind"
        FilePrefix = "Effect_Wind_Target_Hit"
        TexGuid = "8a07021000000000000000000000e210"
        AnimGuid = "7a07021000000000000000000000210a"
        CtrlGuid = "7a07021000000000000000000000210c"
        PrefabGuid = "7a07021000000000000000000000210p"
        DrawFunc = { param($g, $x, $y, $f) Draw-WindHitFrame $g $x $y $f 1.0 }
    },
    @{
        ID = 211
        Name = "Earth"
        FilePrefix = "Effect_Earth_Target_Hit"
        TexGuid = "8a07021100000000000000000000e211"
        AnimGuid = "7a07021100000000000000000000211a"
        CtrlGuid = "7a07021100000000000000000000211c"
        PrefabGuid = "7a07021100000000000000000000211p"
        DrawFunc = { param($g, $x, $y, $f) Draw-EarthHitFrame $g $x $y $f 1.0 }
    },
    @{
        ID = 212
        Name = "Light"
        FilePrefix = "Effect_Light_Target_Hit"
        TexGuid = "8a07021200000000000000000000e212"
        AnimGuid = "7a07021200000000000000000000212a"
        CtrlGuid = "7a07021200000000000000000000212c"
        PrefabGuid = "7a07021200000000000000000000212p"
        DrawFunc = { param($g, $x, $y, $f) Draw-LightHitFrame $g $x $y $f 1.0 }
    },
    @{
        ID = 213
        Name = "Dark"
        FilePrefix = "Effect_Dark_Target_Hit"
        TexGuid = "8a07021300000000000000000000e213"
        AnimGuid = "7a07021300000000000000000000213a"
        CtrlGuid = "7a07021300000000000000000000213c"
        PrefabGuid = "7a07021300000000000000000000213p"
        DrawFunc = { param($g, $x, $y, $f) Draw-DarkHitFrame $g $x $y $f 1.0 }
    }
)

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"
$effectTexDir = Join-Path $baseDir "Assets\4. DotAsset\6. Effect"
$animDir = Join-Path $baseDir "Assets\10.Animation"
$prefabDir = Join-Path $baseDir "Assets\2. Prefab\4. Effect"

if (-not (Test-Path $effectTexDir)) { New-Item -ItemType Directory -Path $effectTexDir -Force | Out-Null }
if (-not (Test-Path $animDir)) { New-Item -ItemType Directory -Path $animDir -Force | Out-Null }
if (-not (Test-Path $prefabDir)) { New-Item -ItemType Directory -Path $prefabDir -Force | Out-Null }

foreach ($elem in $elements) {
    $id = $elem.ID
    $name = $elem.Name
    $filePrefix = $elem.FilePrefix
    $texGuid = $elem.TexGuid
    $animGuid = $elem.AnimGuid
    $ctrlGuid = $elem.CtrlGuid
    $prefabGuid = $elem.PrefabGuid

    Write-Output "Processing Target Hit Effect: E$id ($name)..."

    # 1. Generate 1280x128 Spritesheet (10 frames)
    $bmp = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    for ($f = 0; $f -lt 4; $f++) {
        $cx = $f * 128.0 + 64.0
        $cy = 64.0
        & $elem.DrawFunc $g $cx $cy $f
    }
    # Frames 4-9 are completely transparent
    $g.Dispose()

    $pngPath = Join-Path $effectTexDir "$filePrefix.png"
    if (Test-Path $pngPath) { [System.IO.File]::Delete($pngPath) }
    $bmp.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "  -> Created PNG: $pngPath"

    # 2. Generate Texture .meta with 10 sliced sprites
    $metaLines = [System.Collections.Generic.List[string]]::new()
    $metaLines.Add("fileFormatVersion: 2")
    $metaLines.Add("guid: $texGuid")
    $metaLines.Add("TextureImporter:")
    $metaLines.Add("  internalIDToNameTable:")
    for ($f = 0; $f -lt 10; $f++) {
        $metaLines.Add("  - first:")
        $metaLines.Add("      213: ${id}00${f}")
        $metaLines.Add("    second: ${filePrefix}_${f}")
    }
    $metaLines.Add("  externalObjects: {}")
    $metaLines.Add("  serializedVersion: 13")
    $metaLines.Add("  mipmaps:")
    $metaLines.Add("    mipMapMode: 0")
    $metaLines.Add("    enableMipMap: 0")
    $metaLines.Add("    sRGBTexture: 1")
    $metaLines.Add("    linearTexture: 0")
    $metaLines.Add("    fadeOut: 0")
    $metaLines.Add("    borderMipMap: 0")
    $metaLines.Add("    mipMapsPreserveCoverage: 0")
    $metaLines.Add("    alphaTestReferenceValue: 0.5")
    $metaLines.Add("    mipMapFadeDistanceStart: 1")
    $metaLines.Add("    mipMapFadeDistanceEnd: 3")
    $metaLines.Add("  bumpmap:")
    $metaLines.Add("    convertToNormalMap: 0")
    $metaLines.Add("    externalNormalMap: 0")
    $metaLines.Add("    heightScale: 0.25")
    $metaLines.Add("    normalMapFilter: 0")
    $metaLines.Add("    flipGreenChannel: 0")
    $metaLines.Add("  isReadable: 0")
    $metaLines.Add("  streamingMipmaps: 0")
    $metaLines.Add("  streamingMipmapsPriority: 0")
    $metaLines.Add("  vTOnly: 0")
    $metaLines.Add("  ignoreMipmapLimit: 0")
    $metaLines.Add("  grayScaleToAlpha: 0")
    $metaLines.Add("  generateCubemap: 6")
    $metaLines.Add("  cubemapConvolution: 0")
    $metaLines.Add("  seamlessCubemap: 0")
    $metaLines.Add("  textureFormat: 1")
    $metaLines.Add("  maxTextureSize: 2048")
    $metaLines.Add("  textureSettings:")
    $metaLines.Add("    serializedVersion: 2")
    $metaLines.Add("    filterMode: 0")
    $metaLines.Add("    aniso: 1")
    $metaLines.Add("    mipBias: 0")
    $metaLines.Add("    wrapU: 1")
    $metaLines.Add("    wrapV: 1")
    $metaLines.Add("    wrapW: 1")
    $metaLines.Add("  nPOTScale: 0")
    $metaLines.Add("  lightmap: 0")
    $metaLines.Add("  compressionQuality: 50")
    $metaLines.Add("  spriteMode: 2")
    $metaLines.Add("  spriteExtrude: 1")
    $metaLines.Add("  spriteMeshType: 1")
    $metaLines.Add("  alignment: 0")
    $metaLines.Add("  spritePivot: {x: 0.5, y: 0.5}")
    $metaLines.Add("  spritePixelsToUnits: 64")
    $metaLines.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}")
    $metaLines.Add("  spriteGenerateFallbackPhysicsShape: 1")
    $metaLines.Add("  alphaUsage: 1")
    $metaLines.Add("  alphaIsTransparency: 1")
    $metaLines.Add("  spriteTessellationDetail: -1")
    $metaLines.Add("  textureType: 8")
    $metaLines.Add("  textureShape: 1")
    $metaLines.Add("  singleChannelComponent: 0")
    $metaLines.Add("  flipbookRows: 1")
    $metaLines.Add("  flipbookColumns: 1")
    $metaLines.Add("  maxTextureSizeSet: 0")
    $metaLines.Add("  compressionQualitySet: 0")
    $metaLines.Add("  textureFormatSet: 0")
    $metaLines.Add("  ignorePngGamma: 0")
    $metaLines.Add("  applyGammaDecoding: 0")
    $metaLines.Add("  swizzle: 50462976")
    $metaLines.Add("  cookieLightType: 0")
    $metaLines.Add("  platformSettings:")
    $metaLines.Add("  - serializedVersion: 4")
    $metaLines.Add("    buildTarget: DefaultTexturePlatform")
    $metaLines.Add("    maxTextureSize: 2048")
    $metaLines.Add("    resizeAlgorithm: 0")
    $metaLines.Add("    textureFormat: -1")
    $metaLines.Add("    textureCompression: 0")
    $metaLines.Add("    compressionQuality: 50")
    $metaLines.Add("    crunchedCompression: 0")
    $metaLines.Add("    allowsAlphaSplitting: 0")
    $metaLines.Add("    overridden: 0")
    $metaLines.Add("    ignorePlatformSupport: 0")
    $metaLines.Add("    androidETC2FallbackOverride: 0")
    $metaLines.Add("    forceMaximumCompressionQuality_BC6H_BC7: 0")
    $metaLines.Add("  spriteSheet:")
    $metaLines.Add("    serializedVersion: 2")
    $metaLines.Add("    sprites:")
    for ($f = 0; $f -lt 10; $f++) {
        $xPos = $f * 128
        $metaLines.Add("    - serializedVersion: 2")
        $metaLines.Add("      name: ${filePrefix}_${f}")
        $metaLines.Add("      rect:")
        $metaLines.Add("        serializedVersion: 2")
        $metaLines.Add("        x: $xPos")
        $metaLines.Add("        y: 0")
        $metaLines.Add("        width: 128")
        $metaLines.Add("        height: 128")
        $metaLines.Add("      alignment: 0")
        $metaLines.Add("      pivot: {x: 0.5, y: 0.5}")
        $metaLines.Add("      border: {x: 0, y: 0, z: 0, w: 0}")
        $metaLines.Add("      customData: ")
        $metaLines.Add("      outline: []")
        $metaLines.Add("      physicsShape: []")
        $metaLines.Add("      tessellationDetail: 0")
        $metaLines.Add("      bones: []")
        $metaLines.Add("      spriteID: 070${id}00000${f}00000800000000000000")
        $metaLines.Add("      internalID: ${id}00${f}")
        $metaLines.Add("      vertices: []")
        $metaLines.Add("      indices: ")
        $metaLines.Add("      edges: []")
        $metaLines.Add("      weights: []")
    }
    $metaLines.Add("    outline: []")
    $metaLines.Add("    customData: ")
    $metaLines.Add("    physicsShape: []")
    $metaLines.Add("    bones: []")
    $metaLines.Add("    spriteID: ")
    $metaLines.Add("    internalID: 0")
    $metaLines.Add("    vertices: []")
    $metaLines.Add("    indices: ")
    $metaLines.Add("    edges: []")
    $metaLines.Add("    weights: []")
    $metaLines.Add("    secondaryTextures: []")
    $metaLines.Add("    spriteCustomMetadata:")
    $metaLines.Add("      entries: []")
    $metaLines.Add("    nameFileIdTable:")
    for ($f = 0; $f -lt 10; $f++) {
        $metaLines.Add("      ${filePrefix}_${f}: ${id}00${f}")
    }
    $metaLines.Add("  mipmapLimitGroupName: ")
    $metaLines.Add("  pSDRemoveMatte: 0")
    $metaLines.Add("  userData: ")
    $metaLines.Add("  assetBundleName: ")
    $metaLines.Add("  assetBundleVariant: ")

    $metaPath = "$pngPath.meta"
    [System.IO.File]::WriteAllLines($metaPath, $metaLines)
    Write-Output "  -> Created Meta: $metaPath"

    # 3. Generate AnimationClip E<ID>.anim
    $animContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: E$id
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
    - time: 0.0000
      value: {fileID: ${id}000, guid: $texGuid, type: 3}
    - time: 0.0625
      value: {fileID: ${id}001, guid: $texGuid, type: 3}
    - time: 0.1250
      value: {fileID: ${id}002, guid: $texGuid, type: 3}
    - time: 0.1875
      value: {fileID: ${id}003, guid: $texGuid, type: 3}
    - time: 0.2500
      value: {fileID: ${id}004, guid: $texGuid, type: 3}
    - time: 0.3125
      value: {fileID: ${id}005, guid: $texGuid, type: 3}
    - time: 0.3750
      value: {fileID: ${id}006, guid: $texGuid, type: 3}
    - time: 0.4375
      value: {fileID: ${id}007, guid: $texGuid, type: 3}
    - time: 0.5000
      value: {fileID: ${id}008, guid: $texGuid, type: 3}
    - time: 0.5625
      value: {fileID: ${id}009, guid: $texGuid, type: 3}
    attribute: m_Sprite
    path: 
    classID: 212
    script: {fileID: 0}
    flags: 2
  m_SampleRate: 16
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {fileID: 0}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
    - {fileID: ${id}000, guid: $texGuid, type: 3}
    - {fileID: ${id}001, guid: $texGuid, type: 3}
    - {fileID: ${id}002, guid: $texGuid, type: 3}
    - {fileID: ${id}003, guid: $texGuid, type: 3}
    - {fileID: ${id}004, guid: $texGuid, type: 3}
    - {fileID: ${id}005, guid: $texGuid, type: 3}
    - {fileID: ${id}006, guid: $texGuid, type: 3}
    - {fileID: ${id}007, guid: $texGuid, type: 3}
    - {fileID: ${id}008, guid: $texGuid, type: 3}
    - {fileID: ${id}009, guid: $texGuid, type: 3}
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: 0.625
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 0
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events:
  - time: 0.625
    functionName: DestroyEffect
    data: 
    objectReferenceParameter: {fileID: 0}
    floatParameter: 0
    intParameter: 0
    messageOptions: 0
"@
    $animPath = Join-Path $animDir "E$id.anim"
    [System.IO.File]::WriteAllText($animPath, $animContent)
    Write-Output "  -> Created Anim: $animPath"

    $animMetaPath = "$animPath.meta"
    $animMetaContent = @"
fileFormatVersion: 2
guid: $animGuid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [System.IO.File]::WriteAllText($animMetaPath, $animMetaContent)

    # 4. Generate AnimatorController E<ID>.controller
    $ctrlContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1102 &-3640950755358343151
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: E$id
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions: []
  m_StateMachineBehaviours: []
  m_Position: {x: 50, y: 50, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 7400000, guid: $animGuid, type: 2}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
--- !u!91 &9100000
AnimatorController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: E$id
  serializedVersion: 5
  m_AnimatorParameters: []
  m_AnimatorLayers:
  - serializedVersion: 5
    m_Name: Base Layer
    m_StateMachine: {fileID: 3778961769042114983}
    m_Mask: {fileID: 0}
    m_Motions: []
    m_Behaviours: []
    m_BlendingMode: 0
    m_SyncedLayerIndex: -1
    m_DefaultWeight: 0
    m_IKPass: 0
    m_SyncedLayerAffectsTiming: 0
    m_Controller: {fileID: 9100000}
--- !u!1107 &3778961769042114983
AnimatorStateMachine:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Base Layer
  m_ChildStates:
  - serializedVersion: 1
    m_State: {fileID: -3640950755358343151}
    m_Position: {x: 400, y: 140, z: 0}
  m_ChildStateMachines: []
  m_AnyStateTransitions: []
  m_EntryTransitions: []
  m_StateMachineTransitions: {}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {x: 50, y: 20, z: 0}
  m_EntryPosition: {x: 50, y: 120, z: 0}
  m_ExitPosition: {x: 800, y: 120, z: 0}
  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}
  m_DefaultState: {fileID: -3640950755358343151}
"@
    $ctrlPath = Join-Path $animDir "E$id.controller"
    [System.IO.File]::WriteAllText($ctrlPath, $ctrlContent)
    Write-Output "  -> Created Controller: $ctrlPath"

    $ctrlMetaPath = "$ctrlPath.meta"
    $ctrlMetaContent = @"
fileFormatVersion: 2
guid: $ctrlGuid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 9100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [System.IO.File]::WriteAllText($ctrlMetaPath, $ctrlMetaContent)

    # 5. Generate Prefab E<ID>.prefab
    $prefabContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1142942343058428718
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 4184555963539472167}
  - component: {fileID: 1032462388297281685}
  - component: {fileID: 4966982161684506184}
  - component: {fileID: 374352497675541615}
  - component: {fileID: 305319986910793642}
  m_Layer: 7
  m_Name: E$id
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &4184555963539472167
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1142942343058428718}
  serializedVersion: 2
  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &1032462388297281685
SpriteRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1142942343058428718}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: -1018217311
  m_SortingLayer: 5
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_Sprite: {fileID: ${id}000, guid: $texGuid, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 0.5, y: 0.5}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_SpriteSortPoint: 0
--- !u!95 &4966982161684506184
Animator:
  serializedVersion: 7
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1142942343058428718}
  m_Enabled: 1
  m_Avatar: {fileID: 0}
  m_Controller: {fileID: 9100000, guid: $ctrlGuid, type: 2}
  m_CullingMode: 0
  m_UpdateMode: 0
  m_ApplyRootMotion: 0
  m_LinearVelocityBlending: 0
  m_StabilizeFeet: 0
  m_AnimatePhysics: 0
  m_WarningMessage: 
  m_HasTransformHierarchy: 1
  m_AllowConstantClipSamplingOptimization: 1
  m_KeepAnimatorStateOnDisable: 0
  m_WriteDefaultValuesOnDisable: 0
--- !u!114 &374352497675541615
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1142942343058428718}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: e0facfe5cc7365d469d252cf7019ef98, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::EffectController
--- !u!114 &305319986910793642
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1142942343058428718}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3c2ef45af8816d443ac4d1e3a1a9778a, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::EffectPool2D
  m_particleSystem: {fileID: 0}
"@
    $prefabPath = Join-Path $prefabDir "E$id.prefab"
    [System.IO.File]::WriteAllText($prefabPath, $prefabContent)
    Write-Output "  -> Created Prefab: $prefabPath"

    $prefabMetaPath = "$prefabPath.meta"
    $prefabMetaContent = @"
fileFormatVersion: 2
guid: $prefabGuid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [System.IO.File]::WriteAllText($prefabMetaPath, $prefabMetaContent)
}

# ==============================================================================
# 6. UPDATE EFFECTLIBRARY.ASSET
# ==============================================================================
$effectLibPath = Join-Path $prefabDir "EffectLibrary.asset"
if (Test-Path $effectLibPath) {
    $libContent = [System.IO.File]::ReadAllText($effectLibPath)

    # Check and add 210, 211, 212, 213 if missing
    $toAdd = @(
        @{ ID = 210; Guid = "7a07021000000000000000000000210p" },
        @{ ID = 211; Guid = "7a07021100000000000000000000211p" },
        @{ ID = 212; Guid = "7a07021200000000000000000000212p" },
        @{ ID = 213; Guid = "7a07021300000000000000000000213p" }
    )

    $appended = $false
    foreach ($entry in $toAdd) {
        $checkStr = "effectID: $($entry.ID)"
        if ($libContent -notmatch $checkStr) {
            $newEntry = @"
  - effectID: $($entry.ID)
    effectPrefab: {fileID: 1142942343058428718, guid: $($entry.Guid), type: 3}
"@
            $libContent = $libContent.TrimEnd() + "`r`n" + $newEntry
            $appended = $true
            Write-Output "Added effectID $($entry.ID) to EffectLibrary.asset"
        }
    }

    if ($appended) {
        [System.IO.File]::WriteAllText($effectLibPath, $libContent + "`r`n")
        Write-Output "Successfully updated EffectLibrary.asset!"
    } else {
        Write-Output "EffectLibrary.asset already up to date."
    }
}

Write-Output "ALL 6 TARGET HIT EFFECTS SUCCESSFULLY GENERATED AND CONFIGURED!"
