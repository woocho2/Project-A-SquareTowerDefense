$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskOriginalPath=Join-Path $PSScriptRoot 'before/EffectLibrary.asset'
if(Test-Path -LiteralPath $taskOriginalPath){$taskOriginal=[IO.File]::ReadAllText($taskOriginalPath)}else{
    $taskOriginal=(git show 'HEAD:Assets/2. Prefab/4. Effect/EffectLibrary.asset') -join "`n"
    [IO.File]::WriteAllText($taskOriginalPath,$taskOriginal,[Text.UTF8Encoding]::new($false))
}
$taskMap=@{1=1;2=2;3=3;4=4;5=5;6=6;7=7;8=11;9=12;10=10;11=9;12=13;13=14}
$taskGuidPaths=@{}
foreach($taskMetaPath in (rg --files (Join-Path $taskRoot 'Assets') -g '*.prefab.meta')){
    $taskGuid=[regex]::Match([IO.File]::ReadAllText($taskMetaPath),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
    if($taskGuid){$taskGuidPaths[$taskGuid]=$taskMetaPath.Substring(0,$taskMetaPath.Length-5)}
}
$taskLibrary=$taskOriginal.Substring(0,$taskOriginal.IndexOf('  effectEntries:'))+"  effectEntries:`n"
$taskLinks=@()
function Effect-Link($taskId,$taskPath,$taskPreferredId){
    if(!(Test-Path -LiteralPath $taskPath)){throw ('Missing effect prefab '+$taskPath)}
    $taskGuid=[regex]::Match([IO.File]::ReadAllText($taskPath+'.meta'),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
    $taskText=[IO.File]::ReadAllText($taskPath)
    $taskFileId=$taskPreferredId
    if(!$taskFileId -or $taskText -notmatch ('(?m)^--- !u!1 &'+$taskFileId+'\r?$')){$taskFileId=[regex]::Match($taskText,'(?m)^--- !u!1 &(\d+)').Groups[1].Value}
    return [pscustomobject]@{effectID=$taskId;prefab=$taskPath;guid=$taskGuid;fileID=$taskFileId}
}
foreach($taskMatch in [regex]::Matches($taskOriginal,'(?m)^  - effectID: (\d+)\r?\n    effectPrefab: \{fileID: (\d+), guid: ([a-f0-9]+), type: 3\}')){
    $taskOldId=[int]$taskMatch.Groups[1].Value;$taskClass=[int][Math]::Floor($taskOldId/100);$taskOldEmblem=$taskOldId%100
    $taskId=$taskClass*100+$taskMap[$taskOldEmblem]
    if($taskClass -in @(1,2)){$taskPath=Join-Path $taskRoot ('Assets/2. Prefab/4. Effect/E'+$taskOldId+'.prefab')}
    elseif($taskOldId -eq 302){$taskPath=Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/E102.prefab'}
    else{
        $taskPath=$taskGuidPaths[$taskMatch.Groups[3].Value]
        if(!$taskPath){$taskPath=Join-Path $taskRoot 'Assets/2. Prefab/MapBuff.prefab'}
    }
    $taskLinks+=Effect-Link $taskId $taskPath $taskMatch.Groups[2].Value
}
foreach($taskClass in @(1,2,3,4)){$taskLinks+=Effect-Link ($taskClass*100+8) (Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/Effect_Water_Hit.prefab') '5220447335354414950'}
foreach($taskLink in $taskLinks){$taskLibrary+='  - effectID: '+$taskLink.effectID+"`n    effectPrefab: {fileID: "+$taskLink.fileID+', guid: '+$taskLink.guid+", type: 3}`n"}
[IO.File]::WriteAllText((Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/EffectLibrary.asset'),$taskLibrary,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'effect-bindings.json'),($taskLinks | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
'Rebuilt '+$taskLinks.Count+' canonical effect entries including all 28 attack keys.'
