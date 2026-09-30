param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

# Rebuild only the Tilemap data in the editor Tile Palette prefab. The old
# red/pink cells referred to deleted Tile/Sprite GUIDs; valid assets are kept.
$tileRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround/Tiles'
$palettePath = Join-Path $tileRoot 'Tiles.prefab'

$items = @(
    @{ Name='Board';  X=-4; Y=3;  Path='BoardFrame/BoardFrame_RuleTile.asset'; Flags=1073741826 },
    @{ Name='Spawn';  X=-3; Y=3;  Path='TowerSpawn/Tile_TowerSpawn_Asgard.asset'; Flags=1073741825 },
    @{ Name='Path';   X=-2; Y=3;  Path='Path/Tile_Path_Asgard.asset'; Flags=1073741825 },
    @{ Name='NW';     X=-4; Y=1;  Path='PathOverlay/Tile_Path_Overlay_NW.asset'; Flags=1073741825 },
    @{ Name='N';      X=-3; Y=1;  Path='PathOverlay/Tile_Path_Overlay_N.asset'; Flags=1073741825 },
    @{ Name='NE';     X=-2; Y=1;  Path='PathOverlay/Tile_Path_Overlay_NE.asset'; Flags=1073741825 },
    @{ Name='W';      X=-4; Y=0;  Path='PathOverlay/Tile_Path_Overlay_W.asset'; Flags=1073741825 },
    @{ Name='E';      X=-2; Y=0;  Path='PathOverlay/Tile_Path_Overlay_E.asset'; Flags=1073741825 },
    @{ Name='SW';     X=-4; Y=-1; Path='PathOverlay/Tile_Path_Overlay_SW.asset'; Flags=1073741825 },
    @{ Name='S';      X=-3; Y=-1; Path='PathOverlay/Tile_Path_Overlay_S.asset'; Flags=1073741825 },
    @{ Name='SE';     X=-2; Y=-1; Path='PathOverlay/Tile_Path_Overlay_SE.asset'; Flags=1073741825 },
    @{ Name='START';  X=-4; Y=-3; Path='PathOverlay/Tile_Path_Overlay_START.asset'; Flags=1073741825 },
    @{ Name='END';    X=-3; Y=-3; Path='PathOverlay/Tile_Path_Overlay_END.asset'; Flags=1073741825 },
    @{ Name='RETURN'; X=-2; Y=-3; Path='PathOverlay/Tile_Path_Overlay_RETURN.asset'; Flags=1073741825 },
    @{ Name='JUMP';   X=-1; Y=-3; Path='PathOverlay/Tile_Path_Overlay_JUMP.asset'; Flags=1073741825 }
)

$source = [System.IO.File]::ReadAllText($palettePath)
$newline = if ($source.Contains("`r`n")) { "`r`n" } else { "`n" }
$tileStart = $source.IndexOf('  m_Tiles:')
$matrixStart = $source.IndexOf('  m_TileMatrixArray:', $tileStart)
if ($tileStart -lt 0 -or $matrixStart -lt 0) { throw 'Tile Palette serialization format has changed.' }

$builder = [System.Text.StringBuilder]::new()
[void]$builder.Append("  m_Tiles:$newline")
for ($i = 0; $i -lt $items.Count; $i++) {
    $item = $items[$i]
    $x = $item.X; $y = $item.Y; $flags = $item.Flags
    [void]$builder.Append(@"
  - first: {x: $x, y: $y, z: 0}
    second:
      serializedVersion: 2
      m_TileIndex: $i
      m_TileSpriteIndex: $i
      m_TileMatrixIndex: 0
      m_TileColorIndex: 0
      m_TileObjectToInstantiateIndex: 65535
      dummyAlignment: 0
      m_AllTileFlags: $flags

"@.Replace("`n", $newline))
}
[void]$builder.Append("  m_AnimatedTiles: {}$newline  m_TileAssetArray:$newline")
foreach ($item in $items) {
    $assetPath = Join-Path $tileRoot $item.Path
    if (-not (Test-Path -LiteralPath $assetPath) -or -not (Test-Path -LiteralPath "$assetPath.meta")) {
        throw "Missing palette Tile asset: $assetPath"
    }
    $meta = [System.IO.File]::ReadAllText("$assetPath.meta")
    $guid = [regex]::Match($meta, '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
    if (-not $guid) { throw "Missing Tile GUID: $assetPath" }
    $item.AssetGuid = $guid
    [void]$builder.Append("  - m_RefCount: 1$newline    m_Data: {fileID: 11400000, guid: $guid, type: 2}$newline")
}
[void]$builder.Append("  m_TileSpriteArray:$newline")
foreach ($item in $items) {
    $assetPath = Join-Path $tileRoot $item.Path
    $assetText = [System.IO.File]::ReadAllText($assetPath)
    $sprite = if ($item.Name -eq 'Board') {
        [regex]::Match($assetText, 'm_DefaultSprite: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}')
    } else {
        [regex]::Match($assetText, 'm_Sprite: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: 3\}')
    }
    if (-not $sprite.Success) { throw "Missing sprite reference: $assetPath" }
    $id = $sprite.Groups[1].Value
    $guid = $sprite.Groups[2].Value
    [void]$builder.Append("  - m_RefCount: 1$newline    m_Data: {fileID: $id, guid: $guid, type: 3}$newline")
}

$result = $source.Substring(0, $tileStart) + $builder.ToString() + $source.Substring($matrixStart)
$result = [regex]::Replace($result, '(?m)(^  m_TileMatrixArray:\r?\n  - m_RefCount: )\d+', '${1}' + $items.Count)
$result = [regex]::Replace($result, '(?m)(^  m_TileColorArray:\r?\n  - m_RefCount: )\d+', '${1}' + $items.Count)
$result = [regex]::Replace($result, '(?m)^  m_Origin: \{.*\}$', '  m_Origin: {x: -4, y: -3, z: 0}')
$result = [regex]::Replace($result, '(?m)^  m_Size: \{.*\}$', '  m_Size: {x: 4, y: 7, z: 1}')
[System.IO.File]::WriteAllText($palettePath, $result, [System.Text.UTF8Encoding]::new($false))
