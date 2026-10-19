$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
$before=Join-Path $PSScriptRoot 'before'
if(Test-Path -LiteralPath $before) { throw 'Cleanup already started; inspect backups before retrying.' }
$atlas=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'atlas.json') -Raw | ConvertFrom-Json
$newGuid=$atlas.guid
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class LosslessUiPacking {
    public static void Copy(Bitmap src,Bitmap dst,int sx,int sy,int dx,int dy,int w,int h) {
        var a=src.LockBits(new Rectangle(sx,sy,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var b=dst.LockBits(new Rectangle(dx,dy,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        try {
            var row=new byte[w*4];
            for(int y=0;y<h;y++) {Marshal.Copy(IntPtr.Add(a.Scan0,y*a.Stride),row,0,row.Length);Marshal.Copy(row,0,IntPtr.Add(b.Scan0,y*b.Stride),row.Length);}
        } finally {src.UnlockBits(a);dst.UnlockBits(b);}
    }
    public static int Differences(Bitmap a,Bitmap b,int ax,int ay,int bx,int by,int w,int h) {
        int different=0;
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(a.GetPixel(ax+x,ay+y).ToArgb()!=b.GetPixel(bx+x,by+y).ToArgb())different++;
        return different;
    }
}
'@
$utf8=[Text.UTF8Encoding]::new($false)
$pending=@{};$originals=@{};$pixelChecks=@();$referenceChanges=@()
function Read-Original([string]$path) {
    if(-not $originals.ContainsKey($path)) {$originals[$path]=[IO.File]::ReadAllBytes($path)}
    return [IO.File]::ReadAllText($path)
}
function Queue-Text([string]$path,[string]$text) {
    $old=Read-Original $path
    if($old -ceq $text) {return}
    $bytes=$originals[$path]
    $withBom=$bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
    $encoding=[Text.UTF8Encoding]::new($withBom)
    $pending[$path]=@{text=$text;encoding=$encoding}
}
function Subset-Metadata([string]$meta,[string[]]$names,$coordinates) {
    $matches=[regex]::Matches($meta,'(?ms)^    - serializedVersion: 2\r?\n.*?(?=^    - serializedVersion: 2|^    outline:|\z)')
    if($matches.Count -eq 0) {throw 'No sprite records.'}
    $blocks=@();$removed=@()
    foreach($entry in $matches) {
        $name=[regex]::Match($entry.Value,'(?m)^      name: (.+)\r?$').Groups[1].Value.Trim()
        if($names -notcontains $name) {$removed+=$name;continue}
        $block=$entry.Value
        $block=[regex]::Replace($block,'(?m)^        x: \d+',"        x: $($coordinates[$name][0])")
        $block=[regex]::Replace($block,'(?m)^        y: \d+',"        y: $($coordinates[$name][1])")
        $blocks+=$block
    }
    if($blocks.Count -ne $names.Count) {throw 'Unexpected retained sprite count.'}
    $tailStart=$matches[$matches.Count-1].Index+$matches[$matches.Count-1].Length
    $tail=$meta.Substring($tailStart)
    foreach($name in $removed) {$tail=[regex]::Replace($tail,'(?m)^      '+[regex]::Escape($name)+':[^\r\n]*\r?\n','')}
    return $meta.Substring(0,$matches[0].Index)+($blocks -join '')+$tail
}
# Prepare lossless stat atlas crop. Keep all six sprite IDs and pixels unchanged.
$statPath=Join-Path $ui 'Tower_Card_Icon_Atlas_v1.png'
$originals[$statPath]=[IO.File]::ReadAllBytes($statPath)
$statSource=[Drawing.Bitmap]::new($statPath)
$statPacked=[Drawing.Bitmap]::new(1536,1024,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$statNames=@('CardStat_AttackPower','CardStat_AttackCount','CardStat_Range','CardStat_Action','CardStat_CriticalRate','CardStat_CriticalDamage')
$statCoordinates=@{}
try {
    if($statSource.Width -ne 1536 -or $statSource.Height -ne 1536) {throw 'Unexpected original stat atlas size.'}
    [LosslessUiPacking]::Copy($statSource,$statPacked,0,0,0,0,1536,1024)
    for($i=0;$i -lt 6;$i++) {
        $x=($i%3)*512;$top=[int][Math]::Floor($i/3)*512
        $delta=[LosslessUiPacking]::Differences($statSource,$statPacked,$x,$top,$x,$top,512,512)
        if($delta -ne 0) {throw "Stat pixels changed: $($statNames[$i])"}
        $pixelChecks+=@{sprite=$statNames[$i];differentPixels=$delta}
        $statCoordinates[$statNames[$i]]=@($x,(512-$top))
    }
    $statStage=Join-Path $PSScriptRoot 'stat-packed.png'
    $statPacked.Save($statStage,[Drawing.Imaging.ImageFormat]::Png)
} finally {$statSource.Dispose();$statPacked.Dispose()}
Queue-Text "$statPath.meta" (Subset-Metadata (Read-Original "$statPath.meta") $statNames $statCoordinates)
# Retain the four currency/life icons, moving only their atlas coordinates.
$currencyPath=Join-Path $ui 'UI_Icon_Atlas.png'
$originals[$currencyPath]=[IO.File]::ReadAllBytes($currencyPath)
$currencySource=[Drawing.Bitmap]::new($currencyPath)
$currencyPacked=[Drawing.Bitmap]::new(1024,1024,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$currencyNames=@('Icon_Gold','Icon_Gem','Icon_Heart','Icon_Heart_White')
$currencyCoordinates=@{}
try {
    if($currencySource.Width -ne 2048 -or $currencySource.Height -ne 1024) {throw 'Unexpected original currency atlas size.'}
    for($i=0;$i -lt 4;$i++) {
        $x=($i%2)*512;$top=[int][Math]::Floor($i/2)*512
        [LosslessUiPacking]::Copy($currencySource,$currencyPacked,$i*512,0,$x,$top,512,512)
        $delta=[LosslessUiPacking]::Differences($currencySource,$currencyPacked,$i*512,0,$x,$top,512,512)
        if($delta -ne 0) {throw "Currency pixels changed: $($currencyNames[$i])"}
        $pixelChecks+=@{sprite=$currencyNames[$i];differentPixels=$delta}
        $currencyCoordinates[$currencyNames[$i]]=@($x,(512-$top))
    }
    $currencyStage=Join-Path $PSScriptRoot 'currency-packed.png'
    $currencyPacked.Save($currencyStage,[Drawing.Imaging.ImageFormat]::Png)
} finally {$currencySource.Dispose();$currencyPacked.Dispose()}
Queue-Text "$currencyPath.meta" (Subset-Metadata (Read-Original "$currencyPath.meta") $currencyNames $currencyCoordinates)
# Replace only sprite references; keep object bindings, theme colors and layout intact.
$map=@{}
function Map-Reference([string]$guid,[string]$id,[string]$targetGuid,[string]$targetId) {$map["$guid/$id"]=@($targetGuid,$targetId)}
$speedGuid='d6b123b8dff94d30b9e9b3dcfa6379b7';$menuGuid='dba8c17960b957f42bf1753d04fb77e4'
$colorGuid='355c07fb26724b29ac98ee594f2122dc';$plusGuid='10c28f78066e426fbf103d7981e38406'
$currencyGuid='b201a001000100010001000100010010';$statGuid='a67d8bdf1e6e8cc2619cbd8c9aa63762'
foreach($id in '21300000','-7301054656936543366') {Map-Reference $speedGuid $id $newGuid '21300001'}
foreach($id in '21300000','9132232204949289102') {Map-Reference $menuGuid $id $newGuid '21300002'}
for($i=0;$i -lt 4;$i++) {Map-Reference $colorGuid ([string](21300001+$i)) $newGuid ([string](21300003+$i))}
Map-Reference $currencyGuid '100101' $newGuid '21300007'
Map-Reference $currencyGuid '100102' $newGuid '21300008'
Map-Reference $plusGuid '21300000' $newGuid '21300009'
Map-Reference $statGuid '21300007' '32d345500e722187021b31f6a2532e11' '21300001'
Map-Reference $statGuid '21300008' '19553626f4844d3f7eef9633c82b5e40' '21300001'
Map-Reference $statGuid '21300009' '60fdae6670a86c8b4a9d4e44e2fe3af6' '21300001'
$extensions=@('.prefab','.unity','.asset','.mat','.controller','.overrideController','.anim','.playable','.spriteatlas','.spriteatlasv2')
$files=@(Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -File | Where-Object {$extensions -contains $_.Extension})
$referencePattern='\{fileID:\s*(?<id>-?\d+),\s*guid:\s*(?<guid>[0-9a-f]{32}),\s*type:\s*\d+\}'
foreach($file in $files) {
    $text=[IO.File]::ReadAllText($file.FullName)
    $replacement=[regex]::Replace($text,$referencePattern,[Text.RegularExpressions.MatchEvaluator]{param($m)
        $key=$m.Groups['guid'].Value+'/'+$m.Groups['id'].Value
        if(-not $map.ContainsKey($key)) {return $m.Value}
        $target=$map[$key]
        return '{fileID: '+$target[1]+', guid: '+$target[0]+', type: 3}'
    })
    if($replacement -cne $text) {
        $count=@([regex]::Matches($text,$referencePattern) | Where-Object {$map.ContainsKey($_.Groups['guid'].Value+'/'+$_.Groups['id'].Value)}).Count
        $referenceChanges+=@{file=$file.FullName;replacements=$count}
        Queue-Text $file.FullName $replacement
    }
}
# Collapse the existing three menu images into the middle image with the combined sprite.
$canvasPath=Join-Path $root 'Assets/2. Prefab/Canvas.prefab'
$canvas=if($pending.ContainsKey($canvasPath)) {$pending[$canvasPath].text} else {Read-Original $canvasPath}
$yamlPattern='(?ms)^--- !u!(?<kind>\d+) &(?<id>-?\d+)\r?\n(?<body>.*?)(?=^--- !u!|\z)'
$yaml=[regex]::Matches($canvas,$yamlPattern)
$removeGo=@('3694016219109242302','7282225896047038306')
$removeIds=@();$removeRects=@('2314047688285021924','4606684125787514318')
foreach($block in $yaml) {
    if($removeGo -contains $block.Groups['id'].Value -or $block.Groups['body'].Value -match 'm_GameObject: \{fileID: (3694016219109242302|7282225896047038306)\}') {$removeIds+=$block.Groups['id'].Value}
}
if($removeIds.Count -ne 8) {throw 'Expected two menu objects with RectTransform, CanvasRenderer and Image components.'}
foreach($block in $yaml) {
    if($removeIds -contains $block.Groups['id'].Value) {continue}
    foreach($id in $removeIds) {
        $pattern='fileID: '+$id+'\D'
        if($block.Value -match $pattern -and $removeRects -notcontains $id) {throw "Unexpected external menu object reference: $id"}
    }
}
$middleId='1465714077483702062'
$canvas=[regex]::Replace($canvas,$yamlPattern,[Text.RegularExpressions.MatchEvaluator]{param($m)
    $id=$m.Groups['id'].Value
    if($removeIds -contains $id) {return ''}
    $value=$m.Value
    foreach($rect in $removeRects) {$value=[regex]::Replace($value,'(?m)^  - \{fileID: '+$rect+'\}\r?\n','')}
    if($id -eq $middleId) {$value=[regex]::Replace($value,'(?m)^  m_SizeDelta:.*$', '  m_SizeDelta: {x: 60, y: 60}')}
    if($id -eq '6281549524302108618') {$value=$value.Replace('m_Name: Img_Option_Middle','m_Name: Img_Option_Menu')}
    if($id -eq '293628976946530228') {$value=[regex]::Replace($value,'(?m)^  m_PreserveAspect:.*$', '  m_PreserveAspect: 1')}
    return $value
})
Queue-Text $canvasPath $canvas
# Validate every planned reference before changing any original asset.
$retiredGuids=@($speedGuid,$menuGuid,$colorGuid,$plusGuid)
foreach($file in $files) {
    $text=if($pending.ContainsKey($file.FullName)) {$pending[$file.FullName].text} else {[IO.File]::ReadAllText($file.FullName)}
    foreach($m in [regex]::Matches($text,$referencePattern)) {
        $g=$m.Groups['guid'].Value;$id=$m.Groups['id'].Value
        if($retiredGuids -contains $g) {throw "Unmigrated retired asset reference: $($file.FullName) $g/$id"}
        if($g -eq $currencyGuid -and $id -in @('100101','100102','100103','100104')) {throw "Removed currency slice still referenced: $id"}
        if($g -eq $statGuid -and $id -in @('21300007','21300008','21300009')) {throw "Removed badge slice still referenced: $id"}
        if($g -eq $newGuid -and @($atlas.sprites.fileID | ForEach-Object {[string]$_}) -notcontains $id) {throw "Unknown new action sprite: $id"}
    }
    foreach($id in $removeIds) {if($text -match ('fileID: '+$id+'\D')) {throw "Removed menu object still referenced: $id"}}
}
$retired=@('Btn_Symbol_GameSpeed.png','Menu_Bar_White_PureLight_1024.png','UI_Color_Upgrade_Atlas.png','UI_Upgrade_Plus.png')
foreach($name in $retired) {foreach($suffix in @('','.meta')) {$p=Join-Path $ui ($name+$suffix);$originals[$p]=[IO.File]::ReadAllBytes($p)}}
# Guard against concurrent editor saves and back up the exact original bytes outside Assets.
foreach($entry in $originals.GetEnumerator()) {
    $current=[IO.File]::ReadAllBytes($entry.Key)
    if([Convert]::ToBase64String($current) -cne [Convert]::ToBase64String($entry.Value)) {throw "File changed while preparing consolidation: $($entry.Key)"}
}
foreach($entry in $originals.GetEnumerator()) {
    $relative=$entry.Key.Substring($root.Length).TrimStart('\','/')
    $backupPath=Join-Path $before $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backupPath)) | Out-Null
    [IO.File]::WriteAllBytes($backupPath,$entry.Value)
}
Copy-Item -LiteralPath $statStage -Destination $statPath
Copy-Item -LiteralPath $currencyStage -Destination $currencyPath
foreach($entry in $pending.GetEnumerator()) {[IO.File]::WriteAllText($entry.Key,$entry.Value.text,$entry.Value.encoding)}
foreach($name in $retired) {
    foreach($suffix in @('','.meta')) {
        $target=(Resolve-Path -LiteralPath (Join-Path $ui ($name+$suffix))).Path
        if(-not $target.StartsWith($ui+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {throw "Retirement outside intended UI directory: $target"}
        Remove-Item -LiteralPath $target
    }
}
$report=@{newAtlas=$atlas.atlas;retiredAssets=$retired;statAtlasSize=@(1536,1024);currencyAtlasSize=@(1024,1024);retainedPixelChecks=$pixelChecks;referenceChanges=$referenceChanges;removedMenuObjects=$removeGo;remainingMenuImage='Img_Option_Menu';backup=$before;unityLaunched=$false;modifiedTextFiles=@($pending.Keys)}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'consolidation.json'),($report | ConvertTo-Json -Depth 7),$utf8)
$report | ConvertTo-Json -Depth 7
