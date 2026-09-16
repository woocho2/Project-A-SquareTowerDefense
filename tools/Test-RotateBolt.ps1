Add-Type -AssemblyName System.Drawing

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $a = [int]([Math]::Min(255, [Math]::Max(0, $a)))
    $r = [int]([Math]::Min(255, [Math]::Max(0, $r)))
    $g = [int]([Math]::Min(255, [Math]::Max(0, $g)))
    $b = [int]([Math]::Min(255, [Math]::Max(0, $b)))
    [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Rotate-Point([System.Drawing.PointF]$p, [System.Drawing.PointF]$origin, [float]$angleRad, [float]$scale, [float]$tx, [float]$ty) {
    $dx = $p.X - $origin.X
    $dy = $p.Y - $origin.Y
    $cos = [Math]::Cos($angleRad)
    $sin = [Math]::Sin($angleRad)
    $rx = ($dx * $cos - $dy * $sin) * $scale + $tx
    $ry = ($dx * $sin + $dy * $cos) * $scale + $ty
    New-Object System.Drawing.PointF([float]$rx, [float]$ry)
}

# The vertices from the photo:
# Note: In photo, (57, 408) is bottom-left, (356, 39) is top-right.
# We want top-right (spear tip) to be at +X (Right), and bottom-left (rear) to be at -X (Left).
$origBottom = Pt 57.0 408.0
$origTop    = Pt 356.0 39.0
$origin     = Pt (($origBottom.X + $origTop.X)/2.0) (($origBottom.Y + $origTop.Y)/2.0)

# Calculate rotation angle to align vector(Bottom -> Top) with (+X, 0)
$dx = $origTop.X - $origBottom.X
$dy = $origTop.Y - $origBottom.Y
$currentAngle = [Math]::Atan2($dy, $dx) # negative, pointing up-right
$targetAngle = 0.0 # horizontal +X
$rotAngle = $targetAngle - $currentAngle

# Target length in sprite: around 96 px (total length in photo is ~475px, so scale ~0.20 for 128x128, or ~0.45 for preview)
$scale = 0.45
$tx = 300.0; $ty = 150.0

# Define outer contour vertices (Clockwise in photo):
# 1. Top tip
# 2. Right edge of upper spike
# 3. Kink 1 outer corner
# 4. Kink 1 horizontal step
# 5. Right edge of middle shaft
# 6. Kink 2 step
# 7. Kink 2 outer corner
# 8. Right edge of bottom spike
# 9. Bottom tip
# 10. Left edge of bottom spike
# 11. Kink 2 left corner
# 12. Left edge of middle shaft
# 13. Kink 1 left corner
# 14. Left edge of upper spike back to Top tip

$ptsPhotoUpperFacet = @(
    (Pt 356.0 39.0),
    (Pt 265.0 110.0),  # K1 outer upper
    (Pt 300.0 125.0),  # K1 inner step
    (Pt 200.0 215.0),  # Mid upper
    (Pt 170.0 231.0),  # K2 outer upper
    (Pt 215.0 245.0),  # K2 inner step
    (Pt 57.0 408.0)    # Bottom tip
)

# Let's define the precise 2 facets (Top bright facet, Bottom shadow facet):
# Ridge (spine) line:
$spinePhoto = @(
    (Pt 57.0 408.0),
    (Pt 180.0 235.0),
    (Pt 225.0 248.0),
    (Pt 278.0 118.0),
    (Pt 312.0 128.0),
    (Pt 356.0 39.0)
)

# Left/Top edge (Upper Facet in photo):
$topEdgePhoto = @(
    (Pt 57.0 408.0),
    (Pt 168.0 230.0),
    (Pt 212.0 244.0),
    (Pt 260.0 108.0),
    (Pt 305.0 124.0),
    (Pt 356.0 39.0)
)

# Right/Bottom edge (Lower Facet in photo):
$bottomEdgePhoto = @(
    (Pt 57.0 408.0),
    (Pt 190.0 240.0),
    (Pt 238.0 252.0),
    (Pt 292.0 126.0),
    (Pt 320.0 134.0),
    (Pt 356.0 39.0)
)

# Transform all points:
$polyTop = [System.Drawing.PointF[]]@(
    ($topEdgePhoto | ForEach-Object { Rotate-Point $_ $origin $rotAngle $scale $tx $ty }) +
    ($spinePhoto[-1..-($spinePhoto.Count)] | ForEach-Object { Rotate-Point $_ $origin $rotAngle $scale $tx $ty })
)

$polyBottom = [System.Drawing.PointF[]]@(
    ($spinePhoto | ForEach-Object { Rotate-Point $_ $origin $rotAngle $scale $tx $ty }) +
    ($bottomEdgePhoto[-1..-($bottomEdgePhoto.Count)] | ForEach-Object { Rotate-Point $_ $origin $rotAngle $scale $tx $ty })
)

$bmp = New-Object System.Drawing.Bitmap(600, 300)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gfx.Clear([System.Drawing.Color]::FromArgb(25, 27, 34))

# 1. Subtle soft glow
$pGlow = New-Object System.Drawing.Pen((Clr 80 255 215 0), 10.0)
$pGlow.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$spineTransformed = [System.Drawing.PointF[]]($spinePhoto | ForEach-Object { Rotate-Point $_ $origin $rotAngle $scale $tx $ty })
$gfx.DrawLines($pGlow, $spineTransformed)
$pGlow.Dispose()

# 2. Lower Facet (Shaded Gold)
$bBottom = New-Object System.Drawing.SolidBrush((Clr 255 175 125 35))
$gfx.FillPolygon($bBottom, $polyBottom)
$bBottom.Dispose()

# 3. Upper Facet (Radiant Gold)
$bTop = New-Object System.Drawing.SolidBrush((Clr 255 245 210 100))
$gfx.FillPolygon($bTop, $polyTop)
$bTop.Dispose()

# 4. Metallic Edge Outline
$pEdge = New-Object System.Drawing.Pen((Clr 220 120 70 15), 1.5)
$pEdge.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
$gfx.DrawPolygon($pEdge, $polyTop)
$gfx.DrawPolygon($pEdge, $polyBottom)
$pEdge.Dispose()

# 5. Center Spine Highlight
$pSpine = New-Object System.Drawing.Pen((Clr 255 255 255 240), 1.5)
$pSpine.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Miter
$gfx.DrawLines($pSpine, $spineTransformed)
$pSpine.Dispose()

# 6. Spark glints
$pSpark = New-Object System.Drawing.Pen((Clr 240 255 255 255), 1.2)
$gfx.DrawLines($pSpark, @($spineTransformed[1], (Pt ($spineTransformed[1].X + 4) ($spineTransformed[1].Y - 8)), (Pt ($spineTransformed[2].X - 4) ($spineTransformed[2].Y - 2))))
$gfx.DrawLines($pSpark, @($spineTransformed[3], (Pt ($spineTransformed[3].X - 4) ($spineTransformed[3].Y + 8)), (Pt ($spineTransformed[4].X + 4) ($spineTransformed[4].Y + 2))))
$pSpark.Dispose()

# Tip flare
$bStar = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$tipPt = $spineTransformed[-1]
$gfx.FillEllipse($bStar, ($tipPt.X - 3), ($tipPt.Y - 3), 6, 6)
$bStar.Dispose()

$testPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\test_zeus_bolt_rotated.png"
$bmp.Save($testPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose(); $gfx.Dispose()
Write-Output "Saved to $testPath"
