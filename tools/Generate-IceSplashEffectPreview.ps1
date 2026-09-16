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
    
    $tipX = $cx + $cosA * $len;     $tipY = $cy + $sinA * $len
    $tailX = $cx - $cosA * $len;    $tailY = $cy - $sinA * $len
    $side1X = $cx + $cosP * $width; $side1Y = $cy + $sinP * $width
    $side2X = $cx - $cosP * $width; $side2Y = $cy - $sinP * $width
    
    $pts = [System.Drawing.PointF[]]@(
        (Pt $tipX $tipY),
        (Pt $side1X $side1Y),
        (Pt $tailX $tailY),
        (Pt $side2X $side2Y)
    )
    $gfx.FillPolygon($brush, $pts)
}

# ==============================================================================
# Procedural Snowflake Splash Drawing Logic
# ==============================================================================
function Draw-IceSplashFrame($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }

    # Palette
    $cWhite     = Clr 255 255 255 255
    $cLightCyan = Clr 255 210 245 255
    $cMidCyan   = Clr 255 120 205 255
    $cDeepBlue  = Clr 255 45 110 210
    $cGlowSoft  = Clr 90  90 190 255
    $cGlowInner = Clr 150 160 230 255

    # 6 axes angles (in radians: 0, 60, 120, 180, 240, 300 deg)
    $axes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { $_ * [Math]::PI / 3.0 }
    # 6 intermediate angles (30, 90, 150, 210, 270, 330 deg)
    $interAxes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { ($_ * [Math]::PI / 3.0) + ([Math]::PI / 6.0) }

    if ($f -eq 0) {
        # --- Frame 0: Crystal Seed & Initial Frost Flash ---
        $bGlow = New-Object System.Drawing.SolidBrush($cGlowSoft)
        Fill-EllipseCentered $gfx $bGlow $cx $cy 14.0 14.0
        $bGlow.Dispose()

        $bInnerGlow = New-Object System.Drawing.SolidBrush($cGlowInner)
        Fill-EllipseCentered $gfx $bInnerGlow $cx $cy 8.0 8.0
        $bInnerGlow.Dispose()

        # 6 small initial spokes
        $pSpoke = New-Object System.Drawing.Pen($cMidCyan, 1.8)
        $bTip = New-Object System.Drawing.SolidBrush($cWhite)
        foreach ($a in $axes) {
            $ex = $cx + [Math]::Cos($a) * 11.0
            $ey = $cy + [Math]::Sin($a) * 11.0
            $gfx.DrawLine($pSpoke, $cx, $cy, $ex, $ey)
            Draw-RotatedDiamond $gfx $bTip $ex $ey 2.5 1.5 $a
        }
        $pSpoke.Dispose()
        $bTip.Dispose()

        # Core star
        $bWhite = New-Object System.Drawing.SolidBrush($cWhite)
        Draw-Diamond $gfx $bWhite $cx $cy 3.5 3.5
        $bWhite.Dispose()

    } elseif ($f -eq 1) {
        # --- Frame 1: Snowflake Rapid Growth & Dendritic Branching ---
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 100 80 180 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy 24.0 24.0
        $bGlow.Dispose()

        # Main 6 branches with chevrons
        $pThickSpoke = New-Object System.Drawing.Pen($cDeepBlue, 2.5)
        $pCoreSpoke = New-Object System.Drawing.Pen($cWhite, 1.2)
        $pBranch = New-Object System.Drawing.Pen($cMidCyan, 1.4)
        $bDiamond = New-Object System.Drawing.SolidBrush($cLightCyan)
        $bWhiteDiamond = New-Object System.Drawing.SolidBrush($cWhite)

        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $len = 23.0
            $ex = $cx + $cosA * $len
            $ey = $cy + $sinA * $len

            # Main stem
            $gfx.DrawLine($pThickSpoke, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pCoreSpoke, $cx, $cy, $ex, $ey)

            # 1 pair of chevrons at 11px
            $cx1 = $cx + $cosA * 11.0; $cy1 = $cy + $sinA * 11.0
            $angL = $a + [Math]::PI / 3.0; $angR = $a - [Math]::PI / 3.0
            $gfx.DrawLine($pBranch, $cx1, $cy1, ($cx1 + [Math]::Cos($angL) * 5.0), ($cy1 + [Math]::Sin($angL) * 5.0))
            $gfx.DrawLine($pBranch, $cx1, $cy1, ($cx1 + [Math]::Cos($angR) * 5.0), ($cy1 + [Math]::Sin($angR) * 5.0))

            # Spearhead diamond tip
            Draw-RotatedDiamond $gfx $bWhiteDiamond $ex $ey 4.0 2.2 $a
        }

        # 6 secondary spikes
        foreach ($ia in $interAxes) {
            $iex = $cx + [Math]::Cos($ia) * 11.0
            $iey = $cy + [Math]::Sin($ia) * 11.0
            $gfx.DrawLine($pBranch, $cx, $cy, $iex, $iey)
            Draw-RotatedDiamond $gfx $bDiamond $iex $iey 2.2 1.3 $ia
        }

        $pThickSpoke.Dispose()
        $pCoreSpoke.Dispose()
        $pBranch.Dispose()
        $bDiamond.Dispose()
        $bWhiteDiamond.Dispose()

        # Center hexagon ring
        $pRing = New-Object System.Drawing.Pen($cWhite, 1.5)
        $hexPts = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $hexPts += (Pt ($cx + [Math]::Cos($a) * 5.0) ($cy + [Math]::Sin($a) * 5.0))
        }
        $gfx.DrawPolygon($pRing, $hexPts)
        $pRing.Dispose()

    } elseif ($f -eq 2) {
        # --- Frame 2: Maximum Bloom - Grand Ornate Dendritic Snowflake ---
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 120 70 170 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy 38.0 38.0
        $bGlow.Dispose()

        # Expanding frost dust shockwave ring
        $pWave = New-Object System.Drawing.Pen((Clr 160 180 235 255), 1.2)
        $gfx.DrawEllipse($pWave, ($cx - 30.0), ($cy - 30.0), 60.0, 60.0)
        $pWave.Dispose()

        $pStemDark = New-Object System.Drawing.Pen($cDeepBlue, 2.8)
        $pStemLight = New-Object System.Drawing.Pen($cLightCyan, 1.6)
        $pStemCore = New-Object System.Drawing.Pen($cWhite, 1.0)
        $pChev = New-Object System.Drawing.Pen($cMidCyan, 1.5)
        $pChevCore = New-Object System.Drawing.Pen($cWhite, 0.8)
        $bBigTip = New-Object System.Drawing.SolidBrush($cWhite)
        $bSideTip = New-Object System.Drawing.SolidBrush($cLightCyan)

        # Draw hexagonal connecting ring at 15px
        $pLattice = New-Object System.Drawing.Pen((Clr 180 130 215 255), 1.0)
        $ringPts = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $ringPts += (Pt ($cx + [Math]::Cos($a) * 14.5) ($cy + [Math]::Sin($a) * 14.5))
        }
        $gfx.DrawPolygon($pLattice, $ringPts)
        $pLattice.Dispose()

        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $len = 36.0
            $ex = $cx + $cosA * $len
            $ey = $cy + $sinA * $len

            # Main branch stem
            $gfx.DrawLine($pStemDark, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pStemLight, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pStemCore, $cx, $cy, $ex, $ey)

            # Chevron pair 1 at 14.5px (length 7.0px)
            $cx1 = $cx + $cosA * 14.5; $cy1 = $cy + $sinA * 14.5
            $angL = $a + [Math]::PI / 3.0; $angR = $a - [Math]::PI / 3.0
            $lx1 = $cx1 + [Math]::Cos($angL) * 7.5; $ly1 = $cy1 + [Math]::Sin($angL) * 7.5
            $rx1 = $cx1 + [Math]::Cos($angR) * 7.5; $ry1 = $cy1 + [Math]::Sin($angR) * 7.5
            $gfx.DrawLine($pChev, $cx1, $cy1, $lx1, $ly1)
            $gfx.DrawLine($pChev, $cx1, $cy1, $rx1, $ry1)
            $gfx.DrawLine($pChevCore, $cx1, $cy1, $lx1, $ly1)
            $gfx.DrawLine($pChevCore, $cx1, $cy1, $rx1, $ry1)
            Draw-RotatedDiamond $gfx $bSideTip $lx1 $ly1 2.2 1.2 $angL
            Draw-RotatedDiamond $gfx $bSideTip $rx1 $ry1 2.2 1.2 $angR

            # Chevron pair 2 at 25px (length 5.5px)
            $cx2 = $cx + $cosA * 25.0; $cy2 = $cy + $sinA * 25.0
            $lx2 = $cx2 + [Math]::Cos($angL) * 5.5; $ly2 = $cy2 + [Math]::Sin($angL) * 5.5
            $rx2 = $cx2 + [Math]::Cos($angR) * 5.5; $ry2 = $cy2 + [Math]::Sin($angR) * 5.5
            $gfx.DrawLine($pChev, $cx2, $cy2, $lx2, $ly2)
            $gfx.DrawLine($pChev, $cx2, $cy2, $rx2, $ry2)
            $gfx.DrawLine($pChevCore, $cx2, $cy2, $lx2, $ly2)
            $gfx.DrawLine($pChevCore, $cx2, $cy2, $rx2, $ry2)

            # Majestic diamond spearhead tip at 36px
            Draw-RotatedDiamond $gfx $bBigTip $ex $ey 6.0 3.2 $a
        }

        # 6 secondary intermediate crystal spears at 19px
        foreach ($ia in $interAxes) {
            $iex = $cx + [Math]::Cos($ia) * 19.0
            $iey = $cy + [Math]::Sin($ia) * 19.0
            $gfx.DrawLine($pStemLight, $cx, $cy, $iex, $iey)
            Draw-RotatedDiamond $gfx $bSideTip $iex $iey 3.5 2.0 $ia
        }

        # Central hexagonal jewel
        $bHex = New-Object System.Drawing.SolidBrush($cWhite)
        $centerHex = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $centerHex += (Pt ($cx + [Math]::Cos($a) * 5.5) ($cy + [Math]::Sin($a) * 5.5))
        }
        $gfx.FillPolygon($bHex, $centerHex)
        $bHex.Dispose()

        $pStemDark.Dispose(); $pStemLight.Dispose(); $pStemCore.Dispose()
        $pChev.Dispose(); $pChevCore.Dispose(); $bBigTip.Dispose(); $bSideTip.Dispose()

    } elseif ($f -eq 3) {
        # --- Frame 3: Crystal Fracture & Radial Burst Starts ---
        # Central core shatters into hollow ring
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 90 70 170 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy 44.0 44.0
        $bGlow.Dispose()

        # Expanding shattered ring
        $pShock = New-Object System.Drawing.Pen((Clr 200 180 240 255), 1.5)
        $gfx.DrawEllipse($pShock, ($cx - 36.0), ($cy - 36.0), 72.0, 72.0)
        $pShock.Dispose()

        $bShardWhite = New-Object System.Drawing.SolidBrush($cWhite)
        $bShardCyan = New-Object System.Drawing.SolidBrush($cLightCyan)
        $pShardTrail = New-Object System.Drawing.Pen((Clr 160 100 200 255), 1.0)

        # 6 main spearhead shards flying outward to 44px
        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $dist = 43.5
            $sx = $cx + $cosA * $dist
            $sy = $cy + $sinA * $dist
            $gfx.DrawLine($pShardTrail, ($cx + $cosA * 28.0), ($cy + $sinA * 28.0), $sx, $sy)
            Draw-RotatedDiamond $gfx $bShardWhite $sx $sy 6.5 3.2 $a

            # Small trailing splinter
            $spX = $cx + $cosA * 32.0 + [Math]::Sin($a) * 3.0
            $spY = $cy + $sinA * 32.0 - [Math]::Cos($a) * 3.0
            Draw-RotatedDiamond $gfx $bShardCyan $spX $spY 2.5 1.4 ($a + 0.3)
        }

        # 12 chevron shards flying outward (angles shifted +/- 15 deg from axes, dist 30px)
        for ($i = 0; $i -lt 6; $i++) {
            $a = $axes[$i]
            $aL = $a + 0.32; $aR = $a - 0.32
            $lx = $cx + [Math]::Cos($aL) * 31.0; $ly = $cy + [Math]::Sin($aL) * 31.0
            $rx = $cx + [Math]::Cos($aR) * 31.0; $ry = $cy + [Math]::Sin($aR) * 31.0
            Draw-RotatedDiamond $gfx $bShardCyan $lx $ly 4.0 2.0 ($aL + 0.5)
            Draw-RotatedDiamond $gfx $bShardCyan $rx $ry 4.0 2.0 ($aR - 0.5)
        }

        # 6 secondary spear shards at 27px
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 26.5
            $iy = $cy + [Math]::Sin($ia) * 26.5
            Draw-RotatedDiamond $gfx $bShardWhite $ix $iy 3.5 1.8 $ia
        }

        # Core shattered ring of 6 small glittering dots
        $bCoreDot = New-Object System.Drawing.SolidBrush($cLightCyan)
        foreach ($a in $axes) {
            $dx = $cx + [Math]::Cos($a) * 12.0
            $dy = $cy + [Math]::Sin($a) * 12.0
            Fill-EllipseCentered $gfx $bCoreDot $dx $dy 2.0 2.0
        }
        $bCoreDot.Dispose()

        $bShardWhite.Dispose(); $bShardCyan.Dispose(); $pShardTrail.Dispose()

    } elseif ($f -eq 4) {
        # --- Frame 4: Far Dispersal & Crystal Spinning ---
        $bShardFaded = New-Object System.Drawing.SolidBrush((Clr 210 230 250 255))
        $bShardSub   = New-Object System.Drawing.SolidBrush((Clr 180 130 215 255))
        $bGlitter    = New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))

        # Dispersing wave ring
        $pWave = New-Object System.Drawing.Pen((Clr 90 140 220 255), 1.0)
        $gfx.DrawEllipse($pWave, ($cx - 44.0), ($cy - 44.0), 88.0, 88.0)
        $pWave.Dispose()

        # 6 main spearhead shards at 50px (near edge, safe margin 14px)
        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $sx = $cx + $cosA * 49.5
            $sy = $cy + $sinA * 49.5
            Draw-RotatedDiamond $gfx $bShardFaded $sx $sy 5.5 2.6 ($a + 0.2)

            # 2 ice dust sparkles behind it
            $d1x = $cx + $cosA * 42.0 + [Math]::Sin($a) * 2.5
            $d1y = $cy + $sinA * 42.0 - [Math]::Cos($a) * 2.5
            $d2x = $cx + $cosA * 35.0 - [Math]::Sin($a) * 3.5
            $d2y = $cy + $sinA * 35.0 + [Math]::Cos($a) * 3.5
            Draw-Diamond $gfx $bGlitter $d1x $d1y 1.5 1.5
            Draw-Diamond $gfx $bGlitter $d2x $d2y 1.2 1.2
        }

        # 12 outer shards at 39px
        for ($i = 0; $i -lt 6; $i++) {
            $a = $axes[$i]
            $aL = $a + 0.38; $aR = $a - 0.38
            $lx = $cx + [Math]::Cos($aL) * 38.5; $ly = $cy + [Math]::Sin($aL) * 38.5
            $rx = $cx + [Math]::Cos($aR) * 38.5; $ry = $cy + [Math]::Sin($aR) * 38.5
            Draw-RotatedDiamond $gfx $bShardSub $lx $ly 3.2 1.6 ($aL + 0.8)
            Draw-RotatedDiamond $gfx $bShardSub $rx $ry 3.2 1.6 ($aR - 0.8)
        }

        # 6 intermediate glints at 34px
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 33.5
            $iy = $cy + [Math]::Sin($ia) * 33.5
            Draw-RotatedDiamond $gfx $bGlitter $ix $iy 2.5 1.5 ($ia + 0.4)
        }

        $bShardFaded.Dispose(); $bShardSub.Dispose(); $bGlitter.Dispose()

    } elseif ($f -eq 5) {
        # --- Frame 5: Dissolving Frost Glitters ---
        $bFaintGlitter = New-Object System.Drawing.SolidBrush((Clr 140 210 245 255))
        $bWhiteSparkle = New-Object System.Drawing.SolidBrush((Clr 180 255 255 255))

        # Tiny diamond sparks scattered along perimeter
        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a) * 51.5
            $sy = $cy + [Math]::Sin($a) * 51.5
            Draw-Diamond $gfx $bWhiteSparkle $sx $sy 1.8 1.8

            $sx2 = $cx + [Math]::Cos($a + 0.25) * 44.0
            $sy2 = $cy + [Math]::Sin($a + 0.25) * 44.0
            Draw-Diamond $gfx $bFaintGlitter $sx2 $sy2 1.4 1.4
        }
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 40.0
            $iy = $cy + [Math]::Sin($ia) * 40.0
            Draw-Diamond $gfx $bFaintGlitter $ix $iy 1.5 1.5
        }

        $bFaintGlitter.Dispose(); $bWhiteSparkle.Dispose()

    } elseif ($f -eq 6) {
        # --- Frame 6: Final Dissipating Ice Dust ---
        $bDust = New-Object System.Drawing.SolidBrush((Clr 70 180 235 255))
        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a) * 52.0
            $sy = $cy + [Math]::Sin($a) * 52.0
            Fill-EllipseCentered $gfx $bDust $sx $sy 1.2 1.2
        }
        $bDust.Dispose()
    }
}

# ==============================================================================
# Generate 1280x128 Spritesheet
# ==============================================================================
$spritesheet = New-Object System.Drawing.Bitmap(1280, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gSheet = [System.Drawing.Graphics]::FromImage($spritesheet)
$gSheet.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gSheet.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gSheet.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSheet.Clear([System.Drawing.Color]::Transparent)

for ($f = 0; $f -lt 10; $f++) {
    $cx = $f * 128.0 + 64.0
    $cy = 64.0
    Draw-IceSplashFrame $gSheet $cx $cy $f
}
$gSheet.Dispose()

# Save Spritesheet draft
$sheetPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\Effect_Ice_Splash_Blizzard_Draft.png"
$spritesheet.Save($sheetPath, [System.Drawing.Imaging.ImageFormat]::Png)

# ==============================================================================
# Generate Preview Showcase (Dark Game-like Background with Magnified Frames)
# ==============================================================================
# Preview Canvas: 1320 x 480
$pWidth = 1320
$pHeight = 520
$preview = New-Object System.Drawing.Bitmap($pWidth, $pHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gp = [System.Drawing.Graphics]::FromImage($preview)
$gp.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gp.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gp.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

# Background: Dark navy tower defense grid background
$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 22 34))
$gp.FillRectangle($bBg, 0, 0, $pWidth, $pHeight)
$bBg.Dispose()

# Grid lines
$pGrid = New-Object System.Drawing.Pen((Clr 40 100 150 220), 1.0)
for ($x = 0; $x -lt $pWidth; $x += 32) { $gp.DrawLine($pGrid, $x, 0, $x, $pHeight) }
for ($y = 0; $y -lt $pHeight; $y += 32) { $gp.DrawLine($pGrid, 0, $y, $pWidth, $y) }
$pGrid.Dispose()

# Title
$fontTitle = New-Object System.Drawing.Font("Arial", 14, [System.Drawing.FontStyle]::Bold)
$fontSub   = New-Object System.Drawing.Font("Arial", 10, [System.Drawing.FontStyle]::Regular)
$bTextWhite = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$bTextCyan  = New-Object System.Drawing.SolidBrush((Clr 255 130 220 255))
$gp.DrawString("E108: Splash Ice Tower Hit Effect (Snowflake Crystal Explosion Showcase)", $fontTitle, $bTextWhite, 20.0, 15.0)
$gp.DrawString("Concept: 6-Fold Snowflake Crystal Growth -> Bloom & Shockwave -> Radial Shard Burst & Glitter Dispersal", $fontSub, $bTextCyan, 20.0, 42.0)

# Section 1: 1x Real-time Sequence (10 Frames side by side at y=75)
$gp.DrawString("[ 1x Real-Time Frame Sequence (10 Frames @ 16 FPS / 128x128 each) ]", $fontSub, $bTextWhite, 20.0, 75.0)
for ($f = 0; $f -lt 10; $f++) {
    $destX = 20 + $f * 129
    $destY = 98
    # Frame box
    $pBox = New-Object System.Drawing.Pen((Clr 100 80 140 200), 1.0)
    $gp.DrawRectangle($pBox, $destX, $destY, 128, 128)
    $pBox.Dispose()

    # Draw frame from spritesheet
    $srcRect = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.Rectangle($destX, $destY, 128, 128)
    $gp.DrawImage($spritesheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $gp.DrawString("F$f", $fontSub, $bTextCyan, ($destX + 4.0), ($destY + 4.0))
}

# Section 2: 2x Magnified View of Active Frames (F0 ~ F5 at y=260)
$gp.DrawString("[ 2x Zoom Detailed View (Key Progression: F0 Inception -> F1 Growth -> F2 Bloom -> F3 Shatter -> F4 Dispersal -> F5 Fade) ]", $fontSub, $bTextWhite, 20.0, 240.0)
for ($f = 0; $f -le 5; $f++) {
    $destX = 20 + $f * 215
    $destY = 265
    $pBox = New-Object System.Drawing.Pen((Clr 140 100 180 255), 1.0)
    $gp.DrawRectangle($pBox, $destX, $destY, 200, 200)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.Rectangle($destX, $destY, 200, 200)
    $gp.DrawImage($spritesheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $labels = @("F0: Seed / Flash", "F1: Crystal Growth", "F2: Full Bloom", "F3: Shard Burst", "F4: Far Dispersal", "F5: Frost Glitter")
    $gp.DrawString($labels[$f], $fontSub, $bTextWhite, ($destX + 4.0), ($destY + 180.0))
}

$gp.Dispose()

$previewPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_ice_splash_effect.png"
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)

$preview.Dispose()
$spritesheet.Dispose()

Write-Output "Preview generated at: $previewPath"
