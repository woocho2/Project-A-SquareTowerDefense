$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskAssetDir=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts'
function Save-Layer($taskFile,$taskTemplate,$taskOldNames,$taskNewNames,$taskIdStart){
    $taskTarget=Join-Path $taskAssetDir $taskFile
    $taskMetaPath=$taskTarget+'.meta'
    if(Test-Path -LiteralPath $taskMetaPath){
        Copy-Item -LiteralPath $taskMetaPath -Destination (Join-Path $PSScriptRoot ($taskFile+'.meta'))
    }else{
        $taskMeta=[IO.File]::ReadAllText((Join-Path (Join-Path $PSScriptRoot 'before') $taskTemplate))
        $taskMeta=[regex]::Replace($taskMeta,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
        $taskSpriteMatches=[regex]::Matches($taskMeta,'(?m)^      spriteID: ([a-f0-9]+)')
        $taskIdMatches=[regex]::Matches($taskMeta,'(?m)^      internalID: (\d+)')
        if($taskSpriteMatches.Count -ne $taskNewNames.Count -or $taskIdMatches.Count -ne $taskNewNames.Count){throw 'Unexpected sprite metadata layout'}
        for($taskI=0;$taskI -lt $taskNewNames.Count;$taskI++){
            $taskMeta=$taskMeta.Replace($taskOldNames[$taskI],$taskNewNames[$taskI])
            $taskMeta=$taskMeta.Replace($taskSpriteMatches[$taskI].Groups[1].Value,[Guid]::NewGuid().ToString('N'))
            $taskMeta=$taskMeta.Replace($taskIdMatches[$taskI].Groups[1].Value,($taskIdStart+$taskI).ToString())
        }
        [IO.File]::WriteAllText((Join-Path $PSScriptRoot ($taskFile+'.meta')),$taskMeta,[Text.UTF8Encoding]::new($false))
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot ($taskFile+'.meta')) -Destination $taskMetaPath
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $taskFile) -Destination $taskTarget
}
$taskSwordNames=@('Tower_Sword_Hero','Tower_Sword_Swordsman','Tower_Sword_Paladin','Tower_Sword_DeathKnight')
$taskAxeNames=@('Tower_Axe_Viking','Tower_Axe_Berserker','Tower_Axe_Skald','Tower_Axe_Executioner')
$taskFrameNames=@('Tower_Frame_Bronze','Tower_Frame_Silver','Tower_Frame_Gold','Tower_Frame_Mithril','Tower_Frame_Diamond','Tower_Frame_Synergy')
Save-Layer 'Tower_Sword_FaceHair_Atlas_v1.png' 'Tower_Sword_Characters_Atlas_v2.png.meta' $taskSwordNames @($taskSwordNames | ForEach-Object {$_+'_FaceHair'}) 42101
Save-Layer 'Tower_Sword_HeadgearDecor_Atlas_v1.png' 'Tower_Sword_Characters_Atlas_v2.png.meta' $taskSwordNames @($taskSwordNames | ForEach-Object {$_+'_HeadgearDecor'}) 42111
Save-Layer 'Tower_Axe_FaceHair_Atlas_v1.png' 'Tower_Axe_Characters_Atlas_v1.png.meta' $taskAxeNames @($taskAxeNames | ForEach-Object {$_+'_FaceHair'}) 42121
Save-Layer 'Tower_Axe_HeadgearDecor_Atlas_v1.png' 'Tower_Axe_Characters_Atlas_v1.png.meta' $taskAxeNames @($taskAxeNames | ForEach-Object {$_+'_HeadgearDecor'}) 42131
Save-Layer 'Tower_Frame_Rims_Atlas_v1.png' 'Tower_Frames_Atlas_v1.png.meta' $taskFrameNames @($taskFrameNames | ForEach-Object {$_.Replace('Tower_Frame_','Tower_Frame_Rim_')}) 42141
'Saved four character layer atlases (16 sprites) and one frame rim atlas (6 sprites).'
