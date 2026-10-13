$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Ensure-Guid($taskMetaPath,$taskTemplate){
    if(!(Test-Path -LiteralPath $taskMetaPath)){
        $taskMeta=[regex]::Replace($taskTemplate,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
        [IO.File]::WriteAllText($taskMetaPath,$taskMeta,[Text.UTF8Encoding]::new($false))
    }
    return [regex]::Match([IO.File]::ReadAllText($taskMetaPath),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
}
$taskSpriteTemplate=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/3. Projectile/Cartoon/Bullet/Bullet_Wind.png.meta'))
$taskArt=[ordered]@{}
foreach($taskName in @('Bullet_Water','Splash_Water','Emblem_Water_White')){
    $taskSource=Join-Path $PSScriptRoot ('water-art/source/'+$taskName+'.png')
    $taskImage=[Drawing.Bitmap]::FromFile($taskSource);$taskPpu=$taskImage.Width;$taskImage.Dispose()
    $taskRelative=if($taskName -eq 'Bullet_Water'){'Assets/4. Asset/3. Projectile/Cartoon/Bullet/Bullet_Water.png'}elseif($taskName -eq 'Splash_Water'){'Assets/4. Asset/3. Projectile/Cartoon/Splash/Splash_Water.png'}else{'Assets/4. Asset/2. Tower/Emblem_Water_White.png'}
    $taskTarget=Join-Path $taskRoot $taskRelative
    Copy-Item -LiteralPath $taskSource -Destination $taskTarget
    $taskMeta=[regex]::Replace($taskSpriteTemplate,'(?m)^  spritePixelsToUnits: \d+','  spritePixelsToUnits: '+$taskPpu)
    $taskMeta=[regex]::Replace($taskMeta,'(?m)^    spriteID: [a-f0-9]+','    spriteID: '+[Guid]::NewGuid().ToString('N'))
    $taskGuid=Ensure-Guid ($taskTarget+'.meta') $taskMeta
    $taskArt[$taskName]=[ordered]@{asset=$taskRelative;guid=$taskGuid;fileID=21300000;ppu=$taskPpu}
}
$taskGlyphGuid=$taskArt.Emblem_Water_White.guid
$taskLegacyGuid='7e3f1605aca04ab5b6e525a542b6b7b3'
$taskLegacyIds=@(1001,1005,1002,1004,1003,1006,1007,0,1011,1010,1008,1009,1012,1013)
$taskEmblems="  m_emblemSprites:`n"
for($taskI=0;$taskI -lt 14;$taskI++){
    if($taskI -eq 7){$taskEmblems+="  - {fileID: 21300000, guid: $taskGlyphGuid, type: 3}`n"}else{$taskEmblems+='  - {fileID: '+$taskLegacyIds[$taskI]+", guid: $taskLegacyGuid, type: 3}`n"}
}
$taskPrefabPath=Join-Path $taskRoot 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
$taskPrefab=[IO.File]::ReadAllText($taskPrefabPath)
$taskPrefab=[regex]::Replace($taskPrefab,'(?m)^  m_emblemSprites:\r?\n(?:  - \{[^\r\n]+\}\r?\n)+',$taskEmblems)
[IO.File]::WriteAllText($taskPrefabPath,$taskPrefab,[Text.UTF8Encoding]::new($false))
$taskDebuffPath=Join-Path $taskRoot 'Assets/2. Prefab/DebuffZone.prefab'
$taskDebuff=[IO.File]::ReadAllText($taskDebuffPath)
if($taskDebuff -notmatch '(?m)^  - target: 8\r?$'){
    $taskDebuff=$taskDebuff.Replace('  m_symbolMappings:',("  m_symbolMappings:`n  - target: 8`n    symbolSprite: {fileID: 21300000, guid: $taskGlyphGuid, type: 3}"))
}
[IO.File]::WriteAllText($taskDebuffPath,$taskDebuff,[Text.UTF8Encoding]::new($false))
foreach($taskClass in @(1,2)){
    $taskId=$taskClass*100+8;$taskTemplateId=$taskClass*100+11
    $taskTemplatePath=Join-Path $taskRoot ('Assets/Resources/Projectiles/'+$taskTemplateId+'.prefab')
    $taskProjectile=[IO.File]::ReadAllText($taskTemplatePath)
    $taskSpriteGuid=if($taskClass -eq 1){$taskArt.Splash_Water.guid}else{$taskArt.Bullet_Water.guid}
    $taskProjectile=[regex]::Replace($taskProjectile,'(?m)^  m_Name: '+$taskTemplateId+'\r?$','  m_Name: '+$taskId)
    $taskProjectile=[regex]::Replace($taskProjectile,'(?m)^  m_Sprite: \{[^\r\n]+\}','  m_Sprite: {fileID: 21300000, guid: '+$taskSpriteGuid+', type: 3}')
    if($taskProjectile -match '(?m)^  m_usePrefabVisual:'){
        $taskProjectile=[regex]::Replace($taskProjectile,'(?m)^  m_usePrefabVisual: \d+','  m_usePrefabVisual: 1')
    }else{$taskProjectile=[regex]::Replace($taskProjectile,'(?m)^  IsSplash: ([01])\r?$',("  IsSplash: `$1`n  m_usePrefabVisual: 1`n  m_prefabEffectColor: {r: 0.1, g: 0.75, b: 1, a: 1}"))}
    $taskTarget=Join-Path $taskRoot ('Assets/Resources/Projectiles/'+$taskId+'.prefab')
    [IO.File]::WriteAllText($taskTarget,$taskProjectile,[Text.UTF8Encoding]::new($false))
    [void](Ensure-Guid ($taskTarget+'.meta') ([IO.File]::ReadAllText($taskTemplatePath+'.meta')))
}
$taskAnimPath=Join-Path $taskRoot 'Assets/10.Animation/Effect/Water_Hit.anim'
$taskAnim=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/10.Animation/Effect/E108.anim'))
$taskSplashGuid=$taskArt.Splash_Water.guid
$taskAnim=$taskAnim.Replace('m_Name: E108','m_Name: Water_Hit')
$taskAnim=[regex]::Replace($taskAnim,'\{fileID: \d+, guid: 8a07010800000000000000000000e108, type: 3\}',('{fileID: 21300000, guid: '+$taskSplashGuid+', type: 3}'))
[IO.File]::WriteAllText($taskAnimPath,$taskAnim,[Text.UTF8Encoding]::new($false))
$taskAnimGuid=Ensure-Guid ($taskAnimPath+'.meta') ([IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/10.Animation/Effect/E108.anim.meta')))
$taskControllerPath=Join-Path $taskRoot 'Assets/10.Animation/Effect/Water_Hit.controller'
$taskController=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/10.Animation/Effect/E108.controller'))
$taskController=$taskController.Replace('m_Name: E108','m_Name: Water_Hit').Replace('ef2136c395986894cbe987646d83c804',$taskAnimGuid)
[IO.File]::WriteAllText($taskControllerPath,$taskController,[Text.UTF8Encoding]::new($false))
$taskControllerGuid=Ensure-Guid ($taskControllerPath+'.meta') ([IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/10.Animation/Effect/E108.controller.meta')))
$taskEffectPath=Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/Effect_Water_Hit.prefab'
$taskEffect=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/E108.prefab'))
$taskEffect=$taskEffect.Replace('m_Name: E108','m_Name: Effect_Water_Hit').Replace('a92f69d27b6e3c440a782c341f8879da',$taskControllerGuid)
$taskEffect=[regex]::Replace($taskEffect,'\{fileID: \d+, guid: 8a07010800000000000000000000e108, type: 3\}',('{fileID: 21300000, guid: '+$taskSplashGuid+', type: 3}'))
[IO.File]::WriteAllText($taskEffectPath,$taskEffect,[Text.UTF8Encoding]::new($false))
$taskEffectGuid=Ensure-Guid ($taskEffectPath+'.meta') ([IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/E108.prefab.meta')))
$taskLibraryPath=Join-Path $taskRoot 'Assets/2. Prefab/4. Effect/EffectLibrary.asset'
$taskLibrary=[IO.File]::ReadAllText($taskLibraryPath)
foreach($taskClass in @(1,2,3,4)){
    $taskId=$taskClass*100+8
    if($taskLibrary -notmatch ('(?m)^  - effectID: '+$taskId+'\r?$')){$taskLibrary+="  - effectID: $taskId`n    effectPrefab: {fileID: 5220447335354414950, guid: $taskEffectGuid, type: 3}`n"}
}
[IO.File]::WriteAllText($taskLibraryPath,$taskLibrary,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'water-art/assets.json'),($taskArt | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
'Installed Water emblem, projectiles 108/208, hit effect animation and library keys 108/208/308/408.'
