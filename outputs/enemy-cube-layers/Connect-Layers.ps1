$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetRoot='Assets/4. Asset/4. Enemy'
$utf8=New-Object Text.UTF8Encoding($false)
function Meta-Guid([string]$relative,[string]$kind='Texture'){
    $path=Join-Path $root ($relative+'.meta')
    if(Test-Path -LiteralPath $path){return [regex]::Match([IO.File]::ReadAllText($path),'(?m)^guid: (\w+)').Groups[1].Value}
    $guid=[guid]::NewGuid().ToString('N')
    $body=if($kind -eq 'Script'){"MonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n"}elseif($kind -eq 'Folder'){"folderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n"}else{"NativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n"}
    [IO.File]::WriteAllText($path,"fileFormatVersion: 2`nguid: $guid`n"+$body,$utf8)
    $guid
}
function Color-Value([double[]]$rgb){
    '{r: '+$rgb[0].ToString('0.########',[Globalization.CultureInfo]::InvariantCulture)+', g: '+$rgb[1].ToString('0.########',[Globalization.CultureInfo]::InvariantCulture)+', b: '+$rgb[2].ToString('0.########',[Globalization.CultureInfo]::InvariantCulture)+', a: 1}'
}
function Hue-RGB([double]$h){
    $x=1-[math]::Abs(($h/60)%2-1)
    if($h -lt 60){return @(1,$x,0)}elseif($h -lt 120){return @($x,1,0)}elseif($h -lt 180){return @(0,1,$x)}elseif($h -lt 240){return @(0,$x,1)}elseif($h -lt 300){return @($x,0,1)}else{return @(1,0,$x)}
}
Meta-Guid $assetRoot 'Folder' | Out-Null
$template=[IO.File]::ReadAllText((Join-Path $root 'Assets/4. Asset/4. Enemy/Enemy_Background.png.meta'))
$textureGuids=@{}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $root $assetRoot) -Filter '*.png'){
    $relative=$assetRoot+'/'+$file.Name
    $guid=Meta-Guid $relative
    $meta=[regex]::Replace($template,'(?m)^guid: \w+',('guid: '+$guid))
    $meta=[regex]::Replace($meta,'(?m)^  spriteMode: .*','  spriteMode: 0')
    $meta=[regex]::Replace($meta,'(?m)^  textureType: .*','  textureType: 0')
    if($file.BaseName -like '*EquipmentMask'){$meta=$meta.Replace('    sRGBTexture: 1','    sRGBTexture: 0')}
    [IO.File]::WriteAllText(($file.FullName+'.meta'),$meta,$utf8)
    $textureGuids[$file.BaseName]=$guid
}
$scriptGuid=Meta-Guid 'Assets/3. Scripts/03. Enemy/EnemyCubeAppearance.cs' 'Script'
$profiles=@{}
foreach($character in @('Valkyrie','Brunhild','Loki')){
    $relative=$assetRoot+'/Enemy_'+$character+'_Appearance.asset'
    $profileGuid=Meta-Guid $relative 'Asset'
    $characterGuid=$textureGuids['Enemy_'+$character+'_Character'];$maskGuid=$textureGuids['Enemy_'+$character+'_EquipmentMask']
    $body=@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: Enemy_${character}_Appearance
  m_EditorClassIdentifier: Assembly-CSharp::EnemyCubeAppearance
  m_characterTexture: {fileID: 2800000, guid: $characterGuid, type: 3}
  m_equipmentMask: {fileID: 2800000, guid: $maskGuid, type: 3}
"@
    [IO.File]::WriteAllText((Join-Path $root $relative),$body+"`n",$utf8)
    $profiles[$character]=$profileGuid
}
$states=@('SelfHeal','Defense','Action','Mobility','Locked','Hourglass');$hues=@(351,46,141,198,0,268)
$palettes=Get-Content -LiteralPath (Join-Path $root 'outputs/valkyrie-six-state-faces-v1/generation-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest=New-Object Collections.Generic.List[object]
foreach($character in @('Valkyrie','Brunhild','Loki')){
    for($i=0;$i -lt 6;$i++){
        $state=$states[$i]
        $relative=if($character -eq 'Valkyrie'){'Assets/4. Asset/4. Enemy/EnemyCube_'+$state+'.mat'}else{'Assets/4. Asset/4. Enemy/Enemy_Fallen'+$character+'_'+$state+'.mat'}
        $path=Join-Path $root $relative
        $text=[IO.File]::ReadAllText((Join-Path $PSScriptRoot ('before/'+[IO.Path]::GetFileName($path))))
        $characterGuid=$textureGuids['Enemy_'+$character+'_Character'];$maskGuid=$textureGuids['Enemy_'+$character+'_EquipmentMask']
        $text=[regex]::Replace($text,'(?m)^        m_Texture: .*',('        m_Texture: {fileID: 2800000, guid: '+$characterGuid+', type: 3}'))
        $text=[regex]::Replace($text,'(?m)^        m_Scale: .*','        m_Scale: {x: 1, y: 1}')
        $text=[regex]::Replace($text,'(?m)^        m_Offset: .*','        m_Offset: {x: 0, y: 0}')
        $tex=''
        foreach($entry in @(@('_BackgroundTex','Enemy_Background'),@('_EquipmentMask',('Enemy_'+$character+'_EquipmentMask')),@('_DecorationTex','Enemy_Decoration'),@('_BadgeTex','Enemy_State_Badges_Atlas'))){
            $scale=if($entry[0] -eq '_BadgeTex'){'{x: 0.333333333, y: 0.5}'}else{'{x: 1, y: 1}'}
            $x=($i%3)/3.0;$y=if($i -lt 3){0.5}else{0}
            $offset=if($entry[0] -eq '_BadgeTex'){'{x: '+$x.ToString('0.#########',[Globalization.CultureInfo]::InvariantCulture)+', y: '+$y.ToString('0.#########',[Globalization.CultureInfo]::InvariantCulture)+'}'}else{'{x: 0, y: 0}'}
            $tex+="    - "+$entry[0]+":`n        m_Texture: {fileID: 2800000, guid: "+$textureGuids[$entry[1]]+", type: 3}`n        m_Scale: $scale`n        m_Offset: $offset`n"
        }
        $text=$text.Replace('    m_Ints: []',($tex+'    m_Ints: []'))
        $text=$text.Replace('    m_Floats: []',"    m_Floats:`n    - _UseLayers: 1`n    - _RecolorEquipment: 1")
        $tint=if($i -eq 4){@(1,1,1)}else{@(Hue-RGB $hues[$i])}
        $hex=($palettes | Where-Object key -eq $state).bg.TrimStart('#')
        $bg=@(0,2,4 | ForEach-Object {[Convert]::ToInt32($hex.Substring($_,2),16)/255.0})
        $colors="    - _BackgroundColor: "+(Color-Value $bg)+"`n    - _EquipmentColor: "+(Color-Value $tint)+"`n    - _BadgeRect: {r: 0.375, g: 0.75, b: 0.25, a: 0.25}`n"
        $text=$text.Replace('  m_BuildTextureStacks: []',($colors+'  m_BuildTextureStacks: []'))
        [IO.File]::WriteAllText($path,$text,$utf8)
        $manifest.Add([pscustomobject]@{character=$character;state=$state;poolIndex=$i;material=$relative;profileGuid=$profiles[$character];characterGuid=$characterGuid;maskGuid=$maskGuid;badgeOffset=@($x,$y);background=('#'+$hex);equipmentTint=$tint})
    }
}
$prefabs=@(
    @('Assets/2. Prefab/3. Enemy/Enemy.prefab','Valkyrie'),
    @('Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Brunhild.prefab','Brunhild'),
    @('Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Loki.prefab','Loki'),
    @('Assets/2. Prefab/3. Enemy/4. Enemy_middleBoss.prefab','Brunhild'),
    @('Assets/2. Prefab/3. Enemy/5. Enemy_boss.prefab','Loki')
)
foreach($entry in $prefabs){
    $path=Join-Path $root $entry[0];$text=[IO.File]::ReadAllText($path)
    $dest=Join-Path $PSScriptRoot ('before/'+$entry[0]);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest)) | Out-Null
    if(!(Test-Path -LiteralPath $dest)){[IO.File]::Copy($path,$dest)}
    $profileGuid=$profiles[$entry[1]]
    $text=[regex]::Replace($text,'(?m)^(  m_EditorClassIdentifier: Assembly-CSharp::EnemyCubeMeshVisual)\r?\n(?:  m_appearance: .*\r?\n)?',('${1}'+"`n  m_appearance: {fileID: 11400000, guid: $profileGuid, type: 2}`n"))
    [IO.File]::WriteAllText($path,$text,$utf8)
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Encoding UTF8
$textureGuids | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'texture-guids.json') -Encoding UTF8
'Connected 18 existing material identities and 5 prefabs to 9 shared layer textures and 3 character profiles.'
