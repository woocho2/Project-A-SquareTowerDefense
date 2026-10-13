$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskDone=Join-Path $PSScriptRoot 'emblem-migration.json'
if(Test-Path -LiteralPath $taskDone){throw 'Migration already applied; use recorded mapping rather than remapping canonical data.'}
$taskMap=@{0=0;1=1;2=2;3=3;4=4;5=5;6=6;7=7;8=11;9=12;10=10;11=9;12=13;13=14}
$taskBuffMap=@{0=0;1=1;2=2;3=3;4=4;5=5;6=6;7=7;8=11;9=12;10=10;11=9;12=13;13=14;14=15}
$taskCsvPath=Join-Path $taskRoot 'Assets/Resources/TowerDataCSV.csv'
$taskRows=Import-Csv -LiteralPath $taskCsvPath -Encoding UTF8
if($taskRows.Count -ne 260){throw 'Expected the unmigrated 260-row tower table.'}
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'before'))
Copy-Item -LiteralPath $taskCsvPath -Destination (Join-Path $PSScriptRoot 'before/TowerDataCSV.csv')
$taskWater=@();$taskWaterNames=@('파도술사','운디네','나이아드','세이렌')
foreach($taskTemplate in @($taskRows | Where-Object {([int]$_.ID%100) -eq 10})){
    $taskWaterRow=$taskTemplate.PSObject.Copy();$taskClass=[int][Math]::Floor(([int]$taskTemplate.ID%1000)/100)
    $taskWaterRow.ID=([int]$taskTemplate.ID-10+8).ToString();$taskWaterRow.Name=$taskWaterNames[$taskClass-1]
    $taskWaterRow.HitEffectID=($taskClass*100+8).ToString()
    if($taskClass -eq 3){$taskWaterRow.buffTarget='Water'}
    if($taskClass -eq 4){$taskWaterRow.debuffTarget='Water'}
    $taskWater+=$taskWaterRow
}
foreach($taskRow in $taskRows){
    $taskOldId=[int]$taskRow.ID;$taskRow.ID=($taskOldId-($taskOldId%100)+$taskMap[$taskOldId%100]).ToString()
    $taskOldEffect=[int]$taskRow.HitEffectID;$taskRow.HitEffectID=($taskOldEffect-($taskOldEffect%100)+$taskMap[$taskOldEffect%100]).ToString()
}
$taskAll=@($taskRows+$taskWater | Sort-Object {[int]$_.ID})
$taskColumns=@($taskRows[0].PSObject.Properties.Name)
$taskLines=@(($taskColumns -join ','))
foreach($taskRow in $taskAll){$taskValues=@($taskColumns | ForEach-Object {[string]$taskRow.$_});$taskLines+=($taskValues -join ',')}
[IO.File]::WriteAllLines($taskCsvPath,[string[]]$taskLines,[Text.UTF8Encoding]::new($false))
# Resources paths are keyed by color+emblem. Stage every changed file first to prevent collisions.
$taskProjectileDir=Join-Path $taskRoot 'Assets/Resources/Projectiles'
$taskStage=Join-Path $PSScriptRoot 'projectile-migration-staging'
[void][IO.Directory]::CreateDirectory($taskStage)
$taskMoves=@()
foreach($taskClass in @(1,2)){
    foreach($taskOldEmblem in @(8,9,11,12,13)){
        $taskOld=$taskClass*100+$taskOldEmblem;$taskNew=$taskClass*100+$taskMap[$taskOldEmblem]
        $taskMoves+=[pscustomobject]@{old=$taskOld;new=$taskNew}
        foreach($taskSuffix in @('.prefab','.prefab.meta')){
            $taskSource=Join-Path $taskProjectileDir ($taskOld.ToString()+$taskSuffix)
            $taskStaged=Join-Path $taskStage ($taskOld.ToString()+$taskSuffix)
            if(!(Test-Path -LiteralPath $taskSource)){throw ('Missing projectile '+$taskSource)}
            Move-Item -LiteralPath $taskSource -Destination $taskStaged
        }
    }
}
foreach($taskMove in $taskMoves){
    $taskStaged=Join-Path $taskStage ($taskMove.old.ToString()+'.prefab')
    $taskText=[IO.File]::ReadAllText($taskStaged)
    $taskText=[regex]::Replace($taskText,'(?m)^  m_Name: '+$taskMove.old+'\r?$','  m_Name: '+$taskMove.new)
    [IO.File]::WriteAllText($taskStaged,$taskText,[Text.UTF8Encoding]::new($false))
    foreach($taskSuffix in @('.prefab','.prefab.meta')){
        $taskTarget=Join-Path $taskProjectileDir ($taskMove.new.ToString()+$taskSuffix)
        if(Test-Path -LiteralPath $taskTarget){throw ('Migration target collision: '+$taskTarget)}
        Move-Item -LiteralPath (Join-Path $taskStage ($taskMove.old.ToString()+$taskSuffix)) -Destination $taskTarget
    }
}
# Only typed serialized enum fields and effect keys are remapped; GUID references retain their identities.
$taskChanged=@()
$taskTypedPaths=rg -l 'effectID:|hitEffectID:|buffTarget:|debuffTarget:|m_currentDebuffTarget:' (Join-Path $taskRoot 'Assets') -g '*.asset' -g '*.prefab' -g '*.unity'
foreach($taskPath in $taskTypedPaths){
    $taskText=[IO.File]::ReadAllText($taskPath);$taskOldText=$taskText
    $taskText=[regex]::Replace($taskText,'(?m)^(\s*(?:- )?(?:effectID|hitEffectID): )(\d+)',{param($m) $taskId=[int]$m.Groups[2].Value;$taskEmblem=$taskId%100;if($taskMap.ContainsKey($taskEmblem)){$m.Groups[1].Value+($taskId-$taskEmblem+$taskMap[$taskEmblem])}else{$m.Value}})
    $taskText=[regex]::Replace($taskText,'(?m)^(\s*buffTarget: )(\d+)',{param($m) $taskId=[int]$m.Groups[2].Value;if($taskBuffMap.ContainsKey($taskId)){$m.Groups[1].Value+$taskBuffMap[$taskId]}else{$m.Value}})
    $taskText=[regex]::Replace($taskText,'(?m)^(\s*(?:debuffTarget|m_currentDebuffTarget): )(\d+)',{param($m) $taskId=[int]$m.Groups[2].Value;if($taskMap.ContainsKey($taskId)){$m.Groups[1].Value+$taskMap[$taskId]}else{$m.Value}})
    if($taskText -ne $taskOldText){$taskChanged+=$taskPath;[IO.File]::WriteAllText($taskPath,$taskText,[Text.UTF8Encoding]::new($false))}
}
$taskDebuffPath=Join-Path $taskRoot 'Assets/2. Prefab/DebuffZone.prefab'
$taskDebuff=[IO.File]::ReadAllText($taskDebuffPath)
$taskDebuff=[regex]::Replace($taskDebuff,'(?m)^(  - target: )(\d+)',{param($m) $m.Groups[1].Value+$taskMap[[int]$m.Groups[2].Value]})
[IO.File]::WriteAllText($taskDebuffPath,$taskDebuff,[Text.UTF8Encoding]::new($false))
$taskMapRecord=[ordered]@{};foreach($taskKey in ($taskMap.Keys | Sort-Object)){$taskMapRecord[$taskKey.ToString()]=$taskMap[$taskKey]}
$taskRecord=[ordered]@{oldToNew=$taskMapRecord;waterStatTemplate='Wind (same tier/color), provisional; no existing stat values changed';rows=$taskAll.Count;waterRows=$taskWater.Count;projectiles=$taskMoves;serializedFiles=$taskChanged}
[IO.File]::WriteAllText($taskDone,($taskRecord | ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
'Migrated 260 existing rows without changing stats; added 20 Water rows and remapped projectile/effect/enum references.'
