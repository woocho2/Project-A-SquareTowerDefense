$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetRoot=[IO.Path]::GetFullPath((Join-Path $root 'Assets/4. Asset/4. Enemy'))
$utf8=New-Object Text.UTF8Encoding($false)
function Checked([string]$path){
    $full=[IO.Path]::GetFullPath($path)
    if(!$full.StartsWith($assetRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw ('Outside enemy art directory: '+$full)}
    $full
}
$deleteList=@(Get-Content -LiteralPath (Join-Path $root 'outputs/enemy-cube-layers/unused-baked-images.txt') -Encoding UTF8)
$deleteSet=@{};foreach($path in $deleteList){$deleteSet[[IO.Path]::GetFullPath((Join-Path $root $path))]=$true}
if($deleteList.Count -ne 8){throw 'Expected eight obsolete baked images.'}
$all=@(Get-ChildItem -LiteralPath $assetRoot -Recurse -File)
$assets=@($all | Where-Object Extension -ne '.meta')
$moves=New-Object Collections.Generic.List[object]
foreach($file in $assets){
    $meta=[IO.File]::ReadAllText($file.FullName+'.meta');$guid=[regex]::Match($meta,'(?m)^guid: (\w+)').Groups[1].Value
    if($deleteSet.ContainsKey($file.FullName)){
        $refs=@(rg -l -F $guid (Join-Path $root 'Assets') -g '!*.meta' -g '!*.png')
        if($refs.Count){throw ('Obsolete image still referenced: '+$file.FullName)}
        continue
    }
    $new=Checked (Join-Path $assetRoot $file.Name)
    if($new -ne $file.FullName -and (Test-Path -LiteralPath $new)){throw ('Destination collision: '+$new)}
    $moves.Add([pscustomobject]@{old=$file.FullName;new=$new;guid=$guid;meta=$meta;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash})
}
if(@($moves.new | Select-Object -Unique).Count -ne $moves.Count){throw 'Conflicting filenames.'}
# Back up everything before deleting images or moving assets.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath=Join-Path $PSScriptRoot 'before.zip'
if(Test-Path -LiteralPath $archivePath){throw 'Consolidation backup already exists.'}
$hashes=New-Object Collections.Generic.List[object]
$archive=[IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try{
    foreach($file in $all){
        $relative=$file.FullName.Substring($root.Length+1).Replace('\','/')
        $hashes.Add([pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash})
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}finally{$archive.Dispose()}
$archive=[IO.Compression.ZipFile]::OpenRead($archivePath)
try{
    foreach($record in $hashes){
        $stream=$archive.GetEntry($record.path).Open();$sha=[Security.Cryptography.SHA256]::Create()
        try{$hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
        if($hash -cne $record.sha256){throw ('Backup differs: '+$record.path)}
    }
}finally{$archive.Dispose()}
$moves | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'move-manifest.json') -Encoding UTF8
$hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'before-hashes.json') -Encoding UTF8
$deleteGuids=New-Object Collections.Generic.List[string]
foreach($relative in $deleteList){
    $path=Checked (Join-Path $root $relative)
    $deleteGuids.Add([regex]::Match([IO.File]::ReadAllText($path+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value)
    foreach($target in @($path,($path+'.meta'))){if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath (Checked $target)}}
}
foreach($move in $moves){
    $old=Checked $move.old;$new=Checked $move.new
    if($old -ne $new){
        # Restore original metadata after all moves as well, to handle live auto-import.
        [IO.File]::WriteAllText(($new+'.meta'),$move.meta,$utf8)
        Move-Item -LiteralPath $old -Destination $new
        if(Test-Path -LiteralPath ($old+'.meta')){Remove-Item -LiteralPath (Checked ($old+'.meta'))}
    }
}
foreach($move in $moves){[IO.File]::WriteAllText(($move.new+'.meta'),$move.meta,$utf8)}
foreach($directory in @(Get-ChildItem -LiteralPath $assetRoot -Recurse -Directory | Sort-Object {$_.FullName.Length} -Descending)){
    $path=Checked $directory.FullName
    if(@(Get-ChildItem -LiteralPath $path -Force).Count -eq 0){
        Remove-Item -LiteralPath $path
        if(Test-Path -LiteralPath ($path+'.meta')){Remove-Item -LiteralPath (Checked ($path+'.meta'))}
    }
}
foreach($move in $moves){
    if((Get-FileHash -LiteralPath $move.new).Hash -cne $move.sha256){throw 'Moved asset contents changed.'}
    if([regex]::Match([IO.File]::ReadAllText($move.new+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value -cne $move.guid){throw 'Moved asset GUID changed.'}
}
foreach($guid in $deleteGuids){
    $refs=@(rg -l -F $guid (Join-Path $root 'Assets') -g '!*.meta' -g '!*.png')
    if($refs.Count){throw ('Deleted image GUID remains referenced: '+$guid)}
}
if(@(Get-ChildItem -LiteralPath $assetRoot -Directory).Count){throw 'Unexpected subfolders remain.'}
$deleteList | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deleted-images.txt') -Encoding UTF8
'PASS: removed 8 unused baked images and metadata; consolidated '+$moves.Count+' live assets in Assets/4. Asset/4. Enemy.'
'PASS: all moved asset hashes and GUIDs unchanged; obsolete folders removed; backup SHA256 checked.'
