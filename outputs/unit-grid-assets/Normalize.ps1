$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8 = [Text.UTF8Encoding]::new($false)
$changes = [Collections.Generic.List[object]]::new()

function Add-Change([string]$relative, [string]$old, [string]$new, [int]$count = 1) {
    $path = Join-Path $repoRoot $relative
    $bytes = [IO.File]::ReadAllBytes($path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
    $source = [IO.File]::ReadAllText($path)
    $matches = [regex]::Matches($source, [regex]::Escape($old)).Count
    if ($matches -ne $count) { throw "$relative expected $count matches; found $matches for $old" }
    $existing = $changes | Where-Object { $_.Path -eq $relative } | Select-Object -First 1
    if ($existing) {
        $existing.After = $existing.After.Replace($old, $new)
    } else {
        $changes.Add([pscustomobject]@{
            Path = $relative; Before = $source; After = $source.Replace($old, $new)
            Bytes = $bytes; Bom = $hasBom
        })
    }
}

# PPU follows one sprite cell, not the size of the complete atlas.
$portraitMetas = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/4. Asset/2. Tower/TowerCharacter') -Filter '*.png.meta')
$enemyMetas = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/4. Asset/4. Enemy') -Filter '*_Character.png.meta' -Recurse)
if ($portraitMetas.Count -ne 15 -or $enemyMetas.Count -ne 27) { throw 'Unexpected character atlas inventory' }
foreach ($meta in @($portraitMetas) + @($enemyMetas)) {
    $relative = $meta.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
    Add-Change $relative '  spritePixelsToUnits: 384' '  spritePixelsToUnits: 512'
}
Add-Change 'Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png.meta' '  spritePixelsToUnits: 128' '  spritePixelsToUnits: 192'
Add-Change 'Assets/4. Asset/1. BackGround/Tiles/Tiles.prefab' '  m_CellSize: {x: 1.3333334, y: 1.3333334, z: 0}' '  m_CellSize: {x: 1, y: 1, z: 0}'

# The old render-size overrides would otherwise enlarge normalized sprites again.
$tower = 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
Add-Change $tower '  m_canvasWorldSize: 1.3333334' '  m_canvasWorldSize: 1'
Add-Change $tower '  m_synergyCanvasWorldSize: 1.3333334' '  m_synergyCanvasWorldSize: 1'
Add-Change $tower '  m_Size: {x: 1.3333334, y: 1.3333334}' '  m_Size: {x: 1, y: 1}'
# Preserve the emblem's proportion and relative placement on the smaller canvas.
Add-Change $tower '  m_LocalPosition: {x: 0, y: 0.475, z: 0}' '  m_LocalPosition: {x: 0, y: 0.35625, z: 0}'
Add-Change $tower '  m_LocalScale: {x: 0.25, y: 0.25, z: 1}' '  m_LocalScale: {x: 0.1875, y: 0.1875, z: 1}'
foreach ($name in @('1. Enemy_normal', '2. Enemy_middleBoss', '3. Enemy_boss')) {
    $relative = "Assets/2. Prefab/3. Enemy/$name.prefab"
    Add-Change $relative '  m_LocalScale: {x: 1.3333334, y: 1.3333334, z: 1.3333334}' '  m_LocalScale: {x: 1, y: 1, z: 1}'
    Add-Change $relative '  m_LocalPosition: {x: 0, y: 0, z: -0.6666667}' '  m_LocalPosition: {x: 0, y: 0, z: -0.5}'
    Add-Change $relative '  m_sideLength: 1.3333334' '  m_sideLength: 1'
}
Add-Change 'Assets/3. Scripts/01. Tower/TowerVisual.cs' ' = 256f / 192f;' ' = 1f;' 2
Add-Change 'Assets/3. Scripts/03. Enemy/EnemyCubeMeshVisual.cs' ' = 256f / 192f;' ' = 1f;'

# Prepare all changes before writing. Refuse to overwrite any concurrent editor save.
foreach ($change in $changes) {
    $path = Join-Path $repoRoot $change.Path
    if ([IO.File]::ReadAllText($path) -cne $change.Before) { throw "File changed during inspection: $($change.Path)" }
    $backup = Join-Path $PSScriptRoot ('before/' + $change.Path)
    if (Test-Path -LiteralPath $backup) { throw "Backup already exists: $backup" }
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
    [IO.File]::WriteAllBytes($backup, $change.Bytes)
}
$pngPaths = @($portraitMetas + $enemyMetas | ForEach-Object { $_.FullName.Substring(0, $_.FullName.Length - 5) })
$pngPaths += Join-Path $repoRoot 'Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png'
$pngHashes = @($pngPaths | ForEach-Object {
    [pscustomobject]@{ Path = $_.Substring($repoRoot.Length + 1); Hash = (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash }
})
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'png-before.json'), (ConvertTo-Json -InputObject $pngHashes -Depth 5), $utf8)
foreach ($change in $changes) {
    $path = Join-Path $repoRoot $change.Path
    if ([IO.File]::ReadAllText($path) -cne $change.Before) { throw "File changed before write: $($change.Path)" }
    [IO.File]::WriteAllText($path, $change.After, [Text.UTF8Encoding]::new($change.Bom))
}
$report = @($changes | ForEach-Object {
    [pscustomobject]@{ Path = $_.Path; BeforeHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot ('before/' + $_.Path))).Hash; AfterHash = (Get-FileHash -LiteralPath (Join-Path $repoRoot $_.Path)).Hash }
})
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'changes.json'), (ConvertTo-Json -InputObject $report -Depth 5), $utf8)
"Normalized $($changes.Count) files; retained all PNG pixels and sprite identities."
