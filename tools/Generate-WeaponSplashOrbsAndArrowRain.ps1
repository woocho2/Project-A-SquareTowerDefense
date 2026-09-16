# ==============================================================================
# Generate-WeaponSplashOrbsAndArrowRain.ps1
# 1. 6 Weapon Splash Tower Gray Symbol Orbs (101~106, 768x128, 6F)
# 2. Splash Bow Tower (102) Arrow Rain Effect (1536x128, 12F)
# 3. Two Showcase Previews (preview_weapon_gray_orbs.png, preview_arrow_rain_effect.png)
# ==============================================================================

Add-Type -AssemblyName System.Drawing

$projectRoot = "d:/MyGitHub/ProjectA/Project-A-SquareTowerDefense"
$artifactDir = "C:/Users/user/.gemini/antigravity/brain/dc024a25-2f90-4a0d-912c-bd52cd35c0ed"

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $ca = [Math]::Max(0, [Math]::Min(255, $a))
    $cr = [Math]::Max(0, [Math]::Min(255, $r))
    $cg = [Math]::Max(0, [Math]::Min(255, $g))
    $cb = [Math]::Max(0, [Math]::Min(255, $b))
    return [System.Drawing.Color]::FromArgb($ca, $cr, $cg, $cb)
}

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

# ==============================================================================
# SECTION 1: Weapon Symbol Drawing Functions (Center cx, cy, Scale scale)
# ==============================================================================

# 1. Sword Symbol
function Draw-Symbol-Sword($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $bladePts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $bladePts.Add((Pt $cx ($cy - 18.0 * $scale)))
    $bladePts.Add((Pt ($cx + 3.0 * $scale) ($cy - 12.0 * $scale)))
    $bladePts.Add((Pt ($cx + 2.5 * $scale) ($cy + 2.0 * $scale)))
    $bladePts.Add((Pt ($cx - 2.5 * $scale) ($cy + 2.0 * $scale)))
    $bladePts.Add((Pt ($cx - 3.0 * $scale) ($cy - 12.0 * $scale)))
    $gfx.FillPolygon($brush, $bladePts.ToArray())
    
    $guardPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $guardPts.Add((Pt ($cx - 9.0 * $scale) ($cy + 2.0 * $scale)))
    $guardPts.Add((Pt ($cx + 9.0 * $scale) ($cy + 2.0 * $scale)))
    $guardPts.Add((Pt ($cx + 8.0 * $scale) ($cy + 5.0 * $scale)))
    $guardPts.Add((Pt ($cx - 8.0 * $scale) ($cy + 5.0 * $scale)))
    $gfx.FillPolygon($brush, $guardPts.ToArray())
    
    $gfx.FillRectangle($brush, ($cx - 1.5 * $scale), ($cy + 5.0 * $scale), (3.0 * $scale), (8.0 * $scale))
    Fill-EllipseCentered $gfx $brush $cx ($cy + 14.5 * $scale) (2.5 * $scale) (2.5 * $scale)

    $brush.Dispose()
}

# 2. Bow / Arrow Symbol
function Draw-Symbol-BowArrow($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $tipPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $tipPts.Add((Pt $cx ($cy - 18.0 * $scale)))
    $tipPts.Add((Pt ($cx + 5.5 * $scale) ($cy - 7.0 * $scale)))
    $tipPts.Add((Pt ($cx + 2.0 * $scale) ($cy - 8.5 * $scale)))
    $tipPts.Add((Pt ($cx - 2.0 * $scale) ($cy - 8.5 * $scale)))
    $tipPts.Add((Pt ($cx - 5.5 * $scale) ($cy - 7.0 * $scale)))
    $gfx.FillPolygon($brush, $tipPts.ToArray())
    
    $gfx.FillRectangle($brush, ($cx - 1.5 * $scale), ($cy - 8.0 * $scale), (3.0 * $scale), (20.0 * $scale))
    
    $fLeft = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $fLeft.Add((Pt ($cx - 1.5 * $scale) ($cy + 6.0 * $scale)))
    $fLeft.Add((Pt ($cx - 7.0 * $scale) ($cy + 13.0 * $scale)))
    $fLeft.Add((Pt ($cx - 7.0 * $scale) ($cy + 16.0 * $scale)))
    $fLeft.Add((Pt ($cx - 1.5 * $scale) ($cy + 12.0 * $scale)))
    $gfx.FillPolygon($brush, $fLeft.ToArray())

    $fRight = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $fRight.Add((Pt ($cx + 1.5 * $scale) ($cy + 6.0 * $scale)))
    $fRight.Add((Pt ($cx + 7.0 * $scale) ($cy + 13.0 * $scale)))
    $fRight.Add((Pt ($cx + 7.0 * $scale) ($cy + 16.0 * $scale)))
    $fRight.Add((Pt ($cx + 1.5 * $scale) ($cy + 12.0 * $scale)))
    $gfx.FillPolygon($brush, $fRight.ToArray())

    $brush.Dispose()
}

# 3. Shield Symbol
function Draw-Symbol-Shield($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $shieldPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $shieldPts.Add((Pt ($cx - 11.0 * $scale) ($cy - 14.0 * $scale)))
    $shieldPts.Add((Pt ($cx + 11.0 * $scale) ($cy - 14.0 * $scale)))
    $shieldPts.Add((Pt ($cx + 12.0 * $scale) ($cy - 4.0 * $scale)))
    $shieldPts.Add((Pt ($cx + 8.0 * $scale) ($cy + 7.0 * $scale)))
    $shieldPts.Add((Pt $cx ($cy + 16.0 * $scale)))
    $shieldPts.Add((Pt ($cx - 8.0 * $scale) ($cy + 7.0 * $scale)))
    $shieldPts.Add((Pt ($cx - 12.0 * $scale) ($cy - 4.0 * $scale)))
    $gfx.FillPolygon($brush, $shieldPts.ToArray())
    
    $innerBrush = New-Object System.Drawing.SolidBrush((Clr 200 80 90 105))
    $gfx.FillRectangle($innerBrush, ($cx - 1.5 * $scale), ($cy - 11.0 * $scale), (3.0 * $scale), (18.0 * $scale))
    $gfx.FillRectangle($innerBrush, ($cx - 7.0 * $scale), ($cy - 5.0 * $scale), (14.0 * $scale), (3.0 * $scale))
    $innerBrush.Dispose()

    $brush.Dispose()
}

# 4. Spear Symbol
function Draw-Symbol-Spear($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $spearTip = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $spearTip.Add((Pt $cx ($cy - 19.0 * $scale)))
    $spearTip.Add((Pt ($cx + 4.5 * $scale) ($cy - 9.0 * $scale)))
    $spearTip.Add((Pt ($cx + 2.0 * $scale) ($cy - 2.0 * $scale)))
    $spearTip.Add((Pt ($cx - 2.0 * $scale) ($cy - 2.0 * $scale)))
    $spearTip.Add((Pt ($cx - 4.5 * $scale) ($cy - 9.0 * $scale)))
    $gfx.FillPolygon($brush, $spearTip.ToArray())
    
    $gfx.FillRectangle($brush, ($cx - 5.5 * $scale), ($cy - 2.0 * $scale), (11.0 * $scale), (2.5 * $scale))
    $gfx.FillRectangle($brush, ($cx - 1.5 * $scale), ($cy + 0.5 * $scale), (3.0 * $scale), (16.0 * $scale))

    $brush.Dispose()
}

# 5. Axe Symbol
function Draw-Symbol-Axe($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $gfx.FillRectangle($brush, ($cx - 1.5 * $scale), ($cy - 14.0 * $scale), (3.0 * $scale), (28.0 * $scale))
    
    $bladePts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $bladePts.Add((Pt ($cx + 1.5 * $scale) ($cy - 11.0 * $scale)))
    $bladePts.Add((Pt ($cx + 11.0 * $scale) ($cy - 15.0 * $scale)))
    $bladePts.Add((Pt ($cx + 14.0 * $scale) ($cy - 4.0 * $scale)))
    $bladePts.Add((Pt ($cx + 11.0 * $scale) ($cy + 6.0 * $scale)))
    $bladePts.Add((Pt ($cx + 1.5 * $scale) ($cy + 2.0 * $scale)))
    $bladePts.Add((Pt ($cx + 4.0 * $scale) ($cy - 4.0 * $scale)))
    $gfx.FillPolygon($brush, $bladePts.ToArray())
    
    $backPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $backPts.Add((Pt ($cx - 1.5 * $scale) ($cy - 9.0 * $scale)))
    $backPts.Add((Pt ($cx - 8.0 * $scale) ($cy - 4.0 * $scale)))
    $backPts.Add((Pt ($cx - 1.5 * $scale) ($cy + 1.0 * $scale)))
    $gfx.FillPolygon($brush, $backPts.ToArray())

    $brush.Dispose()
}

# 6. Hammer Symbol
function Draw-Symbol-Hammer($gfx, [float]$cx, [float]$cy, [float]$scale, $color) {
    $brush = New-Object System.Drawing.SolidBrush($color)
    
    $headPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $headPts.Add((Pt ($cx - 12.0 * $scale) ($cy - 14.0 * $scale)))
    $headPts.Add((Pt ($cx + 12.0 * $scale) ($cy - 14.0 * $scale)))
    $headPts.Add((Pt ($cx + 13.5 * $scale) ($cy - 6.0 * $scale)))
    $headPts.Add((Pt ($cx + 12.0 * $scale) ($cy + 1.0 * $scale)))
    $headPts.Add((Pt ($cx - 12.0 * $scale) ($cy + 1.0 * $scale)))
    $headPts.Add((Pt ($cx - 13.5 * $scale) ($cy - 6.0 * $scale)))
    $gfx.FillPolygon($brush, $headPts.ToArray())
    
    $gfx.FillRectangle($brush, ($cx - 2.0 * $scale), ($cy + 1.0 * $scale), (4.0 * $scale), (15.0 * $scale))
    Fill-EllipseCentered $gfx $brush $cx ($cy + 16.5 * $scale) (3.0 * $scale) (2.5 * $scale)

    $brush.Dispose()
}

# ==============================================================================
# SECTION 2: 6 Weapon Gray Orbs Spritesheets (768x128, 6F)
# ==============================================================================

$weapons = @(
    @{ ID = 101; Name = "Sword";  File = "Projectile_Meteor_Sword.png";  Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-Sword $g $x $y $s $c } },
    @{ ID = 102; Name = "Bow";    File = "Projectile_Meteor_Bow.png";    Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-BowArrow $g $x $y $s $c } },
    @{ ID = 103; Name = "Shield"; File = "Projectile_Meteor_Shield.png"; Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-Shield $g $x $y $s $c } },
    @{ ID = 104; Name = "Spear";  File = "Projectile_Meteor_Spear.png";  Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-Spear $g $x $y $s $c } },
    @{ ID = 105; Name = "Axe";    File = "Projectile_Meteor_Axe.png";    Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-Axe $g $x $y $s $c } },
    @{ ID = 106; Name = "Hammer"; File = "Projectile_Meteor_Hammer.png"; Draw = { param($g,$x,$y,$s,$c) Draw-Symbol-Hammer $g $x $y $s $c } }
)

$outDir = "$projectRoot/Assets/4. DotAsset/3. Projectile"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

foreach ($wp in $weapons) {
    $bmp = New-Object System.Drawing.Bitmap 768, 128, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.Clear([System.Drawing.Color]::Transparent)

    for ($f = 0; $f -lt 6; $f++) {
        $cx = $f * 128.0 + 64.0
        $cy = 64.0
        
        $pulse = [Math]::Sin(($f / 6.0) * [Math]::PI * 2.0)
        $haloR = 40.0 + $pulse * 3.0
        $bodyR = 27.0 + $pulse * 1.5
        $coreR = 17.0 + $pulse * 1.0

        # 1. Outer Silver Halo
        $haloBrush = New-Object System.Drawing.SolidBrush((Clr (60 + [int]($pulse * 15)) 170 185 205))
        Fill-EllipseCentered $gfx $haloBrush $cx $cy $haloR $haloR
        $haloBrush.Dispose()

        $haloRingPen = New-Object System.Drawing.Pen((Clr (110 + [int]($pulse * 20)) 195 210 230), 2.0)
        Draw-EllipseCentered $gfx $haloRingPen $cx $cy ($haloR - 4.0) ($haloR - 4.0)
        $haloRingPen.Dispose()

        # 2. Metallic Silver Orb Body
        $bodyBrush = New-Object System.Drawing.SolidBrush((Clr 245 105 118 135))
        Fill-EllipseCentered $gfx $bodyBrush $cx $cy $bodyR $bodyR
        $bodyBrush.Dispose()

        $bodyRimBrush = New-Object System.Drawing.SolidBrush((Clr 250 145 160 180))
        Fill-EllipseCentered $gfx $bodyRimBrush ($cx - 2.0) ($cy - 2.0) ($bodyR - 3.0) ($bodyR - 3.0)
        $bodyRimBrush.Dispose()

        # 3. Inner White-Hot Core
        $coreBrush = New-Object System.Drawing.SolidBrush((Clr 240 215 228 245))
        Fill-EllipseCentered $gfx $coreBrush ($cx - 1.0) ($cy - 1.0) $coreR $coreR
        $coreBrush.Dispose()

        # 4. Orbiting Silver Sparkle Motes
        for ($m = 0; $m -lt 3; $m++) {
            $ang = (($f / 6.0) * [Math]::PI * 2.0) + ($m * [Math]::PI * 2.0 / 3.0)
            $moteDist = 33.0 + ($m * 2.5)
            $mx = $cx + [Math]::Cos($ang) * $moteDist
            $my = $cy + [Math]::Sin($ang) * ($moteDist * 0.7)
            $moteBrush = New-Object System.Drawing.SolidBrush((Clr 220 240 248 255))
            Fill-EllipseCentered $gfx $moteBrush $mx $my 3.0 3.0
            $moteBrush.Dispose()
        }

        # 5. Engraved Weapon Symbol
        $symGlowColor = Clr 180 50 60 75
        & $wp.Draw $gfx ($cx + 1.0) ($cy + 1.5) (0.95 + $pulse * 0.05) $symGlowColor
        
        $symColor = Clr 255 255 255 255
        & $wp.Draw $gfx $cx $cy (0.95 + $pulse * 0.05) $symColor

        # 6. Glint Highlight
        $glintBrush = New-Object System.Drawing.SolidBrush((Clr (180 + [int]($pulse * 40)) 255 255 255))
        Fill-EllipseCentered $gfx $glintBrush ($cx - 10.0) ($cy - 10.0) 6.0 4.0
        $glintBrush.Dispose()
    }

    $gfx.Dispose()
    $savePath = [System.IO.Path]::Combine($outDir, $wp.File)
    $bmp.Save($savePath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "[Weapon Orbs] Generated $($wp.File) successfully."
}

# ==============================================================================
# SECTION 3: Splash Bow Tower (102) Arrow Rain Effect (1536x128, 12F)
# ==============================================================================

$rainFile = "$projectRoot/Assets/4. DotAsset/6. Effect/Effect_Arrow_Rain.png"
$rainBmp = New-Object System.Drawing.Bitmap 1536, 128, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$rainGfx = [System.Drawing.Graphics]::FromImage($rainBmp)
$rainGfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$rainGfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$rainGfx.Clear([System.Drawing.Color]::Transparent)

$arrowProfiles = @(
    @{ OffsetX = -28.0; FinalY = 82.0; AngleDeg = -75.0; Scale = 0.85; Delay = 0 },
    @{ OffsetX = -18.0; FinalY = 70.0; AngleDeg = -72.0; Scale = 0.95; Delay = 1 },
    @{ OffsetX =  -8.0; FinalY = 88.0; AngleDeg = -70.0; Scale = 1.05; Delay = 0 },
    @{ OffsetX =   2.0; FinalY = 74.0; AngleDeg = -68.0; Scale = 1.10; Delay = 1 },
    @{ OffsetX =  12.0; FinalY = 84.0; AngleDeg = -66.0; Scale = 1.00; Delay = 0 },
    @{ OffsetX =  22.0; FinalY = 68.0; AngleDeg = -64.0; Scale = 0.90; Delay = 2 },
    @{ OffsetX =  32.0; FinalY = 80.0; AngleDeg = -62.0; Scale = 0.80; Delay = 1 },
    @{ OffsetX =   0.0; FinalY = 92.0; AngleDeg = -70.0; Scale = 1.15; Delay = 0 }
)

function Draw-FallingArrow($gfx, [float]$x, [float]$y, [float]$scale, [float]$angleDeg, [int]$alpha, [bool]$showTrail) {
    if ($alpha -le 0) { return }
    $rad = $angleDeg * [Math]::PI / 180.0
    $cos = [Math]::Cos($rad); $sin = [Math]::Sin($rad)

    if ($showTrail) {
        $trailLen = 32.0 * $scale
        $tx = $x - $cos * $trailLen
        $ty = $y - $sin * $trailLen
        $trailPen = New-Object System.Drawing.Pen((Clr ([int]($alpha * 0.45)) 200 230 255), (3.0 * $scale))
        $gfx.DrawLine($trailPen, $tx, $ty, $x, $y)
        $trailPen.Dispose()
    }

    $arrowBrush = New-Object System.Drawing.SolidBrush((Clr $alpha 245 250 255))
    $arrowShaftPen = New-Object System.Drawing.Pen((Clr $alpha 170 185 200), (2.0 * $scale))

    $shaftLen = 22.0 * $scale
    $bx = $x - $cos * $shaftLen
    $by = $y - $sin * $shaftLen
    $gfx.DrawLine($arrowShaftPen, $bx, $by, $x, $y)

    $tipLen = 8.0 * $scale
    $tipW = 4.0 * $scale
    $nx = -$sin; $ny = $cos
    
    $tipPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $tipPts.Add((Pt ($x + $cos * $tipLen) ($y + $sin * $tipLen)))
    $tipPts.Add((Pt ($x + $nx * $tipW) ($y + $ny * $tipW)))
    $tipPts.Add((Pt ($x - $nx * $tipW) ($y - $ny * $tipW)))
    $gfx.FillPolygon($arrowBrush, $tipPts.ToArray())

    $fletchBrush = New-Object System.Drawing.SolidBrush((Clr $alpha 210 230 255))
    $fx = $bx + $nx * (3.5 * $scale)
    $fy = $by + $ny * (3.5 * $scale)
    $gfx.FillEllipse($fletchBrush, ($fx - 2.0), ($fy - 2.0), 4.0, 4.0)
    $fletchBrush.Dispose()

    $arrowBrush.Dispose(); $arrowShaftPen.Dispose()
}

for ($f = 0; $f -lt 12; $f++) {
    $cx = $f * 128.0 + 64.0
    $cy = 64.0

    if ($f -eq 0) {
        $flash = New-Object System.Drawing.SolidBrush((Clr 230 255 255 255))
        Fill-EllipseCentered $rainGfx $flash $cx 70.0 22.0 16.0
        $flash.Dispose()

        $ring = New-Object System.Drawing.Pen((Clr 190 180 230 255), 3.0)
        Draw-EllipseCentered $rainGfx $ring $cx 70.0 32.0 20.0
        $ring.Dispose()

        $beam = New-Object System.Drawing.Pen((Clr 200 230 245 255), 4.0)
        $rainGfx.DrawLine($beam, $cx, 70.0, $cx, 10.0)
        $beam.Dispose()
    }
    elseif ($f -eq 1) {
        $ring = New-Object System.Drawing.Pen((Clr 140 180 220 250), 2.5)
        Draw-EllipseCentered $rainGfx $ring $cx 70.0 46.0 26.0
        $ring.Dispose()

        $cloudBrush = New-Object System.Drawing.SolidBrush((Clr 120 200 230 255))
        Fill-EllipseCentered $rainGfx $cloudBrush $cx 16.0 44.0 12.0
        $cloudBrush.Dispose()
    }
    elseif ($f -eq 2) {
        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX - 20.0
            $ay = 18.0 + ($ap.Delay * 6.0)
            Draw-FallingArrow $rainGfx $ax $ay ($ap.Scale * 0.9) $ap.AngleDeg 240 $true
        }
    }
    elseif ($f -eq 3) {
        foreach ($ap in $arrowProfiles) {
            $prog = 0.55 + ($ap.Delay * 0.1)
            $ax = $cx + $ap.OffsetX - (1.0 - $prog) * 20.0
            $ay = 18.0 + ($ap.FinalY - 18.0) * $prog
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 255 $true
        }
    }
    elseif ($f -eq 4) {
        $shock = New-Object System.Drawing.Pen((Clr 200 255 255 255), 3.5)
        Draw-EllipseCentered $rainGfx $shock $cx 78.0 42.0 18.0
        $shock.Dispose()

        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 255 $false
        }
    }
    elseif ($f -eq 5) {
        $shock = New-Object System.Drawing.Pen((Clr 240 220 240 255), 4.0)
        Draw-EllipseCentered $rainGfx $shock $cx 78.0 56.0 24.0
        $shock.Dispose()

        $dust = New-Object System.Drawing.SolidBrush((Clr 140 180 195 210))
        Fill-EllipseCentered $rainGfx $dust ($cx - 24.0) 76.0 14.0 8.0
        Fill-EllipseCentered $rainGfx $dust ($cx + 24.0) 76.0 14.0 8.0
        $dust.Dispose()

        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 255 $false
        }
    }
    elseif ($f -eq 6) {
        $shock = New-Object System.Drawing.Pen((Clr 110 180 210 240), 2.0)
        Draw-EllipseCentered $rainGfx $shock $cx 78.0 62.0 26.0
        $shock.Dispose()

        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX + ([Math]::Sin($f * 3.0) * 1.0)
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 230 $false
        }
    }
    elseif ($f -eq 7) {
        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 180 $false
            
            $mote = New-Object System.Drawing.SolidBrush((Clr 200 255 255 255))
            Fill-EllipseCentered $rainGfx $mote ($ax - 2.0) ($ay - 14.0) 3.0 3.0
            $mote.Dispose()
        }
    }
    elseif ($f -eq 8) {
        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 110 $false
        }
    }
    elseif ($f -eq 9) {
        foreach ($ap in $arrowProfiles) {
            $ax = $cx + $ap.OffsetX
            $ay = $ap.FinalY
            Draw-FallingArrow $rainGfx $ax $ay $ap.Scale $ap.AngleDeg 45 $false
        }
    }
}

$rainGfx.Dispose()
$rainBmp.Save($rainFile, [System.Drawing.Imaging.ImageFormat]::Png)
$rainBmp.Dispose()
Write-Host "[Arrow Rain Effect] Generated Effect_Arrow_Rain.png successfully."

# ==============================================================================
# SECTION 4: Showcase Previews
# ==============================================================================

# 1. preview_weapon_gray_orbs.png
$scW = 1080; $scH = 420
$scBmp = New-Object System.Drawing.Bitmap $scW, $scH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$scGfx = [System.Drawing.Graphics]::FromImage($scBmp)
$scGfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$scGfx.Clear((Clr 255 18 20 28))

$titleFont = New-Object System.Drawing.Font ("Segoe UI", [float]18, [System.Drawing.FontStyle]::Bold)
$subFont   = New-Object System.Drawing.Font ("Segoe UI", [float]11, [System.Drawing.FontStyle]::Regular)
$labelFont = New-Object System.Drawing.Font ("Segoe UI", [float]13, [System.Drawing.FontStyle]::Bold)
$descFont  = New-Object System.Drawing.Font ("Segoe UI", [float]10, [System.Drawing.FontStyle]::Regular)

$titleBrush = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$subBrush   = New-Object System.Drawing.SolidBrush((Clr 255 160 175 195))
$cardBrush  = New-Object System.Drawing.SolidBrush((Clr 255 26 30 42))
$cardBorder = New-Object System.Drawing.Pen((Clr 255 50 58 78), 1.5)

$scGfx.DrawString("Weapon Splash Tower Gray Symbol Orbs Lineup (101-106)", $titleFont, $titleBrush, 30.0, 22.0)
$subText = "6-Frame Animated Metallic Silver Orbs with Engraved Weapon Icons"
$scGfx.DrawString($subText, $subFont, $subBrush, 32.0, 56.0)

$cardW = 156; $cardH = 300
for ($i = 0; $i -lt 6; $i++) {
    $wp = $weapons[$i]
    $x = 30.0 + ($i * 172.0)
    $y = 95.0

    $scGfx.FillRectangle($cardBrush, $x, $y, [float]$cardW, [float]$cardH)
    $scGfx.DrawRectangle($cardBorder, $x, $y, [float]$cardW, [float]$cardH)

    $filePath = [System.IO.Path]::Combine($outDir, $wp.File)
    $orbImg = [System.Drawing.Image]::FromFile($filePath)
    $srcRect = New-Object System.Drawing.Rectangle (2 * 128), 0, 128, 128
    $dstRect = New-Object System.Drawing.Rectangle ([int]($x + 14)), ([int]($y + 20)), 128, 128
    $scGfx.DrawImage($orbImg, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
    $orbImg.Dispose()

    $idBrush = New-Object System.Drawing.SolidBrush((Clr 255 100 200 255))
    $scGfx.DrawString("ID: " + $wp.ID, $descFont, $idBrush, [float]($x + 16), [float]($y + 165))
    $idBrush.Dispose()

    $nameBrush = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
    $scGfx.DrawString($wp.Name, $labelFont, $nameBrush, [float]($x + 16), [float]($y + 185))
    $nameBrush.Dispose()

    $descText = switch ($wp.ID) {
        101 { "Splash Sword`nEngraved Blade" }
        102 { "Splash Bow`nEngraved Arrow" }
        103 { "Splash Shield`nEngraved Shield" }
        104 { "Splash Spear`nEngraved Spear" }
        105 { "Splash Axe`nEngraved Battleaxe" }
        106 { "Splash Hammer`nEngraved Warhammer" }
    }
    $scGfx.DrawString($descText, $descFont, $subBrush, [float]($x + 16), [float]($y + 215))
}

$titleFont.Dispose(); $subFont.Dispose(); $labelFont.Dispose(); $descFont.Dispose()
$titleBrush.Dispose(); $subBrush.Dispose(); $cardBrush.Dispose(); $cardBorder.Dispose()
$scGfx.Dispose()

$scPath1 = "$projectRoot/preview_weapon_gray_orbs.png"
$scBmp.Save($scPath1, [System.Drawing.Imaging.ImageFormat]::Png)
$scBmp.Save("$artifactDir/preview_weapon_gray_orbs.png", [System.Drawing.Imaging.ImageFormat]::Png)
$scBmp.Dispose()
Write-Host "[Showcase] Generated preview_weapon_gray_orbs.png successfully."

# 2. preview_arrow_rain_effect.png
$arW = 1200; $arH = 460
$arBmp = New-Object System.Drawing.Bitmap $arW, $arH, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$arGfx = [System.Drawing.Graphics]::FromImage($arBmp)
$arGfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$arGfx.Clear((Clr 255 16 18 25))

$arTitleFont = New-Object System.Drawing.Font ("Segoe UI", [float]18, [System.Drawing.FontStyle]::Bold)
$arSubFont   = New-Object System.Drawing.Font ("Segoe UI", [float]11, [System.Drawing.FontStyle]::Regular)
$arFFont     = New-Object System.Drawing.Font ("Segoe UI", [float]9, [System.Drawing.FontStyle]::Bold)
$arFDescFont = New-Object System.Drawing.Font ("Segoe UI", [float]8, [System.Drawing.FontStyle]::Regular)

$titleBrush = New-Object System.Drawing.SolidBrush((Clr 255 240 248 255))
$subBrush   = New-Object System.Drawing.SolidBrush((Clr 255 160 180 205))
$cellBorder = New-Object System.Drawing.Pen((Clr 255 45 55 75), 1.0)
$tileBrush  = New-Object System.Drawing.SolidBrush((Clr 255 24 28 38))

$arGfx.DrawString("Splash Bow Tower (102) Arrow Rain Explosion Effect (12-Frame)", $arTitleFont, $titleBrush, 30.0, 20.0)
$flowText = "Orb Burst (F0-F1) -> Arrow Shower (F2-F3) -> Ground Impact (F4-F5) -> Arrow Vibration (F6-F7) -> Dissolve (F8-F9)"
$arGfx.DrawString($flowText, $arSubFont, $subBrush, 32.0, 54.0)

$rainImg = [System.Drawing.Image]::FromFile($rainFile)
for ($f = 0; $f -lt 12; $f++) {
    $col = $f % 6
    $row = [int]($f / 6)
    $bx = 30.0 + ($col * 190.0)
    $by = 95.0 + ($row * 175.0)

    $arGfx.FillRectangle($tileBrush, $bx, $by, 180.0, 165.0)
    $arGfx.DrawRectangle($cellBorder, $bx, $by, 180.0, 165.0)

    $floorPen = New-Object System.Drawing.Pen((Clr 80 100 130 160), 1.0)
    $arGfx.DrawLine($floorPen, [float]($bx + 20), [float]($by + 115), [float]($bx + 160), [float]($by + 115))
    $floorPen.Dispose()

    $srcRect = New-Object System.Drawing.Rectangle ($f * 128), 0, 128, 128
    $dstRect = New-Object System.Drawing.Rectangle ([int]($bx + 26)), ([int]($by + 10)), 128, 128
    $arGfx.DrawImage($rainImg, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $fColor = Clr 255 120 210 255
    $fb = New-Object System.Drawing.SolidBrush($fColor)
    $arGfx.DrawString("Frame " + $f, $arFFont, $fb, [float]($bx + 10), [float]($by + 142))
    $fb.Dispose()

    $fDesc = switch ($f) {
        0 { "Orb Burst" }
        1 { "Signal Bloom" }
        2 { "Arrows Emerge" }
        3 { "High-Speed Descent" }
        4 { "Ground Impact" }
        5 { "Shockwave Blast" }
        6 { "Embedded Arrows" }
        7 { "Sparkle Dissipation" }
        8 { "Arrows Fade" }
        9 { "Glint Dissolve" }
        10 { "Complete Fade" }
        11 { "Pool Return" }
    }
    $arGfx.DrawString($fDesc, $arFDescFont, $subBrush, [float]($bx + 66), [float]($by + 143))
}
$rainImg.Dispose()

$arTitleFont.Dispose(); $arSubFont.Dispose(); $arFFont.Dispose(); $arFDescFont.Dispose()
$titleBrush.Dispose(); $subBrush.Dispose(); $cellBorder.Dispose(); $tileBrush.Dispose()
$arGfx.Dispose()

$scPath2 = "$projectRoot/preview_arrow_rain_effect.png"
$arBmp.Save($scPath2, [System.Drawing.Imaging.ImageFormat]::Png)
$arBmp.Save("$artifactDir/preview_arrow_rain_effect.png", [System.Drawing.Imaging.ImageFormat]::Png)
$arBmp.Dispose()
Write-Host "[Showcase] Generated preview_arrow_rain_effect.png successfully."
