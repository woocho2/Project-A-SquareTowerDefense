$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assets = [IO.Path]::GetFullPath((Join-Path $root 'Assets'))
$controllerGuid = '42abc7f6d3ac4c40a467fb007ee45f49'
$surfaceGuid = 'f43424de21bfacd130bf5c0d3f0d9400'
$panelGuid = '5490e0460ebb7953510c8ca8aa97ad75'
$blockPattern = '(?ms)^--- !u!\d+ &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)'
$utf8 = [Text.UTF8Encoding]::new($false)
$report = @()
foreach($file in Get-ChildItem -LiteralPath $assets -Recurse -File | Where-Object {$_.Extension -in '.prefab','.unity','.asset'}) {
    $full = [IO.Path]::GetFullPath($file.FullName)
    if(-not $full.StartsWith($assets+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw 'Target outside Assets'}
    $original = [IO.File]::ReadAllText($full)
    if($original -notmatch "$controllerGuid|$surfaceGuid|$panelGuid") {continue}
    $result = $original
    $removed=0;$surfaces=0;$panels=0
    foreach($block in [regex]::Matches($original,$blockPattern)) {
        $body=$block.Value
        if($body -match "m_Script:.*guid: $controllerGuid") {
            $id=$block.Groups['id'].Value
            $result=$result.Replace($body,'')
            $result=[regex]::Replace($result,"(?m)^  - component: \{fileID: $id\}\r?\n",'')
            $removed++
        } elseif($body -match "m_Script:.*guid: $surfaceGuid") {
            $clean=[regex]::Replace($body,'(?m)^  (useStageTheme|customColor):[^\r\n]*(?:\r?\n|$)','')
            if($clean -ne $body){$result=$result.Replace($body,$clean);$surfaces++}
        } elseif($body -match "m_Script:.*guid: $panelGuid") {
            $clean=[regex]::Replace($body,'(?m)^  (useStageTheme|backgroundColor|frameColor):[^\r\n]*(?:\r?\n|$)','')
            if($clean -ne $body){$result=$result.Replace($body,$clean);$panels++}
        }
    }
    if($result -eq $original) {continue}
    $beforeColors=([regex]::Matches($original,'(?m)^  m_Color:[^\r\n]*') | ForEach-Object {$_.Value}) -join "`n"
    $afterColors=([regex]::Matches($result,'(?m)^  m_Color:[^\r\n]*') | ForEach-Object {$_.Value}) -join "`n"
    if($beforeColors -cne $afterColors){throw "Serialized graphic colors changed: $full"}
    $beforeSprites=([regex]::Matches($original,'(?m)^  m_Sprite:[^\r\n]*') | ForEach-Object {$_.Value}) -join "`n"
    $afterSprites=([regex]::Matches($result,'(?m)^  m_Sprite:[^\r\n]*') | ForEach-Object {$_.Value}) -join "`n"
    if($beforeSprites -cne $afterSprites){throw "Serialized sprites changed: $full"}
    $rel=$full.Substring($root.Length+1)
    $backup=Join-Path (Join-Path $PSScriptRoot 'before') ($rel+'.txt')
    if(Test-Path -LiteralPath $backup){throw "Backup already exists: $backup"}
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($backup)) -Force | Out-Null
    Copy-Item -LiteralPath $full -Destination $backup
    [IO.File]::WriteAllText($full,$result,$utf8)
    $report+=@{path=$rel;removedControllers=$removed;cleanedSurfaces=$surfaces;cleanedPanels=$panels;graphicColorsUnchanged=$true;spritesUnchanged=$true}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'serialized-verification.json'),(ConvertTo-Json -InputObject @($report) -Depth 5),$utf8)
$report|ConvertTo-Json -Compress
