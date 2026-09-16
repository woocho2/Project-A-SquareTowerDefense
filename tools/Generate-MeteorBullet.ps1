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
# DRAW 1 FRAME OF METEOR BULLET (Sphere head + dynamic flickering meteor tail)
# ==============================================================================
function Draw-MeteorFrame($gfx, [float]$cellX, [float]$cellY, [int]$frameIndex, [int]$totalFrames) {
    $phase = ($frameIndex / [float]$totalFrames) * 2.0 * [Math]::PI
    $cy = $cellY + 64.0
    $sphereX = $cellX + 76.0 # Sphere head centered at (76, 64)
    $radius = 15.0

    # --------------------------------------------------------------------------
    # 1. METEOR TAIL (Flickering, undulating aerodynamic fiery wake trailing left)
    # --------------------------------------------------------------------------
    $tailLen1 = 58.0 + [Math]::Sin($phase * 2.0) * 5.0
    $tailLen2 = 36.0 + [Math]::Cos($phase * 2.0) * 4.0
    $wWave1 = [Math]::Sin($phase) * 2.2
    $wWave2 = [Math]::Cos($phase) * 2.2

    # Outer Crimson Tail Polygon
    $tailPtsRed = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 14.0)),
        (Pt ($sphereX - 22.0) ($cy - 12.0 + $wWave1)),
        (Pt ($sphereX - $tailLen2) ($cy - 7.0 + $wWave2)),
        (Pt ($sphereX - $tailLen1) ($cy + [Math]::Sin($phase) * 3.0)), # Main central tip
        (Pt ($sphereX - $tailLen2) ($cy + 7.0 + $wWave1)),
        (Pt ($sphereX - 22.0) ($cy + 12.0 + $wWave2)),
        (Pt ($sphereX - 2.0) ($cy + 14.0))
    )
    $bTailRed = New-Object System.Drawing.SolidBrush((Clr 190 220 35 0))
    $gfx.FillPolygon($bTailRed, $tailPtsRed)
    $bTailRed.Dispose()

    # Mid Fiery Orange Plasma Stream
    $oLen1 = $tailLen1 * 0.76
    $oLen2 = $tailLen2 * 0.72
    $tailPtsOrange = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 9.5)),
        (Pt ($sphereX - 18.0) ($cy - 7.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - $oLen2) ($cy - 4.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - $oLen1) ($cy + [Math]::Sin($phase) * 2.0)),
        (Pt ($sphereX - $oLen2) ($cy + 4.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - 18.0) ($cy + 7.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - 2.0) ($cy + 9.5))
    )
    $bTailOrange = New-Object System.Drawing.SolidBrush((Clr 240 255 125 0))
    $gfx.FillPolygon($bTailOrange, $tailPtsOrange)
    $bTailOrange.Dispose()

    # Inner Golden Core Flame Streak
    $yLen = $tailLen1 * 0.48
    $tailPtsYellow = [System.Drawing.PointF[]]@(
        (Pt ($sphereX) ($cy - 5.5)),
        (Pt ($sphereX - 14.0) ($cy - 3.5 + $wWave1 * 0.4)),
        (Pt ($sphereX - $yLen) ($cy + [Math]::Sin($phase) * 1.0)),
        (Pt ($sphereX - 14.0) ($cy + 3.5 + $wWave2 * 0.4)),
        (Pt ($sphereX) ($cy + 5.5))
    )
    $bTailYellow = New-Object System.Drawing.SolidBrush((Clr 255 255 220 50))
    $gfx.FillPolygon($bTailYellow, $tailPtsYellow)
    $bTailYellow.Dispose()

    # Trailing Filament Lines (Plasma Whisps)
    $pWhisp1 = New-Object System.Drawing.Pen((Clr 160 255 160 30), 1.8)
    $pWhisp2 = New-Object System.Drawing.Pen((Clr 140 255 80 0), 1.6)
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy - 4.0), ($sphereX - $tailLen1 - 6.0), ($cy - 3.0 + $wWave1))
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy + 4.0), ($sphereX - $tailLen1 - 6.0), ($cy + 3.0 + $wWave2))
    $gfx.DrawLine($pWhisp2, ($sphereX - 10.0), $cy, ($sphereX - $tailLen1 - 12.0), $cy)
    $pWhisp1.Dispose(); $pWhisp2.Dispose()

    # --------------------------------------------------------------------------
    # 2. TRAILING BURNING CINDERS / SPARKS (Flowing backwards seamlessly)
    # --------------------------------------------------------------------------
    $bSpkGold = New-Object System.Drawing.SolidBrush((Clr 250 255 235 80))
    $bSpkRed  = New-Object System.Drawing.SolidBrush((Clr 200 255 70 0))

    # 5 looping sparks
    $sparkData = @(
        @(0.15, -7.0, 2.2),
        @(0.35,  6.5, 1.9),
        @(0.55, -4.0, 2.4),
        @(0.75,  5.0, 1.8),
        @(0.92, -1.0, 1.6)
    )
    foreach ($spk in $sparkData) {
        $loopProgress = ($spk[0] + ($frameIndex / [float]$totalFrames)) % 1.0
        $spkX = $sphereX - 10.0 - ($loopProgress * 54.0)
        $spkY = $cy + [float]$spk[1] + [Math]::Sin($loopProgress * 4.0 + $phase) * 2.0
        $sz   = [float]$spk[2] * (1.1 - $loopProgress * 0.4)

        Fill-EllipseCentered $gfx $bSpkRed  $spkX $spkY ($sz * 1.3) ($sz * 1.3)
        Fill-EllipseCentered $gfx $bSpkGold $spkX $spkY ($sz * 0.7) ($sz * 0.7)
    }
    $bSpkGold.Dispose(); $bSpkRed.Dispose()

    # --------------------------------------------------------------------------
    # 3. SPHERE HEAD (Clean, solid, incandescent glowing spherical orb)
    # --------------------------------------------------------------------------
    # Outer Atmospheric Compression / Heat Halo
    $bAura = New-Object System.Drawing.SolidBrush((Clr 110 255 45 0))
    Fill-EllipseCentered $gfx $bAura $sphereX $cy 19.5 19.5
    $bAura.Dispose()

    # Bow Shock (Atmospheric heat barrier on leading edge +X)
    $pBowShock = New-Object System.Drawing.Pen((Clr 190 255 230 110), 2.2)
    # Draw arc on right side (angles from -70 deg to +70 deg)
    $gfx.DrawArc($pBowShock, ($sphereX - 19.5), ($cy - 19.5), 39.0, 39.0, -70.0, 140.0)
    $pBowShock.Dispose()

    # Main Spherical Orb Body (Perfect circle)
    $bSphereBody = New-Object System.Drawing.SolidBrush((Clr 255 225 35 0))
    Fill-EllipseCentered $gfx $bSphereBody $sphereX $cy $radius $radius
    $bSphereBody.Dispose()

    # Spherical Volume Layer (Offset towards front +X for 3D sphere look)
    $bSphereVol = New-Object System.Drawing.SolidBrush((Clr 255 255 115 0))
    Fill-EllipseCentered $gfx $bSphereVol ($sphereX + 2.5) $cy 11.0 11.0
    $bSphereVol.Dispose()

    # Superheated Incandescent Core
    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 235 60))
    Fill-EllipseCentered $gfx $bCore ($sphereX + 4.5) $cy 6.5 6.5
    $bCore.Dispose()

    # Pure White Hot Spot
    $bHotSpot = New-Object System.Drawing.SolidBrush((Clr 255 255 255 245))
    Fill-EllipseCentered $gfx $bHotSpot ($sphereX + 6.0) $cy 3.5 3.5
    $bHotSpot.Dispose()
}

# ==============================================================================
# GENERATE 6-FRAME SPRITESHEET (768 x 128)
# ==============================================================================
$totalFrames = 6
$sheetW = $totalFrames * 128
$sheetH = 128

$bmpSheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gSheet = [System.Drawing.Graphics]::FromImage($bmpSheet)
$gSheet.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gSheet.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gSheet.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSheet.Clear([System.Drawing.Color]::Transparent)

for ($i = 0; $i -lt $totalFrames; $i++) {
    $cellX = $i * 128
    Draw-MeteorFrame $gSheet $cellX 0 $i $totalFrames
}

$sheetPath = "Assets/4. DotAsset/Projectile_Meteor_Fire.png"
if (Test-Path $sheetPath) { [System.IO.File]::Delete($sheetPath) }
$bmpSheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Saved Meteor Spritesheet to $sheetPath!"

# ==============================================================================
# GENERATE PREVIEW (Side-by-Side 6 Frames + Enlarged Showcase)
# ==============================================================================
$prevW = 768
$prevH = 340
$bmpPrev = New-Object System.Drawing.Bitmap($prevW, $prevH)
$gPrev = [System.Drawing.Graphics]::FromImage($bmpPrev)
$gPrev.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gPrev.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gPrev.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

# Top title
$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 11, [System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$bSub   = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))
$sf     = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center

$title = [System.Text.Encoding]::UTF8.GetString(@(0xEB, 0x8B, 0xA8, 0xEC, 0x88, 0x9C, 0x20, 0xEA, 0xB5, 0xAC, 0xEC, 0xB2, 0xB4, 0x20, 0xEC, 0x9A, 0xB4, 0xEC, 0x84, 0x9D, 0xED, 0x98, 0x95, 0x20, 0xEC, 0xB4, 0x9D, 0xEC, 0x95, 0x8C, 0x20, 0xEC, 0x95, 0xA0, 0xEB, 0x8B, 0x88, 0xEB, 0xA9, 0x94, 0xEC, 0x9D, 0xB4, 0xEC, 0x85, 0x98, 0x20, 0x28, 0x36, 0x20, 0x46, 0x72, 0x61, 0x6D, 0x65, 0x73, 0x29))
$gPrev.DrawString($title, $fTitle, $bWhite, 384.0, 12.0, $sf)

# 6 Frames row (Y: 45 ~ 173)
$pCardBorder = New-Object System.Drawing.Pen((Clr 60 255 255 255), 1.0)
for ($i = 0; $i -lt $totalFrames; $i++) {
    $x = $i * 128
    $gPrev.DrawRectangle($pCardBorder, ($x + 2), 42, 124, 124)
    $srcRect = New-Object System.Drawing.Rectangle($x, 0, 128, 128)
    $dstRect = New-Object System.Drawing.Rectangle($x, 40, 128, 128)
    $gPrev.DrawImage($bmpSheet, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $lbl = "Frame $i"
    $fMini = New-Object System.Drawing.Font('Arial', 9, [System.Drawing.FontStyle]::Regular)
    $gPrev.DrawString($lbl, $fMini, $bSub, [float]($x + 64), 168.0, $sf)
    $fMini.Dispose()
}

# Bottom section: 2x Enlarged Close-up of Meteor (Left to Right)
$gPrev.DrawLine($pCardBorder, 20, 195, 748, 195)
$closeUpTitle = [System.Text.Encoding]::UTF8.GetString(@(0xEA, 0xB5, 0xAC, 0xEC, 0xB2, 0xB4, 0x20, 0xEB, 0xB3, 0xB8, 0xEC, 0xB2, 0xB4, 0x20, 0x2B, 0x20, 0xEC, 0x9A, 0xB4, 0xEC, 0x84, 0x9D, 0x20, 0xED, 0x99, 0x94, 0xEC, 0x97, 0xBC, 0x20, 0xED, 0x9B, 0x84, 0xEB, 0xA5, 0x98, 0x20, 0xED, 0x99, 0x95, 0xEB, 0x8C, 0x80, 0x20, 0x28, 0x32, 0x78, 0x20, 0x5A, 0x6F, 0x6F, 0x6D, 0x29))
$gPrev.DrawString($closeUpTitle, $fTitle, $bWhite, 384.0, 205.0, $sf)

# Draw 2x zoomed Frame 0 centered at bottom
$srcCrop = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
$dstCrop = New-Object System.Drawing.Rectangle((384 - 128), 220, 256, 110)
$gPrev.DrawImage($bmpSheet, $dstCrop, $srcCrop, [System.Drawing.GraphicsUnit]::Pixel)

$fTitle.Dispose(); $bWhite.Dispose(); $bSub.Dispose(); $pCardBorder.Dispose(); $sf.Dispose()

$prevPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_meteor_bullet.png"
if (Test-Path $prevPath) { [System.IO.File]::Delete($prevPath) }
$bmpPrev.Save($prevPath, [System.Drawing.Imaging.ImageFormat]::Png)

$rootPrev = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\preview_meteor_bullet.png"
Copy-Item $prevPath $rootPrev -Force

$gSheet.Dispose(); $bmpSheet.Dispose()
$gPrev.Dispose(); $bmpPrev.Dispose()

Write-Output "Saved Preview to $prevPath and $rootPrev!"
