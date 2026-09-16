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

$elements = @(
    @{
        ID = 107; Name = "Fire"; File = "Projectile_Meteor_Fire.png"
        Head = (Clr 255 255 255 255); Mid = (Clr 230 249 115 22); Tail = (Clr 0 220 38 38)
        PColor1 = (Clr 240 254 240 138); PColor2 = (Clr 180 234 88 12)
        TrailDesc = "Trail: White-Hot -> Flame Orange -> Crimson"
        PartDesc  = "Particles: Fiery Ember Sparks"
    },
    @{
        ID = 108; Name = "Ice"; File = "Projectile_Meteor_Ice.png"
        Head = (Clr 255 255 255 255); Mid = (Clr 230 56 189 248); Tail = (Clr 0 2 132 199)
        PColor1 = (Clr 240 224 242 254); PColor2 = (Clr 180 14 165 233)
        TrailDesc = "Trail: Pure White -> Crystalline Azure -> Glacial Blue"
        PartDesc  = "Particles: Frost Glitter Flakes"
    },
    @{
        ID = 109; Name = "Electric"; File = "Projectile_Meteor_Electric.png"
        Head = (Clr 255 255 255 255); Mid = (Clr 240 253 224 71); Tail = (Clr 0 202 138 4)
        PColor1 = (Clr 255 255 255 255); PColor2 = (Clr 200 250 204 21)
        TrailDesc = "Trail: White Flash -> Electric Yellow -> Amber Gold"
        PartDesc  = "Particles: High-Voltage Zaps"
    },
    @{
        ID = 110; Name = "Wind"; File = "Projectile_Meteor_Wind.png"
        Head = (Clr 255 204 251 241); Mid = (Clr 230 45 212 191); Tail = (Clr 0 5 150 105)
        PColor1 = (Clr 240 167 243 208); PColor2 = (Clr 180 16 185 129)
        TrailDesc = "Trail: Mint White -> Cyan Turquoise -> Emerald Green"
        PartDesc  = "Particles: Swirling Gale Petals"
    },
    @{
        ID = 111; Name = "Earth"; File = "Projectile_Meteor_Earth.png"
        Head = (Clr 255 254 243 199); Mid = (Clr 235 245 158 11); Tail = (Clr 0 146 64 14)
        PColor1 = (Clr 240 253 230 138); PColor2 = (Clr 180 120 53 15)
        TrailDesc = "Trail: Sandy Glint -> Golden Amber -> Terracotta Earth"
        PartDesc  = "Particles: Rock Dust Crystals"
    },
    @{
        ID = 112; Name = "Light"; File = "Projectile_Meteor_Light.png"
        Head = (Clr 255 255 255 255); Mid = (Clr 245 254 240 138); Tail = (Clr 0 250 204 21)
        PColor1 = (Clr 255 255 255 255); PColor2 = (Clr 220 254 249 195)
        TrailDesc = "Trail: Solar White -> Radiant Pale Yellow -> Holy Gold"
        PartDesc  = "Particles: Twinkling Stardust"
    },
    @{
        ID = 113; Name = "Darkness"; File = "Projectile_Meteor_Dark.png"
        Head = (Clr 255 243 232 255); Mid = (Clr 240 192 132 252); Tail = (Clr 0 88 28 135)
        PColor1 = (Clr 240 233 213 255); PColor2 = (Clr 180 59 7 100)
        TrailDesc = "Trail: Lavender Glint -> Neon Purple -> Void Shadow"
        PartDesc  = "Particles: Dark Matter Motes"
    }
)

$bannerW = 1320
$bannerH = 780
$banner = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$gfx = [System.Drawing.Graphics]::FromImage($banner)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 20 28))
$gfx.FillRectangle($bBg, 0, 0, $bannerW, $bannerH)
$bBg.Dispose()

$fHeader = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$fRow = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fSub = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$fDesc = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))
$bHighlight = New-Object System.Drawing.SolidBrush((Clr 255 255 220 100))

$gfx.DrawString("Hybrid Projectile Visual Effects - TrailRenderer & Elemental Particle Stream (107 ~ 113)", $fHeader, $bWhite, 20, 15)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $rowY = 55 + ($i * 100)

    $pBox = New-Object System.Drawing.Pen((Clr 255 45 50 68), 1.0)
    $gfx.DrawRectangle($pBox, 15, $rowY, 1290, 92)
    $pBox.Dispose()

    # Load orb texture
    $sheetFile = "$projectRoot/Assets/4. DotAsset/3. Projectile/$($elem.File)"
    $sheetImg = [System.Drawing.Image]::FromFile($sheetFile)

    # Element Label & description on the left
    $gfx.DrawString("$($elem.ID): $($elem.Name)", $fRow, $bHighlight, 25, ($rowY + 12))
    $gfx.DrawString($elem.TrailDesc, $fDesc, $bGray, 25, ($rowY + 40))
    $gfx.DrawString($elem.PartDesc, $fDesc, $bGray, 25, ($rowY + 60))

    # --- 1. SATELLITE / IDLE PREVIEW (Left Box: 380, rowY+8) ---
    $pSatBox = New-Object System.Drawing.Pen((Clr 255 60 65 85), 1.0)
    $gfx.DrawRectangle($pSatBox, 380, ($rowY + 10), 90, 56)
    $pSatBox.Dispose()

    # Draw pure orb from Frame 0
    $srcR = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
    $dstR = New-Object System.Drawing.Rectangle(402, ($rowY + 14), 46, 46)
    $gfx.DrawImage($sheetImg, $dstR, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
    $gfx.DrawString("Satellite (No Tail)", $fSub, $bGray, 382, ($rowY + 70))

    # Arrow separator
    $bArrow = New-Object System.Drawing.SolidBrush((Clr 255 100 110 135))
    $gfx.DrawString("Launch >>", $fRow, $bArrow, 485, ($rowY + 28))
    $bArrow.Dispose()

    # --- 2. IN-FLIGHT HYBRID TRAIL & PARTICLES (Right Area: 580 ~ 1280) ---
    $orbX = 1180.0
    $orbY = $rowY + 36.0
    $trailLength = 480.0

    # Draw curved TrailRenderer mesh simulation
    $trailPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $trailPointsTop = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    $trailPointsBot = New-Object System.Collections.Generic.List[System.Drawing.PointF]

    $numNodes = 40
    for ($s = 0; $s -le $numNodes; $s++) {
        $t = $s / [float]$numNodes # 0 (tail tip) to 1 (orb head)
        $tx = $orbX - ($trailLength * (1.0 - $t))
        $ty = $orbY + [Math]::Sin($t * [Math]::PI * 1.5) * 8.0

        $hw = [Math]::Pow($t, 1.4) * 14.0

        $trailPointsTop.Add((Pt $tx ($ty - $hw)))
        $trailPointsBot.Add((Pt $tx ($ty + $hw)))
    }

    $allTrailPts = New-Object System.Collections.Generic.List[System.Drawing.PointF]
    foreach ($pt in $trailPointsTop) { $allTrailPts.Add($pt) }
    for ($idx = $trailPointsBot.Count - 1; $idx -ge 0; $idx--) { $allTrailPts.Add($trailPointsBot[$idx]) }

    $trailPath.AddPolygon($allTrailPts.ToArray())

    $rectTrail = New-Object System.Drawing.RectangleF(($orbX - $trailLength), ($orbY - 20), $trailLength, 40)
    $lgbTrail = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rectTrail, $elem.Tail, $elem.Head, [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal)
    
    $cb = New-Object System.Drawing.Drawing2D.ColorBlend 3
    $cb.Colors = @([System.Drawing.Color]$elem.Tail, [System.Drawing.Color]$elem.Mid, [System.Drawing.Color]$elem.Head)
    $cb.Positions = @(0.0, 0.65, 1.0)
    $lgbTrail.InterpolationColors = $cb

    $gfx.FillPath($lgbTrail, $trailPath)
    $lgbTrail.Dispose()
    $trailPath.Dispose()

    # Streaming Elemental Particles
    for ($p = 0; $p -lt 18; $p++) {
        $ptT = $p / 18.0
        $px = $orbX - 25.0 - ($ptT * $trailLength * 0.85) + (([Math]::Sin($p * 3.7) * 15.0))
        $py = $orbY + [Math]::Sin($ptT * [Math]::PI * 1.5) * 8.0 + (([Math]::Cos($p * 5.2) * 12.0 * (1.0 - $ptT * 0.4)))
        
        $pSize = (1.5 + (1.0 - $ptT) * 2.5)
        $pAlpha = [int]((1.0 - $ptT * 0.8) * 220)
        $pBrushColor = if ($p % 2 -eq 0) { Clr $pAlpha $elem.PColor1.R $elem.PColor1.G $elem.PColor1.B } else { Clr $pAlpha $elem.PColor2.R $elem.PColor2.G $elem.PColor2.B }
        $bP = New-Object System.Drawing.SolidBrush($pBrushColor)
        Fill-EllipseCentered $gfx $bP $px $py $pSize $pSize
        $bP.Dispose()
    }

    # Draw the Orb at the head
    $srcOrb = New-Object System.Drawing.Rectangle((2 * 128), 0, 128, 128)
    $dstOrb = New-Object System.Drawing.Rectangle([int]($orbX - 28), [int]($orbY - 28), 56, 56)
    $gfx.DrawImage($sheetImg, $dstOrb, $srcOrb, [System.Drawing.GraphicsUnit]::Pixel)

    $gfx.DrawString("In-Flight: TrailRenderer + Particle Stream", $fSub, $bWhite, 1000, ($rowY + 12))

    $sheetImg.Dispose()
}

$bannerPath1 = "$artifactDir/preview_elemental_hybrid_trails.png"
$bannerPath2 = "$projectRoot/preview_elemental_hybrid_trails.png"
$banner.Save($bannerPath1, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath1 $bannerPath2 -Force

$gfx.Dispose()
$banner.Dispose()

Write-Host "Hybrid Trail Showcase Banner generated successfully: $bannerPath1"
