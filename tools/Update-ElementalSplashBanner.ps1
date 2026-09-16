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

$elements = @(
    @{ ID = 109; Name = "전기 (Thunder Storm - Cloud & Lightning Strike)"; TexEff = "Effect_Electric_Splash_Thunder"; Detail = "먹구름(상단) + 내부 번쩍임 + 지면 강타 거대 벼락 + 지면 전격 충격파" },
    @{ ID = 110; Name = "바람 (Cyan Typhoon - Whirlwind & Wind Blades)";   TexEff = "Effect_Wind_Splash_Typhoon";    Detail = "지면 회오리 발아 + 3D 나선 태풍 깔때기 + 윈드 블레이드 회전 + 폭풍 충격파" },
    @{ ID = 111; Name = "대지 (Earth - Rising Spires & Shockwave)";        TexEff = "Effect_Earth_Splash_Quake";      Detail = "지면 균열 폭발 + 5개 거대 암석 기둥 솟구침 + 지면 충격파 & 파편" },
    @{ ID = 112; Name = "빛 (Light - Solar Flare & Corona Nova)";          TexEff = "Effect_Light_Splash_Supernova"; Detail = "태양 코로나 흑점 플레어 + 4방향 거대 광휘 다이아몬드 + 펄스 파동" },
    @{ ID = 113; Name = "어둠 (Darkness - Gargantua Black Hole)";          TexEff = "Effect_Dark_Splash_BlackHole";  Detail = "가르간튀아 블랙홀 + 후면 렌징 돔 & 전면 강착 리본 + 3개 궤도 암흑체" }
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
$fRowTitle = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$fFrameLbl = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Regular)
$fDesc = New-Object System.Drawing.Font("Malgun Gothic", 9, [System.Drawing.FontStyle]::Regular)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 240 245))
$bGray = New-Object System.Drawing.SolidBrush((Clr 255 170 175 190))
$bHighlight = New-Object System.Drawing.SolidBrush((Clr 255 255 220 100))

$gfx.DrawString("Elemental Splash Towers - Hit Splash Effects 5 Types (E109 ~ E113)", $fHeader, $bWhite, 20, 15)

for ($i = 0; $i -lt $elements.Count; $i++) {
    $elem = $elements[$i]
    $rowY = 55 + ($i * 142)

    $pBox = New-Object System.Drawing.Pen((Clr 255 45 50 68), 1.0)
    $gfx.DrawRectangle($pBox, 15, $rowY, 1290, 136)
    $pBox.Dispose()

    # Load sheet
    $sheetFile = "$projectRoot/Assets/4. DotAsset/6. Effect/$($elem.TexEff).png"
    if (-not (Test-Path $sheetFile)) {
        Write-Warning "File not found: $sheetFile"
        continue
    }
    $sheetImg = [System.Drawing.Image]::FromFile($sheetFile)

    # Element Label
    $gfx.DrawString("E$($elem.ID): $($elem.Name)", $fRowTitle, $bHighlight, 25, ($rowY + 8))

    # 7 frames (F0 ~ F6)
    for ($f = 0; $f -le 6; $f++) {
        $dx = 25 + ($f * 122)
        $dy = $rowY + 30
        $srcR = New-Object System.Drawing.Rectangle(($f * 128), 0, 128, 128)
        $dstR = New-Object System.Drawing.Rectangle($dx, $dy, 96, 96)

        $pFrameBox = New-Object System.Drawing.Pen((Clr 255 35 40 55), 1.0)
        $gfx.DrawRectangle($pFrameBox, $dx, $dy, 96, 96)
        $pFrameBox.Dispose()

        $gfx.DrawImage($sheetImg, $dstR, $srcR, [System.Drawing.GraphicsUnit]::Pixel)
        $gfx.DrawString("F$f", $fFrameLbl, $bGray, ($dx + 40), ($dy + 82))
    }

    # Zoom of Key Impact / Bloom Frame
    $keyF = if ($elem.ID -eq 109) { 1 } elseif ($elem.ID -eq 110) { 2 } elseif ($elem.ID -eq 111) { 2 } else { 2 }
    $srcZoom = New-Object System.Drawing.Rectangle(($keyF * 128), 0, 128, 128)
    $dstZoom = New-Object System.Drawing.Rectangle(900, ($rowY + 12), 120, 120)
    
    $pZoomBox = New-Object System.Drawing.Pen((Clr 255 70 78 105), 1.5)
    $gfx.DrawRectangle($pZoomBox, 900, ($rowY + 12), 120, 120)
    $pZoomBox.Dispose()

    $gfx.DrawImage($sheetImg, $dstZoom, $srcZoom, [System.Drawing.GraphicsUnit]::Pixel)
    $zoomLbl = if ($elem.ID -eq 109) { "F1 Strike Zoom" } elseif ($elem.ID -eq 110) { "F2 Typhoon Zoom" } elseif ($elem.ID -eq 111) { "F2 Spires Zoom" } elseif ($elem.ID -eq 113) { "F2 BlackHole Zoom" } else { "Peak Zoom" }
    $gfx.DrawString($zoomLbl, $fFrameLbl, $bWhite, 915, ($rowY + 115))

    # Zoom of Late Frame
    $keyF2 = if ($elem.ID -eq 109) { 2 } elseif ($elem.ID -eq 110) { 3 } elseif ($elem.ID -eq 111) { 3 } else { 3 }
    $srcZoom2 = New-Object System.Drawing.Rectangle(($keyF2 * 128), 0, 128, 128)
    $dstZoom2 = New-Object System.Drawing.Rectangle(1040, ($rowY + 12), 120, 120)

    $pZoomBox2 = New-Object System.Drawing.Pen((Clr 255 70 78 105), 1.5)
    $gfx.DrawRectangle($pZoomBox2, 1040, ($rowY + 12), 120, 120)
    $pZoomBox2.Dispose()

    $gfx.DrawImage($sheetImg, $dstZoom2, $srcZoom2, [System.Drawing.GraphicsUnit]::Pixel)
    $zoomLbl2 = if ($elem.ID -eq 109) { "F2 Shockwave Zoom" } elseif ($elem.ID -eq 110) { "F3 Peak Zoom" } elseif ($elem.ID -eq 111) { "F3 Peak Zoom" } elseif ($elem.ID -eq 113) { "F3 Inflow Zoom" } else { "Wave Zoom" }
    $gfx.DrawString($zoomLbl2, $fFrameLbl, $bWhite, 1055, ($rowY + 115))

    # Extra Detail Description
    $gfx.DrawString($elem.Detail, $fDesc, $bGray, 1170, ($rowY + 45))

    $sheetImg.Dispose()
}

$fDesc.Dispose()

$outPath1 = "$artifactDir/preview_elemental_splash_effects.png"
$outPath2 = "$projectRoot/preview_elemental_splash_effects.png"

$banner.Save($outPath1, [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item $outPath1 $outPath2 -Force

$gfx.Dispose()
$banner.Dispose()

Write-Host "Updated 5-Elemental Splash Effects Banner successfully!"
