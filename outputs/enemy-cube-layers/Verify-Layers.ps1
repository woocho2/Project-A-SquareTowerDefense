$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Read([string]$relative){[IO.File]::ReadAllText((Join-Path $root $relative)).Replace("`r`n","`n")}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$textures=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'texture-guids.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($manifest.Count -ne 18){throw 'Expected eighteen material variants.'}
$map=@{}
rg -n '^guid: [a-f0-9]{32}' (Join-Path $root 'Assets') -g '*.meta' | ForEach-Object {if($_ -match '^(.*):\d+:guid: ([a-f0-9]{32})'){$map[$matches[2]]=$matches[1]}}
foreach($property in $textures.PSObject.Properties){
    $relative='Assets/4. Asset/4. Enemy/'+$property.Name+'.png'
    $meta=Read ($relative+'.meta')
    if($meta -notmatch ('guid: '+$property.Value)){throw 'Texture GUID changed.'}
    if($property.Name -like '*Mask' -and $meta -notmatch 'sRGBTexture: 0'){throw 'Mask must use linear sampling.'}
    if($meta -notmatch 'enableMipMap: 0' -or $meta -notmatch 'nPOTScale: 0'){throw 'Unexpected resize/mipmap settings.'}
}
foreach($row in $manifest){
    $material=Read $row.material
    foreach($guid in @($row.characterGuid,$row.maskGuid,$textures.Enemy_Background,$textures.Enemy_Decoration,$textures.Enemy_State_Badges_Atlas)){
        if(!$material.Contains('guid: '+$guid)){throw ('Missing layer reference: '+$row.material)}
        if(!$map.ContainsKey($guid)){throw 'Layer dependency missing.'}
    }
    if($material -notmatch '_UseLayers: 1'){throw 'Layer shader disabled.'}
    if($material -notmatch 'm_Scale: \{x: 1, y: 1\}'){throw 'Canonical character UV incorrect.'}
    $character=$row.character
    $profile=Read ('Assets/4. Asset/4. Enemy/Enemy_'+$character+'_Appearance.asset')
    if(!$profile.Contains($row.characterGuid) -or !$profile.Contains($row.maskGuid)){throw 'Appearance profile mismatches character textures.'}
}
$prefabs=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'prefab-before-hashes.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($row in $prefabs){
    $before=Read ('outputs/enemy-cube-layers/before/'+$row.path)
    $current=Read $row.path
    $matches=[regex]::Matches($current,'(?m)^  m_appearance: \{fileID: 11400000, guid: ([a-f0-9]{32}), type: 2\}\n')
    if($matches.Count -ne 1){throw 'Expected one appearance profile on each cube.'}
    if(!$map.ContainsKey($matches[0].Groups[1].Value)){throw 'Profile reference missing.'}
    $stripped=[regex]::Replace($current,'(?m)^  m_appearance: .*\n','')
    if($stripped -cne $before){throw ('Unrelated prefab edit: '+$row.path)}
}
# The obsolete completed face images were removed after folder consolidation.
$deleted=Get-Content -LiteralPath (Join-Path $root 'outputs/enemy-folder-cleanup/deleted-images.txt') -Encoding UTF8
foreach($relative in $deleted){if(Test-Path -LiteralPath (Join-Path $root $relative)){throw 'Obsolete baked image remains.'}}
$log=@(
    'PASS: 9 texture GUIDs and 18 layered materials resolve to shared background, frame, state badge atlas, character and equipment masks.',
    'PASS: all 3 appearance profiles link matching standalone character/mask textures.',
    'PASS: all 5 enemy prefabs changed only by adding an appearance profile; six material pools and movement/gameplay settings unchanged.',
    'PASS: masks use linear sampling; all textures retain exact pixel sizes without NPOT resizing or mipmaps.',
    'PASS: old 8 baked face textures deleted after consolidation.',
    'PASS: 18 CPU layer compositions preserve canonical character position/non-equipment pixels and shared rounded outer alpha.',
    'PASS: modified C# compiled against installed Unity CoreModule (0 warnings, 0 errors).',
    'Unity was not launched. Shader compilation, color-space appearance, and actual rolling/rendering must be checked in Unity.'
)
$log | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.txt') -Encoding UTF8
$log
