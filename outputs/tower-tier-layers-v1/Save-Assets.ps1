$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$folder=Join-Path $projectRoot 'Assets/4. Asset/2. Tower/TierLayers'
$template=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'importer-template.meta.txt'))
$utf8=[Text.UTF8Encoding]::new($false)
$tiers=@('Bronze','Silver','Gold','Mithril','Diamond','Synergy')
$records=[Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Force -Path $folder | Out-Null
if(-not(Test-Path -LiteralPath ($folder+'.meta'))) {
    [IO.File]::WriteAllText(($folder+'.meta'),("fileFormatVersion: 2`nguid: $([guid]::NewGuid().ToString('N'))`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"),$utf8)
}
foreach($kind in @('Background','Decoration')) {
    $fileName=if($kind -eq 'Background'){'Tower_Backgrounds_Atlas_v1.png'}else{'Tower_Decorations_Atlas_v1.png'}
    $target=Join-Path $folder $fileName
    $metadata=[regex]::Replace($template,'(?m)^guid: \w+',('guid: '+[guid]::NewGuid().ToString('N')))
    $metadata=[regex]::Replace($metadata,'(?m)^(\s*spriteID: )([0-9a-f]{32})\r?$',{param($m) $m.Groups[1].Value+[guid]::NewGuid().ToString('N')})
    $metadata=[regex]::Replace($metadata,'(?m)^  spritePixelsToUnits: .*','  spritePixelsToUnits: 192')
    $metadata=[regex]::Replace($metadata,'(?m)^  spriteMeshType: .*','  spriteMeshType: 1')
    $metadata=[regex]::Replace($metadata,'(?m)^  spriteGenerateFallbackPhysicsShape: .*','  spriteGenerateFallbackPhysicsShape: 0')
    for($i=0;$i -lt 6;$i++) {
        $metadata=$metadata.Replace(('Tower_Frame_'+$tiers[$i]),('Tower_'+$kind+'_'+$tiers[$i]))
        $baseId=if($kind -eq 'Background'){54011}else{55011}
        $metadata=$metadata.Replace([string](42011+$i),[string]($baseId+$i))
    }
    if(-not(Test-Path -LiteralPath ($target+'.meta'))) {[IO.File]::WriteAllText(($target+'.meta'),$metadata,$utf8)}
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $fileName) -Destination $target -Force
    if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $fileName) -Algorithm SHA256).Hash){throw 'PNG copy mismatch.'}
    $actual=[IO.File]::ReadAllText(($target+'.meta'))
    if([regex]::Matches($actual,'(?m)^      name: Tower_'+$kind+'_').Count -ne 6){throw 'Expected 6 named sprite slices.'}
    if($actual -notmatch '(?m)^  spriteMode: 2\r?$' -or $actual -notmatch '(?m)^  spritePixelsToUnits: 192\r?$' -or $actual -notmatch '(?m)^  spriteMeshType: 1\r?$'){throw 'Importer validation failed.'}
    $guid=[regex]::Match($actual,'(?m)^guid: (\w+)').Groups[1].Value
    for($i=0;$i -lt 6;$i++) {
        $spriteName='Tower_'+$kind+'_'+$tiers[$i]
        $baseId=if($kind -eq 'Background'){54011}else{55011}
        $id=$baseId+$i
        $x=($i%3)*256;$y=(1-[int][Math]::Floor($i/3))*256
        if($actual -notmatch ('(?ms)name: '+$spriteName+'\r?\n\s+rect:\r?\n\s+serializedVersion: 2\r?\n\s+x: '+$x+'\r?\n\s+y: '+$y+'\r?\n\s+width: 256\r?\n\s+height: 256')){throw 'Incorrect slice rectangle.'}
        if($actual -notmatch ('(?m)^      '+$spriteName+': '+$id+'\r?$')){throw 'Sprite name/id table mismatch.'}
        $records.Add([pscustomobject]@{tier=$tiers[$i];layer=$kind;sprite=$spriteName;atlas=('Assets/4. Asset/2. Tower/TierLayers/'+$fileName);guid=$guid;fileID=$id;x=$x;y=$y;width=256;height=256;ppu=192})
    }
    Write-Output ('Saved '+$fileName+' with 6 slices, PPU 192.')
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'asset-manifest.json'),($records | ConvertTo-Json -Depth 4),$utf8)
$records | Export-Csv -LiteralPath (Join-Path $PSScriptRoot 'sprite-map.csv') -NoTypeInformation -Encoding UTF8
[IO.File]::AppendAllText((Join-Path $PSScriptRoot 'verification.txt'),"`nFinal Assets PNG hashes match packed files; both importers have 6 valid 256x256 slices, PPU 192 and Full Rect. 12 sprite names/IDs/rectangles verified.`n",$utf8)
