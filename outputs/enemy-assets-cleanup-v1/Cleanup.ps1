$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetArtRoot = [IO.Path]::GetFullPath((Join-Path $root 'Assets/4. Asset/4. Enemy'))
$prefabRoot = [IO.Path]::GetFullPath((Join-Path $root 'Assets/2. Prefab/3. Enemy'))
$rows = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'candidates.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$candidateSet = @{}
function Check-Target([string]$path) {
    $full = [IO.Path]::GetFullPath($path)
    if (!$full.StartsWith($assetArtRoot + '\', [StringComparison]::OrdinalIgnoreCase) -and !$full.StartsWith($prefabRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw ('Deletion outside enemy asset directories: ' + $full) }
    return $full
}
$files = New-Object Collections.Generic.List[string]
foreach ($row in $rows) {
    $path = Check-Target (Join-Path $root $row.Path)
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw ('Candidate missing: ' + $row.Path) }
    $candidateSet[$row.Path] = $true
    $files.Add($path)
    $files.Add((Check-Target ($path + '.meta')))
}
# Include only directories whose entire contents are scheduled for removal.
$fileSet = @{}
foreach ($file in $files) { $fileSet[$file] = $true }
$emptyFolders = @(Get-ChildItem -LiteralPath $assetArtRoot,$prefabRoot -Recurse -Directory | Where-Object {
    $contents = @(Get-ChildItem -LiteralPath $_.FullName -Recurse -File)
    $contents.Count -gt 0 -and @($contents | Where-Object {
        !$fileSet.ContainsKey($_.FullName) -and !($_.Extension -eq '.meta' -and (Test-Path -LiteralPath $_.FullName.Substring(0,$_.FullName.Length - 5) -PathType Container))
    }).Count -eq 0
} | Sort-Object { $_.FullName.Length } -Descending)
foreach ($folder in $emptyFolders) {
    $metaPath = $folder.FullName + '.meta'
    if (Test-Path -LiteralPath $metaPath) {
        $checked = Check-Target $metaPath
        if (!$fileSet.ContainsKey($checked)) { $files.Add($checked); $fileSet[$checked] = $true }
    }
}
# Snapshot current files and verify archive bytes before deleting anything.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath = Join-Path $PSScriptRoot 'removed-assets.zip'
if (Test-Path -LiteralPath $archivePath) { throw 'Archive already exists. Inspect the previous cleanup before rerunning.' }
$inventory = New-Object Collections.Generic.List[object]
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in $files) {
        $rel = $file.Substring($root.Length + 1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file, $rel, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $inventory.Add([pscustomobject]@{Path=$rel; Bytes=(Get-Item -LiteralPath $file).Length; SHA256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash})
    }
} finally { $archive.Dispose() }
$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($record in $inventory) {
        $stream = $archive.GetEntry($record.Path).Open()
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $hash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') } finally { $sha.Dispose(); $stream.Dispose() }
        if ($hash -ne $record.SHA256) { throw ('Archive checksum mismatch: ' + $record.Path) }
    }
} finally { $archive.Dispose() }
$inventory | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'removed-files.json') -Encoding UTF8

$referrers = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'referrer-paths.txt') -Encoding UTF8)
$changes = New-Object Collections.Generic.List[string]
foreach ($relative in $referrers) {
    $relative = $relative.Replace('\','/')
    if ($candidateSet.ContainsKey($relative)) { continue }
    $path = Join-Path $root $relative
    $original = [IO.File]::ReadAllText($path)
    $pattern = '\{fileID: (?<id>-?\d+), guid: 565bffcfa3c50e14cb7dd6553827e48f, type: 3\}'
    $updated = [regex]::Replace($original, $pattern, {
        param($match)
        $id = $match.Groups['id'].Value
        $prefix = $original.Substring([Math]::Max(0,$match.Index - 170), [Math]::Min(170,$match.Index))
        if ($id -eq '1969506814401737429' -or $prefix -match '(?s)(?:enemyPrefab:|propertyPath: enemyPrefab)\s*[^{}]*$') {
            $newId = '236192059372975241' # EnemyHealthController on the current cube
        } elseif ($id -eq '9067481978086507008') {
            $newId = '2036411223144491597' # EnemyMoveController on the current cube
        } else { throw ('Unmapped legacy prefab reference: ' + $id) }
        '{fileID: ' + $newId + ', guid: 1679782d7e31a1143b96b4e8288a006f, type: 3}'
    })
    if ($updated -cne $original) {
        $backup = Join-Path $PSScriptRoot ('references-before/' + $relative)
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
        [IO.File]::Copy($path, $backup, $false)
        [IO.File]::WriteAllText($path, $updated, (New-Object Text.UTF8Encoding($false)))
        $changes.Add($relative)
    }
    foreach ($row in $rows) {
        if ($updated.Contains($row.Guid)) { throw ('Reference still requires a deleted asset: ' + $relative + ' -> ' + $row.Path) }
    }
}
$changes | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'updated-references.txt') -Encoding UTF8
foreach ($file in $files) { Remove-Item -LiteralPath (Check-Target $file) }
foreach ($folder in $emptyFolders) {
    $checked = Check-Target $folder.FullName
    if (@(Get-ChildItem -LiteralPath $checked -Force).Count -eq 0) { Remove-Item -LiteralPath $checked }
}
Write-Output ('Removed assets: ' + $rows.Count + '; total files including metadata: ' + $files.Count)
Write-Output ('Original asset bytes removed: ' + (($rows | Measure-Object Bytes -Sum).Sum))
Write-Output ('Reference files updated: ' + $changes.Count)
Write-Output ('Checksum-verified archive: ' + $archivePath)
