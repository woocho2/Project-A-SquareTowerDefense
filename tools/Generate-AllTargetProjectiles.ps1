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
# 1. DRAW FUNCTIONS FOR EACH PROJECTILE
# ==============================================================================

# --- 1) Fire: 소형 파이어볼 (Small Fireball) ---
function Draw-SmallFireball($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $radius = 9.0 * $scale
    $tailLen = (26.0 + [Math]::Sin($phase * 2.0) * 3.5) * $scale
    $wWave = [Math]::Sin($phase) * (1.6 * $scale)

    # 1. Flame Tail
    $tailPtsRed = [System.Drawing.PointF[]]@(
        (Pt ($cx - 2.0 * $scale) ($cy - 8.0 * $scale)),
        (Pt ($cx - 13.0 * $scale) ($cy - 6.5 * $scale + $wWave)),
        (Pt ($cx - $tailLen) ($cy + $wWave * 0.5)),
        (Pt ($cx - 13.0 * $scale) ($cy + 6.5 * $scale + $wWave)),
        (Pt ($cx - 2.0 * $scale) ($cy + 8.0 * $scale))
    )
    $bTailRed = New-Object System.Drawing.SolidBrush((Clr 200 230 40 0))
    $gfx.FillPolygon($bTailRed, $tailPtsRed)
    $bTailRed.Dispose()

    $tailPtsOrange = [System.Drawing.PointF[]]@(
        (Pt ($cx - 1.0 * $scale) ($cy - 5.0 * $scale)),
        (Pt ($cx - 9.0 * $scale) ($cy - 4.0 * $scale + $wWave * 0.6)),
        (Pt ($cx - $tailLen * 0.65) ($cy + $wWave * 0.3)),
        (Pt ($cx - 9.0 * $scale) ($cy + 4.0 * $scale + $wWave * 0.6)),
        (Pt ($cx - 1.0 * $scale) ($cy + 5.0 * $scale))
    )
    $bTailOrange = New-Object System.Drawing.SolidBrush((Clr 245 255 130 0))
    $gfx.FillPolygon($bTailOrange, $tailPtsOrange)
    $bTailOrange.Dispose()

    # 2. Fireball Head
    $bAura = New-Object System.Drawing.SolidBrush((Clr 120 255 50 0))
    Fill-EllipseCentered $gfx $bAura $cx $cy (11.5 * $scale) (11.5 * $scale)
    $bAura.Dispose()

    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 240 45 0))
    Fill-EllipseCentered $gfx $bBody $cx $cy $radius $radius
    $bBody.Dispose()

    $bVol = New-Object System.Drawing.SolidBrush((Clr 255 255 150 0))
    Fill-EllipseCentered $gfx $bVol ($cx + 1.5 * $scale) ($cy - 1.0 * $scale) ($radius * 0.65) ($radius * 0.65)
    $bVol.Dispose()

    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 255 230))
    Fill-EllipseCentered $gfx $bCore ($cx + 2.5 * $scale) ($cy - 1.0 * $scale) ($radius * 0.35) ($radius * 0.35)
    $bCore.Dispose()
}

# --- 2) Ice: 날씬한 고드름 (Slender Icicle) ---
function Draw-Icicle($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    # Sharp crystalline frost dagger pointing +X
    $len = 44.0 * $scale
    $w = 7.0 * $scale

    $xBack = $cx - ($len * 0.48)
    $xTip  = $cx + ($len * 0.52)
    $xStep = $cx - ($len * 0.05)

    # Frost Aura
    $bAura = New-Object System.Drawing.SolidBrush((Clr 45 100 220 255))
    Fill-EllipseCentered $gfx $bAura ($cx - 1 * $scale) $cy ($len * 0.50) (11.0 * $scale)
    $bAura.Dispose()

    # Shaded Lower Facet
    $ptsBot = [System.Drawing.PointF[]]@(
        (Pt $xTip $cy),
        (Pt $xStep $cy),
        (Pt $xBack ($cy + $w * 0.85)),
        (Pt ($xBack + 3 * $scale) ($cy + $w * 0.3))
    )
    $bBot = New-Object System.Drawing.SolidBrush((Clr 255 25 125 190))
    $gfx.FillPolygon($bBot, $ptsBot)
    $bBot.Dispose()

    # Upper Bright Crystal Facet
    $ptsTop = [System.Drawing.PointF[]]@(
        (Pt $xTip $cy),
        (Pt $xBack ($cy - $w * 0.95)),
        (Pt ($xBack + 3 * $scale) ($cy - $w * 0.3)),
        (Pt $xStep $cy)
    )
    $bTop = New-Object System.Drawing.SolidBrush((Clr 255 160 235 255))
    $gfx.FillPolygon($bTop, $ptsTop)
    $bTop.Dispose()

    # Center Specular Spine Ridge
    $pSpine = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.5 * $scale))
    $pSpine.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $gfx.DrawLine($pSpine, $xBack, $cy, $xTip, $cy)
    $pSpine.Dispose()

    # Outline
    $pOut = New-Object System.Drawing.Pen((Clr 240 10 70 140), (1.2 * $scale))
    $pOut.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $allPts = [System.Drawing.PointF[]]@(
        (Pt $xTip $cy),
        (Pt $xBack ($cy - $w * 0.95)),
        (Pt ($xBack + 3 * $scale) $cy),
        (Pt $xBack ($cy + $w * 0.85))
    )
    $gfx.DrawPolygon($pOut, $allPts)
    $pOut.Dispose()

    # Floating Frost Particles (staying close to back)
    $bSpark = New-Object System.Drawing.SolidBrush((Clr 230 220 250 255))
    for ($sp = 0; $sp -lt 3; $sp++) {
        $spDist = (5.0 + $sp * 6.0 + [Math]::Sin($phase + $sp) * 2.5) * $scale
        $spY = ([Math]::Sin($phase * 1.5 + $sp * 2.0) * (4.0 * $scale))
        Fill-EllipseCentered $gfx $bSpark ($xBack - $spDist) ($cy + $spY) (1.6 * $scale) (1.6 * $scale)
    }
    $bSpark.Dispose()
}

# --- 3) Electricity: 토르 러브앤썬더 제우스의 번개볼트 (Zeus Thunderbolt Spear) ---
function Draw-ZeusThunderbolt($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    # Faithfully matched to Thor: Love and Thunder reference photo!
    # Symmetrical 3-segment golden lightning bolt spear pointing +X:
    # 1. Needle-sharp rear point (-L) -> Kink 1
    # 2. Kink 1: Sharp horizontal-cut jog
    # 3. Middle shaft: Straight golden section connecting across center
    # 4. Kink 2: Sharp horizontal-cut jog
    # 5. Needle-sharp front point (+L)

    $L = 46.0 * $scale
    $w = 3.5 * $scale

    $xR = $cx - $L
    $xF = $cx + $L
    $xK1_out = $cx - 14.0 * $scale
    $xK1_in  = $cx - 6.5 * $scale
    $xK2_in  = $cx + 6.5 * $scale
    $xK2_out = $cx + 14.0 * $scale

    $yR = $cy
    $yF = $cy
    $yK1_out = $cy - 5.5 * $scale
    $yK1_in  = $cy + 4.5 * $scale
    $yK2_in  = $cy - 4.5 * $scale
    $yK2_out = $cy + 5.5 * $scale

    $spinePts = [System.Drawing.PointF[]]@(
        (Pt $xR $yR),
        (Pt $xK1_out $yK1_out),
        (Pt $xK1_in  $yK1_in),
        (Pt $xK2_in  $yK2_in),
        (Pt $xK2_out $yK2_out),
        (Pt $xF $yF)
    )

    # 1. Subtle Golden Aura Glow
    $pulse = [Math]::Sin($phase * 2.0) * 0.15 + 1.0
    $pGlow = New-Object System.Drawing.Pen((Clr (50 * $pulse) 255 210 20), (6.0 * $scale))
    $pGlow.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $pGlow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pGlow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLines($pGlow, $spinePts)
    $pGlow.Dispose()

    # 2. Lower Facet (Shaded Antique Bronze Gold)
    $polyBottom = [System.Drawing.PointF[]]@(
        (Pt $xR $yR),
        (Pt $xK1_out $yK1_out),
        (Pt $xK1_in  $yK1_in),
        (Pt $xK2_in  $yK2_in),
        (Pt $xK2_out $yK2_out),
        (Pt $xF $yF),
        (Pt ($xK2_out + 0.8 * $scale) ($yK2_out + $w)),
        (Pt ($xK2_in  + 0.8 * $scale) ($yK2_in  + $w)),
        (Pt ($xK1_in  + 0.8 * $scale) ($yK1_in  + $w)),
        (Pt ($xK1_out + 0.8 * $scale) ($yK1_out + $w))
    )
    $bBottom = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt ($cx - $L) $cy), (Pt ($cx + $L) $cy),
        (Clr 255 170 115 25), (Clr 255 125 80 12)
    )
    $gfx.FillPolygon($bBottom, $polyBottom)
    $bBottom.Dispose()

    # 3. Upper Facet (Radiant Sunlit Metallic Gold)
    $polyTop = [System.Drawing.PointF[]]@(
        (Pt $xR $yR),
        (Pt ($xK1_out - 0.8 * $scale) ($yK1_out - $w)),
        (Pt ($xK1_in  - 0.8 * $scale) ($yK1_in  - $w)),
        (Pt ($xK2_in  - 0.8 * $scale) ($yK2_in  - $w)),
        (Pt ($xK2_out - 0.8 * $scale) ($yK2_out - $w)),
        (Pt $xF $yF),
        (Pt $xK2_out $yK2_out),
        (Pt $xK2_in  $yK2_in),
        (Pt $xK1_in  $yK1_in),
        (Pt $xK1_out $yK1_out)
    )
    $bTop = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt ($cx - $L) $cy), (Pt ($cx + $L) $cy),
        (Clr 255 255 240 140), (Clr 255 235 195 70)
    )
    $gfx.FillPolygon($bTop, $polyTop)
    $bTop.Dispose()

    # 4. Sharp Metallic Outer Outline
    $pEdge = New-Object System.Drawing.Pen((Clr 240 100 65 15), (1.2 * $scale))
    $pEdge.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $allOuter = [System.Drawing.PointF[]]@(
        (Pt $xR $yR),
        (Pt ($xK1_out - 0.8 * $scale) ($yK1_out - $w)),
        (Pt ($xK1_in  - 0.8 * $scale) ($yK1_in  - $w)),
        (Pt ($xK2_in  - 0.8 * $scale) ($yK2_in  - $w)),
        (Pt ($xK2_out - 0.8 * $scale) ($yK2_out - $w)),
        (Pt $xF $yF),
        (Pt ($xK2_out + 0.8 * $scale) ($yK2_out + $w)),
        (Pt ($xK2_in  + 0.8 * $scale) ($yK2_in  + $w)),
        (Pt ($xK1_in  + 0.8 * $scale) ($yK1_in  + $w)),
        (Pt ($xK1_out + 0.8 * $scale) ($yK1_out + $w))
    )
    $gfx.DrawPolygon($pEdge, $allOuter)
    $pEdge.Dispose()

    # 5. Specular Center Spine Highlight (Sharp metallic ridge)
    $pSpine = New-Object System.Drawing.Pen((Clr 255 255 255 240), (1.4 * $scale))
    $pSpine.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $gfx.DrawLines($pSpine, $spinePts)
    $pSpine.Dispose()

    # 6. Crackling Electric Arcs (Dynamic across kinks)
    $sparkSeed = [int]([Math]::Floor($phase * 1.5)) % 3
    $pSpark = New-Object System.Drawing.Pen((Clr 240 255 255 255), (1.1 * $scale))
    $pSpark.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
    if ($sparkSeed -eq 0 -or $sparkSeed -eq 2) {
        $sPts1 = [System.Drawing.PointF[]]@(
            (Pt ($xK1_out - 2 * $scale) ($yK1_out - 2 * $scale)),
            (Pt ($xK1_out + 2 * $scale) ($yK1_out - 5 * $scale)),
            (Pt ($xK1_in  - 1 * $scale) ($yK1_in  + 4 * $scale))
        )
        $gfx.DrawLines($pSpark, $sPts1)
    }
    if ($sparkSeed -eq 1 -or $sparkSeed -eq 2) {
        $sPts2 = [System.Drawing.PointF[]]@(
            (Pt ($xK2_in  + 1 * $scale) ($yK2_in  - 4 * $scale)),
            (Pt ($xK2_out - 2 * $scale) ($yK2_out + 5 * $scale)),
            (Pt ($xK2_out + 2 * $scale) ($yK2_out + 2 * $scale))
        )
        $gfx.DrawLines($pSpark, $sPts2)
    }
    $pSpark.Dispose()

    # 7. Sharp Tip Specular Glint
    $pCross = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.2 * $scale))
    $gfx.DrawLine($pCross, ($xF - 2 * $scale), $yF, ($xF + 2 * $scale), $yF)
    $gfx.DrawLine($pCross, $xF, ($yF - 2 * $scale), $xF, ($yF + 2 * $scale))
    $pCross.Dispose()
}

# --- 4) Wind: 청록 심플 구체 (Teal Breeze Orb) ---
function Draw-WindSphere($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $r = 11.0 * $scale

    # 1. Soft Outer Teal Halo
    $bHalo = New-Object System.Drawing.SolidBrush((Clr 50 0 255 210))
    Fill-EllipseCentered $gfx $bHalo $cx $cy ($r * 1.55) ($r * 1.55)
    $bHalo.Dispose()

    # 2. Main Teal Body
    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 0 195 160))
    Fill-EllipseCentered $gfx $bBody $cx $cy $r $r
    $bBody.Dispose()

    # 3. Bright Inner Core
    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 100 255 235))
    Fill-EllipseCentered $gfx $bCore ($cx + 1.5 * $scale) ($cy - 1.5 * $scale) ($r * 0.65) ($r * 0.65)
    $bCore.Dispose()

    $bCenter = New-Object System.Drawing.SolidBrush((Clr 255 230 255 250))
    Fill-EllipseCentered $gfx $bCenter ($cx + 2.5 * $scale) ($cy - 2.0 * $scale) ($r * 0.3) ($r * 0.3)
    $bCenter.Dispose()

    # 4. Clean Edge Outline
    $pEdge = New-Object System.Drawing.Pen((Clr 220 0 110 90), (1.3 * $scale))
    Draw-EllipseCentered $gfx $pEdge $cx $cy $r $r
    $pEdge.Dispose()

    # 5. Orbiting Breeze Particles (360 deg rotation with phase)
    $bOrb = New-Object System.Drawing.SolidBrush((Clr 240 180 255 245))
    for ($k = 0; $k -lt 2; $k++) {
        $ang = $phase + $k * [Math]::PI
        $orbitRx = 16.0 * $scale
        $orbitRy = 6.0 * $scale
        $px = $cx + [Math]::Cos($ang) * $orbitRx
        $py = $cy + [Math]::Sin($ang) * $orbitRy
        Fill-EllipseCentered $gfx $bOrb $px $py (2.0 * $scale) (2.0 * $scale)
    }
    $bOrb.Dispose()
}

# --- 5) Earth: 갈색 흙 구체 (Brown Earth Orb) ---
function Draw-EarthSphere($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $r = 11.5 * $scale

    # 1. Earth Atmosphere / Dust Halo
    $bHalo = New-Object System.Drawing.SolidBrush((Clr 50 200 130 50))
    Fill-EllipseCentered $gfx $bHalo $cx $cy ($r * 1.45) ($r * 1.45)
    $bHalo.Dispose()

    # 2. Main Soil/Rock Core
    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 125 70 25))
    Fill-EllipseCentered $gfx $bBody $cx $cy $r $r
    $bBody.Dispose()

    # 3. Rocky Terracotta Highlights
    $bHigh = New-Object System.Drawing.SolidBrush((Clr 255 195 125 60))
    Fill-EllipseCentered $gfx $bHigh ($cx + 1.5 * $scale) ($cy - 2.0 * $scale) ($r * 0.65) ($r * 0.6)
    $bHigh.Dispose()

    $bSpot = New-Object System.Drawing.SolidBrush((Clr 255 235 175 110))
    Fill-EllipseCentered $gfx $bSpot ($cx + 3.0 * $scale) ($cy - 3.0 * $scale) ($r * 0.28) ($r * 0.28)
    $bSpot.Dispose()

    # 4. Surface Soil Spots / Craters
    $bDarkSpot = New-Object System.Drawing.SolidBrush((Clr 220 75 40 15))
    Fill-EllipseCentered $gfx $bDarkSpot ($cx - 3.5 * $scale) ($cy + 2.5 * $scale) (2.5 * $scale) (2.0 * $scale)
    Fill-EllipseCentered $gfx $bDarkSpot ($cx + 1.0 * $scale) ($cy + 4.5 * $scale) (2.0 * $scale) (1.8 * $scale)
    $bDarkSpot.Dispose()

    # 5. Clean Edge Outline
    $pEdge = New-Object System.Drawing.Pen((Clr 230 65 35 10), (1.4 * $scale))
    Draw-EllipseCentered $gfx $pEdge $cx $cy $r $r
    $pEdge.Dispose()

    # 6. Orbiting Rock Motes (staying within card)
    $bMote = New-Object System.Drawing.SolidBrush((Clr 220 215 145 75))
    for ($k = 0; $k -lt 2; $k++) {
        $ang = -$phase + $k * [Math]::PI
        $orbitRx = 15.0 * $scale
        $orbitRy = 5.5 * $scale
        $px = $cx + [Math]::Cos($ang) * $orbitRx
        $py = $cy + [Math]::Sin($ang) * $orbitRy
        Fill-EllipseCentered $gfx $bMote $px $py (1.8 * $scale) (1.8 * $scale)
    }
    $bMote.Dispose()
}

# --- 6) Light: 관통 레이저 빔 (Piercing Laser Beam) ---
function Draw-LightLaser($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $len = 50.0 * $scale
    $w = 5.5 * $scale

    $xBack = $cx - ($len * 0.52)
    $xHead = $cx + ($len * 0.48)

    # 1. Outer Radiant Golden Sheath
    $pGlow = New-Object System.Drawing.Pen((Clr 70 255 240 100), ($w * 2.6))
    $pGlow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pGlow.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pGlow, $xBack, $cy, $xHead, $cy)
    $pGlow.Dispose()

    # 2. Golden Energy Core Sheath
    $pSheath = New-Object System.Drawing.Pen((Clr 255 245 220 120), ($w * 1.4))
    $pSheath.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pSheath.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pSheath, $xBack, $cy, $xHead, $cy)
    $pSheath.Dispose()

    # 3. Pure Blinding White Core Stream
    $pWhite = New-Object System.Drawing.Pen((Clr 255 255 255 255), ($w * 0.70))
    $pWhite.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pWhite.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pWhite, ($xBack + 2 * $scale), $cy, ($xHead - 1 * $scale), $cy)
    $pWhite.Dispose()

    # 4. Front Tip Star Flare / Cross Glint
    $pFlare = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.6 * $scale))
    $flareLen = (8.0 + [Math]::Sin($phase * 2.0) * 1.8) * $scale
    $gfx.DrawLine($pFlare, $xHead, ($cy - $flareLen), $xHead, ($cy + $flareLen))
    $gfx.DrawLine($pFlare, ($xHead - $flareLen * 0.65), $cy, ($xHead + $flareLen * 0.65), $cy)
    $pFlare.Dispose()

    $bStar = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
    Fill-EllipseCentered $gfx $bStar $xHead $cy (3.0 * $scale) (3.0 * $scale)
    $bStar.Dispose()
}

# --- 7) Darkness: 심연 암흑 구체 (Void Dark Orb) ---
function Draw-DarkSphere($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $r = 11.5 * $scale

    # 1. Outer Purple Void Aura
    $bAura = New-Object System.Drawing.SolidBrush((Clr 60 145 35 235))
    Fill-EllipseCentered $gfx $bAura $cx $cy ($r * 1.6) ($r * 1.6)
    $bAura.Dispose()

    # 2. Neon Purple Accretion Ring
    $pulse = [Math]::Sin($phase * 2.0) * 0.15 + 1.0
    $pRing = New-Object System.Drawing.Pen((Clr 255 175 60 255), (2.4 * $scale * $pulse))
    Draw-EllipseCentered $gfx $pRing $cx $cy ($r * 1.22) ($r * 1.22)
    $pRing.Dispose()

    # 3. Obsidian Pitch Black Void Core
    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 12 10 20))
    Fill-EllipseCentered $gfx $bCore $cx $cy $r $r
    $bCore.Dispose()

    # 4. Sinister Inner Eye / Event Horizon Ring
    $pInner = New-Object System.Drawing.Pen((Clr 255 215 130 255), (1.4 * $scale))
    Draw-EllipseCentered $gfx $pInner $cx $cy ($r * 0.45) ($r * 0.45)
    $pInner.Dispose()

    $bPupil = New-Object System.Drawing.SolidBrush((Clr 255 5 2 10))
    Fill-EllipseCentered $gfx $bPupil $cx $cy ($r * 0.25) ($r * 0.25)
    $bPupil.Dispose()
}


# ==============================================================================
# 2. BUILD ALL 7 TARGET BULLETS SHOWCASE BANNER (1200 x 380)
# ==============================================================================
Write-Output "Rendering Target Bullets Showcase Banner..."
$bmpShowcase = New-Object System.Drawing.Bitmap(1200, 380)
$gShow = [System.Drawing.Graphics]::FromImage($bmpShowcase)
$gShow.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gShow.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gShow.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

# Title
$titleText = Get-Utf8Str @(0xED, 0x83, 0x80, 0xEA, 0xB2, 0x8F, 0x28, 0xEB, 0x8B, 0xA8, 0xEC, 0x9D, 0xBC, 0x29, 0x20, 0xED, 0x83, 0x80, 0xEC, 0x9B, 0x8C, 0x20, 0xEC, 0xB4, 0x9D, 0xEC, 0x95, 0x8C, 0x20, 0xEB, 0x94, 0x94, 0xEC, 0x9E, 0x90, 0xEC, 0x9D, 0xB8, 0x20, 0xEC, 0x8B, 0x9C, 0xEC, 0x95, 0x88, 0x20, 0x28, 0x54, 0x61, 0x72, 0x67, 0x65, 0x74, 0x20, 0x54, 0x6F, 0x77, 0x65, 0x72, 0x20, 0x50, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x69, 0x6C, 0x65, 0x73, 0x29)
$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center

$gShow.DrawString($titleText, $fTitle, $bWhite, 600.0, 16.0, $sfCenter)

$targetItems = @(
    [pscustomobject]@{ Key = 'Fire';        Label = ((Get-Utf8Str @(0xEB, 0xB6, 0x88)) + " (Fire)");       Desc = (Get-Utf8Str @(0xEC, 0x86, 0x8C, 0xED, 0x98, 0x95, 0x20, 0xED, 0x8C, 0x8C, 0xEC, 0x9D, 0xB4, 0xEC, 0x96, 0xB4, 0xEB, 0xB3, 0xBC));                   Color = (Clr 255 255 85 85) },
    [pscustomobject]@{ Key = 'Ice';         Label = ((Get-Utf8Str @(0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C)) + " (Ice)");      Desc = (Get-Utf8Str @(0xEB, 0x82, 0xA0, 0xEC, 0x89, 0x90, 0xED, 0x95, 0x9C, 0x20, 0xEA, 0xB3, 0xA0, 0xEB, 0x93, 0x9C, 0xEB, 0xA6, 0x84));                   Color = (Clr 255 100 210 255) },
    [pscustomobject]@{ Key = 'Electricity'; Label = ((Get-Utf8Str @(0xEC, 0xA0, 0x84, 0xEA, 0xB8, 0xB0)) + " (Electric)"); Desc = (Get-Utf8Str @(0xED, 0x86, 0xA0, 0xEB, 0xA5, 0xB4, 0x20, 0xEC, 0xA0, 0x9C, 0xEC, 0x9A, 0xB0, 0xEC, 0x8A, 0xA4, 0x20, 0xEB, 0xB2, 0x88, 0xEA, 0xB0, 0x9C, 0xEB, 0xB3, 0xBC, 0xED, 0x8A, 0xB8)); Color = (Clr 255 255 235 59) },
    [pscustomobject]@{ Key = 'Wind';        Label = ((Get-Utf8Str @(0xEB, 0xB0, 0x94, 0xEB, 0x9E, 0x8C)) + " (Wind)");     Desc = (Get-Utf8Str @(0xEC, 0xB2, 0xAD, 0xEB, 0x87, 0x9D, 0x20, 0xEC, 0x8B, 0xAC, 0xED, 0x94, 0x8C, 0x20, 0xEA, 0xB5, 0xAC, 0xEC, 0xB2, 0xB4));             Color = (Clr 255 64 255 218) },
    [pscustomobject]@{ Key = 'Earth';       Label = ((Get-Utf8Str @(0xEB, 0x8C, 0x80, 0xEC, 0xA7, 0x80)) + " (Earth)");    Desc = (Get-Utf8Str @(0xEA, 0xB0, 0x88, 0xEC, 0x83, 0x89, 0x20, 0xED, 0x9D, 0x99, 0x20, 0xEA, 0xB5, 0xAC, 0xEC, 0xB2, 0xB4));                   Color = (Clr 255 215 140 70) },
    [pscustomobject]@{ Key = 'Light';       Label = ((Get-Utf8Str @(0xEB, 0xB9, 0x9B)) + " (Light)");      Desc = (Get-Utf8Str @(0xEA, 0xB4, 0x80, 0xED, 0x86, 0xB5, 0x20, 0xEB, 0x97, 0x88, 0xEC, 0x9D, 0xB4, 0xEC, 0xA0, 0x80, 0x20, 0xEB, 0xB9, 0x94));             Color = (Clr 255 255 255 190) },
    [pscustomobject]@{ Key = 'Darkness';    Label = ((Get-Utf8Str @(0xEC, 0x96, 0xB4, 0xEB, 0x91, 0xA0)) + " (Dark)");     Desc = (Get-Utf8Str @(0xEC, 0x8B, 0xAC, 0xEC, 0x97, 0xB0, 0x20, 0xEC, 0x95, 0x94, 0xED, 0x9D, 0x94, 0x20, 0xEA, 0xB5, 0xAC, 0xEC, 0xB2, 0xB4));             Color = (Clr 255 195 125 255) }
)

$fCardName = New-Object System.Drawing.Font('Malgun Gothic', 11, [System.Drawing.FontStyle]::Bold)
$fCardSub  = New-Object System.Drawing.Font('Malgun Gothic', 9, [System.Drawing.FontStyle]::Regular)
$bSubColor = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))

$cardW = 154
$cardH = 285
$cardY = 58

for ($i = 0; $i -lt $targetItems.Count; $i++) {
    $item = $targetItems[$i]
    $cardX = 22 + $i * 168

    $bCardBg = New-Object System.Drawing.SolidBrush((Clr 255 30 33 42))
    $pBorder = New-Object System.Drawing.Pen((Clr 80 255 255 255), 1.2)
    $cardRect = New-Object System.Drawing.Rectangle($cardX, $cardY, $cardW, $cardH)
    $gShow.FillRectangle($bCardBg, $cardRect)
    $gShow.DrawRectangle($pBorder, $cardRect)
    $bCardBg.Dispose(); $pBorder.Dispose()

    $centerX = $cardX + ($cardW / 2.0)
    $centerY = $cardY + 110.0

    switch ($item.Key) {
        'Fire'        { Draw-SmallFireball $gShow ($centerX + 8) $centerY 0.0 2.0 }
        'Ice'         { Draw-Icicle $gShow ($centerX + 4) $centerY 0.0 1.9 }
        'Electricity' { Draw-ZeusThunderbolt $gShow $centerX $centerY 0.0 1.45 }
        'Wind'        { Draw-WindSphere $gShow $centerX $centerY 0.0 2.1 }
        'Earth'       { Draw-EarthSphere $gShow $centerX $centerY 0.0 2.1 }
        'Light'       { Draw-LightLaser $gShow $centerX $centerY 0.0 1.6 }
        'Darkness'    { Draw-DarkSphere $gShow $centerX $centerY 0.0 2.1 }
    }

    $bLabel = New-Object System.Drawing.SolidBrush($item.Color)
    $gShow.DrawString($item.Label, $fCardName, $bLabel, [float]$centerX, [float]($cardY + 205), $sfCenter)
    $gShow.DrawString($item.Desc, $fCardSub, $bSubColor, [float]$centerX, [float]($cardY + 235), $sfCenter)
    $bLabel.Dispose()
}

$fTitle.Dispose(); $fCardName.Dispose(); $fCardSub.Dispose(); $bWhite.Dispose(); $bSubColor.Dispose(); $sfCenter.Dispose()

$showcasePath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_target_bullets.png"
if (Test-Path $showcasePath) { [System.IO.File]::Delete($showcasePath) }
$bmpShowcase.Save($showcasePath, [System.Drawing.Imaging.ImageFormat]::Png)

$rootShowcase = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\preview_target_bullets.png"
Copy-Item $showcasePath $rootShowcase -Force
$gShow.Dispose(); $bmpShowcase.Dispose()
Write-Output "Saved Showcase Banner to $showcasePath and $rootShowcase"


# ==============================================================================
# 3. BUILD 4-FRAME SPRITESHEETS FOR ALL 6 REMAINING TARGET BULLETS (512x128)
# ==============================================================================
$sheets = @(
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Icicle_Small.png";     Scale = 1.30; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-Icicle $g ($cx + 4) $cy $ph $sc } },
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Zeus_Thunderbolt.png"; Scale = 1.15; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-ZeusThunderbolt $g $cx $cy $ph $sc } },
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Wind_Orb.png";          Scale = 1.40; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-WindSphere $g $cx $cy $ph $sc } },
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Earth_Orb.png";         Scale = 1.40; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-EarthSphere $g $cx $cy $ph $sc } },
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Light_Laser.png";       Scale = 1.20; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-LightLaser $g $cx $cy $ph $sc } },
    [pscustomobject]@{ File = "Assets/4. DotAsset/Projectile_Dark_Orb.png";          Scale = 1.40; Draw = { param($g, $cx, $cy, $ph, $sc) Draw-DarkSphere $g $cx $cy $ph $sc } }
)

foreach ($sheet in $sheets) {
    $bmp = New-Object System.Drawing.Bitmap(512, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    for ($f = 0; $f -lt 4; $f++) {
        $cellCx = $f * 128.0 + 64.0
        $cellCy = 64.0
        $phase = ($f / 4.0) * 2.0 * [Math]::PI
        & $sheet.Draw $g $cellCx $cellCy $phase $sheet.Scale
    }

    if (Test-Path $sheet.File) { [System.IO.File]::Delete($sheet.File) }
    $bmp.Save($sheet.File, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Output "Saved Spritesheet: $($sheet.File)"
}
