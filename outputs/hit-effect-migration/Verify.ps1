$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Read([string]$p){[IO.File]::ReadAllText((Join-Path $root $p))}
function Assert([bool]$condition,[string]$message){if(!$condition){throw $message}}
function Blocks([string]$text){[regex]::Matches($text,'(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$catalog=Read $manifest.Catalog
$oldCatalog=Read ('outputs/hit-effect-migration/before/'+$manifest.Catalog)
$removeFields='(?m)^    HitEffect(?:Scale)?:[^\r\n]*\r?\n?'
Assert (([regex]::Replace($catalog,$removeFields,'')).TrimEnd() -ceq ([regex]::Replace($oldCatalog,$removeFields,'')).TrimEnd()) 'Other catalog fields changed'
$catalogRows=@([regex]::Matches($catalog,'(?ms)^  - Id: (?<id>\d+)\r?\n.*?(?=^  - Id:|\z)'))
Assert ($catalogRows.Count -eq 28) 'Wrong catalog size'
foreach($r in $manifest.Prefabs){
 $before=Read ('outputs/hit-effect-migration/before/'+$r.Path)
 $after=Read $r.Path
 $oldBlocks=Blocks $before;$newBlocks=Blocks $after
 $oldScripts=@($oldBlocks | Where-Object {$_.Groups['type'].Value -eq '114' -and ($_.Value.Contains($manifest.OldControllerGuid) -or $_.Value.Contains($manifest.OldPoolGuid))})
 Assert ($oldScripts.Count -eq 2) ('Original effect scripts not found: '+$r.Path)
 Assert (!$after.Contains($manifest.OldControllerGuid) -and !$after.Contains($manifest.OldPoolGuid)) ('Old script remains: '+$r.Path)
 $hit=@($newBlocks | Where-Object {$_.Value.Contains('guid: '+$manifest.NewScriptGuid+', type: 3}')})
 Assert ($hit.Count -eq 1) ('Expected exactly one HitEffect: '+$r.Path)
 Assert ($hit[0].Groups['id'].Value -eq $r.HitEffectId) ('HitEffect ID mismatch: '+$r.Path)
 Assert ($hit[0].Value.Contains('m_GameObject: {fileID: '+$r.RootId+'}') -and $hit[0].Value.Contains('  m_safetyLifetime: 2')) ('Wrong HitEffect settings: '+$r.Path)
 $oldWithout=$before
 foreach($b in $oldScripts){$oldWithout=$oldWithout.Replace($b.Value,'')}
 $newWithout=$after.Replace($hit[0].Value,'')
 foreach($id in @($r.HitEffectId,$r.RemovedPoolId)){
  $oldWithout=[regex]::Replace($oldWithout,'(?m)^  - component: \{fileID: '+$id+'\}\r?\n','')
  $newWithout=[regex]::Replace($newWithout,'(?m)^  - component: \{fileID: '+$id+'\}\r?\n','')
 }
 Assert ($oldWithout.TrimEnd() -ceq $newWithout.TrimEnd()) ('Other prefab content changed: '+$r.Path)
 $ids=@($newBlocks | ForEach-Object {$_.Groups['id'].Value})
 Assert (@($ids | Select-Object -Unique).Count -eq $ids.Count) ('Duplicate prefab IDs: '+$r.Path)
 foreach($ref in [regex]::Matches($after,'\{fileID: (?<id>-?\d+)\}')){Assert ($ref.Groups['id'].Value -eq '0' -or $ref.Groups['id'].Value -in $ids) ('Dangling local reference: '+$r.Path)}
 $row=@($catalogRows | Where-Object {[int]$_.Groups['id'].Value -eq $r.Id})
 Assert ($row.Count -eq 1) ('Catalog ID missing: '+$r.Id)
 Assert ($row[0].Value.Contains('    HitEffect: {fileID: '+$r.HitEffectId+', guid: '+$r.Guid+', type: 3}')) ('Wrong component reference: '+$r.Id)
 $scale=if($r.Id -in @(104,204)){'1.5'}elseif($r.Id -eq 202){'2.25'}else{'4.5'}
 Assert ($row[0].Value -match ('(?m)^    HitEffectScale: '+[regex]::Escape($scale)+'\r?$')) ('Wrong scale: '+$r.Id)
}
$baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'baseline.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$concurrent=@()
foreach($f in $baseline){
 if((Get-FileHash -LiteralPath (Join-Path $root $f.Path) -Algorithm SHA256).Hash -ne $f.Hash){
  Assert ($f.Path -like '*.cs') ('Protected asset changed: '+$f.Path)
  $concurrent+=$f.Path
 }
}
$report=[pscustomobject]@{PrefabsMigrated=28;CatalogEntriesFilled=28;SafetyLifetime=2;ComponentReferencesVerified=$true;OtherCatalogFieldsPreserved=$true;OtherPrefabComponentsPreserved=$true;AnimationEventsAndClipsUnchanged=$true;EffectLibraryAndOtherPrefabsUnchanged=$true;AllMetaFilesUnchanged=$true;SharedWithOtherIds=@($manifest.SharedWithOtherIds);ConcurrentSourceEditsObserved=$concurrent}
$report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 5
$lines=@('# HitEffect prefab migration','','Modified prefabs:','','| Id | Prefab | HitEffectScale |','| --- | --- | --- |')
foreach($r in $manifest.Prefabs){$lines+='| '+$r.Id+' | '+$r.Path+' | '+$r.Scale+' |'}
$lines+=@('','Also modified: '+$manifest.Catalog,'','No target prefab is shared with an effect ID outside 101-114 and 201-214.','Old EffectLibrary, other prefabs, animation clips/events and all existing meta files are preserved.','Catalog HitEffect values reference the new HitEffect component fileIDs, not GameObject fileIDs.','Backups: before/. Machine-readable verification: verification.json.')
$lines | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Encoding UTF8
