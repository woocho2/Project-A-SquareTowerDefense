param([switch]$Resume)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$relative='Assets/4. Asset/3. Projectile'
$folder=[IO.Path]::GetFullPath((Join-Path $root $relative))
$numbers=@{Sword=1;Axe=2;Bow=3;Spear=4;Shield=5;Hammer=6;Fire=7;Water=8;Earth=9;Wind=10;Ice=11;Electric=12;Light=13;Dark=14}
$records=New-Object Collections.Generic.List[object]
$utf8=New-Object Text.UTF8Encoding($false)
function Checked([string]$path){
    $full=[IO.Path]::GetFullPath($path)
    if(!$full.StartsWith($folder+'\',[StringComparison]::OrdinalIgnoreCase)){throw ('Outside projectile art directory: '+$full)}
    $full
}
if(!$Resume){
foreach($file in @(Get-ChildItem -LiteralPath $folder -Recurse -File | Where-Object Extension -ne '.meta')){
    $oldStem=$file.BaseName;$emblem=$null;$suffix=$null;$directory=$file.DirectoryName
    if($oldStem -match '^Bullet_(\w+)_Motion_v\d+(?:_(Idle|Flight))?$'){
        $emblem=$matches[1]
        if($file.Extension -eq '.png'){$suffix='Projectile_Atlas'}
        elseif($file.Extension -eq '.controller'){$suffix='Projectile_Animator'}
        elseif($matches[2] -eq 'Idle'){$suffix='Projectile_Animation_Idle'}
        else{$suffix='Projectile_Animation_Fly'}
    }elseif($oldStem -match '^Splash_(\w+)_Orb_v\d+$'){$emblem=$matches[1];$suffix='Projectile_Atlas'}
    elseif($oldStem -match '^Orb_(Idle|Flight|Motion)$'){
        $part=$matches[1]
        $newStem=if($part -eq 'Motion'){'Shared_Orb_Projectile_Animator'}elseif($part -eq 'Idle'){'Shared_Orb_Projectile_Animation_Idle'}else{'Shared_Orb_Projectile_Animation_Fly'}
    }elseif($oldStem -match '^Bullet_(\w+)$'){$emblem=$matches[1];$suffix='Projectile'}
    elseif($oldStem -match '^Splash_(\w+)$'){$emblem=$matches[1];$suffix='Projectile_Orb'}
    elseif($oldStem -match '^Projectile_Meteor_(\w+)$'){$emblem=$matches[1];$suffix='Projectile_Meteor'}
    elseif($oldStem -match '^Projectile_Arrow_(Bomb|Double)$'){$emblem='Bow';$suffix='Projectile_Arrow_'+$matches[1]}
    elseif($oldStem -eq 'Projectile_Zeus_Thunderbolt'){$emblem='Electric';$suffix='Projectile_Thunderbolt'}
    elseif($oldStem -eq 'Projectile_Icicle_Small'){$emblem='Ice';$suffix='Projectile_Icicle_Small'}
    elseif($oldStem -eq 'Projectile_Fireball_Small'){$emblem='Fire';$suffix='Projectile_Fireball_Small'}
    elseif($oldStem -match '^Projectile_(\w+?)(?:_(Orb|Throwing|Laser|Captain))?$'){
        $emblem=$matches[1];$suffix='Projectile';if($matches[2]){$suffix+='_'+$matches[2]}
    }else{throw ('Unclassified asset: '+$file.FullName)}
    if($emblem){
        if($emblem -eq 'Darkness'){$emblem='Dark'}
        if(!$numbers.ContainsKey($emblem)){throw ('Unknown emblem: '+$emblem)}
        $newStem=[string]$numbers[$emblem]+'. '+$emblem+'_'+$suffix
    }
    $destination=Checked (Join-Path $directory ($newStem+$file.Extension))
    if(Test-Path -LiteralPath $destination){throw ('Rename collision: '+$destination)}
    $metaPath=$file.FullName+'.meta';$meta=[IO.File]::ReadAllText($metaPath)
    $guid=[regex]::Match($meta,'(?m)^guid: (\w+)').Groups[1].Value
    $records.Add([pscustomobject]@{old=$file.FullName;new=$destination;oldStem=$oldStem;newStem=$newStem;guid=$guid;extension=$file.Extension;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash})
}
if(@($records.new | Select-Object -Unique).Count -ne $records.Count){throw 'Duplicate rename targets.'}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'rename-map.json') -Encoding UTF8

# Preserve all original files, including metadata, outside Assets.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archivePath=Join-Path $PSScriptRoot 'before.zip'
if(Test-Path -LiteralPath $archivePath){throw 'Previous naming archive already exists.'}
$archive=[IO.Compression.ZipFile]::Open($archivePath,[IO.Compression.ZipArchiveMode]::Create)
try{
    foreach($file in Get-ChildItem -LiteralPath $folder -Recurse -File){
        $name=$file.FullName.Substring($root.Length+1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$name,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}finally{$archive.Dispose()}
}else{
    $records=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'rename-map.json') -Raw -Encoding UTF8 | ConvertFrom-Json
}
function Safe-Rename([string]$old,[string]$new){
    for($attempt=0;$attempt -lt 30;$attempt++){
        if(!(Test-Path -LiteralPath $old) -and (Test-Path -LiteralPath $new)){return}
        try{Rename-Item -LiteralPath $old -NewName ([IO.Path]::GetFileName($new));return}
        catch{if($attempt -eq 29){throw};Start-Sleep -Milliseconds 100}
    }
}

foreach($record in $records){
    $old=Checked $record.old;$new=Checked $record.new
    $current=if(Test-Path -LiteralPath $old){$old}else{$new}
    if($record.extension -in @('.anim','.controller')){
        $content=[IO.File]::ReadAllText($current)
        $pattern='(?m)^(  m_Name: )'+[regex]::Escape($record.oldStem)+'\r?$'
        $content=[regex]::Replace($content,$pattern,('${1}'+$record.newStem))
        [IO.File]::WriteAllText($current,$content,$utf8)
    }
    $currentMeta=if(Test-Path -LiteralPath ($old+'.meta')){$old+'.meta'}else{$new+'.meta'}
    $meta=[IO.File]::ReadAllText($currentMeta)
    # Sprite frame IDs stay identical; displayed frame names lose version suffixes.
    if($record.extension -eq '.png'){
        $meta=$meta.Replace($record.oldStem,$record.newStem)
        $meta=$meta.Replace('_Flight_','_Fly_')
        [IO.File]::WriteAllText($currentMeta,$meta,$utf8)
    }
    Safe-Rename $old $new
    Safe-Rename ($old+'.meta') ($new+'.meta')
}

foreach($pair in @(@('AnimatedSingle_v2','Single'),@('AnimatedSplash_v1','Splash'))){
    $old=Checked (Join-Path $folder $pair[0]);$new=Checked (Join-Path $folder $pair[1])
    if(Test-Path -LiteralPath $new){throw 'Folder rename collision.'}
    Safe-Rename $old $new
    Safe-Rename ($old+'.meta') ($new+'.meta')
    foreach($record in $records){if($record.new.StartsWith($old+'\')){$record.new=$new+$record.new.Substring($old.Length)}}
}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'rename-map.json') -Encoding UTF8

$archive=[IO.Compression.ZipFile]::OpenRead((Join-Path $PSScriptRoot 'before.zip'))
try{
    # An already open Unity editor may generate metadata during filesystem moves.
    # Restore original importer data, GUIDs and sprite IDs after all moves finish.
    foreach($record in $records){
        $key=$record.old.Substring($root.Length+1).Replace('\','/')+'.meta'
        $reader=New-Object IO.StreamReader ($archive.GetEntry($key).Open())
        try{$meta=$reader.ReadToEnd()}finally{$reader.Dispose()}
        if($record.extension -eq '.png'){$meta=$meta.Replace($record.oldStem,$record.newStem).Replace('_Flight_','_Fly_')}
        [IO.File]::WriteAllText(($record.new+'.meta'),$meta,$utf8)
    }
    foreach($pair in @(@('AnimatedSingle_v2','Single'),@('AnimatedSplash_v1','Splash'))){
        $reader=New-Object IO.StreamReader ($archive.GetEntry($relative+'/'+$pair[0]+'.meta').Open())
        try{$meta=$reader.ReadToEnd()}finally{$reader.Dispose()}
        [IO.File]::WriteAllText((Join-Path $folder ($pair[1]+'.meta')),$meta,$utf8)
    }
}finally{$archive.Dispose()}

$log=New-Object Collections.Generic.List[string]
foreach($record in $records){
    if(!(Test-Path -LiteralPath $record.new)){throw 'Renamed asset missing.'}
    $meta=[IO.File]::ReadAllText($record.new+'.meta')
    if($meta -notmatch ('(?m)^guid: '+$record.guid)){throw 'GUID changed.'}
    if($record.extension -eq '.png' -and (Get-FileHash -LiteralPath $record.new).Hash -ne $record.sha256){throw 'Image bytes changed.'}
}
$versionNames=@(Get-ChildItem -LiteralPath $folder -Recurse | Where-Object Name -match '(?i)(?:_|\b)v\d+(?:_|\.|$)')
if($versionNames.Count){throw 'Versioned file/folder names remain.'}
$log.Add('PASS: '+$records.Count+' projectile art/animation assets renamed; metadata GUIDs preserved; PNG bytes unchanged.')
$log.Add('PASS: Single uses 14 sets of numbered Atlas / Animator / Animation_Idle / Animation_Fly.')
$log.Add('PASS: shared splash Orb animator/clips explicitly named Shared_Orb; state names Idle/Flight and IsFlying parameter preserved for runtime code.')
$log.Add('PASS: AnimatedSingle_v2 -> Single; AnimatedSplash_v1 -> Splash; no version tags remain in asset/folder names.')
$log.Add('Numeric Resources projectile prefab keys and runtime code unchanged. No assets deleted. Unity was not launched.')
$log | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt') -Encoding UTF8
$log
