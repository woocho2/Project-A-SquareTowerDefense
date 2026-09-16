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
# DRAW ELEMENTAL BULLET (Scale configurable)
# ==============================================================================
function Draw-ElementalBulletLarge($gfx, [float]$cx, [float]$cy, [string]$element, [float]$scale) {
    switch ($element) {
        'Fire' {
            $cAura  = Clr 110 255 35 0
            $cBody  = Clr 245 255 60 0
            $cCore  = Clr 255 255 250 190
            $cTrail = Clr 175 255 125 0
            $cSpark = Clr 245 255 220 40
        }
        'Ice' {
            $cAura  = Clr 100 0 180 255
            $cBody  = Clr 235 0 215 255
            $cCore  = Clr 255 240 255 255
            $cTrail = Clr 165 100 230 255
            $cSpark = Clr 245 200 245 255
        }
        'Electricity' {
            $cAura  = Clr 110 255 210 0
            $cBody  = Clr 245 255 235 59
            $cCore  = Clr 255 255 255 255
            $cTrail = Clr 175 255 214 0
            $cSpark = Clr 255 255 255 210
        }
        'Wind' {
            $cAura  = Clr 100 0 195 165
            $cBody  = Clr 235 0 235 195
            $cCore  = Clr 255 230 255 250
            $cTrail = Clr 165 30 235 185
            $cSpark = Clr 245 170 255 235
        }
        'Earth' {
            $cAura  = Clr 110 140 70 18
            $cBody  = Clr 235 210 120 40
            $cCore  = Clr 255 255 238 185
            $cTrail = Clr 175 180 95 30
            $cSpark = Clr 245 255 190 90
        }
        'Light' {
            $cAura  = Clr 110 255 235 120
            $cBody  = Clr 245 255 248 190
            $cCore  = Clr 255 255 255 255
            $cTrail = Clr 185 255 248 170
            $cSpark = Clr 255 255 255 255
        }
        'Darkness' {
            $cAura  = Clr 150 120 30 210
            $cBody  = Clr 250 35 12 55
            $cCore  = Clr 255 210 120 255
            $cTrail = Clr 175 100 25 170
            $cSpark = Clr 235 210 140 255
        }
    }

    $bAura  = New-Object System.Drawing.SolidBrush($cAura)
    $bBody  = New-Object System.Drawing.SolidBrush($cBody)
    $bCore  = New-Object System.Drawing.SolidBrush($cCore)
    $bTrail = New-Object System.Drawing.SolidBrush($cTrail)
    $bSpark = New-Object System.Drawing.SolidBrush($cSpark)
    $pTrail = New-Object System.Drawing.Pen($cTrail, (2.2 * $scale))
    $pAura  = New-Object System.Drawing.Pen($cAura, (2.8 * $scale))

    # 1. Outer Flame / Plasma Comet Tail
    $tailPts = [System.Drawing.PointF[]]@(
        (Pt ($cx - 28 * $scale) ($cy)),
        (Pt ($cx - 8 * $scale) ($cy - 8 * $scale)),
        (Pt ($cx + 4 * $scale) ($cy - 6 * $scale)),
        (Pt ($cx + 10 * $scale) ($cy)),
        (Pt ($cx + 4 * $scale) ($cy + 6 * $scale)),
        (Pt ($cx - 8 * $scale) ($cy + 8 * $scale))
    )
    $gfx.FillPolygon($bTrail, $tailPts)

    # Whisps trailing further back
    $gfx.DrawLine($pTrail, ($cx - 8 * $scale), ($cy - 4 * $scale), ($cx - 36 * $scale), ($cy - 6 * $scale))
    $gfx.DrawLine($pTrail, ($cx - 8 * $scale), ($cy + 4 * $scale), ($cx - 36 * $scale), ($cy + 6 * $scale))
    $gfx.DrawLine($pTrail, ($cx - 10 * $scale), $cy, ($cx - 42 * $scale), $cy)

    # 2. Outer Glow Halo
    Fill-EllipseCentered $gfx $bAura $cx $cy (14.0 * $scale) (11.5 * $scale)
    Draw-EllipseCentered $gfx $pAura $cx $cy (14.0 * $scale) (11.5 * $scale)

    # 3. Vibrant Elemental Orb Body
    Fill-EllipseCentered $gfx $bBody ($cx + 1.5 * $scale) $cy (9.5 * $scale) (8.0 * $scale)

    # 4. Superheated Core Glow
    Fill-EllipseCentered $gfx $bCore ($cx + 3.5 * $scale) $cy (5.2 * $scale) (4.2 * $scale)

    # 5. Orbiting Energy Spark Motes
    Fill-EllipseCentered $gfx $bSpark ($cx - 12 * $scale) ($cy - 9 * $scale) (2.2 * $scale) (2.2 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx - 14 * $scale) ($cy + 8 * $scale) (2.0 * $scale) (2.0 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx + 9 * $scale) ($cy - 7 * $scale) (1.8 * $scale) (1.8 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($cx - 3 * $scale) ($cy + 10 * $scale) (2.0 * $scale) (2.0 * $scale)

    $bAura.Dispose(); $bBody.Dispose(); $bCore.Dispose(); $bTrail.Dispose(); $bSpark.Dispose()
    $pTrail.Dispose(); $pAura.Dispose()
}

# ==============================================================================
# BUILD ULTRA LARGE SHOWCASE BANNER (1200 x 360)
# ==============================================================================
$bmp = New-Object System.Drawing.Bitmap(1200, 360)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gfx.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

function Get-Utf8Str([byte[]]$bytes) { [System.Text.Encoding]::UTF8.GetString($bytes) }

# "원소 타워 통합 총알 디자인 시안 (Elemental Projectile Draft)"
$titleText = [System.Text.Encoding]::UTF8.GetString(@(0xEC, 0x9B, 0x90, 0xEC, 0x86, 0x8C, 0x20, 0xED, 0x83, 0x80, 0xEC, 0x9B, 0x8C, 0x20, 0xED, 0x86, 0xB5, 0xED, 0x95, 0xA9, 0x20, 0xEC, 0xB4, 0x9D, 0xEC, 0x95, 0x8C, 0x20, 0xEB, 0x94, 0x94, 0xEC, 0x9E, 0x90, 0xEC, 0x9D, 0xB8, 0x20, 0xEC, 0x8B, 0x9C, 0xEC, 0x95, 0x88, 0x20, 0x28, 0x45, 0x6C, 0x65, 0x6D, 0x65, 0x6E, 0x74, 0x61, 0x6C, 0x20, 0x50, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x69, 0x6C, 0x65, 0x20, 0x44, 0x72, 0x61, 0x66, 0x74, 0x29))

$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center

$gfx.DrawString($titleText, $fTitle, $bWhite, 600.0, 16.0, $sfCenter)

$elements = @(
    [pscustomobject]@{ Name = 'Fire';        Label = ((Get-Utf8Str @(0xEB, 0xB6, 0x88)) + " (Fire)");       Sub = (Get-Utf8Str @(0xED, 0x83, 0x80, 0xEC, 0x98, 0xA4, 0xEB, 0xA5, 0xB4, 0xEB, 0x8A, 0x94, 0x20, 0xEB, 0xB9, 0xA8, 0xEA, 0xB0, 0x84, 0xEC, 0x83, 0x89)); Color = (Clr 255 255 85 85) },
    [pscustomobject]@{ Name = 'Ice';         Label = ((Get-Utf8Str @(0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C)) + " (Ice)");      Sub = (Get-Utf8Str @(0xEC, 0xB2, 0xAD, 0xEB, 0xAA, 0x85, 0xED, 0x95, 0x9C, 0x20, 0xED, 0x95, 0x98, 0xEB, 0x8A, 0x94, 0xEC, 0x83, 0x89)); Color = (Clr 255 100 210 255) },
    [pscustomobject]@{ Name = 'Electricity'; Label = ((Get-Utf8Str @(0xEC, 0xA0, 0x84, 0xEA, 0xB8, 0xB0)) + " (Electric)"); Sub = (Get-Utf8Str @(0xEC, 0x84, 0xA0, 0xEB, 0xAA, 0x85, 0xED, 0x95, 0x9C, 0x20, 0xEB, 0x85, 0xB8, 0xEB, 0x9E, 0x80, 0xEC, 0x83, 0x89)); Color = (Clr 255 255 235 59) },
    [pscustomobject]@{ Name = 'Wind';        Label = ((Get-Utf8Str @(0xEB, 0xB0, 0x94, 0xEB, 0x9E, 0x8C)) + " (Wind)");     Sub = (Get-Utf8Str @(0xEC, 0x83, 0x81, 0xEC, 0x99, 0x8C, 0xED, 0x95, 0x9C, 0x20, 0xEC, 0xB2, 0xAD, 0xEB, 0x87, 0x9D, 0xEC, 0x83, 0x89)); Color = (Clr 255 64 255 218) },
    [pscustomobject]@{ Name = 'Earth';       Label = ((Get-Utf8Str @(0xEB, 0x8C, 0x80, 0xEC, 0xA7, 0x80)) + " (Earth)");    Sub = (Get-Utf8Str @(0xEB, 0x8B, 0xA8, 0xEB, 0x8B, 0xA8, 0xED, 0x95, 0x9C, 0x20, 0xEA, 0xB0, 0x88, 0xEC, 0x83, 0x89)); Color = (Clr 255 215 140 70) },
    [pscustomobject]@{ Name = 'Light';       Label = ((Get-Utf8Str @(0xEB, 0xB9, 0x9B)) + " (Light)");      Sub = (Get-Utf8Str @(0xED, 0x95, 0x98, 0xEC, 0x96, 0x80, 0x20, 0xEC, 0x97, 0xB0, 0xEB, 0x85, 0xB8, 0xEB, 0x9E, 0x80)); Color = (Clr 255 255 255 190) },
    [pscustomobject]@{ Name = 'Darkness';    Label = ((Get-Utf8Str @(0xEC, 0x96, 0xB4, 0xEB, 0x91, 0xA0)) + " (Dark)");     Sub = (Get-Utf8Str @(0xEC, 0x8B, 0xAC, 0xEC, 0x97, 0xB0, 0xEC, 0x9D, 0x98, 0x20, 0xEA, 0xB2, 0x80, 0xEC, 0x9D, 0x80, 0xEC, 0x83, 0x89)); Color = (Clr 255 195 125 255) }
)

$fCardName = New-Object System.Drawing.Font('Malgun Gothic', 11, [System.Drawing.FontStyle]::Bold)
$fCardSub  = New-Object System.Drawing.Font('Malgun Gothic', 9, [System.Drawing.FontStyle]::Regular)
$bSubColor = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))

$cardW = 154
$cardH = 270
$cardY = 60

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $elemName = $elem.Name
    $label    = $elem.Label
    $subDesc  = $elem.Sub
    $lblColor = $elem.Color

    $cardX = 22 + $i * 168

    # Card background with subtle inner glow
    $bCardBg = New-Object System.Drawing.SolidBrush((Clr 255 30 33 42))
    $pBorder = New-Object System.Drawing.Pen((Clr 80 255 255 255), 1.2)
    $cardRect = New-Object System.Drawing.Rectangle($cardX, $cardY, $cardW, $cardH)
    $gfx.FillRectangle($bCardBg, $cardRect)
    $gfx.DrawRectangle($pBorder, $cardRect)
    $bCardBg.Dispose(); $pBorder.Dispose()

    # Center target area for bullet (subtle radial grid / circle)
    $centerX = $cardX + ($cardW / 2.0)
    $centerY = $cardY + 105.0

    $pGuide = New-Object System.Drawing.Pen((Clr 35 255 255 255), 1.0)
    $pGuide.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
    Draw-EllipseCentered $gfx $pGuide $centerX $centerY 48 48
    $pGuide.Dispose()

    # Draw Bullet at large 2.4x scale for crystal clear visibility
    Draw-ElementalBulletLarge $gfx ($centerX + 12) $centerY $elemName 2.4

    # Text Labels below
    $bLabel = New-Object System.Drawing.SolidBrush($lblColor)
    $gfx.DrawString($label, $fCardName, $bLabel, [float]$centerX, [float]($cardY + 195), $sfCenter)
    $gfx.DrawString($subDesc, $fCardSub, $bSubColor, [float]$centerX, [float]($cardY + 225), $sfCenter)
    $bLabel.Dispose()
}

$fTitle.Dispose(); $fCardName.Dispose(); $fCardSub.Dispose(); $bWhite.Dispose(); $bSubColor.Dispose(); $sfCenter.Dispose()

$largePath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_bullets_large.png"
if (Test-Path $largePath) { [System.IO.File]::Delete($largePath) }
$bmp.Save($largePath, [System.Drawing.Imaging.ImageFormat]::Png)
$gfx.Dispose(); $bmp.Dispose()
Write-Output "Saved large preview to $largePath!"

# Copy directly to project root for easy user opening
$rootCopy = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\preview_bullets_large.png"
Copy-Item $largePath $rootCopy -Force
Write-Output "Copied large preview to $rootCopy!"

# Export individual 64x64 transparent sprites to Unity assets folder
$bulletDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\Assets\4. DotAsset\3. Projectile\NewElementalBullets"
if (-not (Test-Path $bulletDir)) { New-Item -ItemType Directory -Path $bulletDir -Force | Out-Null }

foreach ($elem in $elements) {
    $eName = $elem.Name
    $singleBmp = New-Object System.Drawing.Bitmap(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $sg = [System.Drawing.Graphics]::FromImage($singleBmp)
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $sg.Clear([System.Drawing.Color]::Transparent)
    
    # Scale 0.8 fits perfectly in 64x64
    Draw-ElementalBulletLarge $sg 36 32 $eName 0.75
    
    $outPath = Join-Path $bulletDir "Projectile_$eName.png"
    if (Test-Path $outPath) { [System.IO.File]::Delete($outPath) }
    $singleBmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $sg.Dispose(); $singleBmp.Dispose()
    Write-Output "Exported individual bullet: $outPath"
}

