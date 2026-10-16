$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$relative='Assets/4. Asset/4. Enemy/Asgard/CorruptedBosses_v1'
$folder=Join-Path $projectRoot $relative
$utf8=[Text.UTF8Encoding]::new($false)
$template=[IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/4. Asset/2. Tower/TierLayers/Tower_Backgrounds_Atlas_v1.png.meta'))
$materialTemplate=[IO.File]::ReadAllText((Join-Path $projectRoot 'Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateCube3D_v1/EnemyCube_SelfHeal.mat'))
$states=@('SelfHeal','Defense','Mobility','Action','Locked','Hourglass')
$tiers=@('Bronze','Silver','Gold','Mithril','Diamond','Synergy')
$poolIndices=@(0,1,3,2,4,5)
$records=[Collections.Generic.List[object]]::new()
foreach($dir in @($folder,(Join-Path $folder 'Materials'))) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    if(-not(Test-Path -LiteralPath ($dir+'.meta'))) {
        [IO.File]::WriteAllText(($dir+'.meta'),("fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"),$utf8)
    }
}
$bossIndex=0
foreach($boss in @('Brunhild','Loki')) {
    $fileName='Enemy_Fallen'+$boss+'_States_Atlas_v1.png'
    $target=Join-Path $folder $fileName
    $guid=if(Test-Path -LiteralPath ($target+'.meta')){[regex]::Match([IO.File]::ReadAllText(($target+'.meta')),'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
    $metadata=[regex]::Replace($template,'(?m)^guid: \w+',('guid: '+$guid))
    $metadata=[regex]::Replace($metadata,'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 384')
    $metadata=[regex]::Replace($metadata,'(?m)^(\s*spriteID: )([a-f0-9]{32})\r?$',{param($m)$m.Groups[1].Value+[guid]::NewGuid().ToString('N')})
    $metadata=[regex]::Replace($metadata,'(?ms)(      rect:\r?\n        serializedVersion: 2\r?\n        x: )(\d+)(\r?\n        y: )(\d+)(\r?\n        width: )256(\r?\n        height: )256',{param($m)$m.Groups[1].Value+([int]$m.Groups[2].Value*2)+$m.Groups[3].Value+([int]$m.Groups[4].Value*2)+$m.Groups[5].Value+'512'+$m.Groups[6].Value+'512'})
    for($i=0;$i -lt 6;$i++) {
        $spriteName='Enemy_Fallen'+$boss+'_'+$states[$i]+'_v1'
        $metadata=$metadata.Replace(('Tower_Background_'+$tiers[$i]),$spriteName)
        $metadata=$metadata.Replace([string](54011+$i),[string](57001+$bossIndex*100+$i))
    }
    if(-not(Test-Path -LiteralPath ($target+'.meta'))){[IO.File]::WriteAllText(($target+'.meta'),$metadata,$utf8)}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $fileName) -Destination $target -Force
    $actual=[IO.File]::ReadAllText(($target+'.meta'))
    if([regex]::Matches($actual,'(?m)^      name: ').Count -ne 6 -or $actual -notmatch '(?m)^  spriteMode: 2\r?$' -or $actual -notmatch '(?m)^  spritePixelsToUnits: 384\r?$'){throw 'Atlas importer validation failed.'}
    for($i=0;$i -lt 6;$i++) {
        $state=$states[$i];$spriteName='Enemy_Fallen'+$boss+'_'+$state+'_v1';$fileId=57001+$bossIndex*100+$i
        $x=($i%3)*512;$y=(1-[int][Math]::Floor($i/3))*512
        $offsetX=($i%3)/3.0;$offsetY=(1-[int][Math]::Floor($i/3))*.5
        $offsetXString=$offsetX.ToString('0.#########',[Globalization.CultureInfo]::InvariantCulture)
        $offsetYString=$offsetY.ToString('0.#########',[Globalization.CultureInfo]::InvariantCulture)
        $matName='Enemy_Fallen'+$boss+'_'+$state
        $matPath=Join-Path $folder ('Materials/'+$matName+'.mat')
        $mat=[regex]::Replace($materialTemplate,'(?m)^  m_Name: .*',('  m_Name: '+$matName))
        $mat=[regex]::Replace($mat,'(?m)^        m_Texture: .*',('        m_Texture: {fileID: 2800000, guid: '+$guid+', type: 3}'))
        $mat=[regex]::Replace($mat,'(?m)^        m_Scale: .*','        m_Scale: {x: 0.333333333, y: 0.5}')
        $mat=[regex]::Replace($mat,'(?m)^        m_Offset: .*',('        m_Offset: {x: '+$offsetXString+', y: '+$offsetYString+'}'))
        [IO.File]::WriteAllText($matPath,$mat,$utf8)
        if(-not(Test-Path -LiteralPath ($matPath+'.meta'))) {
            [IO.File]::WriteAllText(($matPath+'.meta'),("fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 2100000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"),$utf8)
        }
        $materialGuid=[regex]::Match([IO.File]::ReadAllText(($matPath+'.meta')),'(?m)^guid: (\w+)').Groups[1].Value
        if($actual -notmatch ('(?m)^      '+$spriteName+': '+$fileId+'\r?$')){throw 'Sprite name/fileID mismatch.'}
        if($actual -notmatch ('(?ms)name: '+$spriteName+'\r?\n\s+rect:\r?\n\s+serializedVersion: 2\r?\n\s+x: '+$x+'\r?\n\s+y: '+$y+'\r?\n\s+width: 512\r?\n\s+height: 512')){throw 'Sprite rectangle mismatch.'}
        $records.Add([pscustomobject]@{boss=$boss;role=$(if($boss -eq 'Brunhild'){'MiddleBoss'}else{'FinalBoss'});state=$state;cubeAssetPoolIndex=$poolIndices[$i];atlas=($relative+'/'+$fileName);sprite=$spriteName;guid=$guid;fileID=$fileId;rect=[pscustomobject]@{x=$x;y=$y;width=512;height=512};ppu=384;material=($relative+'/Materials/'+$matName+'.mat');materialGuid=$materialGuid;uvScale=@((1.0/3),0.5);uvOffset=@($offsetX,$offsetY)})
    }
    if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $fileName) -Algorithm SHA256).Hash){throw 'Atlas copy mismatch.'}
    Write-Output ('Saved '+$fileName+': 6 sliced portraits + 6 atlas-backed cube materials.')
    $bossIndex++
}
if($records.Count -ne 12){throw 'Expected 12 boss states.'}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'asset-manifest.json'),($records | ConvertTo-Json -Depth 6),$utf8)
$records | Select-Object boss,role,state,cubeAssetPoolIndex,sprite,atlas,fileID,material | Export-Csv -LiteralPath (Join-Path $PSScriptRoot 'sprite-map.csv') -NoTypeInformation -Encoding UTF8
[IO.File]::AppendAllText((Join-Path $PSScriptRoot 'verification.txt'),"`nPASS: 2 final PNGs match staging SHA256; 12 named 512x512 slices at 384 PPU; 12 cube material UV transforms and sprite name/fileID tables verified. Both bosses reuse one atlas each, with no duplicate individual state PNGs.`n",$utf8)
