$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$scopes=@((Join-Path $root 'Assets/4. Asset/3. Projectile'),(Join-Path $root 'Assets/10.Animation/Projectile'))
function Check-Path([string]$path){
    $full=[IO.Path]::GetFullPath($path)
    $allowed=$false
    foreach($scope in $scopes){if($full -eq $scope -or $full -eq ($scope+'.meta') -or $full.StartsWith($scope+'\',[StringComparison]::OrdinalIgnoreCase)){$allowed=$true}}
    if(!$allowed){throw ('Deletion outside reviewed Projectile directories: '+$full)}
    $full
}
$rows=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$removed=@($rows | Where-Object {!$_.keep});$kept=@($rows | Where-Object keep)
if($removed.Count -ne 97 -or $kept.Count -ne 73){throw 'Unexpected audit; review before deleting.'}
$removeGuids=@{};$removePaths=@{};$files=New-Object Collections.Generic.List[string]
foreach($row in $removed){
    $path=Check-Path (Join-Path $root $row.path)
    $removeGuids[$row.guid]=$true;$removePaths[$row.path]=$true
    $files.Add($path);$files.Add(($path+'.meta'))
}
$removed.guid | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'removed-guids.txt') -Encoding ASCII
# Recheck all surviving serialized assets, including metadata dependencies.
$referrers=@(rg -l -F -f (Join-Path $PSScriptRoot 'removed-guids.txt') (Join-Path $root 'Assets') -g '!*.png' -g '!*.jpg' -g '!*.dll' -g '!*.xlsx' -g '!*.ttf' -g '!*.otf')
foreach($path in $referrers){
    $rel=$path.Substring($root.Length+1).Replace('\','/')
    $asset=if($rel.EndsWith('.meta')){$rel.Substring(0,$rel.Length-5)}else{$rel}
    if($removePaths.ContainsKey($asset)){continue}
    $text=[IO.File]::ReadAllText($path)
    foreach($match in [regex]::Matches($text,'guid: ([a-f0-9]{32})')){if($removeGuids.ContainsKey($match.Groups[1].Value)){throw ('New surviving reference found: '+$rel)}}
}
$folders=@('Assets/4. Asset/3. Projectile/Cartoon/Bullet','Assets/4. Asset/3. Projectile/Projectile','Assets/10.Animation/Projectile')
foreach($folder in $folders){$meta=Check-Path ((Join-Path $root $folder)+'.meta');if(Test-Path -LiteralPath $meta){$files.Add($meta)}}
$snapshots=New-Object Collections.Generic.List[object]
foreach($row in $kept){foreach($suffix in @('','.meta')){$path=Join-Path $root ($row.path+$suffix);$snapshots.Add([pscustomobject]@{path=$path;sha256=(Get-FileHash -LiteralPath $path).Hash})}}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $root 'Assets/Resources/Projectiles') -File){$snapshots.Add([pscustomobject]@{path=$file.FullName;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash})}
$snapshots | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'retained-hashes.json') -Encoding UTF8
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath=Join-Path $PSScriptRoot 'removed-assets.zip'
if(Test-Path -LiteralPath $archivePath){throw 'Existing removal archive found; cleanup already started.'}
$records=New-Object Collections.Generic.List[object]
$archive=[IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try{
    foreach($path in $files){
        $relative=$path.Substring($root.Length+1).Replace('\','/')
        $records.Add([pscustomobject]@{path=$relative;sha256=(Get-FileHash -LiteralPath $path).Hash;bytes=(Get-Item -LiteralPath $path).Length})
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$path,$relative,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}finally{$archive.Dispose()}
$archive=[IO.Compression.ZipFile]::OpenRead($archivePath)
try{
    foreach($record in $records){
        $stream=$archive.GetEntry($record.path).Open();$sha=[Security.Cryptography.SHA256]::Create()
        try{$hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$stream.Dispose();$sha.Dispose()}
        if($hash -cne $record.sha256){throw ('Archive checksum mismatch: '+$record.path)}
    }
}finally{$archive.Dispose()}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'removed-files.json') -Encoding UTF8
foreach($path in $files){$checked=Check-Path $path;if(Test-Path -LiteralPath $checked){Remove-Item -LiteralPath $checked}}
foreach($folder in $folders){
    $path=Check-Path (Join-Path $root $folder)
    if(Test-Path -LiteralPath $path){
        if(@(Get-ChildItem -LiteralPath $path -Force).Count){throw ('Unexpected files remain in '+$folder)}
        Remove-Item -LiteralPath $path
    }
}
foreach($record in $snapshots){if((Get-FileHash -LiteralPath $record.path).Hash -cne $record.sha256){throw ('Retained asset changed: '+$record.path)}}
$hits=@(rg -l -F -f (Join-Path $PSScriptRoot 'removed-guids.txt') (Join-Path $root 'Assets') -g '!*.png' -g '!*.jpg' -g '!*.dll' -g '!*.xlsx' -g '!*.ttf' -g '!*.otf')
if($hits.Count){throw ('Deleted GUIDs still present in Assets: '+($hits -join ', '))}
$log=@(
    'PASS: removed 97 unused assets (57 images, 20 animation clips, 20 animator controllers).',
    ('PASS: '+$files.Count+' files including metadata archived and SHA256-verified before removal.'),
    'PASS: no deleted asset GUID remains in Assets.',
    'PASS: all 73 retained projectile art/animation assets and their metadata remain byte-identical.',
    'PASS: all 28 Resources projectile prefabs and their metadata remain byte-identical.',
    'PASS: removed three empty obsolete folders; no runtime code or active prefab changes.',
    'Unity was not launched by Codex.'
)
$log | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt') -Encoding UTF8
$log
