$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8=[Text.UTF8Encoding]::new($false)
function Read([string]$p){[IO.File]::ReadAllText((Join-Path $root $p))}
function Assert([bool]$condition,[string]$message){if(!$condition){throw $message}}
function Guid([string]$p){[regex]::Match((Read $p),'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value}
function Blocks([string]$text){[regex]::Matches($text,'(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')}
Assert (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'manifest.json'))) 'Already migrated; do not run twice'
$libraryPath='Assets/2. Prefab/4. Effect/EffectLibrary.asset'
$catalogPath='Assets/2. Prefab/4. Effect/ProjectileCatalog.asset'
$library=Read $libraryPath; $catalog=Read $catalogPath
$oldController=Guid 'Assets/3. Scripts/06. Effect/EffectController.cs.meta'
$oldPool=Guid 'Assets/3. Scripts/06. Effect/EffectPool2D.cs.meta'
$newScript=Guid 'Assets/3. Scripts/New/08. Projectile/HitEffect.cs.meta'
Assert ($newScript.Length -eq 32) 'HitEffect script must have an imported meta'
$entries=@([regex]::Matches($library,'(?m)^  - effectID: (?<id>\d+)\r?\n    effectPrefab: \{fileID: (?<root>-?\d+), guid: (?<guid>[a-f0-9]{32}), type: 3\}') | ForEach-Object {[pscustomobject]@{Id=[int]$_.Groups['id'].Value; RootId=$_.Groups['root'].Value; Guid=$_.Groups['guid'].Value}})
$targetIds=@(101..114)+@(201..214)
$targets=@($entries | Where-Object {$_.Id -in $targetIds} | Sort-Object Id)
Assert ($targets.Count -eq 28 -and @($targets.Id | Select-Object -Unique).Count -eq 28) 'Expected all 28 effect entries'
Assert (@($targets.Guid | Select-Object -Unique).Count -eq 28) 'Target entries share a prefab; inspect before migration'
$shared=@($entries | Where-Object {$_.Id -notin $targetIds -and $_.Guid -in $targets.Guid})
Assert ($shared.Count -eq 0) ('Target prefabs are shared with other effect IDs: '+($shared.Id -join ','))
$guidPaths=@{}
Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -Filter '*.prefab.meta' | ForEach-Object {
 $g=[regex]::Match([IO.File]::ReadAllText($_.FullName),'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
 $guidPaths[$g]=$_.FullName.Substring($root.Length+1).Replace('\','/').Replace('.meta','')
}
$changes=[Collections.Generic.List[object]]::new(); $records=[Collections.Generic.List[object]]::new()
foreach($target in $targets){
 Assert ($guidPaths.ContainsKey($target.Guid)) ('Missing prefab: '+$target.Id)
 $path=$guidPaths[$target.Guid];$source=Read $path;$blocks=Blocks $source
 $controller=@($blocks | Where-Object {$_.Groups['type'].Value -eq '114' -and $_.Value.Contains('guid: '+$oldController+',')})
 $pool=@($blocks | Where-Object {$_.Groups['type'].Value -eq '114' -and $_.Value.Contains('guid: '+$oldPool+',')})
 $anim=@($blocks | Where-Object {$_.Groups['type'].Value -eq '95'})
 $go=@($blocks | Where-Object {$_.Groups['type'].Value -eq '1' -and $_.Groups['id'].Value -eq $target.RootId})
 Assert ($controller.Count -eq 1 -and $pool.Count -eq 1 -and $anim.Count -eq 1 -and $go.Count -eq 1) ('Unexpected component layout: '+$path)
 foreach($component in @($controller[0],$pool[0],$anim[0])){Assert ($component.Value.Contains('m_GameObject: {fileID: '+$target.RootId+'}')) ('Effect component is not on animator root: '+$path)}
 $hitId=$controller[0].Groups['id'].Value;$poolId=$pool[0].Groups['id'].Value
 $replacement=@"
--- !u!114 &$hitId
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $($target.RootId)}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $newScript, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::SquareTD.HitEffect
  m_safetyLifetime: 2
"@
 $nl=if($source.Contains("`r`n")){"`r`n"}else{"`n"}
 $replacement=$replacement.Replace("`r`n","`n").Replace("`n",$nl).TrimEnd("`r","`n")+$nl
 $after=$source.Replace($controller[0].Value,$replacement).Replace($pool[0].Value,'')
 $after=[regex]::Replace($after,'(?m)^  - component: \{fileID: '+$poolId+'\}\r?\n','')
 Assert (!$after.Contains('{fileID: '+$poolId+'}')) ('Removed pool still referenced: '+$path)
 $changes.Add([pscustomobject]@{Path=$path;Before=$source;After=$after})
 $scale=if($target.Id -in @(104,204)){'1.5'}elseif($target.Id -eq 202){'2.25'}else{'4.5'}
 $records.Add([pscustomobject]@{Id=$target.Id;Path=$path;Guid=$target.Guid;RootId=$target.RootId;HitEffectId=$hitId;RemovedPoolId=$poolId;Scale=$scale})
}
$rows=@([regex]::Matches($catalog,'(?ms)^  - Id: (?<id>\d+)\r?\n.*?(?=^  - Id:|\z)'))
Assert ($rows.Count -eq 28) 'Expected 28 projectile catalog rows'
$newCatalog=$catalog
foreach($row in $rows){
 $id=[int]$row.Groups['id'].Value;$record=@($records | Where-Object {$_.Id -eq $id})
 Assert ($record.Count -eq 1) ('Unexpected projectile look ID: '+$id)
 $r=$record[0];$nl=if($row.Value.Contains("`r`n")){"`r`n"}else{"`n"}
 $clean=[regex]::Replace($row.Value,'(?m)^    HitEffect(?:Scale)?:[^\r\n]*\r?\n?','').TrimEnd("`r","`n")
 # The catalog field is HitEffect, so use the component fileID, not the root GameObject fileID.
 $newRow=$clean+$nl+'    HitEffect: {fileID: '+$r.HitEffectId+', guid: '+$r.Guid+', type: 3}'+$nl+'    HitEffectScale: '+$r.Scale+$nl
 $newCatalog=$newCatalog.Replace($row.Value,$newRow)
}
$changes.Add([pscustomobject]@{Path=$catalogPath;Before=$catalog;After=$newCatalog})
$targetPaths=@($records.Path)
$protected=@(Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -File | Where-Object {
 $_.Extension -in @('.cs','.anim','.meta','.prefab') -and $_.FullName.Substring($root.Length+1).Replace('\','/') -notin $targetPaths
})
$protected+=Get-Item -LiteralPath (Join-Path $root $libraryPath)
$baseline=@($protected | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($root.Length+1);Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
foreach($change in $changes){
 Assert ((Read $change.Path) -ceq $change.Before) ('File changed during preparation: '+$change.Path)
 $backup=Join-Path $PSScriptRoot ('before/'+$change.Path)
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
 [IO.File]::Copy((Join-Path $root $change.Path),$backup)
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'baseline.json'),(ConvertTo-Json -InputObject $baseline -Depth 4),$utf8)
foreach($change in $changes){
 Assert ((Read $change.Path) -ceq $change.Before) ('File changed before write: '+$change.Path)
 $bytes=[IO.File]::ReadAllBytes((Join-Path $root $change.Path));$bom=$bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
 [IO.File]::WriteAllText((Join-Path $root $change.Path),$change.After,[Text.UTF8Encoding]::new($bom))
}
$manifest=[pscustomobject]@{Prefabs=@($records);Catalog=$catalogPath;NewScriptGuid=$newScript;OldControllerGuid=$oldController;OldPoolGuid=$oldPool;SharedWithOtherIds=@($shared)}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'manifest.json'),($manifest | ConvertTo-Json -Depth 7),$utf8)
$records | Format-Table Id,Path,HitEffectId,Scale -AutoSize
'Migrated 28 effect prefabs and filled 28 projectile look entries; no target prefab shared with other IDs.'
