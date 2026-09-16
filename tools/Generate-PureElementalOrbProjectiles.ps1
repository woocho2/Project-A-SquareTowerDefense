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

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

function Draw-EllipseCentered($gfx, $pen, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.DrawEllipse($pen, ($cx - $rx), ($cy - $ry), ($rx * 2.0), ($ry * 2.0))
}

# Elemental definitions
$elements = @(
    @{
        ID = 107; Name = "Fire"; File = "Projectile_Meteor_Fire.png"
        Halo = (Clr 110 255 80 20); PenHalo = (Clr 150 255 140 40); Body = (Clr 245 235 65 15)
        Core = (Clr 255 255 230 110); Glint = (Clr 255 255 255 255); Mote = (Clr 220 255 190 50)
        DarkCore = $false
    },
    @{
        ID = 108; Name = "Ice"; File = "Projectile_Meteor_Ice.png"
        Halo = (Clr 100 56 189 248); PenHalo = (Clr 150 125 211 252); Body = (Clr 245 2 132 199)
        Core = (Clr 255 186 230 253); Glint = (Clr 255 255 255 255); Mote = (Clr 220 125 211 252)
        DarkCore = $false
    },
    @{
        ID = 109; Name = "Electric"; File = "Projectile_Meteor_Electric.png"
        Halo = (Clr 110 250 204 21); PenHalo = (Clr 160 254 240 138); Body = (Clr 245 234 179 8)
        Core = (Clr 255 254 249 195); Glint = (Clr 255 255 255 255); Mote = (Clr 230 253 224 71)
        DarkCore = $false
    },
    @{
        ID = 110; Name = "Wind"; File = "Projectile_Meteor_Wind.png"
        Halo = (Clr 100 16 185 129); PenHalo = (Clr 150 52 211 153); Body = (Clr 245 5 150 105)
        Core = (Clr 255 167 243 208); Glint = (Clr 255 255 255 255); Mote = (Clr 220 110 231 183)
        DarkCore = $false
    },
    @{
        ID = 111; Name = "Earth"; File = "Projectile_Meteor_Earth.png"
        Halo = (Clr 110 217 119 6); PenHalo = (Clr 160 245 158 11); Body = (Clr 245 146 64 14)
        Core = (Clr 255 253 230 138); Glint = (Clr 255 255 255 255); Mote = (Clr 220 251 191 36)
        DarkCore = $false
    },
    @{
        ID = 112; Name = "Light"; File = "Projectile_Meteor_Light.png"
        Halo = (Clr 120 253 224 71); PenHalo = (Clr 170 254 240 138); Body = (Clr 250 254 249 195)
        Core = (Clr 255 255 255 255); Glint = (Clr 255 255 255 255); Mote = (Clr 240 255 255 255)
        DarkCore = $false
    },
    @{
        ID = 113; Name = "Darkness"; File = "Projectile_Meteor_Dark.png"
        Halo = (Clr 130 147 51 234); PenHalo = (Clr 170 192 132 252); Body = (Clr 255 22 8 36)
        Core = (Clr 255 192 132 252); Glint = (Clr 255 233 213 255); Mote = (Clr 220 216 180 254)
        DarkCore = $true
    }
)

# Function to draw pure elemental orb at (cx, cy)
function Draw-PureOrb($gfx, [float]$cx, [float]$cy, $elem, [int]$f, [int]$totalF = 6, [float]$scale = 1.0) {
    $phase = ($f / [float]$totalF) * 2.0 * [Math]::PI
    $pulse = [Math]::Sin($phase)
    $pulseCos = [Math]::Cos($phase)

    $rHalo = (18.0 + 1.8 * $pulse) * $scale
    $rBody = (13.0 + 0.8 * $pulse) * $scale
    $rCore = (7.5 + 0.5 * $pulseCos) * $scale

    # 1. Outer Diffuse Halo Bloom
    $pathHalo = New-Object System.Drawing.Drawing2D.GraphicsPath
    $pathHalo.AddEllipse(($cx - $rHalo), ($cy - $rHalo), ($rHalo * 2.0), ($rHalo * 2.0))
    $pgbHalo = New-Object System.Drawing.Drawing2D.PathGradientBrush($pathHalo)
    $pgbHalo.CenterColor = $elem.Halo
    $pgbHalo.SurroundColors = @([System.Drawing.Color](Clr 0 0 0 0))
    $gfx.FillPath($pgbHalo, $pathHalo)
    $pgbHalo.Dispose()
    $pathHalo.Dispose()

    # Corona edge pen
    $pHalo = New-Object System.Drawing.Pen($elem.PenHalo, (1.2 * $scale))
    Draw-EllipseCentered $gfx $pHalo $cx $cy ($rHalo * 0.88) ($rHalo * 0.88)
    $pHalo.Dispose()

    # 2. Main Spherical Body
    $bBody = New-Object System.Drawing.SolidBrush($elem.Body)
    Fill-EllipseCentered $gfx $bBody $cx $cy $rBody $rBody
    $bBody.Dispose()

    # 3. 3D Specular / Core Glow (Spherical Shading)
    $offX = -1.5 * $scale
    $offY = -1.5 * $scale

    if ($elem.DarkCore) {
        # Darkness: Radiant purple accretion ring + Black event horizon center
        $bAccretion = New-Object System.Drawing.SolidBrush($elem.Core)
        Fill-EllipseCentered $gfx $bAccretion $cx $cy ($rBody * 0.72) ($rBody * 0.72)
        $bAccretion.Dispose()

        $bVoid = New-Object System.Drawing.SolidBrush((Clr 255 12 4 20))
        Fill-EllipseCentered $gfx $bVoid $cx $cy ($rBody * 0.48) ($rBody * 0.48)
        $bVoid.Dispose()

        $pRing = New-Object System.Drawing.Pen($elem.Glint, (1.0 * $scale))
        Draw-EllipseCentered $gfx $pRing $cx $cy ($rBody * 0.52) ($rBody * 0.52)
        $pRing.Dispose()
    } else {
        # Standard Elements: Glowing hot core & specular glint
        $bCore = New-Object System.Drawing.SolidBrush($elem.Core)
        Fill-EllipseCentered $gfx $bCore ($cx + $offX) ($cy + $offY) $rCore $rCore
        $bCore.Dispose()

        $bGlint = New-Object System.Drawing.SolidBrush($elem.Glint)
        Fill-EllipseCentered $gfx $bGlint ($cx + $offX * 1.6) ($cy + $offY * 1.6) (2.8 * $scale) (2.8 * $scale)
        $bGlint.Dispose()
    }

    # 4. 4 Orbiting Energy Spark Motes (Seamless 360 rotation over 6 frames)
    $bMote = New-Object System.Drawing.SolidBrush($elem.Mote)
    for ($k = 0; $k -lt 4; $k++) {
        $ang = $phase + ($k * [Math]::PI * 0.5)
        $mDist = (17.5 + 2.2 * [Math]::Sin($phase * 2.0 + $k)) * $scale
        $mx = $cx + ($mDist * [Math]::Cos($ang))
        $my = $cy + ($mDist * [Math]::Sin($ang))
        $mSize = (1.8 + 0.4 * [Math]::Sin($ang)) * $scale
        Fill-EllipseCentered $gfx $bMote $mx $my $mSize $mSize
    }
    $bMote.Dispose()
}

# Generate 7 Projectile Sheets (768x128, 6 frames)
foreach ($elem in $elements) {
    $sheet = New-Object System.Drawing.Bitmap(768, 128)
    $gfx = [System.Drawing.Graphics]::FromImage($sheet)
    $gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $gfx.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    for ($f = 0; $f -lt 6; $f++) {
        $cx = ($f * 128.0) + 64.0
        $cy = 64.0
        Draw-PureOrb $gfx $cx $cy $elem $f 6 1.0
    }

    $outPath = "$projectRoot/Assets/4. DotAsset/3. Projectile/$($elem.File)"
    $sheet.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "Generated: $outPath"

    $gfx.Dispose()
    $sheet.Dispose()
}

# Also update 207 (Projectile_Fireball_Small.png: 512x128, 4 frames)
$fireElem = $elements[0]
$fireSmall = New-Object System.Drawing.Bitmap(512, 128)
$fgfx = [System.Drawing.Graphics]::FromImage($fireSmall)
$fgfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$fgfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
for ($f = 0; $f -lt 4; $f++) {
    $cx = ($f * 128.0) + 64.0
    $cy = 64.0
    Draw-PureOrb $fgfx $cx $cy $fireElem $f 4 1.0
}
$fireSmallPath = "$projectRoot/Assets/4. DotAsset/3. Projectile/Projectile_Fireball_Small.png"
$fireSmall.Save($fireSmallPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Host "Updated Fireball Small: $fireSmallPath"
$fgfx.Dispose()
$fireSmall.Dispose()

# Also update NewElementalBullets standalone 64x64 sprites
$newDir = "$projectRoot/Assets/4. DotAsset/3. Projectile/NewElementalBullets"
if (Test-Path $newDir) {
    foreach ($elem in $elements) {
        $bmp64 = New-Object System.Drawing.Bitmap(64, 64)
        $g64 = [System.Drawing.Graphics]::FromImage($bmp64)
        $g64.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        Draw-PureOrb $g64 32.0 32.0 $elem 0 6 0.85
        $p64 = "$newDir/Projectile_$($elem.Name).png"
        $bmp64.Save($p64, [System.Drawing.Imaging.ImageFormat]::Png)
        $g64.Dispose()
        $bmp64.Dispose()
    }
    Write-Host "Updated NewElementalBullets standalone 64x64 orbs."
}

# ==============================================================================
# BUILD SHOWCASE BANNER (1320 x 680)
# ==============================================================================
$bannerW = 1320
$bannerH = 680
$banner = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$bgfx = [System.Drawing.Graphics]::FromImage($banner)
$bgfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$bgfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bBg = New-Object System.Drawing.SolidBrush((Clr 255 18 20 28))
$bgfx.FillRectangle($bBg, 0, 0, $bannerW, $bannerH)
$bBg.Dispose()

$fHeader = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
$fRow = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fSub = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))
$bHighlight = New-Object System.Drawing.SolidBrush((Clr 255 255 220 100))

$bgfx.DrawString("Pure Elemental Orb Projectiles (No Tail) - 7 Elements (107 ~ 113)", $fHeader, $bWhite, 20, 15)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $rowY = 52 + ($i * 86)

    $pBox = New-Object System.Drawing.Pen((Clr 255 45 50 68), 1.0)
    $bgfx.DrawRectangle($pBox, 15, $rowY, 1290, 80)
    $pBox.Dispose()

    # Load sheet
    $sheetFile = "$projectRoot/Assets/4. DotAsset/3. Projectile/$($elem.File)"
    $sheetImg = [System.Drawing.Image]::FromFile($sheetFile)

    # Element Label
    $bgfx.DrawString("$($elem.ID): $($elem.Name) Orb", $fRow, $bHighlight, 25, ($rowY + 12))
    $bgfx.DrawString("Centered (64,64) | Seamless Loop | 4 Orbiting Motes", $fSub, $bGray, 25, ($rowY + 44))

    # 6 frames
    for ($f = 0; $f -lt 6; $f++) {
        $dx = 340 + ($f * 95)
        $dy = $rowY + 8
        $srcR = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
        $dstR = New-Object System.Drawing.Rectangle($dx, $dy, 64, 64)

        $pFrameBox = New-Object System.Drawing.Pen((Clr 255 35 40 55), 1.0)
        $bgfx.DrawRectangle($pFrameBox, $dx, $dy, 64, 64)
        $pFrameBox.Dispose()

        $bgfx.DrawImage($sheetImg, $dstR, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
        $bgfx.DrawString("F$f", $fSub, $bGray, ($dx + 24), ($dy + 66))
    }

    # 2x Zoom Preview
    $srcZoom = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
    $dstZoom = New-Object System.Drawing.Rectangle(980, ($rowY + 4), 72, 72)
    $pZoomBox = New-Object System.Drawing.Pen((Clr 255 70 78 105), 1.5)
    $bgfx.DrawRectangle($pZoomBox, 980, ($rowY + 4), 72, 72)
    $pZoomBox.Dispose()
    $bgfx.DrawImage($sheetImg, $dstZoom, $srcZoom, [System.Drawing.GraphicsUnit]::Pixel)
    $bgfx.DrawString("2x Zoom (Pivot Center)", $fSub, $bWhite, 1065, ($rowY + 32))

    $sheetImg.Dispose()
}

$bannerPath1 = "$artifactDir/preview_pure_elemental_orbs.png"
$bannerPath2 = "$projectRoot/preview_pure_elemental_orbs.png"
$banner.Save($bannerPath1, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $bannerPath1 $bannerPath2 -Force

$bgfx.Dispose()
$banner.Dispose()

Write-Host "Showcase banner generated successfully: $bannerPath1"
