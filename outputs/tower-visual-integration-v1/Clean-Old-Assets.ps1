$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$allowedRoots=@('Assets/4. Asset/2. Tower/CubeConcepts','Assets/4. Asset/2. Tower/SynergyCube') | ForEach-Object {[IO.Path]::GetFullPath((Join-Path $projectRoot $_))}
$audit=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cleanup-audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$remove=@($audit | Where-Object {$_.references.Count -eq 0})
$paths=[Collections.Generic.List[string]]::new()
$guids=[Collections.Generic.HashSet[string]]::new()
function Assert-Allowed([string]$path) {
    $resolved=[IO.Path]::GetFullPath($path)
    if(-not @($allowedRoots | Where-Object {$resolved.StartsWith($_+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $resolved -eq $_ -or $resolved -eq ($_+'.meta')}).Count){throw ('Outside authorized cleanup roots: '+$resolved)}
}
foreach($entry in $remove) {
    $path=[IO.Path]::GetFullPath((Join-Path $projectRoot $entry.path));Assert-Allowed $path
    if(-not(Test-Path -LiteralPath $path)){throw 'Cleanup audit is stale: target missing.'}
    $paths.Add($path);$paths.Add($path+'.meta');[void]$guids.Add($entry.guid)
}
$directories=@(foreach($root in $allowedRoots){Get-ChildItem -LiteralPath $root -Directory -Recurse;Get-Item -LiteralPath $root}) | Sort-Object @{Expression={$_.FullName.Length};Descending=$true}
foreach($directory in $directories) {
    Assert-Allowed $directory.FullName
    $meta=$directory.FullName+'.meta';Assert-Allowed $meta
    if(Test-Path -LiteralPath $meta) {
        $paths.Add($meta)
        $guid=[regex]::Match([IO.File]::ReadAllText($meta),'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
        if($guid){[void]$guids.Add($guid)}
    }
}
$deleting=[Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach($path in $paths){[void]$deleting.Add($path)}
$extensions=@('.prefab','.unity','.asset','.mat','.controller','.anim','.spriteatlas','.spriteatlasv2','.meta','.cs','.json','.txt','.shader')
foreach($file in (Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -File | Where-Object {$_.Extension -in $extensions -and -not $deleting.Contains($_.FullName)})) {
    foreach($match in [regex]::Matches([IO.File]::ReadAllText($file.FullName),'[a-f0-9]{32}')) {
        if($guids.Contains($match.Value)){throw ('Live reference prevents deletion: '+$file.FullName+' -> '+$match.Value)}
    }
}
$archivePath=Join-Path $PSScriptRoot 'removed-assets.zip'
if(Test-Path -LiteralPath $archivePath){throw 'Backup archive already exists; refusing to replace it.'}
$archive=[IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try {
    foreach($path in $paths) {
        Assert-Allowed $path
        $relative=$path.Substring($projectRoot.Length+1).Replace('\','/')
        [void][IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$path,$relative,[IO.Compression.CompressionLevel]::Optimal)
    }
} finally {$archive.Dispose()}
# Verify every archived byte before any file removal.
$archive=[IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    if($archive.Entries.Count -ne $paths.Count){throw 'Archive entry count mismatch.'}
    foreach($entry in $archive.Entries) {
        $source=Join-Path $projectRoot $entry.FullName
        $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
        try {$archivedHash=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
        if($archivedHash -ne (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash){throw 'Archive byte verification failed.'}
    }
} finally {$archive.Dispose()}
$bytes=0
foreach($path in $paths){Assert-Allowed $path;$bytes+=(Get-Item -LiteralPath $path).Length;Remove-Item -LiteralPath $path}
foreach($directory in $directories) {
    Assert-Allowed $directory.FullName
    if(@(Get-ChildItem -LiteralPath $directory.FullName -Force).Count -eq 0){Remove-Item -LiteralPath $directory.FullName}
}
$report="Removed $($remove.Count) unreferenced old PNGs, their metadata and empty folder metadata from Assets. $bytes bytes removed from active Assets. Verified SHA256 for every archived file. Backup: removed-assets.zip. No live references to removed GUIDs."
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'cleanup-result.txt'),$report,[Text.UTF8Encoding]::new($false))
Write-Output $report
