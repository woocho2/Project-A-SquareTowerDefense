$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetRelative='Assets/4. Asset/2. Tower/Portraits_SD_Front_v2'
$assetFolder=Join-Path $taskRoot $assetRelative
$roster=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'roster.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$template=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'importer-template.meta.txt') -Raw -Encoding UTF8
$utf8=[Text.UTF8Encoding]::new($false)
$records=[Collections.Generic.List[object]]::new()

# Check every source before adding anything to Assets.
foreach($g in $roster | Where-Object kind -eq 'normal'){
    if(-not(Test-Path -LiteralPath (Join-Path $PSScriptRoot ('packed/'+$g.key+'.png')))){throw ('Missing '+$g.key)}
}
if(-not(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'packed/Synergy_All14.png'))){throw 'Missing packed synergy atlas'}
New-Item -ItemType Directory -Force -Path $assetFolder | Out-Null
if(-not(Test-Path -LiteralPath ($assetFolder+'.meta'))){
    $folderMeta="fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
    [IO.File]::WriteAllText(($assetFolder+'.meta'),$folderMeta,$utf8)
}

function Save-Atlas([string]$source,[string]$fileName,[object[]]$sprites,[int]$columns,[int]$rows){
    $target=Join-Path $assetFolder $fileName
    $metaPath=$target+'.meta'
    $atlasGuid=if(Test-Path -LiteralPath $metaPath){[regex]::Match([IO.File]::ReadAllText($metaPath),'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
    $metadata=[regex]::Replace($template,'(?m)^guid: \w+',('guid: '+$atlasGuid))
    $metadata=[regex]::Replace($metadata,'(?m)^  spriteGenerateFallbackPhysicsShape: \d+','  spriteGenerateFallbackPhysicsShape: 0')
    $metadata=[regex]::Replace($metadata,'(?m)^  spriteMeshType: \d+','  spriteMeshType: 1')
    $metadata=[regex]::Replace($metadata,'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 192')
    $spriteLines=[Collections.Generic.List[string]]::new()
    $spriteLines.Add('  spriteSheet:');$spriteLines.Add('    serializedVersion: 2');$spriteLines.Add('    sprites:')
    foreach($s in $sprites){
        $x=($s.index % $columns)*256;$y=($rows-1-[int][Math]::Floor($s.index/$columns))*256
        $spriteId=[guid]::NewGuid().ToString('N')
        $spriteLines.Add(@"
    - serializedVersion: 2
      name: $($s.name)
      rect:
        serializedVersion: 2
        x: $x
        y: $y
        width: 256
        height: 256
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteId
      internalID: $($s.fileID)
      vertices: []
      indices: 
      edges: []
      weights: []
"@)
        $records.Add([pscustomobject]@{kind=$s.kind;emblemId=$s.emblem;attackClass=$s.attack;towerIdExample=$s.towerID;concept=$s.concept;spriteName=$s.name;atlas=($assetRelative+'/'+$fileName);guid=$atlasGuid;fileID=$s.fileID;rect=[pscustomobject]@{x=$x;y=$y;width=256;height=256}})
    }
    $spriteLines.Add(@"
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
"@)
    foreach($s in $sprites){$spriteLines.Add('      '+$s.name+': '+$s.fileID)}
    $block=($spriteLines -join "`n")+"`n"
    $metadata=[regex]::Replace($metadata,'(?ms)^  spriteSheet:\r?\n.*?(?=^  mipmapLimitGroupName:)',{param($m)$block})
    if([regex]::Matches($metadata,'(?m)^      name: ').Count -ne $sprites.Count){throw 'Sprite metadata construction failed'}
    # On a repeat run retain the previously assigned GUIDs/spriteIDs and importer settings.
    if(-not(Test-Path -LiteralPath $metaPath)){[IO.File]::WriteAllText($metaPath,$metadata,$utf8)}
    Copy-Item -LiteralPath $source -Destination $target -Force
    if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash){throw 'Asset copy mismatch'}
    Write-Output ('Saved '+$fileName+'; sprites='+$sprites.Count)
}

foreach($g in $roster | Where-Object kind -eq 'normal'){
    $groupName=$g.key.Substring(3)
    $sprites=@(for($i=0;$i -lt 4;$i++){
        [pscustomobject]@{index=$i;name=('Tower_'+$groupName+'_'+$g.portraits[$i][0]+'_Front_v2');fileID=(52000+$g.emblem*10+$i);kind='normal';emblem=$g.emblem;attack=($i+1);towerID=(1000+($i+1)*100+$g.emblem);concept=$g.portraits[$i][1]}
    })
    Save-Atlas (Join-Path $PSScriptRoot ('packed/'+$g.key+'.png')) ('Tower_'+$groupName+'_Front_Atlas_v2.png') $sprites 2 2
}
$synergySprites=@();$index=0
foreach($g in $roster | Where-Object kind -eq 'synergy'){
    foreach($p in $g.portraits){
        $synergySprites += [pscustomobject]@{index=$index;name=('Synergy_'+$p[0]+'_Front_v2');fileID=(53001+$index);kind='synergy';emblem=0;attack=0;towerID=(6001+$index);concept=$p[1]}
        $index++
    }
}
Save-Atlas (Join-Path $PSScriptRoot 'packed/Synergy_All14.png') 'Synergy_All14_Front_Atlas_v2.png' $synergySprites 4 4
if($records.Count -ne 70){throw ('Expected 70 portraits, got '+$records.Count)}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'asset-manifest.json'),($records | ConvertTo-Json -Depth 6),$utf8)
$records | Select-Object kind,emblemId,attackClass,towerIdExample,concept,spriteName,atlas,fileID | Export-Csv -LiteralPath (Join-Path $PSScriptRoot 'portrait-map.csv') -NoTypeInformation -Encoding UTF8
Write-Output 'Saved 56 regular portraits + 14 synergy portraits in 15 atlases. No prefab, catalog, frame, emblem or runtime changes.'
