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
# 1. DRAW FUNCTIONS FOR EACH TARGET TOWER PROJECTILE
# ==============================================================================

# 1) Fire: 소형 파이어볼 (Small Fireball)
function Draw-SmallFireball($gfx, [float]$cx, [float]$cy, [float]$phase, [float]$scale) {
    $radius = 9.0 * $scale
    $tailLen = (28.0 + [Math]::Sin($phase * 2.0) * 4.0) * $scale
    $wWave = [Math]::Sin($phase) * (1.8 * $scale)

    # 1. Flame Tail
    $tailPtsRed = [System.Drawing.PointF[]]@(
        (Pt ($cx - 2.0 * $scale) ($cy - 8.5 * $scale)),
        (Pt ($cx - 14.0 * $scale) ($cy - 7.0 * $scale + $wWave)),
        (Pt ($cx - $tailLen) ($cy + $wWave * 0.5)),
        (Pt ($cx - 14.0 * $scale) ($cy + 7.0 * $scale + $wWave)),
        (Pt ($cx - 2.0 * $scale) ($cy + 8.5 * $scale))
    )
    $bTailRed = New-Object System.Drawing.SolidBrush((Clr 200 230 40 0))
    $gfx.FillPolygon($bTailRed, $tailPtsRed)
    $bTailRed.Dispose()

    $tailPtsOrange = [System.Drawing.PointF[]]@(
        (Pt ($cx - 1.0 * $scale) ($cy - 5.5 * $scale)),
        (Pt ($cx - 10.0 * $scale) ($cy - 4.5 * $scale + $wWave * 0.6)),
        (Pt ($cx - $tailLen * 0.7) ($cy + $wWave * 0.3)),
        (Pt ($cx - 10.0 * $scale) ($cy + 4.5 * $scale + $wWave * 0.6)),
        (Pt ($cx - 1.0 * $scale) ($cy + 5.5 * $scale))
    )
    $bTailOrange = New-Object System.Drawing.SolidBrush((Clr 245 255 130 0))
    $gfx.FillPolygon($bTailOrange, $tailPtsOrange)
    $bTailOrange.Dispose()

    # 2. Fireball Head (Compact sphere)
    $bAura = New-Object System.Drawing.SolidBrush((Clr 120 255 50 0))
    Fill-EllipseCentered $gfx $bAura $cx $cy (12.0 * $scale) (12.0 * $scale)
    $bAura.Dispose()

    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 240 45 0))
    Fill-EllipseCentered $gfx $bBody $cx $cy $radius $radius
    $bBody.Dispose()

    $bVol = New-Object System.Drawing.SolidBrush((Clr 255 255 150 0))
    Fill-EllipseCentered $gfx $bVol ($cx + 1.5 * $scale) $cy (6.5 * $scale) (6.5 * $scale)
    $bVol.Dispose()

    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 255 245 180))
    Fill-EllipseCentered $gfx $bCore ($cx + 2.8 * $scale) $cy (3.8 * $scale) (3.8 * $scale)
    $bCore.Dispose()

    # Sparks
    $bSpk = New-Object System.Drawing.SolidBrush((Clr 240 255 220 50))
    $spkX1 = $cx - 12.0 * $scale - ([Math]::Cos($phase) * 6.0 * $scale)
    $spkY1 = $cy - 5.0 * $scale + ([Math]::Sin($phase) * 2.0 * $scale)
    Fill-EllipseCentered $gfx $bSpk $spkX1 $spkY1 (1.6 * $scale) (1.6 * $scale)
    $spkX2 = $cx - 18.0 * $scale - ([Math]::Sin($phase) * 5.0 * $scale)
    $spkY2 = $cy + 4.5 * $scale - ([Math]::Cos($phase) * 2.0 * $scale)
    Fill-EllipseCentered $gfx $bSpk $spkX2 $spkY2 (1.4 * $scale) (1.4 * $scale)
    $bSpk.Dispose()
}

# 2) Ice: 고드름 (Icicle - sharp crystalline frost spike)
function Draw-Icicle($gfx, [float]$cx, [float]$cy, [float]$scale) {
    # Tip at (cx + 26), Base at (cx - 22), Width 14
    $tipX  = $cx + 26.0 * $scale
    $baseX = $cx - 22.0 * $scale
    $halfW = 7.0 * $scale

    # Outer frost aura
    $bAura = New-Object System.Drawing.SolidBrush((Clr 80 0 190 255))
    $ptsAura = [System.Drawing.PointF[]]@(
        (Pt ($baseX - 3.0 * $scale) ($cy - ($halfW + 3.0 * $scale))),
        (Pt ($baseX + 16.0 * $scale) ($cy - ($halfW + 1.0 * $scale))),
        (Pt ($tipX + 4.0 * $scale) $cy),
        (Pt ($baseX + 16.0 * $scale) ($cy + ($halfW + 1.0 * $scale))),
        (Pt ($baseX - 3.0 * $scale) ($cy + ($halfW + 3.0 * $scale)))
    )
    $gfx.FillPolygon($bAura, $ptsAura)
    $bAura.Dispose()

    # Bottom facet (deep cyan ice shadow)
    $ptsBot = [System.Drawing.PointF[]]@(
        (Pt $baseX ($cy + $halfW)),
        (Pt ($baseX + 18.0 * $scale) ($cy + $halfW * 0.7)),
        (Pt $tipX $cy),
        (Pt ($baseX + 14.0 * $scale) $cy),
        (Pt $baseX $cy)
    )
    $bBot = New-Object System.Drawing.SolidBrush((Clr 245 0 150 230))
    $gfx.FillPolygon($bBot, $ptsBot)
    $bBot.Dispose()

    # Top facet (pale ice cyan light)
    $ptsTop = [System.Drawing.PointF[]]@(
        (Pt $baseX ($cy - $halfW)),
        (Pt ($baseX + 18.0 * $scale) ($cy - $halfW * 0.7)),
        (Pt $tipX $cy),
        (Pt ($baseX + 14.0 * $scale) $cy),
        (Pt $baseX $cy)
    )
    $bTop = New-Object System.Drawing.SolidBrush((Clr 255 120 225 255))
    $gfx.FillPolygon($bTop, $ptsTop)
    $bTop.Dispose()

    # Center ridge crystal highlight (sharp white spine)
    $pSpine = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.6 * $scale))
    $gfx.DrawLine($pSpine, ($baseX + 4.0 * $scale), $cy, ($tipX - 1.0 * $scale), $cy)
    $pSpine.Dispose()

    # Frost sparkles
    $bSpark = New-Object System.Drawing.SolidBrush((Clr 240 220 250 255))
    Fill-EllipseCentered $gfx $bSpark ($tipX - 6.0 * $scale) ($cy - 4.0 * $scale) (1.8 * $scale) (1.8 * $scale)
    Fill-EllipseCentered $gfx $bSpark ($baseX + 12.0 * $scale) ($cy + 5.0 * $scale) (1.5 * $scale) (1.5 * $scale)
    $bSpark.Dispose()
}

# 3) Electricity: 제우스 번개막대기 (Zeus Thunderbolt - Thor Love & Thunder)
function Draw-ZeusThunderbolt($gfx, [float]$cx, [float]$cy, [float]$scale) {
    # Iconic golden zigzagging lightning javelin
    $ptsBolt = [System.Drawing.PointF[]]@(
        (Pt ($cx - 28.0 * $scale) ($cy - 1.0 * $scale)),
        (Pt ($cx - 16.0 * $scale) ($cy - 7.5 * $scale)),
        (Pt ($cx - 10.0 * $scale) ($cy - 2.0 * $scale)),
        (Pt ($cx + 4.0 * $scale) ($cy - 8.0 * $scale)),
        (Pt ($cx + 10.0 * $scale) ($cy - 2.5 * $scale)),
        (Pt ($cx + 28.0 * $scale) $cy), # Spearhead tip
        (Pt ($cx + 12.0 * $scale) ($cy + 2.5 * $scale)),
        (Pt ($cx + 6.0 * $scale) ($cy + 8.0 * $scale)),
        (Pt ($cx - 8.0 * $scale) ($cy + 2.0 * $scale)),
        (Pt ($cx - 14.0 * $scale) ($cy + 7.5 * $scale)),
        (Pt ($cx - 28.0 * $scale) ($cy + 1.0 * $scale))
    )

    # 1. Electric Glow Aura
    $pAura = New-Object System.Drawing.Pen((Clr 110 255 210 0), (7.0 * $scale))
    $pAura.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $gfx.DrawPolygon($pAura, $ptsBolt)
    $pAura.Dispose()

    # 2. Golden Lightning Body
    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 255 200 0))
    $gfx.FillPolygon($bBody, $ptsBolt)
    $bBody.Dispose()

    # 3. Supercharged White Core Lightning
    $pCore = New-Object System.Drawing.Pen((Clr 255 255 255 240), (2.0 * $scale))
    $pCore.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $ptsCore = [System.Drawing.PointF[]]@(
        (Pt ($cx - 24.0 * $scale) $cy),
        (Pt ($cx - 13.0 * $scale) ($cy - 3.5 * $scale)),
        (Pt ($cx - 7.0 * $scale) ($cy - 0.5 * $scale)),
        (Pt ($cx + 7.0 * $scale) ($cy - 3.5 * $scale)),
        (Pt ($cx + 25.0 * $scale) $cy)
    )
    $gfx.DrawLines($pCore, $ptsCore)
    $pCore.Dispose()

    # Orbiting electric arcs / sparks
    $bArc = New-Object System.Drawing.SolidBrush((Clr 250 255 255 200))
    Fill-EllipseCentered $gfx $bArc ($cx + 18.0 * $scale) ($cy - 6.0 * $scale) (1.8 * $scale) (1.8 * $scale)
    Fill-EllipseCentered $gfx $bArc ($cx - 4.0 * $scale) ($cy + 7.0 * $scale) (1.6 * $scale) (1.6 * $scale)
    Fill-EllipseCentered $gfx $bArc ($cx - 20.0 * $scale) ($cy - 5.0 * $scale) (1.5 * $scale) (1.5 * $scale)
    $bArc.Dispose()
}

# 4) Wind: 심플한 청록 구체 (Simple Teal Wind Sphere)
function Draw-WindSphere($gfx, [float]$cx, [float]$cy, [float]$scale) {
    $r = 12.0 * $scale

    # Soft breeze halo
    $bAura = New-Object System.Drawing.SolidBrush((Clr 90 0 200 170))
    Fill-EllipseCentered $gfx $bAura $cx $cy ($r * 1.35) ($r * 1.35)
    $bAura.Dispose()

    # Main Teal Sphere
    $bBody = New-Object System.Drawing.SolidBrush((Clr 245 0 220 185))
    Fill-EllipseCentered $gfx $bBody $cx $cy $r $r
    $bBody.Dispose()

    # Inner Shading
    $bVol = New-Object System.Drawing.SolidBrush((Clr 255 60 245 210))
    Fill-EllipseCentered $gfx $bVol ($cx + 2.0 * $scale) ($cy - 1.5 * $scale) ($r * 0.65) ($r * 0.65)
    $bVol.Dispose()

    # Clean wind swirl arcs
    $pSwirl = New-Object System.Drawing.Pen((Clr 240 220 255 245), (1.8 * $scale))
    $gfx.DrawArc($pSwirl, ($cx - $r * 0.7), ($cy - $r * 0.7), ($r * 1.4), ($r * 1.4), 160.0, 110.0)
    $pSwirl.Dispose()

    # Tiny airflow motes
    $bMote = New-Object System.Drawing.SolidBrush((Clr 220 180 255 240))
    Fill-EllipseCentered $gfx $bMote ($cx - 15.0 * $scale) ($cy - 4.0 * $scale) (1.6 * $scale) (1.6 * $scale)
    Fill-EllipseCentered $gfx $bMote ($cx - 18.0 * $scale) ($cy + 3.0 * $scale) (1.4 * $scale) (1.4 * $scale)
    $bMote.Dispose()
}

# 5) Earth: 심플한 흙 구체 (Simple Earth/Soil Brown Sphere)
function Draw-EarthSphere($gfx, [float]$cx, [float]$cy, [float]$scale) {
    $r = 12.0 * $scale

    # Warm dust aura
    $bAura = New-Object System.Drawing.SolidBrush((Clr 85 130 65 15))
    Fill-EllipseCentered $gfx $bAura $cx $cy ($r * 1.35) ($r * 1.35)
    $bAura.Dispose()

    # Main Soil Brown Sphere
    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 145 75 25))
    Fill-EllipseCentered $gfx $bBody $cx $cy $r $r
    $bBody.Dispose()

    # 3D Spherical Volume (Amber/Ochre highlight)
    $bVol = New-Object System.Drawing.SolidBrush((Clr 255 200 120 45))
    Fill-EllipseCentered $gfx $bVol ($cx + 2.5 * $scale) ($cy - 2.0 * $scale) ($r * 0.65) ($r * 0.65)
    $bVol.Dispose()

    # Bright mineral core point
    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 250 215 150))
    Fill-EllipseCentered $gfx $bCore ($cx + 4.0 * $scale) ($cy - 3.0 * $scale) (2.5 * $scale) (2.5 * $scale)
    $bCore.Dispose()

    # Subtle rock texture flecks
    $bFleck = New-Object System.Drawing.SolidBrush((Clr 200 90 40 10))
    Fill-EllipseCentered $gfx $bFleck ($cx - 4.0 * $scale) ($cy + 3.0 * $scale) (1.8 * $scale) (1.8 * $scale)
    Fill-EllipseCentered $gfx $bFleck ($cx + 1.0 * $scale) ($cy + 6.0 * $scale) (1.5 * $scale) (1.5 * $scale)
    $bFleck.Dispose()
}

# 6) Light: 레이저 빔 이펙트 (Laser Beam Effect)
function Draw-LightLaser($gfx, [float]$cx, [float]$cy, [float]$scale) {
    # Horizontal piercing beam: from (cx - 38) to (cx + 38)
    $startX = $cx - 38.0 * $scale
    $endX   = $cx + 38.0 * $scale

    # 1. Outer Radiant Glow Beam
    $pGlow = New-Object System.Drawing.Pen((Clr 85 255 240 130), (14.0 * $scale))
    $pGlow.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pGlow.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pGlow, $startX, $cy, $endX, $cy)
    $pGlow.Dispose()

    # 2. Mid Gold Beam
    $pMid = New-Object System.Drawing.Pen((Clr 210 255 250 180), (7.0 * $scale))
    $pMid.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pMid.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pMid, $startX, $cy, $endX, $cy)
    $pMid.Dispose()

    # 3. Pure Intense White Laser Core
    $pCore = New-Object System.Drawing.Pen((Clr 255 255 255 255), (3.0 * $scale))
    $pCore.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pCore.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
    $gfx.DrawLine($pCore, $startX, $cy, $endX, $cy)
    $pCore.Dispose()

    # Piercing Star Flare at the leading head (endX)
    $pFlare = New-Object System.Drawing.Pen((Clr 255 255 255 255), (1.8 * $scale))
    $gfx.DrawLine($pFlare, ($endX - 10.0 * $scale), $cy, ($endX + 10.0 * $scale), $cy)
    $gfx.DrawLine($pFlare, $endX, ($cy - 10.0 * $scale), $endX, ($cy + 10.0 * $scale))
    $pFlare.Dispose()

    # Sparkle burst
    $bSpark = New-Object System.Drawing.SolidBrush((Clr 255 255 255 240))
    Fill-EllipseCentered $gfx $bSpark $endX $cy (4.0 * $scale) (4.0 * $scale)
    $bSpark.Dispose()
}

# 7) Darkness: 심플한 암흑 구체 (Simple Dark/Obsidian Sphere)
function Draw-DarkSphere($gfx, [float]$cx, [float]$cy, [float]$scale) {
    $r = 12.0 * $scale

    # Neon void violet outer halo
    $bAura = New-Object System.Drawing.SolidBrush((Clr 130 130 30 220))
    Fill-EllipseCentered $gfx $bAura $cx $cy ($r * 1.35) ($r * 1.35)
    $bAura.Dispose()

    # Dark void body (Abyss Black)
    $bBody = New-Object System.Drawing.SolidBrush((Clr 255 18 8 28))
    Fill-EllipseCentered $gfx $bBody $cx $cy $r $r
    $bBody.Dispose()

    # Neon violet edge rim (so it pops clearly on dark backgrounds)
    $pRim = New-Object System.Drawing.Pen((Clr 240 190 70 255), (1.8 * $scale))
    Draw-EllipseCentered $gfx $pRim $cx $cy $r $r
    $pRim.Dispose()

    # Pulsing magenta void eye
    $bEye = New-Object System.Drawing.SolidBrush((Clr 240 180 80 255))
    Fill-EllipseCentered $gfx $bEye ($cx + 1.5 * $scale) $cy (4.5 * $scale) (4.5 * $scale)
    $bEye.Dispose()

    $bPupil = New-Object System.Drawing.SolidBrush((Clr 255 20 8 30))
    Fill-EllipseCentered $gfx $bPupil ($cx + 1.5 * $scale) $cy (2.2 * $scale) (2.2 * $scale)
    $bPupil.Dispose()
}


# ==============================================================================
# 2. BUILD FIREBALL SMALL SPRITESHEET (512x128, 4 frames for E207 bullet)
# ==============================================================================
$bmpFireball = New-Object System.Drawing.Bitmap(512, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gFireball = [System.Drawing.Graphics]::FromImage($bmpFireball)
$gFireball.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gFireball.Clear([System.Drawing.Color]::Transparent)

for ($f = 0; $f -lt 4; $f++) {
    $cellX = $f * 128
    $phase = ($f / 4.0) * 2.0 * [Math]::PI
    # Center fireball at (cellX + 68, 64) with scale 1.4
    Draw-SmallFireball $gFireball ($cellX + 68) 64.0 $phase 1.4
}

$fireballPath = "Assets/4. DotAsset/Projectile_Fireball_Small.png"
if (Test-Path $fireballPath) { [System.IO.File]::Delete($fireballPath) }
$bmpFireball.Save($fireballPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Saved Fireball Small Spritesheet to $fireballPath!"


# ==============================================================================
# 3. BUILD ALL 7 TARGET BULLETS SHOWCASE BANNER (1200 x 380)
# ==============================================================================
$bmpShowcase = New-Object System.Drawing.Bitmap(1200, 380)
$gShow = [System.Drawing.Graphics]::FromImage($bmpShowcase)
$gShow.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gShow.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gShow.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

function Get-Utf8Str([byte[]]$bytes) { [System.Text.Encoding]::UTF8.GetString($bytes) }

# Title
$titleText = [System.Text.Encoding]::UTF8.GetString(@(0xED, 0x83, 0x80, 0xEA, 0xB2, 0x8F, 0x28, 0xEB, 0x8B, 0xA8, 0xEC, 0x9D, 0xBC, 0x29, 0x20, 0xED, 0x83, 0x80, 0xEC, 0x9B, 0x8C, 0x20, 0xEC, 0xB4, 0x9D, 0xEC, 0x95, 0x8C, 0x20, 0xEB, 0x94, 0x94, 0xEC, 0x9E, 0x90, 0xEC, 0x9D, 0xB8, 0x20, 0xEC, 0x8B, 0x9C, 0xEC, 0x95, 0x88, 0x20, 0x28, 0x54, 0x61, 0x72, 0x67, 0x65, 0x74, 0x20, 0x54, 0x6F, 0x77, 0x65, 0x72, 0x20, 0x50, 0x72, 0x6F, 0x6A, 0x65, 0x63, 0x74, 0x69, 0x6C, 0x65, 0x73, 0x29))
$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 13, [System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center

$gShow.DrawString($titleText, $fTitle, $bWhite, 600.0, 16.0, $sfCenter)

$targetItems = @(
    [pscustomobject]@{ Key = 'Fire';        Label = ((Get-Utf8Str @(0xEB, 0xB6, 0x88)) + " (Fire)");       Desc = (Get-Utf8Str @(0xEC, 0x86, 0x8C, 0xED, 0x98, 0x95, 0x20, 0xED, 0x8C, 0x8C, 0xEC, 0x9D, 0xB4, 0xEC, 0x96, 0xB4, 0xEB, 0xB3, 0xBC));                   Color = (Clr 255 255 85 85) },
    [pscustomobject]@{ Key = 'Ice';         Label = ((Get-Utf8Str @(0xEC, 0x96, 0xBC, 0xEC, 0x9D, 0x8C)) + " (Ice)");      Desc = (Get-Utf8Str @(0xEB, 0x82, 0xA0, 0xEC, 0x89, 0x90, 0xED, 0x95, 0x9C, 0x20, 0xEA, 0xB3, 0xA0, 0xEB, 0x93, 0x9C, 0xEB, 0xA6, 0x84));                   Color = (Clr 255 100 210 255) },
    [pscustomobject]@{ Key = 'Electricity'; Label = ((Get-Utf8Str @(0xEC, 0xA0, 0x84, 0xEA, 0xB8, 0xB0)) + " (Electric)"); Desc = (Get-Utf8Str @(0xEC, 0xA0, 0x9C, 0xEC, 0x9A, 0xB0, 0xEC, 0x8A, 0xA4, 0x20, 0xEB, 0xB2, 0x88, 0xEA, 0xB0, 0x9C, 0xEB, 0xA7, 0x89, 0xEB, 0x8C, 0x80, 0xEA, 0xB8, 0xB0)); Color = (Clr 255 255 235 59) },
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

    # Draw specific projectile with scale 2.2
    switch ($item.Key) {
        'Fire'        { Draw-SmallFireball $gShow ($centerX + 10) $centerY 0.0 2.0 }
        'Ice'         { Draw-Icicle $gShow $centerX $centerY 2.0 }
        'Electricity' { Draw-ZeusThunderbolt $gShow $centerX $centerY 2.0 }
        'Wind'        { Draw-WindSphere $gShow $centerX $centerY 2.2 }
        'Earth'       { Draw-EarthSphere $gShow $centerX $centerY 2.2 }
        'Light'       { Draw-LightLaser $gShow $centerX $centerY 1.7 }
        'Darkness'    { Draw-DarkSphere $gShow $centerX $centerY 2.2 }
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

$gFireball.Dispose(); $bmpFireball.Dispose()
$gShow.Dispose(); $bmpShowcase.Dispose()

Write-Output "Saved Target Bullets Showcase to $showcasePath and $rootShowcase!"
