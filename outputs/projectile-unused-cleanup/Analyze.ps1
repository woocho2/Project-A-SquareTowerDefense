$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$artRoot=Join-Path $root 'Assets/4. Asset/3. Projectile'
$legacyAnimationRoot=Join-Path $root 'Assets/10.Animation/Projectile'
$rows=@(Get-ChildItem -LiteralPath $artRoot,$legacyAnimationRoot -Recurse -File | Where-Object Extension -ne '.meta' | ForEach-Object{
    $guid=[regex]::Match([IO.File]::ReadAllText($_.FullName+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value
    [pscustomobject]@{path=$_.FullName.Substring($root.Length+1).Replace('\','/');guid=$guid;bytes=$_.Length;keep=$false;referrers=@()}
})
$guidMap=@{};$pathMap=@{};$edges=@{}
foreach($row in $rows){$guidMap[$row.guid]=$row;$pathMap[$row.path]=$row;$edges[$row.guid]=New-Object Collections.Generic.List[string]}
$rows.guid | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'guids.txt') -Encoding ASCII
$refPaths=@(rg -l -F -f (Join-Path $PSScriptRoot 'guids.txt') (Join-Path $root 'Assets') -g '!*.png' -g '!*.jpg' -g '!*.dll' -g '!*.xlsx' -g '!*.ttf' -g '!*.otf')
$queue=New-Object Collections.Generic.Queue[string]
foreach($path in $refPaths){
    $rel=$path.Substring($root.Length+1).Replace('\','/')
    $source=if($rel.EndsWith('.meta')){$rel.Substring(0,$rel.Length-5)}else{$rel}
    $text=[IO.File]::ReadAllText($path)
    if($rel.EndsWith('.meta')){$text=[regex]::Replace($text,'(?m)^guid: \w+\r?$','')}
    foreach($match in [regex]::Matches($text,'guid: ([a-f0-9]{32})')){
        $guid=$match.Groups[1].Value
        if(!$guidMap.ContainsKey($guid)){continue}
        $row=$guidMap[$guid]
        if($row.referrers -notcontains $rel){$row.referrers+=,$rel}
        if($pathMap.ContainsKey($source)){$edges[$pathMap[$source].guid].Add($guid)}
        elseif(!$row.keep){$row.keep=$true;$queue.Enqueue($guid)}
    }
}
while($queue.Count){
    $guid=$queue.Dequeue()
    foreach($dependency in $edges[$guid]){if(!$guidMap[$dependency].keep){$guidMap[$dependency].keep=$true;$queue.Enqueue($dependency)}}
}
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'audit.json') -Encoding UTF8
$remove=@($rows | Where-Object {!$_.keep});$keep=@($rows | Where-Object keep)
'KEEP: '+$keep.Count+'; REMOVE: '+$remove.Count+'; removable MiB: '+[math]::Round((($remove | Measure-Object bytes -Sum).Sum)/1MB,2)
$remove | ForEach-Object { 'REMOVE '+$_.path }
'--- Retained assets by directory ---'
$keep | Group-Object {[IO.Path]::GetDirectoryName($_.path)} | Select-Object Name,Count
foreach($row in $keep | Where-Object { $_.path -notmatch '/(Single|Splash)/' }){'KEEP '+$row.path+' <- '+($row.referrers -join ', ')}
