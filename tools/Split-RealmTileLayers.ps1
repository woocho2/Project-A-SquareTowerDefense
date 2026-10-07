param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$PreviewOnly
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -Path (Join-Path $PSScriptRoot 'RealmTileAtlasBuilder.cs') -ReferencedAssemblies System.Drawing
$assetRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround'
$outputRoot = Join-Path $ProjectRoot 'outputs/realm-tile-layers'
$sourceRoot = Join-Path $outputRoot 'source'
$buildRoot = Join-Path $outputRoot 'build'
$previewRoot = Join-Path $outputRoot 'preview'
$utf8 = [System.Text.UTF8Encoding]::new($false)

# Preserve inputs once. Palette adjustments and repeated builds always start here.
New-Item -ItemType Directory -Path $sourceRoot -Force | Out-Null
$names = @('Tile_Path_Atlas.png', 'Tile_TowerSpawn_Atlas.png')
$names += [RealmTileAtlasBuilder]::Realms | ForEach-Object { "Tile_Path_Overlay_${_}_Atlas.png" }
foreach ($name in $names) {
    foreach ($suffix in @('', '.meta')) {
        $snapshot = Join-Path $sourceRoot ($name + $suffix)
        if (-not (Test-Path -LiteralPath $snapshot)) {
            Copy-Item -LiteralPath (Join-Path $assetRoot ($name + $suffix)) -Destination $snapshot
        }
    }
}
[RealmTileAtlasBuilder]::Build($sourceRoot, $buildRoot, $previewRoot)
Write-Output "Built 20 floor sprites and 10 frame/direction atlases. Preview: $previewRoot"
if ($PreviewOnly) { return }

# Reuse the path atlas GUID and both sets of fileIDs/spriteIDs. Only the spawn
# texture GUID changes; Tile asset GUIDs, scene cells and animation timing stay intact.
$pathMeta = [System.IO.File]::ReadAllText((Join-Path $sourceRoot 'Tile_Path_Atlas.png.meta'))
$spawnMeta = [System.IO.File]::ReadAllText((Join-Path $sourceRoot 'Tile_TowerSpawn_Atlas.png.meta'))
$pathGuid = [regex]::Match($pathMeta, '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
$spawnGuid = [regex]::Match($spawnMeta, '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
$tablePattern = '(?s)(  internalIDToNameTable:\r?\n)(.*?)(  externalObjects:)'
$sheetPattern = '(?sm)(^    sprites:\r?\n)(.*?)(^    outline:)'
$namePattern = '(?s)(    nameFileIdTable:\r?\n)(.*?)(  mipmapLimitGroupName:)'
$spawnTable = [regex]::Match($spawnMeta, $tablePattern).Groups[2].Value
$spawnSheet = [regex]::Match($spawnMeta, $sheetPattern).Groups[2].Value
$spawnNames = [regex]::Match($spawnMeta, $namePattern).Groups[2].Value
if (-not $spawnTable -or -not $spawnSheet -or -not $spawnNames) { throw 'Unexpected importer format.' }
$spawnSheet = [regex]::Replace($spawnSheet, '(?m)^(        x: )(\d+)', {
    param($match) $match.Groups[1].Value + ([int]$match.Groups[2].Value + 1024)
})
$combinedMeta = [regex]::Replace($pathMeta, $tablePattern, {
    param($match) $match.Groups[1].Value + $match.Groups[2].Value + $spawnTable + $match.Groups[3].Value
})
$combinedMeta = [regex]::Replace($combinedMeta, $sheetPattern, {
    param($match) $match.Groups[1].Value + $match.Groups[2].Value + $spawnSheet + $match.Groups[3].Value
})
$combinedMeta = [regex]::Replace($combinedMeta, $namePattern, {
    param($match) $match.Groups[1].Value + $match.Groups[2].Value + $spawnNames + $match.Groups[3].Value
})
if (([regex]::Matches($combinedMeta, '(?m)^      internalID: \d+')).Count -ne 20) {
    throw 'Expected 20 floor slices.'
}
$floorPath = Join-Path $assetRoot 'Tile_Floor_Atlas.png'
Copy-Item -LiteralPath (Join-Path $buildRoot 'Tile_Floor_Atlas.png') -Destination $floorPath -Force
[System.IO.File]::WriteAllText("$floorPath.meta", $combinedMeta, $utf8)
foreach ($realm in [RealmTileAtlasBuilder]::Realms) {
    $name = "Tile_Path_Overlay_${realm}_Atlas.png"
    Copy-Item -LiteralPath (Join-Path $buildRoot $name) -Destination (Join-Path $assetRoot $name) -Force
}

# Unity Tilemaps cache sprite references in scenes/prefabs, as well as in Tiles.
# Migrate every serialized reference, preserving the user's other edits byte-for-byte.
$referenceRoot = Join-Path $outputRoot 'reference-backup'
$changed = [System.Collections.Generic.List[string]]::new()
$files = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File |
    Where-Object { $_.Extension -in @('.asset', '.prefab', '.unity', '.anim', '.mat', '.controller') }
foreach ($file in $files) {
    $contents = [System.IO.File]::ReadAllText($file.FullName)
    if (-not $contents.Contains($spawnGuid)) { continue }
    $relative = $file.FullName.Substring($ProjectRoot.Length).TrimStart('\', '/')
    $backup = Join-Path $referenceRoot $relative
    if (-not (Test-Path -LiteralPath $backup)) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $backup
    }
    [System.IO.File]::WriteAllText($file.FullName, $contents.Replace($spawnGuid, $pathGuid), $utf8)
    $changed.Add($relative)
}

# Originals live in outputs/source; avoid duplicate GUIDs after renaming the atlas.
foreach ($name in @('Tile_Path_Atlas.png', 'Tile_TowerSpawn_Atlas.png')) {
    foreach ($suffix in @('', '.meta')) {
        $legacy = Join-Path $assetRoot ($name + $suffix)
        if (Test-Path -LiteralPath $legacy) { Remove-Item -LiteralPath $legacy }
    }
}
$referenceLog = Join-Path $outputRoot 'migrated-references.txt'
if ($changed.Count -gt 0 -or -not (Test-Path -LiteralPath $referenceLog)) {
    [System.IO.File]::WriteAllLines($referenceLog, $changed, $utf8)
}
Write-Output "Applied floor atlas using GUID $pathGuid; migrated $($changed.Count) serialized files."
& (Join-Path $PSScriptRoot 'Install-TowerSpawnFrames.ps1') -ProjectRoot $ProjectRoot
& (Join-Path $PSScriptRoot 'Merge-PathEndpointLayers.ps1') -ProjectRoot $ProjectRoot
