$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskRuntimeMap=@{Bow=3;Spear=4;Shield=5;Hammer=6;Fire=7;Water=8;Earth=9;Wind=10;Ice=11;Electric=12;Light=13;Dark=14}
$taskRegular=Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'outputs/remaining-tower-assets-v1/asset-manifest.json') | ConvertFrom-Json
$taskSynergy=Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $taskRoot 'outputs/synergy-towers-14-v3/manifest.json') | ConvertFrom-Json
function Ensure-Meta($taskPath,$taskImporter){
    $taskMetaPath=$taskPath+'.meta'
    if(!(Test-Path -LiteralPath $taskMetaPath)){
        [IO.File]::WriteAllText($taskMetaPath,("fileFormatVersion: 2`nguid: "+[Guid]::NewGuid().ToString('N')+"`n$taskImporter`n  externalObjects: {}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n"),[Text.UTF8Encoding]::new($false))
    }
    return [regex]::Match([IO.File]::ReadAllText($taskMetaPath),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
}
$taskScriptGuid=Ensure-Meta (Join-Path $taskRoot 'Assets/3. Scripts/01. Tower/TowerCharacterCatalog.cs') 'MonoImporter:'
$taskCatalogPath=Join-Path $taskRoot 'Assets/Resources/TowerCharacterCatalog.asset'
$taskCatalogGuid=Ensure-Meta $taskCatalogPath 'NativeFormatImporter:'
$taskCatalog="%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!114 &11400000`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 0}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: $taskScriptGuid, type: 3}`n  m_Name: TowerCharacterCatalog`n  m_EditorClassIdentifier: Assembly-CSharp::TowerCharacterCatalog`n  m_entries:`n"
$taskConnections=@()
foreach($taskGroup in ($taskRegular | Group-Object atlas)){
    $taskGroupKey=[regex]::Match($taskGroup.Name,'^Tower_(.+)_Characters_').Groups[1].Value
    $taskRuntimeId=$taskRuntimeMap[$taskGroupKey]
    if($null -eq $taskRuntimeId){throw ('Unknown tower family: '+$taskGroupKey)}
    $taskItems=@($taskGroup.Group | Sort-Object classId)
    $taskMeta=[IO.File]::ReadAllText((Join-Path $taskRoot ('Assets/4. Asset/2. Tower/CubeConcepts/Remaining_v1/'+$taskGroup.Name+'.meta')))
    $taskGuid=[regex]::Match($taskMeta,'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
    $taskCatalog+="  - emblemId: $taskRuntimeId`n    emblemName: "+$taskItems[0].emblem+"`n    characters:`n"
    foreach($taskItem in $taskItems){
        $taskMatch=[regex]::Match($taskMeta,'(?ms)      name: '+[regex]::Escape($taskItem.sprite)+'\r?\n.*?      internalID: (\d+)')
        if(!$taskMatch.Success){throw ('Missing sprite: '+$taskItem.sprite)}
        $taskFileId=$taskMatch.Groups[1].Value
        $taskCatalog+="    - {fileID: $taskFileId, guid: $taskGuid, type: 3}`n"
        $taskConnections += [ordered]@{name=$taskItem.name;emblem=$taskItem.emblem;runtimeEmblemId=$taskRuntimeId;planningEmblemId=$taskItem.emblemId;classId=$taskItem.classId;sprite=$taskItem.sprite;fileID=[int]$taskFileId;guid=$taskGuid}
    }
}
[IO.File]::WriteAllText($taskCatalogPath,$taskCatalog,[Text.UTF8Encoding]::new($false))
$taskPrefabPath=Join-Path $taskRoot 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
$taskPrefab=[IO.File]::ReadAllText($taskPrefabPath)
$taskCatalogLine="  m_regularCharacters: {fileID: 11400000, guid: $taskCatalogGuid, type: 2}"
if($taskPrefab -match '(?m)^  m_regularCharacters:'){
    $taskPrefab=[regex]::Replace($taskPrefab,'(?m)^  m_regularCharacters: [^\r\n]+',$taskCatalogLine)
}else{
    $taskPrefab=$taskPrefab.Replace('  m_synergyCharacters:',($taskCatalogLine+"`n  m_synergyCharacters:"))
}
[IO.File]::WriteAllText($taskPrefabPath,$taskPrefab,[Text.UTF8Encoding]::new($false))
$taskSynergyIds=@{Odin=6001;Thor=6002;Loki=6003;Hel=6004;Surtr=6005;Freyr=6006;Baldr=6007;Skadi=6008;Freyja=6009;Ymir=6010;Ullr=6011;Brunhild=6012;BrokEitri=6013;Njord=6014}
$taskSynergyPath=Join-Path $taskRoot 'Assets/Resources/SynergyCharacterCatalog.asset'
$taskOldSynergy=[IO.File]::ReadAllText($taskSynergyPath)
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'before'))
if(!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'before/SynergyCharacterCatalog.asset'))){
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'before/SynergyCharacterCatalog.asset'),$taskOldSynergy,[Text.UTF8Encoding]::new($false))
}
$taskNewSynergy=$taskOldSynergy.Substring(0,$taskOldSynergy.IndexOf('  m_entries:'))+"  m_entries:`n"
foreach($taskEntry in ($taskSynergy | Sort-Object {$taskSynergyIds[$_.name]})){
    $taskId=$taskSynergyIds[$taskEntry.name]
    if(!$taskId){throw 'Unknown synergy'}
    $taskMeta=[IO.File]::ReadAllText((Join-Path $taskRoot ($taskEntry.unityAsset+'.meta')))
    $taskGuid=[regex]::Match($taskMeta,'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
    $taskNewSynergy+="  - towerID: $taskId`n    characterName: "+$taskEntry.nameKo+"`n    front: {fileID: 21300000, guid: $taskGuid, type: 3}`n"
    $taskConnections += [ordered]@{name=$taskEntry.nameKo;towerID=$taskId;fileID=21300000;guid=$taskGuid}
}
[IO.File]::WriteAllText($taskSynergyPath,$taskNewSynergy,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'bindings.json'),($taskConnections | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
'Connected 48 new normal portraits and 14 synergy portraits; preserved existing Sword/Axe layer bindings.'
