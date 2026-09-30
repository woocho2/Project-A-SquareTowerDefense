param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

# Packs ten 3x3 board sets into one texture. Every slice remains 192px / PPU 128.
# RuleTile assets stay separate because each stage selects one realm's nine sprites.
Add-Type -AssemblyName System.Drawing

$assetRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround'
$ruleRoot = Join-Path $assetRoot 'Tiles/BoardFrame'
$atlasPath = Join-Path $assetRoot 'Tile_BoardFrame_All_Atlas.png'
$atlasGuid = '6c579fae2b1d423bb4578c410fa83520'
$oldAsgardGuid = 'b41f7a29e3014a5cb820f1883de4c71a'
$realms = @('Asgard','Vanaheim','Alfheim','Jotunheim','Midgard','Nidavellir','Niflheim','Hel','Muspelheim','Ragnarok')
$pieces = @('TL','T','TR','L','C','R','BL','B','BR')

function GetSpriteId([int]$realmIndex, [int]$pieceIndex) {
    return 4001 + $realmIndex * 9 + $pieceIndex
}

$bitmap = [System.Drawing.Bitmap]::new(2880, 1152, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
try {
    $graphics.Clear([System.Drawing.Color]::Transparent)
    for ($realmIndex = 0; $realmIndex -lt $realms.Count; $realmIndex++) {
        $realm = $realms[$realmIndex]
        $inputName = if ($realm -eq 'Asgard') { 'Tile_BoardFrame_Atlas.png' } else { "Tile_BoardFrame_${realm}_Atlas.png" }
        $inputPath = Join-Path $assetRoot $inputName
        $source = [System.Drawing.Bitmap]::new($inputPath)
        try {
            if ($source.Width -ne 576 -or $source.Height -ne 576) { throw "Unexpected board atlas dimensions: $inputPath" }
            $column = $realmIndex % 5
            $row = [Math]::Floor($realmIndex / 5)
            $graphics.DrawImageUnscaled($source, $column * 576, $row * 576)
        } finally {
            $source.Dispose()
        }
    }
    $bitmap.Save($atlasPath, [System.Drawing.Imaging.ImageFormat]::Png)
} finally {
    $graphics.Dispose()
    $bitmap.Dispose()
}

# Reuse the existing Unity importer settings but replace its nine-sprite slice
# table with named entries for all ninety sprites.
$sourceMeta = [System.IO.File]::ReadAllText((Join-Path $assetRoot 'Tile_BoardFrame_Atlas.png.meta'))
$tableStart = $sourceMeta.IndexOf('  internalIDToNameTable:')
$externalStart = $sourceMeta.IndexOf('  externalObjects:', $tableStart)
$sheetStart = $sourceMeta.IndexOf('  spriteSheet:', $externalStart)
$suffixStart = $sourceMeta.IndexOf('  userData:', $sheetStart)
if ($tableStart -lt 0 -or $externalStart -lt 0 -or $sheetStart -lt 0 -or $suffixStart -lt 0) {
    throw 'Board atlas importer format has changed.'
}
$lineEnding = if ($sourceMeta.Contains("`r`n")) { "`r`n" } else { "`n" }
$builder = [System.Text.StringBuilder]::new()
[void]$builder.Append($sourceMeta.Substring(0, $tableStart).Replace($oldAsgardGuid, $atlasGuid))
[void]$builder.Append("  internalIDToNameTable:$lineEnding")
for ($realmIndex = 0; $realmIndex -lt $realms.Count; $realmIndex++) {
    for ($pieceIndex = 0; $pieceIndex -lt $pieces.Count; $pieceIndex++) {
        $id = GetSpriteId $realmIndex $pieceIndex
        $name = "Board_$($realms[$realmIndex])_$($pieces[$pieceIndex])"
        [void]$builder.Append("  - first:$lineEnding      213: $id$lineEnding    second: $name$lineEnding")
    }
}
[void]$builder.Append($sourceMeta.Substring($externalStart, $sheetStart - $externalStart).Replace('maxTextureSize: 2048', 'maxTextureSize: 4096'))
[void]$builder.Append("  spriteSheet:$lineEnding    serializedVersion: 2$lineEnding    sprites:$lineEnding")

for ($realmIndex = 0; $realmIndex -lt $realms.Count; $realmIndex++) {
    $column = $realmIndex % 5
    $row = [Math]::Floor($realmIndex / 5)
    for ($pieceIndex = 0; $pieceIndex -lt $pieces.Count; $pieceIndex++) {
        $id = GetSpriteId $realmIndex $pieceIndex
        $name = "Board_$($realms[$realmIndex])_$($pieces[$pieceIndex])"
        $x = $column * 576 + ($pieceIndex % 3) * 192
        $y = (1 - $row) * 576 + (2 - [Math]::Floor($pieceIndex / 3)) * 192
        $spriteId = $atlasGuid.Substring(0, 27) + $id.ToString('x5')
        [void]$builder.Append(@"
    - serializedVersion: 2
      name: $name
      rect:
        serializedVersion: 2
        x: $x
        y: $y
        width: 192
        height: 192
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteId
      internalID: $id
      vertices: []
      indices:
      edges: []
      weights: []

"@.Replace("`n", $lineEnding))
    }
}
[void]$builder.Append(@"
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:

"@.Replace("`n", $lineEnding))
for ($realmIndex = 0; $realmIndex -lt $realms.Count; $realmIndex++) {
    for ($pieceIndex = 0; $pieceIndex -lt $pieces.Count; $pieceIndex++) {
        $id = GetSpriteId $realmIndex $pieceIndex
        $name = "Board_$($realms[$realmIndex])_$($pieces[$pieceIndex])"
        [void]$builder.Append("      ${name}: $id$lineEnding")
    }
}
[void]$builder.Append($sourceMeta.Substring($suffixStart))
$newMeta = $builder.ToString()
$newMeta = $newMeta.Replace('maxTextureSize: 2048', 'maxTextureSize: 4096')
[System.IO.File]::WriteAllText("$atlasPath.meta", $newMeta, [System.Text.UTF8Encoding]::new($false))

# The Asgard RuleTile remains the scene's existing asset/GUID. Rebuild the nine
# other assets from it so their neighbor rules never drift apart.
$masterRulePath = Join-Path $ruleRoot 'BoardFrame_RuleTile.asset'
$masterRule = [System.IO.File]::ReadAllText($masterRulePath)
$masterRule = $masterRule.Replace($oldAsgardGuid, $atlasGuid)
[System.IO.File]::WriteAllText($masterRulePath, $masterRule, [System.Text.UTF8Encoding]::new($false))
for ($realmIndex = 1; $realmIndex -lt $realms.Count; $realmIndex++) {
    $realm = $realms[$realmIndex]
    $rule = $masterRule.Replace('BoardFrame_RuleTile', "BoardFrame_${realm}_RuleTile")
    $rule = [regex]::Replace($rule, '\{fileID: (400[1-9]), guid: ' + $atlasGuid + ', type: 3\}', {
        param($match)
        $pieceIndex = [int]$match.Groups[1].Value - 4001
        $id = GetSpriteId $realmIndex $pieceIndex
        return "{fileID: $id, guid: $atlasGuid, type: 3}"
    })
    [System.IO.File]::WriteAllText((Join-Path $ruleRoot "BoardFrame_${realm}_RuleTile.asset"), $rule, [System.Text.UTF8Encoding]::new($false))
}

# Tilemap YAML caches sprite references in addition to the RuleTile reference.
$referenceFiles = @(
    (Join-Path $ProjectRoot 'Assets/1. Scenes/3. Stage1.unity'),
    (Join-Path $assetRoot 'Tiles/Tiles.prefab')
)
$referenceFiles += Get-ChildItem -LiteralPath $ruleRoot -Filter 'Tile_Frame_*.asset' | ForEach-Object { $_.FullName }
foreach ($path in $referenceFiles) {
    $contents = [System.IO.File]::ReadAllText($path)
    if ($contents.Contains($oldAsgardGuid)) {
        [System.IO.File]::WriteAllText($path, $contents.Replace($oldAsgardGuid, $atlasGuid), [System.Text.UTF8Encoding]::new($false))
    }
}
