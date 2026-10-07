param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$assetRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround'

# All frame sprites share a 256px footprint, including word plates.
foreach ($role in @('START', 'END', 'FRAME', 'RETURN')) {
    $path = Join-Path $assetRoot "Tiles/PathOverlay/Tile_Path_Overlay_${role}.asset"
    $text = [System.IO.File]::ReadAllText($path)
    $text = [regex]::Replace($text, '(?m)^(    e(?:00|11): )1\r?$', '${1}1.3333334')
    [System.IO.File]::WriteAllText($path, $text, $utf8)
}

function Get-Section([string]$text, [string]$name) {
    [regex]::Match($text, '(?ms)^  ' + $name + ':\r?\n(.*?)(?=^  \w|\z)').Groups[1].Value
}
function Set-Section([string]$text, [string]$name, [string]$body) {
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $pattern = '(?ms)^  ' + $name + ':(?:\r?\n.*?|[^\r\n]*\r?\n)(?=^  \w|\z)'
    $replacement = if ($body.Length -eq 0) { "  ${name}: []$newline" } else { "  ${name}:$newline$body" }
    [regex]::Replace($text, $pattern, { param($match) $replacement })
}
function Ref-Entries([string]$text, [string]$name) {
    @([regex]::Matches((Get-Section $text $name), '(?m)^    m_Data: (\{.*\})').ForEach({ $_.Groups[1].Value }))
}
function Counted-Refs([string[]]$refs, [int[]]$counts, [string]$newline) {
    $builder = [System.Text.StringBuilder]::new()
    for ($i = 0; $i -lt $refs.Length; $i++) {
        [void]$builder.Append("  - m_RefCount: $($counts[$i])${newline}    m_Data: $($refs[$i])${newline}")
    }
    $builder.ToString()
}

$scenePath = Join-Path $ProjectRoot 'Assets/1. Scenes/3. Stage1.unity'
$scene = [System.IO.File]::ReadAllText($scenePath)
$newline = if ($scene.Contains("`r`n")) { "`r`n" } else { "`n" }
$directionPattern = '(?ms)^--- !u!1839735485 &561276725\r?\n.*?(?=^--- |\z)'
$specialPattern = '(?ms)^--- !u!1839735485 &1768091552\r?\n.*?(?=^--- |\z)'
$direction = [regex]::Match($scene, $directionPattern).Value
$special = [regex]::Match($scene, $specialPattern).Value
if (-not $direction) { throw 'Stage1 path direction layer ID has changed.' }
$specialObject = [regex]::Match($scene, '(?ms)^--- !u!1 &1768091549\r?\n.*?(?=^--- |\z)').Value
# Once this formerly empty layer holds spawn decorations, it is not an endpoint
# source. Rebuilding must never merge its frames into the pulsing path layer.
$specialCells = if ($special -and $specialObject.Contains('m_Name: Path Special')) {
    @( [regex]::Matches((Get-Section $special 'm_Tiles'), '(?ms)^  - first: .*?(?=^  - first: |\z)') )
} else { @() }
if ($specialCells.Count -gt 0) {
    $assetRefs = Ref-Entries $direction 'm_TileAssetArray'
    $spriteRefs = Ref-Entries $direction 'm_TileSpriteArray'
    $specialAssets = Ref-Entries $special 'm_TileAssetArray'
    $specialSprites = Ref-Entries $special 'm_TileSpriteArray'
    $cells = Get-Section $direction 'm_Tiles'
    foreach ($match in $specialCells) {
        $cell = $match.Value
        $coordinate = [regex]::Match($cell, '^  - first: (\{.*\})').Groups[1].Value
        $assetIndex = [int][regex]::Match($cell, 'm_TileIndex: (\d+)').Groups[1].Value
        $spriteIndex = [int][regex]::Match($cell, 'm_TileSpriteIndex: (\d+)').Groups[1].Value
        $newAssetIndex = $assetRefs.Count
        $newSpriteIndex = $spriteRefs.Count
        $assetRefs += $specialAssets[$assetIndex]
        $spriteRefs += $specialSprites[$spriteIndex]
        $replacement = [regex]::Replace($cell, 'm_TileIndex: \d+', "m_TileIndex: $newAssetIndex")
        $replacement = [regex]::Replace($replacement, 'm_TileSpriteIndex: \d+', "m_TileSpriteIndex: $newSpriteIndex")
        $cellPattern = '(?ms)^  - first: ' + [regex]::Escape($coordinate) + '\r?\n.*?(?=^  - first: |\z)'
        if (-not [regex]::IsMatch($cells, $cellPattern)) { throw "Endpoint not on direction layer: $coordinate" }
        $cells = [regex]::Replace($cells, $cellPattern, { param($old) $replacement })
    }
    $assetCounts = New-Object int[] $assetRefs.Count
    $spriteCounts = New-Object int[] $spriteRefs.Count
    foreach ($match in [regex]::Matches($cells, 'm_TileIndex: (\d+)')) { $assetCounts[[int]$match.Groups[1].Value]++ }
    foreach ($match in [regex]::Matches($cells, 'm_TileSpriteIndex: (\d+)')) { $spriteCounts[[int]$match.Groups[1].Value]++ }
    $direction = Set-Section $direction 'm_Tiles' $cells
    $direction = Set-Section $direction 'm_TileAssetArray' (Counted-Refs $assetRefs $assetCounts $newline)
    $direction = Set-Section $direction 'm_TileSpriteArray' (Counted-Refs $spriteRefs $spriteCounts $newline)
    foreach ($name in @('m_Tiles', 'm_TileAssetArray', 'm_TileSpriteArray', 'm_TileMatrixArray', 'm_TileColorArray')) {
        $special = Set-Section $special $name ''
    }
    $scene = [regex]::Replace($scene, $directionPattern, { param($match) $direction })
    $scene = [regex]::Replace($scene, $specialPattern, { param($match) $special })
    [System.IO.File]::WriteAllText($scenePath, $scene, $utf8)
    Write-Output "Moved $($specialCells.Count) endpoint cells to the pulsing direction layer."
}

# Match the editor palette's cached matrices to the updated Tile assets.
$palettePath = Join-Path $assetRoot 'Tiles/Tiles.prefab'
$palette = [System.IO.File]::ReadAllText($palettePath)
$cells = Get-Section $palette 'm_Tiles'
$cells = [regex]::Replace($cells, '(?ms)^  - first: .*?(?=^  - first: |\z)', {
    param($match)
    $index = [int][regex]::Match($match.Value, 'm_TileIndex: (\d+)').Groups[1].Value
    if ($index -ge 11 -and $index -le 14) {
        [regex]::Replace($match.Value, 'm_TileMatrixIndex: \d+', 'm_TileMatrixIndex: 1')
    } else { $match.Value }
})
$palette = Set-Section $palette 'm_Tiles' $cells
$matrix = Get-Section $palette 'm_TileMatrixArray'
$matrixEntries = @([regex]::Matches($matrix, '(?ms)^  - m_RefCount: .*?(?=^  - m_RefCount: |\z)'))
for ($i = 0; $i -lt $matrixEntries.Count; $i++) {
    $count = [regex]::Matches($cells, "(?m)m_TileMatrixIndex: $i\r?$").Count
    $old = $matrixEntries[$i].Value
    $new = [regex]::Replace($old, 'm_RefCount: \d+', "m_RefCount: $count")
    $matrix = $matrix.Replace($old, $new)
}
$palette = Set-Section $palette 'm_TileMatrixArray' $matrix
[System.IO.File]::WriteAllText($palettePath, $palette, $utf8)
