$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$enemyRoot=Join-Path $root 'Assets/4. Asset/4. Enemy'
$before=Join-Path $PSScriptRoot 'before'
if(Test-Path -LiteralPath $before) {throw 'A prior backup already exists; inspect it before repeating.'}
$files=@(Get-ChildItem -LiteralPath $enemyRoot -Recurse -Filter '*_Character.png' -File | Sort-Object FullName)
if($files.Count -eq 0) {throw 'No enemy character PNGs found.'}
$encoding=[Text.UTF8Encoding]::new($false)
$plans=@()
foreach($file in $files) {
    $metaPath=$file.FullName+'.meta'
    $bytes=[IO.File]::ReadAllBytes($metaPath)
    $text=[IO.File]::ReadAllText($metaPath)
    $mode=[regex]::Match($text,'(?m)^  spriteMode: (\d+)').Groups[1].Value
    $type=[regex]::Match($text,'(?m)^  textureType: (\d+)').Groups[1].Value
    if($mode -ne '0' -or $type -ne '0') {throw "Character already has custom sprite settings; review manually: $($file.FullName)"}
    if($text -notmatch '(?m)^    sprites: \[\]') {throw "Unexpected existing sprite slices: $metaPath"}
    $changed=[regex]::Replace($text,'(?m)^  spriteMode: 0\r?$', '  spriteMode: 1')
    $changed=[regex]::Replace($changed,'(?m)^  textureType: 0\r?$', '  textureType: 8')
    $changed=[regex]::Replace($changed,'(?m)^  spriteMeshType: 0\r?$', '  spriteMeshType: 1')
    # Preserve the original line ending convention on the three changed lines.
    if($text.Contains("`r`n")) {$changed=$changed.Replace("`r`n","`n").Replace("`n","`r`n")}
    $normalizedOld=[regex]::Replace($text,'(?m)^  (spriteMode|textureType|spriteMeshType): \d+\r?$', '  $1: <configured>')
    $normalizedNew=[regex]::Replace($changed,'(?m)^  (spriteMode|textureType|spriteMeshType): \d+\r?$', '  $1: <configured>')
    if($normalizedOld -cne $normalizedNew) {throw "Unrelated metadata change detected: $metaPath"}
    $guid=[regex]::Match($text,'(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
    if($guid.Length -ne 32) {throw "Missing texture GUID: $metaPath"}
    $relative=$metaPath.Substring($enemyRoot.Length).TrimStart('\','/')
    $hasBom=$bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
    $plans+=@{path=$metaPath;relative=$relative;originalBytes=$bytes;text=$changed;encoding=[Text.UTF8Encoding]::new($hasBom);guid=$guid;png=$file.FullName;pngHash=(Get-FileHash -LiteralPath $file.FullName).Hash}
}
$appearanceHashes=@{}
foreach($file in Get-ChildItem -LiteralPath $enemyRoot -Recurse -Filter '*_Appearance.asset' -File) {$appearanceHashes[$file.FullName]=(Get-FileHash -LiteralPath $file.FullName).Hash}
foreach($plan in $plans) {
    if([Convert]::ToBase64String([IO.File]::ReadAllBytes($plan.path)) -cne [Convert]::ToBase64String($plan.originalBytes)) {throw "Metadata changed during preparation: $($plan.path)"}
}
foreach($plan in $plans) {
    $backupPath=Join-Path $before $plan.relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backupPath)) | Out-Null
    [IO.File]::WriteAllBytes($backupPath,$plan.originalBytes)
    [IO.File]::WriteAllText($plan.path,$plan.text,$plan.encoding)
}
$report=@()
foreach($plan in $plans) {
    $saved=[IO.File]::ReadAllText($plan.path)
    if($saved -notmatch '(?m)^  spriteMode: 1\r?$' -or $saved -notmatch '(?m)^  textureType: 8\r?$' -or $saved -notmatch '(?m)^  spriteMeshType: 1\r?$') {throw "Sprite configuration did not persist: $($plan.path)"}
    if([regex]::Match($saved,'(?m)^guid: ([0-9a-f]{32})').Groups[1].Value -ne $plan.guid) {throw 'Texture GUID changed.'}
    if((Get-FileHash -LiteralPath $plan.png).Hash -ne $plan.pngHash) {throw 'Character PNG content changed.'}
    $report+=@{file=$plan.png.Substring($root.Length).TrimStart('\','/');spriteMode='Single';meshType='FullRect';guid=$plan.guid;pngUnchanged=$true;textureFileId=2800000;spriteFileId=21300000}
}
foreach($entry in $appearanceHashes.GetEnumerator()) {if((Get-FileHash -LiteralPath $entry.Key).Hash -ne $entry.Value) {throw "Appearance texture reference asset changed: $($entry.Key)"}}
$summary=@{characterCount=$report.Count;sprites=$report;appearanceAssetsUnchanged=$appearanceHashes.Count;onlyImporterMetadataChanged=$true;unityLaunched=$false}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($summary | ConvertTo-Json -Depth 6),$encoding)
[PSCustomObject]@{characters=$report.Count;appearanceAssetsUnchanged=$appearanceHashes.Count;pngsUnchanged=$true;guidsPreserved=$true;mode='Sprite Single / Full Rect';unityLaunched=$false} | ConvertTo-Json
