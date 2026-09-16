Add-Type -AssemblyName System.Drawing

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $a = [int]([Math]::Min(255, [Math]::Max(0, $a)))
    $r = [int]([Math]::Min(255, [Math]::Max(0, $r)))
    $g = [int]([Math]::Min(255, [Math]::Max(0, $g)))
    $b = [int]([Math]::Min(255, [Math]::Max(0, $b)))
    [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Draw-ZeusThunderboltRefined($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    # Exact Thor: Love and Thunder Thunderbolt geometry
    # 3 linear segments forming a sharp Z-bolt spear pointing +X:
    # 1) Rear spike: (-L, -y) -> (K1_x, K1_y)
    # 2) Step Kink 1: outward bevel point & step to middle shaft
    # 3) Middle shaft: connects through the center
    # 4) Step Kink 2: outward bevel point & step to front spike
    # 5) Front spike: tapers to (+L, +y) needle-sharp tip

    $L = 52.0 * $scale
    $w = 3.6 * $scale      # facet half-width

    # In movie photo: the angle is roughly 45 deg, but in 2D projectile it flies along +X.
    # We angle the 3 segments so the overall envelope is streamlined along X:
    # Front tip at (+L, 0)
    # Rear tip at (-L, 0)
    
    # Spine (center ridge line):
    # TipRear -> K1_outer -> K1_inner -> K2_inner -> K2_outer -> TipFront
    # Let's align:
    $pRear  = Pt ($cx - $L) ($cy + 2.0 * $scale)
    $pK1_out = Pt ($cx - 12.0 * $scale) ($cy - 7.0 * $scale)
    $pK1_in  = Pt ($cx - 5.0 * $scale) ($cy + 6.0 * $scale)
    $pK2_in  = Pt ($cx + 5.0 * $scale) ($cy - 6.0 * $scale)
    $pK2_out = Pt ($cx + 12.0 * $scale) ($cy + 7.0 * $scale)
    $pFront = Pt ($cx + $L) ($cy - 2.0 * $scale)

    # Let's create the faceted 3D metallic polygon:
    # Upper facet (illuminated, bright metallic gold)
    # Lower facet (shadowed, deep antique bronze)
    
    # Upper edge vertices:
    $polyTop = [System.Drawing.PointF[]]@(
        $pRear,
        (Pt ($pK1_out.X - 1.0 * $scale) ($pK1_out.Y - $w * 1.3)),
        (Pt ($pK1_in.X - 1.0 * $scale) ($pK1_in.Y - $w * 1.3)),
        (Pt ($pK2_in.X - 1.0 * $scale) ($pK2_in.Y - $w * 1.3)),
        (Pt ($pK2_out.X - 1.0 * $scale) ($pK2_out.Y - $w * 1.3)),
        $pFront,
        # Return along spine:
        $pK2_out,
        $pK2_in,
        $pK1_in,
        $pK1_out
    )

    # Lower edge vertices:
    $polyBottom = [System.Drawing.PointF[]]@(
        $pRear,
        # Forward along spine:
        $pK1_out,
        $pK1_in,
        $pK2_in,
        $pK2_out,
        $pFront,
        # Return along bottom edge:
        (Pt ($pK2_out.X + 1.0 * $scale) ($pK2_out.Y + $w * 1.3)),
        (Pt ($pK2_in.X + 1.0 * $scale) ($pK2_in.Y + $w * 1.3)),
        (Pt ($pK1_in.X + 1.0 * $scale) ($pK1_in.Y + $w * 1.3)),
        (Pt ($pK1_out.X + 1.0 * $scale) ($pK1_out.Y + $w * 1.3))
    )

    # 1. Outer Golden Glow
    $pulse = [Math]::Sin($phase * 2.0) * 0.15 + 1.0
    $pGlow = New-Object System.Drawing.Pen((Clr (70 * $pulse) 255 210 0), (12.0 * $scale))
    $pGlow.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $spinePts = [System.Drawing.PointF[]]@($pRear, $pK1_out, $pK1_in, $pK2_in, $pK2_out, $pFront)
    $gfx.DrawLines($pGlow, $spinePts)
    $pGlow.Dispose()

    # 2. Lower Facet (Antique bronze shadow)
    $bBottom = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt ($cx - $L) $cy), (Pt ($cx + $L) $cy),
        (Clr 255 170 120 20), (Clr 255 130 90 10)
    )
    $gfx.FillPolygon($bBottom, $polyBottom)
    $bBottom.Dispose()

    # 3. Upper Facet (Radiant Sunlit Gold)
    $bTop = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (Pt ($cx - $L) $cy), (Pt ($cx + $L) $cy),
        (Clr 255 255 235 130), (Clr 255 235 190 70)
    )
    $gfx.FillPolygon($bTop, $polyTop)
    $bTop.Dispose()

    # 4. Metallic Outline
    $pEdge = New-Object System.Drawing.Pen((Clr 240 100 65 10), (1.3 * $scale))
    $pEdge.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $allOuter = [System.Drawing.PointF[]]@(
        $pRear,
        (Pt ($pK1_out.X - 1.0 * $scale) ($pK1_out.Y - $w * 1.3)),
        (Pt ($pK1_in.X - 1.0 * $scale) ($pK1_in.Y - $w * 1.3)),
        (Pt ($pK2_in.X - 1.0 * $scale) ($pK2_in.Y - $w * 1.3)),
        (Pt ($pK2_out.X - 1.0 * $scale) ($pK2_out.Y - $w * 1.3)),
        $pFront,
        (Pt ($pK2_out.X + 1.0 * $scale) ($pK2_out.Y + $w * 1.3)),
        (Pt ($pK2_in.X + 1.0 * $scale) ($pK2_in.Y + $w * 1.3)),
        (Pt ($pK1_in.X + 1.0 * $scale) ($pK1_in.Y + $w * 1.3)),
        (Pt ($pK1_out.X + 1.0 * $scale) ($pK1_out.Y + $w * 1.3))
    )
    $gfx.DrawPolygon($pEdge, $allOuter)
    $pEdge.Dispose()

    # 5. Sharp Specular Ridge Line (Center Spine)
    $pSpine = New-Object System.Drawing.Pen((Clr 240 255 255 240), (1.5 * $scale))
    $pSpine.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
    $gfx.DrawLines($pSpine, $spinePts)
    $pSpine.Dispose()

    # 6. Crackling Electric Arcs (Dynamic across kinks)
    $pSpark = New-Object System.Drawing.Pen((Clr 230 255 255 230), (1.2 * $scale))
    $pSpark.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Bevel
    $sPts1 = [System.Drawing.PointF[]]@(
        (Pt ($pK1_out.X - 3 * $scale) ($pK1_out.Y - 2 * $scale)),
        (Pt ($pK1_out.X + 2 * $scale) ($pK1_out.Y - 6 * $scale)),
        (Pt ($pK1_in.X - 2 * $scale) ($pK1_in.Y + 4 * $scale))
    )
    $gfx.DrawLines($pSpark, $sPts1)
    
    $sPts2 = [System.Drawing.PointF[]]@(
        (Pt ($pK2_in.X + 1 * $scale) ($pK2_in.Y - 4 * $scale)),
        (Pt ($pK2_out.X - 2 * $scale) ($pK2_out.Y + 6 * $scale)),
        (Pt ($pK2_out.X + 4 * $scale) ($pK2_out.Y + 2 * $scale))
    )
    $gfx.DrawLines($pSpark, $sPts2)
    $pSpark.Dispose()

    # 7. Tip Glint Flare
    $bStar = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
    $gfx.FillEllipse($bStar, ($pFront.X - 2.5 * $scale), ($pFront.Y - 2.5 * $scale), (5.0 * $scale), (5.0 * $scale))
    $bStar.Dispose()
}

$bmp = New-Object System.Drawing.Bitmap(600, 300)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gfx.Clear([System.Drawing.Color]::FromArgb(25, 27, 34))

Draw-ZeusThunderboltRefined $gfx 300.0 150.0 0.0 2.4

$testPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\test_zeus_bolt.png"
$bmp.Save($testPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose(); $gfx.Dispose()
