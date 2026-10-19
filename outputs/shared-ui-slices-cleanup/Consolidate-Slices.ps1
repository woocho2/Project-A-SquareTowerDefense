$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assets = Join-Path $root 'Assets'
$atlas = Join-Path $assets '4. Asset/5. UI/Shared_UI_Atlas.png'
$metaPath = "$atlas.meta"
$original = [IO.File]::ReadAllText($metaPath)
$atlasHash = (Get-FileHash -LiteralPath $atlas).Hash
$guid = [regex]::Match($original,'(?m)^guid: (\w+)').Groups[1].Value
$keepNames = @('Shared_A_Surface','Shared_Frame_Decorated','Shared_Frame_Compact')
$blockPattern = '(?ms)^    - serializedVersion: 2\r?\n.*?(?=^    - serializedVersion: 2|^    outline:)'
$blocks = [regex]::Matches($original,$blockPattern)
$records = @()
foreach($block in $blocks) {
    $name = [regex]::Match($block.Value,'(?m)^      name: (.+)\r?$').Groups[1].Value.Trim()
    $id = [regex]::Match($block.Value,'(?m)^      internalID: (-?\d+)').Groups[1].Value
    $rectValues = @('x','y','width','height') | ForEach-Object {
        [regex]::Match($block.Value,"(?m)^        ${_}: (-?\d+)").Groups[1].Value
    }
    $records += @{name=$name;id=$id;rect=($rectValues -join ',');block=$block.Value}
}
$keep = @($records | Where-Object {$_.name -in $keepNames})
if($keep.Count -ne 3 -or @($keep.rect | Select-Object -Unique).Count -ne 3) {throw 'Canonical sprite definitions do not match the expected three originals'}
$idMap = @{}
foreach($record in $records) {
    $target = @($keep | Where-Object {$_.rect -eq $record.rect})
    if($target.Count -ne 1) {throw "Sprite has no unique replacement: $($record.name)"}
    $idMap[$record.id] = $target[0].id
}
$backupRoot = Join-Path $PSScriptRoot 'before'
if(Test-Path -LiteralPath $backupRoot){throw 'Cleanup backup already exists'}
New-Item -ItemType Directory -Path $backupRoot | Out-Null
function Backup([string]$fullPath) {
    $resolved = [IO.Path]::GetFullPath($fullPath)
    if(-not $resolved.StartsWith($assets+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Backup target outside Assets'}
    $rel = $resolved.Substring($root.Length+1)
    $dest = Join-Path $backupRoot ($rel+'.txt')
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($dest)) -Force | Out-Null
    Copy-Item -LiteralPath $resolved -Destination $dest
}
$utf8 = [Text.UTF8Encoding]::new($false)
$referencePattern = '(?<prefix>\{fileID:\s*)(?<id>-?\d+)(?<rest>,\s*guid:\s*'+$guid+',\s*type:\s*\d+\})'
$changedFiles = @()
foreach($file in Get-ChildItem -LiteralPath $assets -Recurse -File | Where-Object {$_.Extension -in '.prefab','.unity','.asset','.mat','.anim','.controller'}) {
    $text = [IO.File]::ReadAllText($file.FullName)
    if(-not $text.Contains($guid)) {continue}
    $result = [regex]::Replace($text,$referencePattern,[Text.RegularExpressions.MatchEvaluator]{param($m)
        $id=$m.Groups['id'].Value
        if($idMap.ContainsKey($id)) {
            return $m.Groups['prefix'].Value+$idMap[$id]+$m.Groups['rest'].Value
        }
        return $m.Value
    })
    if($result -ceq $text) {continue}
    # Ensure only references to this atlas changed; all UI colors/layout stay intact.
    $strip = [Text.RegularExpressions.MatchEvaluator]{param($m) $m.Groups['prefix'].Value+'SPRITE_ID'+$m.Groups['rest'].Value}
    if([regex]::Replace($text,$referencePattern,$strip) -cne [regex]::Replace($result,$referencePattern,$strip)) {throw 'Unexpected serialized data change'}
    Backup $file.FullName
    [IO.File]::WriteAllText($file.FullName,$result,$utf8)
    $changedFiles += $file.FullName.Substring($root.Length+1)
}
$meta = $original
foreach($record in $records) {
    if($record.name -notin $keepNames) {$meta=$meta.Replace($record.block,'')}
}
$table = "    nameFileIdTable:`r`n" + (($keep | ForEach-Object {"      $($_.name): $($_.id)"}) -join "`r`n") + "`r`n"
$meta = [regex]::Replace($meta,'(?ms)^    nameFileIdTable:\r?\n.*?(?=^  \S|\z)',[Text.RegularExpressions.MatchEvaluator]{param($m) $table})
Backup $metaPath
[IO.File]::WriteAllText($metaPath,$meta,$utf8)
if((Get-FileHash -LiteralPath $atlas).Hash -ne $atlasHash) {throw 'PNG was changed'}
$newBlocks=[regex]::Matches($meta,$blockPattern)
if($newBlocks.Count -ne 3) {throw 'Unexpected remaining sprite count'}
foreach($entry in $keep) {
    if(-not $meta.Contains($entry.block)) {throw "Canonical sprite definition changed: $($entry.name)"}
}
$remainingBadRefs=@()
foreach($file in Get-ChildItem -LiteralPath $assets -Recurse -File | Where-Object {$_.Extension -in '.prefab','.unity','.asset','.mat','.anim','.controller'}) {
    $text=[IO.File]::ReadAllText($file.FullName)
    if(-not $text.Contains($guid)) {continue}
    foreach($match in [regex]::Matches($text,$referencePattern)) {
        $id=$match.Groups['id'].Value
        if($idMap.ContainsKey($id) -and $idMap[$id] -ne $id) {$remainingBadRefs+=$file.FullName}
    }
}
if($remainingBadRefs.Count -gt 0){throw 'Removed sprite references remain'}
$report=@{atlas='Assets/4. Asset/5. UI/Shared_UI_Atlas.png';guid=$guid;pngSha256=$atlasHash;pngUnchanged=$true;oldSliceCount=$records.Count;newSliceCount=$keep.Count;keptSprites=@($keep | ForEach-Object {@{name=$_.name;fileID=$_.id;rect=$_.rect}});remappedFiles=$changedFiles;colorsAndLayoutUnchanged=$true;removedReferencesRemaining=0;idMap=$idMap}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report|ConvertTo-Json -Depth 7),$utf8)
"Slices: $($records.Count) -> $($keep.Count). Remapped files: $($changedFiles.Count). PNG/colors/layout unchanged."
