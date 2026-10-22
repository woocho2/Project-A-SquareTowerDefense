$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Read([string]$relative){[IO.File]::ReadAllText((Join-Path $repoRoot $relative))}
function Assert([bool]$condition,[string]$message){if(!$condition){throw $message}}
function Entries([string]$text){$text.Substring($text.IndexOf('  m_entries:'))}
function Blocks([string]$yaml){[regex]::Matches($yaml,'(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')}
$wiring=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'wiring.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$catalogReport=@()
$spriteReferences=[Collections.Generic.List[object]]::new()
foreach($c in $wiring.Catalogs){
 $path='Assets/Resources/'+$c.Name+'.asset'
 $original=[IO.File]::ReadAllText((Join-Path $PSScriptRoot ('before/'+$path)))
 $new=Read $path; $old=Read $c.OldPath
 Assert ((Entries $new) -ceq (Entries $original)) ('New entries changed: '+$c.Name)
 Assert ((Entries $old) -ceq (Entries $original)) ('Old entries changed: '+$c.Name)
 Assert ((Read ($c.OldPath+'.meta')) -ceq (Read ('outputs/tower-visual-wiring/before/'+$path+'.meta'))) ('Old meta changed: '+$c.Name)
 Assert ($new.Contains('guid: '+$c.ScriptGuid+', type: 3}')) ('Wrong new script: '+$c.Name)
 Assert ((Read ($path+'.meta')).Contains('guid: '+$c.NewGuid)) ('Wrong new catalog GUID: '+$c.Name)
 Assert ($new.Contains('Assembly-CSharp::SquareTD.'+$c.Name)) ('Wrong class identifier: '+$c.Name)
 if($c.Name -eq 'TowerCharacterCatalog'){
  $items=[regex]::Matches($new,'(?ms)^  - emblemId:.*?(?=^  - emblemId:|\z)')
  Assert ($items.Count -eq 14) 'Expected 14 emblems'
  foreach($item in $items){Assert ([regex]::Matches($item.Value,'(?m)^    - \{fileID:').Count -eq 4) 'Expected 4 portraits per emblem'}
 }else{Assert ([regex]::Matches($new,'(?m)^  - towerID:').Count -eq 14) 'Expected 14 synergy entries'}
 foreach($m in [regex]::Matches((Entries $new),'\{fileID: (?<id>-?\d+), guid: (?<guid>[a-f0-9]{32}), type: 3\}')){
  $spriteReferences.Add([pscustomobject]@{Id=$m.Groups['id'].Value;Guid=$m.Groups['guid'].Value})
 }
 $catalogReport += [pscustomobject]@{Name=$c.Name;EntriesPreserved=$true;OldGuidPreserved=$true;NewScriptConnected=$true}
}
$tower=Read $wiring.Prefab; $blocks=Blocks $tower
$ids=@($blocks | ForEach-Object {$_.Groups['id'].Value})
Assert (@($ids | Group-Object | Where-Object {$_.Count -gt 1}).Count -eq 0) 'Duplicate prefab file IDs'
$visual=@($blocks | Where-Object {$_.Groups['id'].Value -eq $wiring.VisualComponentId})
Assert ($visual.Count -eq 1) 'Missing visual component'
Assert ($visual[0].Value.Contains('guid: '+$wiring.VisualScriptGuid+', type: 3}')) 'Wrong visual script'
foreach($prop in $wiring.RendererMap.psobject.Properties){
 Assert ($visual[0].Value.Contains('  '+$prop.Name+': {fileID: '+$prop.Value+'}')) ('Renderer not connected: '+$prop.Name)
 Assert ($ids -contains $prop.Value) ('Renderer missing in prefab: '+$prop.Name)
}
foreach($c in $wiring.Catalogs){Assert ($visual[0].Value.Contains('guid: '+$c.NewGuid+', type: 2}')) 'Prefab references wrong catalog'}
$oldTower=Read 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
foreach($c in $wiring.Catalogs){Assert ($oldTower.Contains('guid: '+$c.OldGuid+', type: 2}')) 'Old TowerBase catalog reference changed'}
$sourceBlocks=Blocks $oldTower
$sourceVisual=@($sourceBlocks | Where-Object {$_.Value.Contains('m_EditorClassIdentifier: Assembly-CSharp::TowerVisual')})[0].Value
# All serialized presentation settings (sizes, arrays and four colors) are identical.
$sourceSettings=$sourceVisual.Substring($sourceVisual.IndexOf('  m_canvasWorldSize:')).TrimEnd()
$targetSettings=$visual[0].Value.Substring($visual[0].Value.IndexOf('  m_canvasWorldSize:')).TrimEnd()
Assert ($sourceSettings -ceq $targetSettings) 'Presentation settings differ from TowerBase'
Assert ([regex]::Match($targetSettings,'(?ms)^  m_emblemSprites:\r?\n(?<entries>.*?)(?=^  \w)').Groups['entries'].Value.Split("`n").Where({$_ -match '^  - '}).Count -eq 14) 'Expected 14 emblems'
Assert ([regex]::Match($targetSettings,'(?ms)^  m_tierSprites:\r?\n(?<entries>.*?)(?=^  \w)').Groups['entries'].Value.Split("`n").Where({$_ -match '^  - '}).Count -eq 5) 'Expected 5 tiers'
# Existing prefab blocks are untouched except for the single root component-list entry.
$beforeTower=Read 'outputs/tower-visual-wiring/before/Assets/2. Prefab/2. Tower/Tower.prefab'
$removedVisual=$tower.Replace($visual[0].Value,'')
$removedVisual=[regex]::Replace($removedVisual,'(?m)^  - component: \{fileID: '+$wiring.VisualComponentId+'\}\r?\n','')
Assert ($removedVisual.TrimEnd() -ceq $beforeTower.TrimEnd()) 'Unrelated Tower prefab data changed'

$metas=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/4. Asset/2. Tower') -Filter '*.meta' -Recurse)
$spriteMetas=@{}
foreach($meta in $metas){$s=[IO.File]::ReadAllText($meta.FullName);$g=[regex]::Match($s,'(?m)^guid: ([a-f0-9]{32})');if($g.Success){$spriteMetas[$g.Groups[1].Value]=$s}}
foreach($ref in $spriteReferences){
 Assert ($spriteMetas.ContainsKey($ref.Guid)) ('Portrait texture missing: '+$ref.Guid)
 Assert ($spriteMetas[$ref.Guid] -match ('(?m)^      internalID: '+$ref.Id+'\r?$')) ('Portrait sprite ID missing: '+$ref.Id)
}
$baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'baseline.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$concurrent=@()
foreach($file in $baseline){
 if((Get-FileHash -LiteralPath (Join-Path $repoRoot $file.Path) -Algorithm SHA256).Hash -ne $file.Hash){
  Assert ($file.Path -like '*.cs') ('Protected scene/prefab changed: '+$file.Path)
  $concurrent += $file.Path
 }
}
$report=[pscustomobject]@{
 Catalogs=$catalogReport; RegularEntries=14; PortraitsPerEntry=4; SynergyEntries=14
 PortraitSpriteReferencesResolved=$spriteReferences.Count
 PrefabNewComponentOnly=$true; RendererReferencesValid=$true; SettingsMatchTowerBase=$true
 OldTowerBaseAndScenesUnchanged=$true; ConcurrentSourceEditsObserved=$concurrent
 PlayModeVerification='Blocked by existing/in-progress DamageCalculator.cs compile errors; no C# files edited by this task.'
}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 8
