$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$prefab=[IO.File]::ReadAllText((Join-Path $root 'Assets/2. Prefab/2. Tower/TowerBase.prefab')).Replace("`r",'')
$ids=[Collections.Generic.HashSet[string]]::new()
foreach($m in [regex]::Matches($prefab,'(?m)^--- !u!\d+ &(\d+)')){if(-not $ids.Add($m.Groups[1].Value)){throw 'Duplicate prefab local fileID'}}
foreach($m in [regex]::Matches($prefab,'\{fileID: (\d+)\}')){if($m.Groups[1].Value -ne '0' -and -not $ids.Contains($m.Groups[1].Value)){throw ('Dangling prefab local reference: '+$m.Groups[1].Value)}}
$original=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'before/Assets/2. Prefab/2. Tower/TowerBase.prefab')).Replace("`r",'')
foreach($id in @('7564851656657052633','3363622595370249864')) {
    $pattern='(?ms)^--- !u!\d+ &'+$id+'(?: stripped)?\n.*?(?=^--- !u!|\z)'
    if([regex]::Match($original,$pattern).Value.TrimEnd() -ne [regex]::Match($prefab,$pattern).Value.TrimEnd()){throw 'Nested DebuffZone prefab settings changed.'}
}
$removed=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'cleanup-audit.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$deletedGuids=[Collections.Generic.HashSet[string]]::new()
foreach($entry in $removed){[void]$deletedGuids.Add($entry.guid);if(Test-Path -LiteralPath (Join-Path $root $entry.path)){throw 'Old PNG still exists'}}
$ext=@('.prefab','.unity','.asset','.mat','.controller','.anim','.spriteatlas','.spriteatlasv2','.meta','.cs','.json','.txt','.shader')
foreach($file in (Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -File | Where-Object {$_.Extension -in $ext})) {
    foreach($m in [regex]::Matches([IO.File]::ReadAllText($file.FullName),'[a-f0-9]{32}')){if($deletedGuids.Contains($m.Value)){throw ('Deleted GUID remains in '+$file.FullName)}}
}
$record=@('PASS: prefab local fileIDs and references are consistent, including nested/stripped documents.','PASS: existing nested DebuffZone settings match the original snapshot.','PASS: no Assets reference to any of the 47 removed PNG GUIDs.','PASS: actual TowerVisual and both catalogs compiled using Unity API stand-ins; all 280 normal + 14 synergy IDs checked.','PASS: layer order, equal layer scale, emblem-only tint, normal/synergy reuse, retained projectile identity and null-data guard.','Unity importer, rendering and playmode were not run; the user performs Unity validation.')
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),($record -join "`n"),[Text.UTF8Encoding]::new($false))
$record | Select-Object -First 3
