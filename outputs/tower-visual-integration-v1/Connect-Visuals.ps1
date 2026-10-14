$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8=[Text.UTF8Encoding]::new($true)
$portraits=Get-Content -LiteralPath (Join-Path $projectRoot 'outputs/tower-front-portraits-v2/asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$layers=Get-Content -LiteralPath (Join-Path $projectRoot 'outputs/tower-tier-layers-v1/asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$roster=Get-Content -LiteralPath (Join-Path $projectRoot 'outputs/tower-front-portraits-v2/roster.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($portraits.Count -ne 70 -or $layers.Count -ne 12){throw 'Unexpected asset counts.'}
function Ref($entry){'{fileID: '+$entry.fileID+', guid: '+$entry.guid+', type: 3}'}
function Read([string]$relative){[IO.File]::ReadAllText((Join-Path $projectRoot $relative)).Replace("`r",'').TrimStart([char]0xfeff)}
function Header([string]$yaml){$at=$yaml.IndexOf("  m_entries:`n");if($at -lt 0){throw 'Missing catalog entries'};$yaml.Substring(0,$at)+"  m_entries:`n"}
function Field([string]$yaml,[string]$name){$m=[regex]::Match($yaml,'(?m)^  '+$name+': .*');if(-not $m.Success){throw ('Missing '+$name)};$m.Value+"`n"}
function ArrayField([string]$yaml,[string]$name){$m=[regex]::Match($yaml,'(?m)^  '+$name+':\n(?:  - [^\n]+\n)+');if(-not $m.Success){throw ('Missing array '+$name)};$m.Value}
$regularPath='Assets/Resources/TowerCharacterCatalog.asset'
$synergyPath='Assets/Resources/SynergyCharacterCatalog.asset'
$prefabPath='Assets/2. Prefab/2. Tower/TowerBase.prefab'
$regularOld=Read $regularPath;$synergyOld=Read $synergyPath;$prefabOld=Read $prefabPath
$regular=Header $regularOld
foreach($group in @($roster | Where-Object kind -eq 'normal' | Sort-Object emblem)) {
    $regular+='  - emblemId: '+$group.emblem+"`n    emblemName: "+$group.label+"`n    characters:`n"
    $entries=@($portraits | Where-Object {$_.kind -eq 'normal' -and $_.emblemId -eq $group.emblem} | Sort-Object attackClass)
    if($entries.Count -ne 4){throw 'Expected all 4 attack classes.'}
    foreach($entry in $entries){$regular+='    - '+(Ref $entry)+"`n"}
}
$synergy=Header $synergyOld
foreach($entry in @($portraits | Where-Object kind -eq 'synergy' | Sort-Object towerIdExample)) {
    $synergy+='  - towerID: '+$entry.towerIdExample+"`n    characterName: "+$entry.concept+"`n    front: "+(Ref $entry)+"`n"
}
$documents=[regex]::Matches($prefabOld,'(?ms)^--- !u!(\d+) &(\d+)(?: stripped)?\n.*?(?=^--- !u!|\z)')
$visualDoc=@($documents | Where-Object {$_.Groups[2].Value -eq '3374333988479624537'})[0].Value
$visualPrefix=$visualDoc.Substring(0,$visualDoc.IndexOf("  m_emblemRenderer:"))
$visual=$visualPrefix+@'
  m_backgroundRenderer: {fileID: 69109820115675441}
  m_characterRenderer: {fileID: 7911596970232653239}
  m_decorationRenderer: {fileID: 652120831702425239}
  m_emblemRenderer: {fileID: 7100000000000001103}
'@+"`n"
foreach($kind in @('Background','Decoration')) {
    $name=if($kind -eq 'Background'){'m_backgroundSprites'}else{'m_decorationSprites'}
    $visual+='  '+$name+":`n"
    foreach($entry in @($layers | Where-Object layer -eq $kind | Sort-Object fileID)){$visual+='  - '+(Ref $entry)+"`n"}
}
$visual+=(Field $visualDoc 'm_regularCharacters')+(Field $visualDoc 'm_synergyCharacters')
$visual+=(Field $visualDoc 'm_swordCanvasWorldSize').Replace('m_swordCanvasWorldSize','m_canvasWorldSize')+(Field $visualDoc 'm_synergyCanvasWorldSize')
$visual+=(ArrayField $visualDoc 'm_emblemSprites')+(ArrayField $visualDoc 'm_tierSprites')+(Field $visualDoc 'm_backgroundSprite')
foreach($name in @('m_splashColor','m_targetColor','m_buffColor','m_debuffColor')){$visual+=Field $visualDoc $name}
$header=$prefabOld.Substring(0,$prefabOld.IndexOf('--- !u!'))
$result=[Text.StringBuilder]::new($header)
$hero=@($portraits | Where-Object {$_.towerIdExample -eq 1101})[0]
$bg=@($layers | Where-Object {$_.layer -eq 'Background' -and $_.tier -eq 'Bronze'})[0]
$deco=@($layers | Where-Object {$_.layer -eq 'Decoration' -and $_.tier -eq 'Bronze'})[0]
foreach($document in $documents) {
    $id=$document.Groups[2].Value;$value=$document.Value
    if($id -in @('7100000000000001001','7100000000000001002','7100000000000001003')){continue}
    switch($id) {
        '3374333988479624537' {$value=$visual}
        '7922541315568409329' {$value=$value.Replace("  - {fileID: 7100000000000001002}`n",'')}
        '446036322750871900' {$value=$value.Replace('m_Name: Emblem','m_Name: Character')}
        '2127725103623809396' {$value=$value.Replace('m_Name: Tier','m_Name: Decoration')}
        '4214382651906334009' {$value=$value.Replace('m_Name: Color','m_Name: Background')}
        '7100000000000001101' {$value=$value.Replace('m_Name: Headgear','m_Name: Emblem')}
        '2443113694740716601' {$value=$value.Replace('m_towerVisual: {fileID: 0}','m_towerVisual: {fileID: 3374333988479624537}')}
    }
    if($id -in @('777865494511794056','4911461941969686337','4117134642184555889')) {
        $value=[regex]::Replace($value,'(?m)^  m_LocalPosition: .*','  m_LocalPosition: {x: 0, y: 0, z: 0}')
        $value=[regex]::Replace($value,'(?m)^  m_LocalScale: .*','  m_LocalScale: {x: 0.95, y: 0.95, z: 1}')
    }
    if($id -eq '7100000000000001102') {
        $value=[regex]::Replace($value,'(?m)^  m_LocalPosition: .*','  m_LocalPosition: {x: 0, y: 0.475, z: 0}')
        $value=[regex]::Replace($value,'(?m)^  m_LocalScale: .*','  m_LocalScale: {x: 0.25, y: 0.25, z: 1}')
    }
    if($id -in @('7911596970232653239','652120831702425239','69109820115675441','7100000000000001103')) {
        $sprite=switch($id){'7911596970232653239'{Ref $hero};'652120831702425239'{Ref $deco};'69109820115675441'{Ref $bg};'7100000000000001103'{'{fileID: 1001, guid: 7e3f1605aca04ab5b6e525a542b6b7b3, type: 3}'}}
        $order=switch($id){'7911596970232653239'{3};'652120831702425239'{4};'69109820115675441'{2};'7100000000000001103'{5}}
        $value=[regex]::Replace($value,'(?m)^  m_Sprite: .*',('  m_Sprite: '+$sprite))
        $value=[regex]::Replace($value,'(?m)^  m_SortingOrder: .*',('  m_SortingOrder: '+$order))
        $value=[regex]::Replace($value,'(?m)^  m_Enabled: .*','  m_Enabled: 1')
        $value=[regex]::Replace($value,'(?m)^  m_Color: .*',$(if($id -eq '7100000000000001103'){(Field $visualDoc 'm_splashColor').TrimEnd().Replace('m_splashColor','m_Color')}else{'  m_Color: {r: 1, g: 1, b: 1, a: 1}'}))
    }
    [void]$result.Append($value)
}
if([regex]::Matches($regular,'(?m)^  - emblemId:').Count -ne 14 -or [regex]::Matches($synergy,'(?m)^  - towerID:').Count -ne 14){throw 'Catalog entries invalid'}
if($result.ToString() -match '710000000000000100[123]|m_swordCharacterSprites|m_synergyCharacterMaterial'){throw 'Old layers remain in prefab'}
# Retain exact current files before editing; the prefab already contains user changes.
foreach($path in @($regularPath,$synergyPath,$prefabPath)) {
    $backup=Join-Path $PSScriptRoot ('before/'+$path)
    New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($backup)) | Out-Null
    if(-not(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath (Join-Path $projectRoot $path) -Destination $backup}
}
[IO.File]::WriteAllText((Join-Path $projectRoot $regularPath),$regular,$utf8)
[IO.File]::WriteAllText((Join-Path $projectRoot $synergyPath),$synergy,$utf8)
[IO.File]::WriteAllText((Join-Path $projectRoot $prefabPath),$result.ToString(),$utf8)
Write-Output 'Connected 56 regular + 14 synergy portraits, 6 backgrounds and 6 decorations. TowerBase uses Background / Character / Decoration / Emblem.'
