$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$backup = Join-Path $root 'ArtBackups/EmblemAtlas_20261113_a50a6a83'
$restoreBackup = Join-Path $backup 'before-apply'
$manifest = Get-Content -LiteralPath (Join-Path $restoreBackup 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($entry in $manifest.Files) {
    if ((Get-FileHash -LiteralPath (Join-Path $restoreBackup $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "Backup hash mismatch: $($entry.Path)"
    }
    if ($entry.Path -ne 'Assets/1. Scenes/2. Menu.unity' -and (Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "File changed since backup; preserve newer changes before applying: $($entry.Path)"
    }
}

Add-Type -AssemblyName System.Drawing
$prepared = Join-Path $backup 'prepared-atlas.png'
$bmp = New-Object System.Drawing.Bitmap($prepared)
$cells = @()
try {
    if ($bmp.Width -ne 1024 -or $bmp.Height -ne 1024) { throw 'Wrong atlas dimensions' }
    $nonWhite = 0
    $partialAlpha = 0
    for ($cell = 0; $cell -lt 16; $cell++) {
        $left = ($cell % 4) * 256
        $top = [int][Math]::Floor($cell / 4) * 256
        $minX = 256; $minY = 256; $maxX = -1; $maxY = -1; $visible = 0
        for ($y = 0; $y -lt 256; $y++) {
            for ($x = 0; $x -lt 256; $x++) {
                $c = $bmp.GetPixel($left + $x, $top + $y)
                if ($c.A -eq 0) { continue }
                $visible++
                if ($c.R -ne 255 -or $c.G -ne 255 -or $c.B -ne 255) { $nonWhite++ }
                if ($c.A -lt 255) { $partialAlpha++ }
                $minX = [Math]::Min($minX, $x); $minY = [Math]::Min($minY, $y)
                $maxX = [Math]::Max($maxX, $x); $maxY = [Math]::Max($maxY, $y)
            }
        }
        if ($cell -lt 14) {
            if ($visible -eq 0) { throw "Empty emblem cell $cell" }
            $margins = @($minX, $minY, (255 - $maxX), (255 - $maxY))
            if (($margins | Measure-Object -Minimum).Minimum -lt 16) { throw "Insufficient margin: cell $cell" }
        } elseif ($visible -ne 0) { throw "Nonempty unused cell $cell" }
        $cells += [pscustomobject]@{ Cell = $cell; VisiblePixels = $visible; Bounds = @($minX, $minY, $maxX, $maxY) }
    }
    if ($nonWhite -ne 0) { throw "Non-white visible pixels: $nonWhite" }
    $lightCenterAlpha = $bmp.GetPixel(896, 640).A
    if ($lightCenterAlpha -ne 0) { throw 'Light center is not transparent' }
} finally { $bmp.Dispose() }

$guid = '7e3f1605aca04ab5b6e525a542b6b7b3'
$connections = @(
    [pscustomobject]@{ Name = 'Shield'; Component = '21115172'; GameObject = '21115170'; Sprite = 1003 },
    [pscustomobject]@{ Name = 'Spear'; Component = '104574551'; GameObject = '104574549'; Sprite = 1004 },
    [pscustomobject]@{ Name = 'Bow'; Component = '322162967'; GameObject = '322162965'; Sprite = 1002 },
    [pscustomobject]@{ Name = 'Hammer'; Component = '433615421'; GameObject = '433615419'; Sprite = 1006 },
    [pscustomobject]@{ Name = 'Sword'; Component = '599458364'; GameObject = '599458362'; Sprite = 1001 },
    [pscustomobject]@{ Name = 'Fire'; Component = '759242121'; GameObject = '759242119'; Sprite = 1007 },
    [pscustomobject]@{ Name = 'Electricity'; Component = '769075878'; GameObject = '769075876'; Sprite = 1009 },
    [pscustomobject]@{ Name = 'Axe'; Component = '1022370374'; GameObject = '1022370372'; Sprite = 1005 },
    [pscustomobject]@{ Name = 'Ice'; Component = '1268042068'; GameObject = '1268042066'; Sprite = 1008 },
    [pscustomobject]@{ Name = 'Darkness'; Component = '1389912765'; GameObject = '1389912763'; Sprite = 1013 },
    [pscustomobject]@{ Name = 'Light'; Component = '1395854677'; GameObject = '1395854675'; Sprite = 1012 },
    [pscustomobject]@{ Name = 'Wind'; Component = '1764517426'; GameObject = '1764517424'; Sprite = 1010 },
    [pscustomobject]@{ Name = 'Water'; Component = '1823250735'; GameObject = '1823250733'; Sprite = 1014 },
    [pscustomobject]@{ Name = 'Earth'; Component = '1967955380'; GameObject = '1967955378'; Sprite = 1011 }
)
$scenePath = Join-Path $root 'Assets/1. Scenes/2. Menu.unity'
$sceneBackupName = 'menu-before-final-apply-' + [DateTime]::UtcNow.Ticks + '.unity'
$sceneBackupPath = Join-Path $backup $sceneBackupName
Copy-Item -LiteralPath $scenePath -Destination $sceneBackupPath
$sceneExpectedHash = (Get-FileHash -LiteralPath $sceneBackupPath -Algorithm SHA256).Hash
if ((Get-FileHash -LiteralPath $scenePath -Algorithm SHA256).Hash -ne $sceneExpectedHash) { throw 'Scene changed while being backed up' }
$sceneBefore = [IO.File]::ReadAllText($scenePath)
$sceneAfter = $sceneBefore
$spritePattern = 'm_Sprite: \{fileID: \d+, guid: ' + $guid + ', type: 3\}'
$changed = 0
foreach ($link in $connections) {
    $go = [regex]::Match($sceneAfter, '(?ms)^--- !u!1 &' + $link.GameObject + '\r?\n.*?(?=^--- !u!|\z)')
    if (-not $go.Success -or $go.Value -notmatch ('m_Name: Img_Emblem_' + $link.Name + '\r?\n')) { throw "Wrong named GameObject: $($link.Name)" }
    $componentPattern = '(?ms)^--- !u!114 &' + $link.Component + '\r?\n.*?(?=^--- !u!|\z)'
    $matches = [regex]::Matches($sceneAfter, $componentPattern)
    if ($matches.Count -ne 1) { throw "Missing or ambiguous Image: $($link.Name)" }
    $doc = $matches[0]
    if ($doc.Value -notmatch ('m_GameObject: \{fileID: ' + $link.GameObject + '\}')) { throw 'Wrong component owner' }
    if ([regex]::Matches($doc.Value, $spritePattern).Count -ne 1) { throw 'Missing atlas reference' }
    $expected = 'm_Sprite: {fileID: ' + $link.Sprite + ', guid: ' + $guid + ', type: 3}'
    $newDoc = [regex]::Replace($doc.Value, $spritePattern, $expected)
    if ($newDoc -cne $doc.Value) { $changed++ }
    $sceneAfter = $sceneAfter.Substring(0, $doc.Index) + $newDoc + $sceneAfter.Substring($doc.Index + $doc.Length)
}
if ($changed -ne 13) { throw "Unexpected number of repaired references: $changed" }
if ([regex]::Replace($sceneAfter, $spritePattern, 'SPRITE') -cne [regex]::Replace($sceneBefore, $spritePattern, 'SPRITE')) { throw 'Unexpected non-sprite scene edits' }

$iconsPath = Join-Path $root 'Assets/Resources/TowerIcons.asset'
$icons = [IO.File]::ReadAllText($iconsPath)
$iconIDs = @([regex]::Matches($icons, 'fileID: (\d+), guid: ' + $guid) | ForEach-Object { [int]$_.Groups[1].Value })
$expectedIDs = @(1001, 1005, 1002, 1004, 1003, 1006, 1007, 1014, 1011, 1010, 1008, 1009, 1012, 1013)
if (($iconIDs -join ',') -ne ($expectedIDs -join ',')) { throw 'TowerIcons emblem order mismatch' }

# Only these two project files are written. Preserve the scene's original BOM and newline style.
$sceneBytes = [IO.File]::ReadAllBytes($scenePath)
$hasBom = $sceneBytes.Length -ge 3 -and $sceneBytes[0] -eq 239 -and $sceneBytes[1] -eq 187 -and $sceneBytes[2] -eq 191
$encoding = New-Object Text.UTF8Encoding($hasBom)
if ((Get-FileHash -LiteralPath $scenePath -Algorithm SHA256).Hash -ne $sceneExpectedHash) { throw 'Scene changed during preparation; rerun with a fresh backup' }
Copy-Item -LiteralPath $prepared -Destination (Join-Path $root 'Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png') -Force
[IO.File]::WriteAllText($scenePath, $sceneAfter, $encoding)

foreach ($entry in $manifest.Files) {
    if ($entry.Path -eq 'Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png' -or $entry.Path -eq 'Assets/1. Scenes/2. Menu.unity') { continue }
    if ((Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Unexpected change: $($entry.Path)" }
}
if ([IO.File]::ReadAllText($scenePath) -cne $sceneAfter) { throw 'Scene write verification failed' }
if ((Get-FileHash -LiteralPath $prepared).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png')).Hash) { throw 'Atlas copy verification failed' }

$report = [ordered]@{
    AtlasSize = @(1024, 1024)
    AtlasGuid = $guid
    MetaByteIdenticalToBackup = $true
    SpriteCount = 14
    AllVisibleRGBWhite = $true
    AntialiasPixels = $partialAlpha
    MinimumTransparentMargin = 16
    LightCenterAlpha = $lightCenterAlpha
    Cells = $cells
    RepairedMenuReferences = $changed
    MenuChangesLimitedToSpriteReferences = $true
    MenuBeforeApplyBackup = $sceneBackupName
    MenuBeforeApplySHA256 = $sceneExpectedHash
    MenuConnections = $connections
    TowerIcons14ReferencesCorrectAndUnchanged = $true
    UnityPlayTested = $false
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'file-verification.json') -Encoding UTF8
Write-Output ('Applied: 1024x1024 atlas, preserved meta, repaired ' + $changed + ' Menu references. File validation passed; Unity Play not tested.')
