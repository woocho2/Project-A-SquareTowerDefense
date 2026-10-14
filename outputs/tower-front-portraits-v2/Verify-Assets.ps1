$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$records=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($records.Count -ne 70){throw 'Manifest must have 70 portraits'}
if(@($records | Where-Object kind -eq 'normal').Count -ne 56){throw 'Expected 56 regular portraits'}
if(@($records | Where-Object kind -eq 'synergy').Count -ne 14){throw 'Expected 14 synergy portraits'}
$guids=[Collections.Generic.HashSet[string]]::new()
$names=[Collections.Generic.HashSet[string]]::new()
$report=[Collections.Generic.List[string]]::new()
$spriteCount=0
foreach($atlasGroup in ($records | Group-Object atlas)){
    $path=Join-Path $taskRoot $atlasGroup.Name
    $metadata=Get-Content -LiteralPath ($path+'.meta') -Raw -Encoding UTF8
    $guid=[regex]::Match($metadata,'(?m)^guid: (\w+)').Groups[1].Value
    if(-not $guids.Add($guid)){throw 'Duplicate atlas GUID'}
    if($metadata -notmatch '(?m)^  spriteMode: 2\r?$' -or $metadata -notmatch '(?m)^  spritePixelsToUnits: 192\r?$' -or $metadata -notmatch '(?m)^  spriteMeshType: 1\r?$'){throw 'Unexpected importer settings'}
    $metaSprites=[regex]::Matches($metadata,'(?ms)^      name: ([^\r\n]+)\r?\n.*?^      internalID: (\d+)')
    if($metaSprites.Count -ne $atlasGroup.Count){throw 'Sprite metadata count mismatch'}
    $bitmap=[Drawing.Bitmap]::new($path)
    try{
        $expectedSize=if($atlasGroup.Group[0].kind -eq 'normal'){512}else{1024}
        if($bitmap.Width -ne $expectedSize -or $bitmap.Height -ne $expectedSize){throw 'Atlas dimension mismatch'}
        foreach($record in $atlasGroup.Group){
            if($record.guid -ne $guid){throw 'Manifest GUID mismatch'}
            if(-not $names.Add($record.spriteName)){throw 'Duplicate sprite name'}
            $matched=@($metaSprites | Where-Object {$_.Groups[1].Value -eq $record.spriteName -and [int]$_.Groups[2].Value -eq $record.fileID})
            if($matched.Count -ne 1){throw 'Sprite name/fileID mismatch'}
            $x=[int]$record.rect.x;$y=$bitmap.Height-[int]$record.rect.y-256
            if($x % 256 -ne 0 -or $y % 256 -ne 0 -or $x+256 -gt $bitmap.Width -or $y+256 -gt $bitmap.Height){throw 'Invalid sprite rect'}
            $opaque=0;$transparent=0;$left=256;$top=256;$right=-1;$bottom=-1
            for($py=0;$py -lt 256;$py++){for($px=0;$px -lt 256;$px++){
                $alpha=$bitmap.GetPixel($x+$px,$y+$py).A
                if($alpha -eq 0){$transparent++}
                if(($px -lt 16 -or $px -ge 240 -or $py -lt 48 -or $py -ge 248) -and $alpha -ne 0){throw ('Non-transparent gutter in '+$record.spriteName)}
                if($alpha -ge 8){$opaque++;$left=[Math]::Min($left,$px);$top=[Math]::Min($top,$py);$right=[Math]::Max($right,$px);$bottom=[Math]::Max($bottom,$py)}
            }}
            if($opaque -lt 2500 -or $transparent -lt 15000){throw ('Empty or opaque-background sprite: '+$record.spriteName)}
            if($bottom -lt 241 -or $bottom -gt 244){throw ('Incorrect bottom alignment: '+$record.spriteName)}
            $spriteCount++
            $report.Add($record.spriteName+': alpha>=8 bounds=('+ $left+','+$top+','+($right-$left+1)+','+($bottom-$top+1)+'); transparent pixels='+$transparent)
        }
        $report.Add([IO.Path]::GetFileName($path)+': dimensions='+$bitmap.Width+'x'+$bitmap.Height+'; slices='+$atlasGroup.Count+'; GUID/name/fileID verified.')
    }finally{$bitmap.Dispose()}
}
if($guids.Count -ne 15 -or $spriteCount -ne 70){throw 'Incorrect deliverable count'}
$report.Add('RESULT: 56 regular + 14 synergy portraits, 15 atlases, 70 unique named sprite slices.')
$report.Add('All sprite gutters are transparent; foreground present; bottom crop aligns at local y241..244. Atlas dimensions and metadata references checked.')
$report.Add('Visual inspection performed on generated groups and packed previews. Unity was not launched; no prefab/catalog/runtime code changes.')
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),($report -join "`n"),[Text.UTF8Encoding]::new($false))
$report | Select-Object -Last 3 | Write-Output
