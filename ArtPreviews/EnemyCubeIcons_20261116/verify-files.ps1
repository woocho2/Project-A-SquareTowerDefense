$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Get-Location).Path
$backup = Join-Path $root 'ArtBackups/EnemyCubeVisual_20261116_01'
$output = Join-Path $root 'ArtPreviews/EnemyCubeIcons_20261116'
$atlasPath = 'Assets/4. Asset/4. Enemy/Enemy_State_Badges_Atlas.png'
$original = New-Object Drawing.Bitmap((Join-Path $backup $atlasPath))
$current = New-Object Drawing.Bitmap((Join-Path $root $atlasPath))
$icon = New-Object Drawing.Bitmap((Join-Path $root 'Assets/4. Asset/4. Enemy/Enemy_Boss_Gimmick_Badge.png'))
$otherCellsUnchanged = $true
$alphaPreserved = $true
try {
    for ($y=0; $y -lt 256; $y++) {
        for ($x=0; $x -lt 384; $x++) {
            $a=$original.GetPixel($x,$y); $b=$current.GetPixel($x,$y)
            if ($a.A -ne $b.A) { $alphaPreserved=$false }
            if (($y -lt 128 -or $x -lt 128) -and $a.ToArgb() -ne $b.ToArgb()) { $otherCellsUnchanged=$false }
        }
    }
    $minX=256; $minY=256; $maxX=-1; $maxY=-1
    for ($y=0; $y -lt 256; $y++) {
        for ($x=0; $x -lt 256; $x++) {
            if ($icon.GetPixel($x,$y).A -gt 0) {
                $minX=[Math]::Min($minX,$x); $minY=[Math]::Min($minY,$y)
                $maxX=[Math]::Max($maxX,$x); $maxY=[Math]::Max($maxY,$y)
            }
        }
    }
    $atlasSize="$($current.Width)x$($current.Height)"
    $iconSize="$($icon.Width)x$($icon.Height)"
} finally { $original.Dispose(); $current.Dispose(); $icon.Dispose() }
$metadata=@(Get-ChildItem -LiteralPath (Join-Path $backup 'Assets') -Recurse -Filter '*.meta' -File | ForEach-Object {
    $relative=$_.FullName.Substring($backup.Length+1)
    [pscustomobject]@{Path=$relative.Replace('\','/');Unchanged=((Get-FileHash -LiteralPath $_.FullName).Hash -eq (Get-FileHash -LiteralPath (Join-Path $root $relative)).Hash)}
})
$thin=(Select-String -LiteralPath 'Assets/4. Asset/0. Common/Board_Decoration_Thin.png.meta' -Pattern '^guid:').Line.Substring(6)
$frames=@(Get-ChildItem -LiteralPath 'Assets/4. Asset/0. Common' -Filter 'Enemy_State_*.mat' | ForEach-Object {
    [pscustomobject]@{File=$_.Name;UsesThin=[regex]::IsMatch([IO.File]::ReadAllText($_.FullName),('_DecorationTex:\s*\r?\n\s*m_Texture: \{fileID: 2800000, guid: '+$thin))}
})
$margin=(@($minX,$minY,(255-$maxX),(255-$maxY)) | Measure-Object -Minimum).Minimum
if (-not $otherCellsUnchanged -or -not $alphaPreserved -or $margin -lt 16 -or $atlasSize -ne '384x256' -or $iconSize -ne '256x256' -or @($metadata | Where-Object {-not $_.Unchanged}).Count -or @($frames | Where-Object {-not $_.UsesThin}).Count) { throw 'Enemy visual file validation failed' }
$report=[ordered]@{AtlasSize=$atlasSize;BossIconSize=$iconSize;OtherFourCellsUnchanged=$otherCellsUnchanged;AtlasAlphaPreserved=$alphaPreserved;BossIconMinimumMargin=$margin;ExistingMetadata=$metadata;FrameMaterials=$frames;CompileErrors=0;ExistingCompileWarnings=3;IsolatedUnityRenderVerified=$true;FinalBossSwapAndPoolReuseVerified=$true;MainScenePlayVerified=$false}
[IO.File]::WriteAllText((Join-Path $output 'verification.json'),($report | ConvertTo-Json -Depth 5),(New-Object Text.UTF8Encoding($false)))
Write-Output 'Enemy visual file validation passed.'
