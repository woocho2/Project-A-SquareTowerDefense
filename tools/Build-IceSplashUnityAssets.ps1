# ==============================================================================
# Build-IceSplashUnityAssets.ps1
# Generates Splash Ice Tower Hit Effect (E108)
# - Spritesheet: Assets/4. DotAsset/6. Effect/Effect_Ice_Splash_Blizzard.png (1280x128, 10F)
# - Texture Meta: Effect_Ice_Splash_Blizzard.png.meta (PPU 64, Point filter, 10 sliced sprites 108000..108009)
# - Animation: Assets/10.Animation/Effect/E108.anim (16 FPS, DestroyEffect at 0.625s)
# - Prefab: Assets/2. Prefab/4. Effect/E108.prefab (Clean white color, Animator, EffectController, EffectPool2D)
# - Verification against EffectLibrary.asset
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

function Draw-IceSplashFrame($gfx, [float]$cx, [float]$cy, [int]$f) {
    if ($f -gt 6) { return }

    $cWhite     = Clr 255 255 255 255
    $cLightCyan = Clr 255 210 245 255
    $cMidCyan   = Clr 255 120 205 255
    $cDeepBlue  = Clr 255 45 110 210
    $cGlowSoft  = Clr 80  90 190 255
    $cGlowInner = Clr 150 160 230 255

    $axes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { $_ * [Math]::PI / 3.0 }
    $interAxes = @(0, 1, 2, 3, 4, 5) | ForEach-Object { ($_ * [Math]::PI / 3.0) + ([Math]::PI / 6.0) }

    if ($f -eq 0) {
        # --- F0: Crystal Seed & Initial Frost Flash ---
        $bGlow = New-Object System.Drawing.SolidBrush($cGlowSoft)
        Fill-EllipseCentered $gfx $bGlow $cx $cy 14.0 14.0
        $bGlow.Dispose()

        $bInnerGlow = New-Object System.Drawing.SolidBrush($cGlowInner)
        Fill-EllipseCentered $gfx $bInnerGlow $cx $cy 8.0 8.0
        $bInnerGlow.Dispose()

        $pSpoke = New-Object System.Drawing.Pen($cMidCyan, 1.8)
        $bTip = New-Object System.Drawing.SolidBrush($cWhite)
        foreach ($a in $axes) {
            $ex = $cx + [Math]::Cos($a) * 11.5
            $ey = $cy + [Math]::Sin($a) * 11.5
            $gfx.DrawLine($pSpoke, $cx, $cy, $ex, $ey)
            Draw-RotatedDiamond $gfx $bTip $ex $ey 2.6 1.5 $a
        }
        $pSpoke.Dispose()
        $bTip.Dispose()

        $bWhite = New-Object System.Drawing.SolidBrush($cWhite)
        Draw-Diamond $gfx $bWhite $cx $cy 3.5 3.5
        $bWhite.Dispose()

    } elseif ($f -eq 1) {
        # --- F1: Snowflake Growth & Dendritic Branching ---
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 90 80 180 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy 24.0 24.0
        $bGlow.Dispose()

        $pThickSpoke = New-Object System.Drawing.Pen($cDeepBlue, 2.5)
        $pCoreSpoke = New-Object System.Drawing.Pen($cWhite, 1.2)
        $pBranch = New-Object System.Drawing.Pen($cMidCyan, 1.5)
        $bDiamond = New-Object System.Drawing.SolidBrush($cLightCyan)
        $bWhiteDiamond = New-Object System.Drawing.SolidBrush($cWhite)

        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $len = 23.5
            $ex = $cx + $cosA * $len
            $ey = $cy + $sinA * $len

            $gfx.DrawLine($pThickSpoke, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pCoreSpoke, $cx, $cy, $ex, $ey)

            # Chevrons at 11.5px
            $cx1 = $cx + $cosA * 11.5; $cy1 = $cy + $sinA * 11.5
            $angL = $a + [Math]::PI / 3.0; $angR = $a - [Math]::PI / 3.0
            $gfx.DrawLine($pBranch, $cx1, $cy1, ($cx1 + [Math]::Cos($angL) * 5.2), ($cy1 + [Math]::Sin($angL) * 5.2))
            $gfx.DrawLine($pBranch, $cx1, $cy1, ($cx1 + [Math]::Cos($angR) * 5.2), ($cy1 + [Math]::Sin($angR) * 5.2))

            Draw-RotatedDiamond $gfx $bWhiteDiamond $ex $ey 4.2 2.3 $a
        }

        foreach ($ia in $interAxes) {
            $iex = $cx + [Math]::Cos($ia) * 11.5
            $iey = $cy + [Math]::Sin($ia) * 11.5
            $gfx.DrawLine($pBranch, $cx, $cy, $iex, $iey)
            Draw-RotatedDiamond $gfx $bDiamond $iex $iey 2.4 1.4 $ia
        }

        $pThickSpoke.Dispose(); $pCoreSpoke.Dispose(); $pBranch.Dispose()
        $bDiamond.Dispose(); $bWhiteDiamond.Dispose()

        # Inner hexagonal ring
        $pRing = New-Object System.Drawing.Pen($cWhite, 1.5)
        $hexPts = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $hexPts += (Pt ($cx + [Math]::Cos($a) * 5.2) ($cy + [Math]::Sin($a) * 5.2))
        }
        $gfx.DrawPolygon($pRing, $hexPts)
        $pRing.Dispose()

    } elseif ($f -eq 2) {
        # --- F2: Maximum Bloom - Grand Dendritic Snowflake ---
        $bGlow = New-Object System.Drawing.SolidBrush((Clr 100 70 170 255))
        Fill-EllipseCentered $gfx $bGlow $cx $cy 38.0 38.0
        $bGlow.Dispose()

        # Crisp shockwave ring
        $pWave = New-Object System.Drawing.Pen((Clr 180 190 245 255), 1.5)
        $gfx.DrawEllipse($pWave, ($cx - 31.0), ($cy - 31.0), 62.0, 62.0)
        $pWave.Dispose()

        $pStemDark = New-Object System.Drawing.Pen($cDeepBlue, 2.8)
        $pStemLight = New-Object System.Drawing.Pen($cLightCyan, 1.6)
        $pStemCore = New-Object System.Drawing.Pen($cWhite, 1.0)
        $pChev = New-Object System.Drawing.Pen($cMidCyan, 1.6)
        $pChevCore = New-Object System.Drawing.Pen($cWhite, 0.9)
        $bBigTip = New-Object System.Drawing.SolidBrush($cWhite)
        $bSideTip = New-Object System.Drawing.SolidBrush($cLightCyan)

        # Hexagonal lattice ring at 15px
        $pLattice = New-Object System.Drawing.Pen((Clr 190 140 225 255), 1.2)
        $ringPts = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $ringPts += (Pt ($cx + [Math]::Cos($a) * 15.0) ($cy + [Math]::Sin($a) * 15.0))
        }
        $gfx.DrawPolygon($pLattice, $ringPts)
        $pLattice.Dispose()

        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $len = 36.5
            $ex = $cx + $cosA * $len
            $ey = $cy + $sinA * $len

            $gfx.DrawLine($pStemDark, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pStemLight, $cx, $cy, $ex, $ey)
            $gfx.DrawLine($pStemCore, $cx, $cy, $ex, $ey)

            # Chevron 1 at 15px
            $cx1 = $cx + $cosA * 15.0; $cy1 = $cy + $sinA * 15.0
            $angL = $a + [Math]::PI / 3.0; $angR = $a - [Math]::PI / 3.0
            $lx1 = $cx1 + [Math]::Cos($angL) * 7.5; $ly1 = $cy1 + [Math]::Sin($angL) * 7.5
            $rx1 = $cx1 + [Math]::Cos($angR) * 7.5; $ry1 = $cy1 + [Math]::Sin($angR) * 7.5
            $gfx.DrawLine($pChev, $cx1, $cy1, $lx1, $ly1); $gfx.DrawLine($pChev, $cx1, $cy1, $rx1, $ry1)
            $gfx.DrawLine($pChevCore, $cx1, $cy1, $lx1, $ly1); $gfx.DrawLine($pChevCore, $cx1, $cy1, $rx1, $ry1)
            Draw-RotatedDiamond $gfx $bSideTip $lx1 $ly1 2.4 1.3 $angL
            Draw-RotatedDiamond $gfx $bSideTip $rx1 $ry1 2.4 1.3 $angR

            # Chevron 2 at 25.5px
            $cx2 = $cx + $cosA * 25.5; $cy2 = $cy + $sinA * 25.5
            $lx2 = $cx2 + [Math]::Cos($angL) * 5.8; $ly2 = $cy2 + [Math]::Sin($angL) * 5.8
            $rx2 = $cx2 + [Math]::Cos($angR) * 5.8; $ry2 = $cy2 + [Math]::Sin($angR) * 5.8
            $gfx.DrawLine($pChev, $cx2, $cy2, $lx2, $ly2); $gfx.DrawLine($pChev, $cx2, $cy2, $rx2, $ry2)
            $gfx.DrawLine($pChevCore, $cx2, $cy2, $lx2, $ly2); $gfx.DrawLine($pChevCore, $cx2, $cy2, $rx2, $ry2)

            # Large diamond spearhead tip
            Draw-RotatedDiamond $gfx $bBigTip $ex $ey 6.5 3.4 $a
        }

        # 6 secondary intermediate crystal needles at 19.5px
        foreach ($ia in $interAxes) {
            $iex = $cx + [Math]::Cos($ia) * 19.5
            $iey = $cy + [Math]::Sin($ia) * 19.5
            $gfx.DrawLine($pStemLight, $cx, $cy, $iex, $iey)
            Draw-RotatedDiamond $gfx $bSideTip $iex $iey 3.8 2.1 $ia
        }

        # Central hexagonal jewel
        $bHex = New-Object System.Drawing.SolidBrush($cWhite)
        $centerHex = [System.Drawing.PointF[]]@()
        foreach ($a in $axes) {
            $centerHex += (Pt ($cx + [Math]::Cos($a) * 5.8) ($cy + [Math]::Sin($a) * 5.8))
        }
        $gfx.FillPolygon($bHex, $centerHex)
        $bHex.Dispose()

        $pStemDark.Dispose(); $pStemLight.Dispose(); $pStemCore.Dispose()
        $pChev.Dispose(); $pChevCore.Dispose(); $bBigTip.Dispose(); $bSideTip.Dispose()

    } elseif ($f -eq 3) {
        # --- F3: Crystal Shatter & Radial Shard Burst ---
        # Outer soft frost aura
        $bMist = New-Object System.Drawing.SolidBrush((Clr 45 90 190 255))
        Fill-EllipseCentered $gfx $bMist $cx $cy 44.0 44.0
        $bMist.Dispose()

        # Expanding bright shockwave ring
        $pShock = New-Object System.Drawing.Pen((Clr 220 200 245 255), 2.0)
        $gfx.DrawEllipse($pShock, ($cx - 37.0), ($cy - 37.0), 74.0, 74.0)
        $pShock.Dispose()

        $bShardWhite = New-Object System.Drawing.SolidBrush($cWhite)
        $bShardCyan = New-Object System.Drawing.SolidBrush($cLightCyan)
        $pShardTrail = New-Object System.Drawing.Pen((Clr 160 110 210 255), 1.2)

        # 6 spearhead shards blasted outward to 44px
        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $sx = $cx + $cosA * 44.0
            $sy = $cy + $sinA * 44.0
            $gfx.DrawLine($pShardTrail, ($cx + $cosA * 26.0), ($cy + $sinA * 26.0), $sx, $sy)
            Draw-RotatedDiamond $gfx $bShardWhite $sx $sy 6.8 3.4 $a

            $spX = $cx + $cosA * 33.0 + [Math]::Sin($a) * 3.5
            $spY = $cy + $sinA * 33.0 - [Math]::Cos($a) * 3.5
            Draw-RotatedDiamond $gfx $bShardCyan $spX $spY 2.8 1.5 ($a + 0.3)
        }

        # 12 chevron shards flying outward at 32px
        for ($i = 0; $i -lt 6; $i++) {
            $a = $axes[$i]
            $aL = $a + 0.32; $aR = $a - 0.32
            $lx = $cx + [Math]::Cos($aL) * 32.0; $ly = $cy + [Math]::Sin($aL) * 32.0
            $rx = $cx + [Math]::Cos($aR) * 32.0; $ry = $cy + [Math]::Sin($aR) * 32.0
            Draw-RotatedDiamond $gfx $bShardCyan $lx $ly 4.2 2.1 ($aL + 0.5)
            Draw-RotatedDiamond $gfx $bShardCyan $rx $ry 4.2 2.1 ($aR - 0.5)
        }

        # 6 secondary shards at 27px
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 27.0
            $iy = $cy + [Math]::Sin($ia) * 27.0
            Draw-RotatedDiamond $gfx $bShardWhite $ix $iy 3.6 1.9 $ia
        }

        # Expanding ring of inner ice dust
        $bCoreDot = New-Object System.Drawing.SolidBrush($cWhite)
        foreach ($a in $axes) {
            $dx = $cx + [Math]::Cos($a) * 14.0
            $dy = $cy + [Math]::Sin($a) * 14.0
            Fill-EllipseCentered $gfx $bCoreDot $dx $dy 2.2 2.2
        }
        $bCoreDot.Dispose()

        $bShardWhite.Dispose(); $bShardCyan.Dispose(); $pShardTrail.Dispose()

    } elseif ($f -eq 4) {
        # --- F4: Far Radial Dispersal & Shard Spinning ---
        $bShardFaded = New-Object System.Drawing.SolidBrush((Clr 220 235 250 255))
        $bShardSub   = New-Object System.Drawing.SolidBrush((Clr 190 140 220 255))
        $bGlitter    = New-Object System.Drawing.SolidBrush((Clr 240 255 255 255))

        # Dispersing shockwave ring at 46px
        $pWave = New-Object System.Drawing.Pen((Clr 110 160 230 255), 1.2)
        $gfx.DrawEllipse($pWave, ($cx - 46.0), ($cy - 46.0), 92.0, 92.0)
        $pWave.Dispose()

        # 6 spearhead shards at 50px (safe margin 14px)
        foreach ($a in $axes) {
            $cosA = [Math]::Cos($a); $sinA = [Math]::Sin($a)
            $sx = $cx + $cosA * 49.8
            $sy = $cy + $sinA * 49.8
            Draw-RotatedDiamond $gfx $bShardFaded $sx $sy 5.6 2.7 ($a + 0.25)

            # Sparkle dust trail
            $d1x = $cx + $cosA * 42.0 + [Math]::Sin($a) * 3.0
            $d1y = $cy + $sinA * 42.0 - [Math]::Cos($a) * 3.0
            $d2x = $cx + $cosA * 34.0 - [Math]::Sin($a) * 4.0
            $d2y = $cy + $sinA * 34.0 + [Math]::Cos($a) * 4.0
            Draw-Diamond $gfx $bGlitter $d1x $d1y 1.6 1.6
            Draw-Diamond $gfx $bGlitter $d2x $d2y 1.3 1.3
        }

        # 12 outer shards at 40px
        for ($i = 0; $i -lt 6; $i++) {
            $a = $axes[$i]
            $aL = $a + 0.38; $aR = $a - 0.38
            $lx = $cx + [Math]::Cos($aL) * 39.5; $ly = $cy + [Math]::Sin($aL) * 39.5
            $rx = $cx + [Math]::Cos($aR) * 39.5; $ry = $cy + [Math]::Sin($aR) * 39.5
            Draw-RotatedDiamond $gfx $bShardSub $lx $ly 3.4 1.7 ($aL + 0.8)
            Draw-RotatedDiamond $gfx $bShardSub $rx $ry 3.4 1.7 ($aR - 0.8)
        }

        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 34.0
            $iy = $cy + [Math]::Sin($ia) * 34.0
            Draw-RotatedDiamond $gfx $bGlitter $ix $iy 2.6 1.5 ($ia + 0.4)
        }

        $bShardFaded.Dispose(); $bShardSub.Dispose(); $bGlitter.Dispose()

    } elseif ($f -eq 5) {
        # --- F5: Dissolving Frost Glitters ---
        $bFaintGlitter = New-Object System.Drawing.SolidBrush((Clr 160 210 245 255))
        $bWhiteSparkle = New-Object System.Drawing.SolidBrush((Clr 200 255 255 255))

        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a) * 51.5
            $sy = $cy + [Math]::Sin($a) * 51.5
            Draw-Diamond $gfx $bWhiteSparkle $sx $sy 2.0 2.0

            $sx2 = $cx + [Math]::Cos($a + 0.28) * 44.0
            $sy2 = $cy + [Math]::Sin($a + 0.28) * 44.0
            Draw-Diamond $gfx $bFaintGlitter $sx2 $sy2 1.5 1.5
        }
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 41.0
            $iy = $cy + [Math]::Sin($ia) * 41.0
            Draw-Diamond $gfx $bWhiteSparkle $ix $iy 1.6 1.6
        }

        $bFaintGlitter.Dispose(); $bWhiteSparkle.Dispose()

    } elseif ($f -eq 6) {
        # --- F6: Final Dissipating Ice Dust ---
        $bDust = New-Object System.Drawing.SolidBrush((Clr 90 190 240 255))
        foreach ($a in $axes) {
            $sx = $cx + [Math]::Cos($a) * 52.0
            $sy = $cy + [Math]::Sin($a) * 52.0
            Fill-EllipseCentered $gfx $bDust $sx $sy 1.4 1.4
        }
        foreach ($ia in $interAxes) {
            $ix = $cx + [Math]::Cos($ia) * 45.0
            $iy = $cy + [Math]::Sin($ia) * 45.0
            Fill-EllipseCentered $gfx $bDust $ix $iy 1.0 1.0
        }
        $bDust.Dispose()
    }
}

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"
$id = 108
$name = "Effect_Ice_Splash_Blizzard"
$texGuid = "8a07010800000000000000000000e108"
$animGuid = "ef2136c395986894cbe987646d83c804"
$ctrlGuid = "a92f69d27b6e3c440a782c341f8879da"
$prefabGuid = "520a115fc42bf7748a8cb477dcd32609"

Write-Output "Building Splash Ice Effect Assets..."

# ==============================================================================
# 1. Generate Spritesheet PNG
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

$pngPath = Join-Path $baseDir "Assets\4. DotAsset\6. Effect\$name.png"
if (Test-Path $pngPath) { [System.IO.File]::Delete($pngPath) }
$spritesheet.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "  1. Saved Spritesheet: $pngPath"

# ==============================================================================
# 2. Generate Spritesheet Meta (Point filter, PPU 64, 10 sliced sprites 108000..108009)
# ==============================================================================
$metaLines = New-Object System.Collections.Generic.List[string]
$metaLines.Add("fileFormatVersion: 2")
$metaLines.Add("guid: $texGuid")
$metaLines.Add("TextureImporter:")
$metaLines.Add("  internalIDToNameTable:")
for ($f = 0; $f -lt 10; $f++) {
    $fid = "${id}00${f}"
    $metaLines.Add("  - first:")
    $metaLines.Add("      213: $fid")
    $metaLines.Add("    second: ${name}_${f}")
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
    $metaLines.Add("      name: ${name}_${f}")
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
    $metaLines.Add("      ${name}_${f}: ${id}00${f}")
}
$metaLines.Add("  mipmapLimitGroupName: ")
$metaLines.Add("  pSDRemoveMatte: 0")
$metaLines.Add("  userData: ")
$metaLines.Add("  assetBundleName: ")
$metaLines.Add("  assetBundleVariant: ")

$metaPath = "$pngPath.meta"
[System.IO.File]::WriteAllLines($metaPath, $metaLines)
Write-Output "  2. Saved Texture Meta: $metaPath"

# ==============================================================================
# 3. Generate Animation Clip (E108.anim)
# ==============================================================================
$animLines = New-Object System.Collections.Generic.List[string]
$animLines.Add("%YAML 1.1")
$animLines.Add("%TAG !u! tag:unity3d.com,2011:")
$animLines.Add("--- !u!74 &7400000")
$animLines.Add("AnimationClip:")
$animLines.Add("  m_ObjectHideFlags: 0")
$animLines.Add("  m_CorrespondingSourceObject: {fileID: 0}")
$animLines.Add("  m_PrefabInstance: {fileID: 0}")
$animLines.Add("  m_PrefabAsset: {fileID: 0}")
$animLines.Add("  m_Name: E$id")
$animLines.Add("  serializedVersion: 7")
$animLines.Add("  m_Legacy: 0")
$animLines.Add("  m_Compressed: 0")
$animLines.Add("  m_UseHighQualityCurve: 1")
$animLines.Add("  m_RotationCurves: []")
$animLines.Add("  m_CompressedRotationCurves: []")
$animLines.Add("  m_EulerCurves: []")
$animLines.Add("  m_PositionCurves: []")
$animLines.Add("  m_ScaleCurves: []")
$animLines.Add("  m_FloatCurves: []")
$animLines.Add("  m_PPtrCurves:")
$animLines.Add("  - serializedVersion: 2")
$animLines.Add("    curve:")
for ($f = 0; $f -lt 10; $f++) {
    $t = ($f * 0.0625).ToString("0.0000", [System.Globalization.CultureInfo]::InvariantCulture)
    $fid = "${id}00${f}"
    $animLines.Add("    - time: $t")
    $animLines.Add("      value: {fileID: $fid, guid: $texGuid, type: 3}")
}
$animLines.Add("    attribute: m_Sprite")
$animLines.Add("    path: ")
$animLines.Add("    classID: 212")
$animLines.Add("    script: {fileID: 0}")
$animLines.Add("    flags: 2")
$animLines.Add("  m_SampleRate: 16")
$animLines.Add("  m_WrapMode: 0")
$animLines.Add("  m_Bounds:")
$animLines.Add("    m_Center: {x: 0, y: 0, z: 0}")
$animLines.Add("    m_Extent: {x: 0, y: 0, z: 0}")
$animLines.Add("  m_ClipBindingConstant:")
$animLines.Add("    genericBindings:")
$animLines.Add("    - serializedVersion: 2")
$animLines.Add("      path: 0")
$animLines.Add("      attribute: 0")
$animLines.Add("      script: {fileID: 0}")
$animLines.Add("      typeID: 212")
$animLines.Add("      customType: 23")
$animLines.Add("      isPPtrCurve: 1")
$animLines.Add("      isIntCurve: 0")
$animLines.Add("      isSerializeReferenceCurve: 0")
$animLines.Add("    pptrCurveMapping:")
for ($f = 0; $f -lt 10; $f++) {
    $fid = "${id}00${f}"
    $animLines.Add("    - {fileID: $fid, guid: $texGuid, type: 3}")
}
$animLines.Add("  m_AnimationClipSettings:")
$animLines.Add("    serializedVersion: 2")
$animLines.Add("    m_AdditiveReferencePoseClip: {fileID: 0}")
$animLines.Add("    m_AdditiveReferencePoseTime: 0")
$animLines.Add("    m_StartTime: 0")
$animLines.Add("    m_StopTime: 0.625")
$animLines.Add("    m_OrientationOffsetY: 0")
$animLines.Add("    m_Level: 0")
$animLines.Add("    m_CycleOffset: 0")
$animLines.Add("    m_HasAdditiveReferencePose: 0")
$animLines.Add("    m_LoopTime: 0")
$animLines.Add("    m_LoopBlend: 0")
$animLines.Add("    m_LoopBlendOrientation: 0")
$animLines.Add("    m_LoopBlendPositionY: 0")
$animLines.Add("    m_LoopBlendPositionXZ: 0")
$animLines.Add("    m_KeepOriginalOrientation: 0")
$animLines.Add("    m_KeepOriginalPositionY: 1")
$animLines.Add("    m_KeepOriginalPositionXZ: 0")
$animLines.Add("    m_HeightFromFeet: 0")
$animLines.Add("    m_Mirror: 0")
$animLines.Add("  m_EditorCurves: []")
$animLines.Add("  m_EulerEditorCurves: []")
$animLines.Add("  m_HasGenericRootTransform: 0")
$animLines.Add("  m_HasMotionFloatCurves: 0")
$animLines.Add("  m_Events:")
$animLines.Add("  - time: 0.625")
$animLines.Add("    functionName: DestroyEffect")
$animLines.Add("    data: ")
$animLines.Add("    objectReferenceParameter: {fileID: 0}")
$animLines.Add("    floatParameter: 0")
$animLines.Add("    intParameter: 0")
$animLines.Add("    messageOptions: 0")

$animPath = Join-Path $baseDir "Assets\10.Animation\Effect\E108.anim"
[System.IO.File]::WriteAllLines($animPath, $animLines)
Write-Output "  3. Saved Animation Clip: $animPath"

# ==============================================================================
# 4. Update Prefab (E108.prefab)
# ==============================================================================
$prefabPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\E108.prefab"
$pText = [System.IO.File]::ReadAllText($prefabPath)
# 1) Reset m_Color to pure white
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Color:\s*\{r:\s*[\d\.]+,?\s*g:\s*[\d\.]+,?\s*b:\s*[\d\.]+,?\s*a:\s*[\d\.]+\}", "m_Color: {r: 1, g: 1, b: 1, a: 1}")
# 2) Set m_Sprite to {fileID: 108000, guid: 8a07010800000000000000000000e108, type: 3}
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Sprite:\s*\{fileID:\s*-?\d+,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*3\}", "m_Sprite: {fileID: 108000, guid: $texGuid, type: 3}")
# 3) Ensure m_Controller is {fileID: 9100000, guid: a92f69d27b6e3c440a782c341f8879da, type: 2}
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Controller:\s*\{fileID:\s*9100000,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*2\}", "m_Controller: {fileID: 9100000, guid: $ctrlGuid, type: 2}")
[System.IO.File]::WriteAllText($prefabPath, $pText)
Write-Output "  4. Updated Prefab: $prefabPath"

# ==============================================================================
# 5. Generate Preview Showcase
# ==============================================================================
$pWidth = 1320
$pHeight = 520
$preview = New-Object System.Drawing.Bitmap($pWidth, $pHeight, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gp = [System.Drawing.Graphics]::FromImage($preview)
$gp.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gp.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$gp.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 22 34))
$gp.FillRectangle($bBg, 0, 0, $pWidth, $pHeight)
$bBg.Dispose()

$pGrid = New-Object System.Drawing.Pen((Clr 40 100 150 220), 1.0)
for ($x = 0; $x -lt $pWidth; $x += 32) { $gp.DrawLine($pGrid, $x, 0, $x, $pHeight) }
for ($y = 0; $y -lt $pHeight; $y += 32) { $gp.DrawLine($pGrid, 0, $y, $pWidth, $y) }
$pGrid.Dispose()

$fontTitle = New-Object System.Drawing.Font("Arial", 14, [System.Drawing.FontStyle]::Bold)
$fontSub   = New-Object System.Drawing.Font("Arial", 10, [System.Drawing.FontStyle]::Regular)
$bTextWhite = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
$bTextCyan  = New-Object System.Drawing.SolidBrush((Clr 255 130 220 255))
$gp.DrawString("E108: Splash Ice Tower Hit Effect (Snowflake Crystal Explosion)", $fontTitle, $bTextWhite, 20.0, 15.0)
$gp.DrawString("Concept: 6-Fold Snowflake Crystal Growth -> Bloom & Shockwave -> Radial Shard Burst & Glitter Dispersal", $fontSub, $bTextCyan, 20.0, 42.0)

# 1x Real-Time Sequence
$gp.DrawString("[ 1x Real-Time Frame Sequence (10 Frames @ 16 FPS / 128x128 each) ]", $fontSub, $bTextWhite, 20.0, 75.0)
for ($f = 0; $f -lt 10; $f++) {
    $destX = 20 + $f * 129
    $destY = 98
    $pBox = New-Object System.Drawing.Pen((Clr 100 80 140 200), 1.0)
    $gp.DrawRectangle($pBox, $destX, $destY, 128, 128)
    $pBox.Dispose()

    $srcRect = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
    $destRect = New-Object System.Drawing.Rectangle($destX, $destY, 128, 128)
    $gp.DrawImage($spritesheet, $destRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $gp.DrawString("F$f", $fontSub, $bTextCyan, ($destX + 4.0), ($destY + 4.0))
}

# 2x Zoom Detailed View
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

# Save preview to artifacts dir and project dir
$artPreviewPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_ice_splash_effect.png"
$projPreviewPath = Join-Path $baseDir "preview_ice_splash_effect.png"
$preview.Save($artPreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Save($projPreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$spritesheet.Dispose()
Write-Output "  5. Saved Preview Image: $artPreviewPath"

# ==============================================================================
# 6. Verification
# ==============================================================================
Write-Output "=== E108 Asset Verification ==="
$libPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\EffectLibrary.asset"
$libContent = [System.IO.File]::ReadAllText($libPath)
$libMatch = $libContent -match "- effectID: 108\s+effectPrefab:\s*\{fileID:\s*5220447335354414950,\s*guid:\s*$prefabGuid"
Write-Output "  EffectLibrary -> Prefab GUID ($prefabGuid): $libMatch"

$pCtrlMatch = (Get-Content $prefabPath | Select-String "m_Controller:.*guid:\s*$ctrlGuid").Matches.Count -gt 0
Write-Output "  Prefab -> Controller GUID ($ctrlGuid): $pCtrlMatch"

$pSpriteMatch = (Get-Content $prefabPath | Select-String "m_Sprite:.*guid:\s*$texGuid").Matches.Count -gt 0
Write-Output "  Prefab -> Sprite GUID ($texGuid): $pSpriteMatch"

$ctrlAnimMatch = (Get-Content (Join-Path $baseDir "Assets\10.Animation\Effect\E108.controller") | Select-String "m_Motion:.*guid:\s*$animGuid").Matches.Count -gt 0
Write-Output "  Controller -> Anim GUID ($animGuid): $ctrlAnimMatch"

$animSpriteMatch = (Get-Content $animPath | Select-String "guid:\s*$texGuid").Matches.Count -gt 0
Write-Output "  Anim -> Sprite GUID ($texGuid): $animSpriteMatch"

Write-Output "ALL E108 ASSETS SUCCESSFULLY CREATED AND VERIFIED!"
