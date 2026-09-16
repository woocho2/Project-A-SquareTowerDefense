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

$bannerW = 1240
$bannerH = 760
$bmp = New-Object System.Drawing.Bitmap($bannerW, $bannerH)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$gfx.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

# 1. Dark Background
$bgBrush = New-Object System.Drawing.SolidBrush((Clr 255 15 17 24))
$gfx.FillRectangle($bgBrush, 0, 0, $bannerW, $bannerH)
$bgBrush.Dispose()

# 2. Fonts & Brushes
$fTitle = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$fSub = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Regular)
$fCardTitle = New-Object System.Drawing.Font("Segoe UI", 11, [System.Drawing.FontStyle]::Bold)
$fCardDesc = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$fFeature = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)

$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 245 245 250))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 160 165 180))
$bCyan = New-Object System.Drawing.SolidBrush((Clr 255 56 189 248))
$bOrange = New-Object System.Drawing.SolidBrush((Clr 255 251 146 60))

# 3. Header
$gfx.DrawString("MapBuff & DebuffZone Satellite Hybrid Tails (TrailRenderer + ParticleSystem)", $fTitle, $bWhite, 25, 16)
$gfx.DrawString("Dual-Orbit 3D Sol Janus Architecture: Dynamic Width Scaling (1.25x Front ~ 0.75x Back), White-Hot Flare, and Clean Lifecycle Control", $fSub, $bGray, 25, 42)

# 4. Define 4 Showcases (3 Buffs + 1 Debuff)
$configs = @(
    @{
        Title = "1. Attack Power Up (Tile Buff - Red/Amber)"
        Subtitle = "X-Orbit (Diagonal 45 deg) | Flame Amber"
        BaseColor = (Clr 255 249 115 22)
        TrailHead = (Clr 255 255 247 237)
        TrailMid  = (Clr 220 251 146 60)
        TrailTail = (Clr 0 194 65 12)
        PartColor = (Clr 230 254 215 170)
        IsDebuff  = $false
        OrbitAngle1 = 45.0; OrbitAngle2 = -45.0
    },
    @{
        Title = "2. Attack Count Up (Tile Buff - Azure/Blue)"
        Subtitle = "X-Orbit (Diagonal 45 deg) | Frost Azure"
        BaseColor = (Clr 255 56 189 248)
        TrailHead = (Clr 255 240 249 255)
        TrailMid  = (Clr 220 56 189 248)
        TrailTail = (Clr 0 3 105 161)
        PartColor = (Clr 230 186 230 253)
        IsDebuff  = $false
        OrbitAngle1 = 45.0; OrbitAngle2 = -45.0
    },
    @{
        Title = "3. Action Count Up (Tile Buff - Emerald/Green)"
        Subtitle = "X-Orbit (Diagonal 45 deg) | Emerald Gale"
        BaseColor = (Clr 255 52 211 153)
        TrailHead = (Clr 255 236 253 245)
        TrailMid  = (Clr 220 52 211 153)
        TrailTail = (Clr 0 5 150 105)
        PartColor = (Clr 230 167 243 208)
        IsDebuff  = $false
        OrbitAngle1 = 45.0; OrbitAngle2 = -45.0
    },
    @{
        Title = "4. Debuff Zone (Cross-Orbit - Purple/Dark)"
        Subtitle = "Cross-Orbit (Horizontal 0 deg / Vertical 90 deg) | Void Darkness"
        BaseColor = (Clr 255 192 132 252)
        TrailHead = (Clr 255 250 245 255)
        TrailMid  = (Clr 220 192 132 252)
        TrailTail = (Clr 0 107 33 168)
        PartColor = (Clr 230 233 213 255)
        IsDebuff  = $true
        OrbitAngle1 = 0.0; OrbitAngle2 = 90.0
    }
)

# Helper function to get orbit (x, y)
function Get-OrbitPoint([float]$theta, [float]$majorR, [float]$minorR, [float]$rotAngleDeg) {
    $rad = $rotAngleDeg * [Math]::PI / 180.0
    $cosR = [Math]::Cos($rad)
    $sinR = [Math]::Sin($rad)

    $u = $majorR * [Math]::Cos($theta)
    $v = $minorR * [Math]::Sin($theta)

    $x = $u * $cosR - $v * $sinR
    $y = -($u * $sinR + $v * $cosR)
    return @{ X = $x; Y = $y; Depth = [Math]::Sin($theta) }
}

# Draw 4 Panels in a 2x2 grid
$cardW = 575
$cardH = 260
$startX = 25
$startY = 75
$gapX = 40
$gapY = 25

for ($idx = 0; $idx -lt $configs.Count; $idx++) {
    $cfg = $configs[$idx]
    $row = [Math]::Floor($idx / 2)
    $col = $idx % 2

    $cardX = $startX + ($col * ($cardW + $gapX))
    $cardY = $startY + ($row * ($cardH + $gapY))

    # Card Background Panel
    $cardBg = New-Object System.Drawing.SolidBrush((Clr 240 22 25 36))
    $cardBorder = New-Object System.Drawing.Pen((Clr 180 45 50 70), [float]1.2)
    $gfx.FillRectangle($cardBg, $cardX, $cardY, $cardW, $cardH)
    $gfx.DrawRectangle($cardBorder, $cardX, $cardY, $cardW, $cardH)
    $cardBg.Dispose()
    $cardBorder.Dispose()

    # Title & Subtitle
    $bTitleColor = New-Object System.Drawing.SolidBrush($cfg.BaseColor)
    $gfx.DrawString($cfg.Title, $fCardTitle, $bTitleColor, ($cardX + 16), ($cardY + 14))
    $gfx.DrawString($cfg.Subtitle, $fSub, $bGray, ($cardX + 16), ($cardY + 34))
    $bTitleColor.Dispose()

    # Center of Orbit in Card
    $centerX = $cardX + 175
    $centerY = $cardY + 145
    $majorR = 100.0
    $minorR = 48.0

    # Central Tile Placeholder
    $tileBrush = New-Object System.Drawing.SolidBrush((Clr 200 30 35 48))
    $tilePen = New-Object System.Drawing.Pen((Clr 100 80 90 120), [float]1.0)
    $gfx.FillRectangle($tileBrush, ($centerX - 30), ($centerY - 30), 60, 60)
    $gfx.DrawRectangle($tilePen, ($centerX - 30), ($centerY - 30), 60, 60)
    $tileBrush.Dispose()
    $tilePen.Dispose()

    # Draw Tower/Tile Symbol Icon in Center
    $centerIconBrush = New-Object System.Drawing.SolidBrush((Clr 120 $cfg.BaseColor.R $cfg.BaseColor.G $cfg.BaseColor.B))
    Fill-EllipseCentered $gfx $centerIconBrush $centerX $centerY 12 12
    $centerIconBrush.Dispose()

    # Draw Dual Orbits
    $orbitAngles = @([float]$cfg.OrbitAngle1, [float]$cfg.OrbitAngle2)
    $phases = @(0.0, ([Math]::PI * 0.5))

    for ($orbIdx = 0; $orbIdx -lt 2; $orbIdx++) {
        $rotAngle = $orbitAngles[$orbIdx]
        $phase = $phases[$orbIdx]

        # 1) Draw Track Line (Elliptical Path)
        $trackPoints = @()
        for ($s = 0; $s -le 72; $s++) {
            $t = ($s / 72.0) * [Math]::PI * 2.0
            $pt = Get-OrbitPoint $t $majorR $minorR $rotAngle
            $trackPoints += Pt ($centerX + $pt.X) ($centerY + $pt.Y)
        }
        $trackPen = New-Object System.Drawing.Pen((Clr 60 $cfg.BaseColor.R $cfg.BaseColor.G $cfg.BaseColor.B), [float]1.2)
        $trackPen.DashStyle = [System.Drawing.Drawing2D.DashStyle]::Dash
        $gfx.DrawLines($trackPen, $trackPoints)
        $trackPen.Dispose()

        # 2) Satellite Position & Trail
        $currentSatelliteAngle = 1.35 + $phase
        $satPt = Get-OrbitPoint $currentSatelliteAngle $majorR $minorR $rotAngle

        # Depth-dependent scale: 1.25x (front) to 0.75x (back)
        $depth = $satPt.Depth
        $scale = if ($depth -ge 0) { 0.95 + (1.25 - 0.95) * $depth } else { 0.95 - (0.95 - 0.75) * (-$depth) }
        $orbRadius = 9.0 * $scale

        # Draw Trail Ribbons behind satellite (approx 1/4 ellipse backwards)
        $trailSegments = 28
        $trailSpanRad = 1.35 # ~77 degrees
        $trailPoints = @()

        for ($step = $trailSegments; $step -ge 0; $step--) {
            $ratio = $step / [float]$trailSegments # 0 at head, 1 at tail
            $trailAngle = $currentSatelliteAngle - ($ratio * $trailSpanRad)
            $tPt = Get-OrbitPoint $trailAngle $majorR $minorR $rotAngle
            $trailPoints += @{ X = ($centerX + $tPt.X); Y = ($centerY + $tPt.Y); Ratio = $ratio }
        }

        # Draw smooth tapered trail ribbon
        for ($s = 0; $s -lt $trailPoints.Count - 1; $s++) {
            $p1 = $trailPoints[$s]
            $p2 = $trailPoints[$s + 1]
            $rMid = ($p1.Ratio + $p2.Ratio) * 0.5

            # Alpha and color interpolation
            $alpha = [int](230 * (1.0 - $rMid) * (1.0 - $rMid))
            $colorR = [int]($cfg.TrailHead.R + ($cfg.TrailTail.R - $cfg.TrailHead.R) * $rMid)
            $colorG = [int]($cfg.TrailHead.G + ($cfg.TrailTail.G - $cfg.TrailHead.G) * $rMid)
            $colorB = [int]($cfg.TrailHead.B + ($cfg.TrailTail.B - $cfg.TrailHead.B) * $rMid)

            # Tapered width (front is thicker)
            $tWidth = [float]((6.0 * (1.0 - $rMid) + 0.8) * $scale)

            $segPen = New-Object System.Drawing.Pen((Clr $alpha $colorR $colorG $colorB), $tWidth)
            $segPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $segPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $gfx.DrawLine($segPen, [float]$p1.X, [float]$p1.Y, [float]$p2.X, [float]$p2.Y)
            $segPen.Dispose()
        }

        # Draw Particles along the trail
        $rnd = New-Object System.Random(($idx * 100 + $orbIdx * 20))
        for ($p = 0; $p -lt 8; $p++) {
            $pRatio = ($p + 0.5) / 8.0
            $pAngle = $currentSatelliteAngle - ($pRatio * $trailSpanRad)
            $ptBase = Get-OrbitPoint $pAngle $majorR $minorR $rotAngle
            $offsetJitterX = ($rnd.NextDouble() - 0.5) * 8.0 * (1.0 + $pRatio)
            $offsetJitterY = ($rnd.NextDouble() - 0.5) * 8.0 * (1.0 + $pRatio)
            $pAlpha = [int](220 * (1.0 - $pRatio))
            $pSize = (4.0 * (1.0 - $pRatio) + 1.2) * $scale

            $pBrush = New-Object System.Drawing.SolidBrush((Clr $pAlpha $cfg.PartColor.R $cfg.PartColor.G $cfg.PartColor.B))
            Fill-EllipseCentered $gfx $pBrush ($centerX + $ptBase.X + $offsetJitterX) ($centerY + $ptBase.Y + $offsetJitterY) ($pSize * 0.5) ($pSize * 0.5)
            $pBrush.Dispose()
        }

        # Draw Satellite Orb (Front Glow + Core)
        $glowAlpha = if ($depth -ge 0) { 180 } else { 90 }
        $glowBrush = New-Object System.Drawing.SolidBrush((Clr $glowAlpha $cfg.BaseColor.R $cfg.BaseColor.G $cfg.BaseColor.B))
        Fill-EllipseCentered $gfx $glowBrush ($centerX + $satPt.X) ($centerY + $satPt.Y) ($orbRadius * 1.8) ($orbRadius * 1.8)
        $glowBrush.Dispose()

        # White Core
        $coreColor = if ($depth -ge 0) { (Clr 255 255 255 255) } else { (Clr 220 220 230 240) }
        $coreBrush = New-Object System.Drawing.SolidBrush($coreColor)
        Fill-EllipseCentered $gfx $coreBrush ($centerX + $satPt.X) ($centerY + $satPt.Y) $orbRadius $orbRadius
        $coreBrush.Dispose()
    }

    # Side Feature Panel inside Card
    $infoX = $cardX + 355
    $infoY = $cardY + 68
    $bLightText = New-Object System.Drawing.SolidBrush((Clr 255 220 225 235))
    $bGreen = New-Object System.Drawing.SolidBrush((Clr 255 52 211 153))

    $gfx.DrawString([string]::Format("{0} TrailRenderer Settings", [char]0x25C6), $fFeature, $bCyan, $infoX, $infoY)
    $gfx.DrawString("- Lifetime: 0.35s (1/4 Orbit Tracking)", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 17))
    $gfx.DrawString("- Width: 0.22 -> 0.0 Tapered Ribbon", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 31))
    $gfx.DrawString("- 3D Depth Width: 1.25x Front ~ 0.75x Back", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 45))

    $gfx.DrawString([string]::Format("{0} ParticleSystem Sparkles", [char]0x25C6), $fFeature, $bOrange, $infoX, ($infoY + 65))
    $gfx.DrawString("- Rate: 12/s, Lifetime 0.3s (World Space)", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 82))
    $gfx.DrawString("- Size: 0.06 Glowing Elemental Motes", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 96))

    $gfx.DrawString([string]::Format("{0} Safe Lifecycle Management", [char]0x25C6), $fFeature, $bGreen, $infoX, ($infoY + 116))
    $gfx.DrawString("- Drag / Snap Auto ClearAllTrails()", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 133))
    $gfx.DrawString("- OnEnable / Awake Clean Initial Frame", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 147))
    $gfx.DrawString("- SetColor() Realtime Gradient Sync", $fCardDesc, $bLightText, ($infoX + 6), ($infoY + 161))

    $bLightText.Dispose()
    $bGreen.Dispose()
}

# 5. Bottom Summary Banner
$bottomY = $bannerH - 70
$bBottomBg = New-Object System.Drawing.SolidBrush((Clr 240 20 22 30))
$pBottomBorder = New-Object System.Drawing.Pen((Clr 150 56 189 248), [float]1.0)
$gfx.FillRectangle($bBottomBg, 25, $bottomY, ($bannerW - 50), 54)
$gfx.DrawRectangle($pBottomBorder, 25, $bottomY, ($bannerW - 50), 54)
$bBottomBg.Dispose()
$pBottomBorder.Dispose()

$fBottom = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$bBottomText = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$bKey = New-Object System.Drawing.SolidBrush((Clr 255 253 224 71))

$gfx.DrawString("UNITY INTEGRATION & AUTOMATION", $fBottom, $bKey, 42, ($bottomY + 10))
$gfx.DrawString("Tools > Setup Satellite Trails (MapBuff & DebuffZone) menu provided | Prefab auto-baking + Runtime fallback creation", $fSub, $bBottomText, 42, ($bottomY + 28))

$bBottomText.Dispose()
$bKey.Dispose()
$fTitle.Dispose(); $fSub.Dispose(); $fCardTitle.Dispose(); $fCardDesc.Dispose(); $fFeature.Dispose(); $fBottom.Dispose()
$bWhite.Dispose(); $bGray.Dispose(); $bCyan.Dispose(); $bOrange.Dispose()

# Save image to project and artifact directory
$outPathProject = Join-Path $projectRoot "preview_buff_satellite_trails.png"
$outPathArtifact = Join-Path $artifactDir "preview_buff_satellite_trails.png"

$bmp.Save($outPathProject, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Save($outPathArtifact, [System.Drawing.Imaging.ImageFormat]::Png)
$gfx.Dispose()
$bmp.Dispose()

Write-Host "Successfully generated Buff & Debuff satellite trails showcase banner:"
Write-Host "  Project:  $outPathProject"
Write-Host "  Artifact: $outPathArtifact"
