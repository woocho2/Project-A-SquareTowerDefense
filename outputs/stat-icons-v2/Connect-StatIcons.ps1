$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskPrefab=Join-Path $taskRoot 'Assets/2. Prefab/Canvas.prefab'
$taskText=[IO.File]::ReadAllText($taskPrefab)
$taskPattern='(?ms)^--- !u!(\d+) &(\d+)[^\r\n]*\r?\n.*?(?=^---|\z)'
$taskBlocks=@{};$taskNames=@{};$taskGoRects=@{};$taskRectGos=@{};$taskParents=@{};$taskImages=@()
foreach($m in [regex]::Matches($taskText,$taskPattern)){
    $id=$m.Groups[2].Value;$type=$m.Groups[1].Value;$b=$m.Value;$taskBlocks[$id]=$b
    if($type -eq '1'){$taskNames[$id]=[regex]::Match($b,'(?m)^  m_Name: ([^\r\n]+)').Groups[1].Value}
    if($type -eq '224'){
        $go=[regex]::Match($b,'m_GameObject: \{fileID: (\d+)').Groups[1].Value
        $taskGoRects[$go]=$id;$taskRectGos[$id]=$go;$taskParents[$id]=[regex]::Match($b,'m_Father: \{fileID: (\d+)').Groups[1].Value
    }
    if($type -eq '114' -and $b -match '(?m)^  m_Sprite: '){$taskImages+=@{id=$id;go=[regex]::Match($b,'m_GameObject: \{fileID: (\d+)').Groups[1].Value;block=$b}}
}
$taskIconMap=@{
    'Img_Power_Symbol'='Icon_Tower_AttackPower';'Img_Range_Symbol'='Icon_Tower_Range'
    'Img_Action_Symbol'='Icon_Tower_ActionInterval';'Img_AttackCount_Symbol'='Icon_Tower_AttackCount'
    'Img_CriticalRate_Symbol'='Icon_Tower_CriticalRate';'Img_CriticalDamage_Symbol'='Icon_Tower_CriticalDamage'
    'Img_AbilityValue_Symbol'='Icon_Tower_AbilityValue';'Img_Mastery_Symbol'='Icon_Tower_Mastery'
    'Img_Stack_Symbol'='Icon_Tower_Stack';'Img_AttackType_Symbol'='Icon_Tower_AttackType'
    'Img_Skill_Symbol'='Icon_Tower_Skill';'Img_Duration_Symbol'='Icon_Tower_Duration'
}
$taskGuid=[regex]::Match([IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/5. UI/Stat_Icon_Atlas_v2.png.meta')),'(?m)^guid: (\w+)').Groups[1].Value
$taskManifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$taskChanged=@{};$taskAssignments=@()
foreach($image in $taskImages){
    $name=$taskNames[$image.go];if(!$taskIconMap.ContainsKey($name)){continue}
    $path=@($name);$rect=$taskGoRects[$image.go]
    while($taskParents[$rect] -and $taskParents[$rect] -ne '0'){$rect=$taskParents[$rect];$path+=$taskNames[$taskRectGos[$rect]]}
    if('TowerInfoPanel' -notin $path){continue}
    $entry=@($taskManifest | Where-Object name -eq $taskIconMap[$name])
    if($entry.Count -ne 1){throw "Missing unique sprite: $name"}
    $newRef="  m_Sprite: {fileID: $($entry[0].fileID), guid: $taskGuid, type: 3}"
    $taskChanged[$image.id]=[regex]::Replace($image.block,'(?m)^  m_Sprite: [^\r\n]+',$newRef)
    $taskAssignments+=[pscustomobject]@{imageID=$image.id;path=($path[($path.Count-1)..0] -join '/');sprite=$entry[0].name;fileID=$entry[0].fileID;guid=$taskGuid;oldReference=[regex]::Match($image.block,'(?m)^  m_Sprite: [^\r\n]+').Value.Trim()}
}
if($taskAssignments.Count -ne 12){throw "Expected twelve TowerInfoPanel icons; found $($taskAssignments.Count)"}
$taskResult=[regex]::Replace($taskText,$taskPattern,[Text.RegularExpressions.MatchEvaluator]{param($m) $id=$m.Groups[2].Value;if($taskChanged.ContainsKey($id)){return $taskChanged[$id]};return $m.Value})
$strip='(?m)^  m_Sprite: [^\r\n]+'
if([regex]::Replace($taskResult,$strip,'') -cne [regex]::Replace($taskText,$strip,'')){throw 'Non-sprite prefab data changed'}
$taskUtf8=New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($taskPrefab,$taskResult,$taskUtf8)
# Update only explicit sprite overrides for these same prefab Images in scenes.
$taskCanvasGuid=[regex]::Match([IO.File]::ReadAllText($taskPrefab+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value
$taskById=@{};foreach($entry in $taskAssignments){$taskById[$entry.imageID]=$entry}
$taskOverrides=0
foreach($scene in Get-ChildItem -LiteralPath (Join-Path $taskRoot 'Assets/1. Scenes') -Filter '*.unity' -File){
    $original=[IO.File]::ReadAllText($scene.FullName)
    $updated=[regex]::Replace($original,'(?m)^    - target: \{fileID: (\d+), guid: '+$taskCanvasGuid+', type: 3\}\r?\n      propertyPath: m_Sprite\r?\n      value: [^\r\n]*\r?\n      objectReference: [^\r\n]+',[Text.RegularExpressions.MatchEvaluator]{param($m)
        $id=$m.Groups[1].Value;if(!$taskById.ContainsKey($id)){return $m.Value}
        $entry=$taskById[$id];$script:taskOverrides++
        return [regex]::Replace($m.Value,'objectReference: [^\r\n]+',"objectReference: {fileID: $($entry.fileID), guid: $taskGuid, type: 3}")
    })
    if($updated -cne $original){[IO.File]::WriteAllText($scene.FullName,$updated,$taskUtf8)}
}
$taskAssignments | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'assignments.json') -Encoding UTF8
Write-Output "PASS: 12 TowerInfoPanel sprites connected; $taskOverrides scene sprite overrides updated. Rects, labels, colors and gameplay fields unchanged."
