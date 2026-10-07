param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$assetRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround'
$backupRoot = Join-Path $ProjectRoot 'outputs/realm-tile-layers/frame-upgrade-backup'

function Save-Backup([string]$path) {
    $relative = $path.Substring($ProjectRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $backupRoot $relative
    if (-not (Test-Path -LiteralPath $destination)) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
        Copy-Item -LiteralPath $path -Destination $destination
    }
}
function Write-Changed([string]$path, [string]$contents) {
    if ([System.IO.File]::ReadAllText($path) -ne $contents) {
        Save-Backup $path
        [System.IO.File]::WriteAllText($path, $contents, $utf8)
    }
}
function Section([string]$text, [string]$name) {
    [regex]::Match($text, '(?ms)^  ' + $name + ':\r?\n(.*?)(?=^  \w|\z)').Groups[1].Value
}
function Replace-Section([string]$text, [string]$name, [string]$body) {
    $newline = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $pattern = '(?ms)^  ' + $name + ':(?:\r?\n.*?|[^\r\n]*\r?\n)(?=^  \w|\z)'
    $replacement = "  ${name}:$newline$body"
    [regex]::Replace($text, $pattern, { param($match) $replacement })
}

# Reuse the JUMP sprite IDs and Tile GUID so existing palette references survive.
foreach ($meta in (Get-ChildItem -LiteralPath $assetRoot -Filter 'Tile_Path_Overlay_*_Atlas.png.meta')) {
    $contents = [System.IO.File]::ReadAllText($meta.FullName)
    Write-Changed $meta.FullName ($contents.Replace('_JUMP', '_FRAME'))
}
$oldAsset = Join-Path $assetRoot 'Tiles/PathOverlay/Tile_Path_Overlay_JUMP.asset'
$frameAsset = Join-Path $assetRoot 'Tiles/PathOverlay/Tile_Path_Overlay_FRAME.asset'
if (Test-Path -LiteralPath $oldAsset) {
    Save-Backup $oldAsset
    Save-Backup "$oldAsset.meta"
    Write-Changed $oldAsset ([System.IO.File]::ReadAllText($oldAsset).Replace('_JUMP', '_FRAME'))
    Move-Item -LiteralPath $oldAsset -Destination $frameAsset
    Move-Item -LiteralPath "$oldAsset.meta" -Destination "$frameAsset.meta"
}
if (-not (Test-Path -LiteralPath $frameAsset)) { throw 'Missing empty frame Tile asset.' }
$frameText = [System.IO.File]::ReadAllText($frameAsset)
$frameReference = [regex]::Match($frameText, 'm_Sprite: (\{.*\})').Groups[1].Value
$tileGuid = [regex]::Match([System.IO.File]::ReadAllText("$frameAsset.meta"), '(?m)^guid: (\w+)').Groups[1].Value

# Realm-specific Tiles let future stages choose a frame without changing the
# shared Asgard palette Tile or affecting another stage's decoration.
foreach ($realm in @('Alfheim','Vanaheim','Midgard','Jotunheim','Nidavellir','Niflheim','Muspelheim','Hel','Ragnarok')) {
    $atlasMeta = [System.IO.File]::ReadAllText((Join-Path $assetRoot "Tile_Path_Overlay_${realm}_Atlas.png.meta"))
    $atlasGuid = [regex]::Match($atlasMeta, '(?m)^guid: (\w+)').Groups[1].Value
    $name = "Tile_Path_Overlay_${realm}_FRAME"
    $path = Join-Path $assetRoot "Tiles/PathOverlay/${name}.asset"
    $contents = $frameText.Replace('m_Name: Tile_Path_Overlay_FRAME', "m_Name: $name")
    $contents = [regex]::Replace($contents, 'm_Sprite: \{.*\}', "m_Sprite: {fileID: 1011, guid: $atlasGuid, type: 3}")
    if (Test-Path -LiteralPath $path) { Write-Changed $path $contents }
    else { [System.IO.File]::WriteAllText($path, $contents, $utf8) }
    if (-not (Test-Path -LiteralPath "$path.meta")) {
        $meta = [System.IO.File]::ReadAllText("$frameAsset.meta")
        $meta = $meta.Replace($tileGuid, [guid]::NewGuid().ToString('N'))
        [System.IO.File]::WriteAllText("$path.meta", $meta, $utf8)
    }
}

# Repurpose the unused, empty Path Special layer into a static spawn decoration
# layer. Gameplay occupancy still belongs exclusively to the original spawn map.
$scenePath = Join-Path $ProjectRoot 'Assets/1. Scenes/3. Stage1.unity'
$scene = [System.IO.File]::ReadAllText($scenePath)
$newline = if ($scene.Contains("`r`n")) { "`r`n" } else { "`n" }
$spawnPattern = '(?ms)^--- !u!1839735485 &1535280681\r?\n.*?(?=^--- |\z)'
$decorationPattern = '(?ms)^--- !u!1839735485 &1768091552\r?\n.*?(?=^--- |\z)'
$objectPattern = '(?ms)^--- !u!1 &1768091549\r?\n.*?(?=^--- |\z)'
$spawn = [regex]::Match($scene, $spawnPattern).Value
$decoration = [regex]::Match($scene, $decorationPattern).Value
$object = [regex]::Match($scene, $objectPattern).Value
if (-not $spawn -or -not $decoration -or -not $object) { throw 'Stage1 spawn/decoration layer IDs have changed.' }
if (-not $object.Contains('m_Name: Path Special') -and -not $object.Contains('m_Name: TowerSpawn Decoration')) {
    throw 'The former Path Special layer has another purpose; refusing to replace it.'
}
if ($object.Contains('m_Name: Path Special') -and -not $decoration.Contains('m_Tiles: []')) {
    throw 'Path Special is not empty; migrate its endpoint tiles first.'
}
$sourceCells = Section $spawn 'm_Tiles'
$count = [regex]::Matches($sourceCells, '(?m)^  - first:').Count
if ($count -eq 0) { throw 'No tower spawn cells to decorate.' }
$cells = [regex]::Replace($sourceCells, 'm_Tile(?:Index|SpriteIndex|MatrixIndex|ColorIndex): \d+', {
    param($match) [regex]::Replace($match.Value, '\d+$', '0')
})
$cells = [regex]::Replace($cells, 'm_AllTileFlags: \d+', 'm_AllTileFlags: 1')
$decoration = Replace-Section $decoration 'm_Tiles' $cells
$decoration = Replace-Section $decoration 'm_TileAssetArray' "  - m_RefCount: $count${newline}    m_Data: {fileID: 11400000, guid: $tileGuid, type: 2}${newline}"
$decoration = Replace-Section $decoration 'm_TileSpriteArray' "  - m_RefCount: $count${newline}    m_Data: $frameReference${newline}"
$matrix = [regex]::Match($frameText, '(?ms)^  m_Transform:\r?\n(.*?)(?=^  \w)').Groups[1].Value
$matrix = [regex]::Replace($matrix, '(?m)^    ', '      ')
$decoration = Replace-Section $decoration 'm_TileMatrixArray' "  - m_RefCount: $count${newline}    m_Data:$newline$matrix"
$decoration = Replace-Section $decoration 'm_TileColorArray' "  - m_RefCount: $count${newline}    m_Data: {r: 1, g: 1, b: 1, a: 1}${newline}"
$object = $object.Replace('m_Name: Path Special', 'm_Name: TowerSpawn Decoration').Replace('m_TagString: Path', 'm_TagString: Untagged')
$scene = [regex]::Replace($scene, $decorationPattern, { param($match) $decoration })
$scene = [regex]::Replace($scene, $objectPattern, { param($match) $object })
Write-Changed $scenePath $scene
Write-Output "Installed themed FRAME slot (1011); decorated $count tower spawn cells."
