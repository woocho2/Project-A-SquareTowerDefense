$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskPrefab=Join-Path $taskRepo 'Assets/2. Prefab/Canvas.prefab'
$taskText=[IO.File]::ReadAllText($taskPrefab)
$taskUtf8=New-Object Text.UTF8Encoding($false)
$taskBlockPattern='(?ms)^--- !u!(\d+) &(\d+)[^\r\n]*\r?\n.*?(?=^---|\z)'
$taskBlocks=@{};$taskNames=@{};$taskRects=@{};$taskByGO=@{};$taskImages=@()
foreach($m in [regex]::Matches($taskText,$taskBlockPattern)){
    $id=$m.Groups[2].Value;$type=$m.Groups[1].Value;$block=$m.Value;$taskBlocks[$id]=$block
    if($type -eq '1'){$taskNames[$id]=[regex]::Match($block,'(?m)^  m_Name: (.*)').Groups[1].Value.Trim()}
    if($type -eq '224'){
        $go=[regex]::Match($block,'m_GameObject: \{fileID: (\d+)').Groups[1].Value
        $size=[regex]::Match($block,'m_SizeDelta: \{x: ([^,]+), y: ([^}]+)\}')
        $taskRects[$id]=@{go=$go;parent=[regex]::Match($block,'m_Father: \{fileID: (\d+)').Groups[1].Value;width=[double]$size.Groups[1].Value;height=[double]$size.Groups[2].Value}
        $taskByGO[$go]=$id
    }
    if($type -eq '114' -and $block.Contains('guid: fe87c0e1cc204ed48ad3b37840f39efc')){
        $taskImages+=@{id=$id;go=[regex]::Match($block,'m_GameObject: \{fileID: (\d+)').Groups[1].Value;block=$block}
    }
}
function Panel-Path([string]$go){
    $names=@();$current=$taskByGO[$go]
    while($taskRects.ContainsKey($current)){$names+=,$taskNames[$taskRects[$current].go];$current=$taskRects[$current].parent}
    return $names
}
$taskChanged=@{};$taskRectChanges=@{}
foreach($rectId in $taskRects.Keys){
    $rect=$taskRects[$rectId];$name=$taskNames[$rect.go];$path=Panel-Path $rect.go
    if('PlayerTurnPanel' -in $path -and 'Group_TurnEnd' -in $path -and $name -match '^(Group_TurnEnd|Btn_TurnEnd|Img_[^ ]*TurnEnd_(BG|Frame)(?: .*)?)$'){
        $taskChanged[$rectId]=[regex]::Replace($taskBlocks[$rectId],'(?m)^  m_SizeDelta: [^\r\n]+','  m_SizeDelta: {x: 400, y: 100}')
        $taskRects[$rectId].width=400;$taskRects[$rectId].height=100;$taskRectChanges[$rectId]=100
        if($name.Contains('5:1')){$taskChanged[$rect.go]=$taskBlocks[$rect.go].Replace('5:1','4:1');$taskNames[$rect.go]=$name.Replace('5:1','4:1')}
    }
}
if($taskRectChanges.Count -ne 4){throw 'Expected group, button hit area, background and frame for TurnEnd'}
$taskMeta=[IO.File]::ReadAllText((Join-Path $taskRepo 'Assets/4. Asset/5. UI/Shared_UI_Atlas.png.meta'))
$taskGuid=[regex]::Match($taskMeta,'(?m)^guid: (\w+)').Groups[1].Value
$taskAtlasSprites=@{}
foreach($m in [regex]::Matches($taskMeta,'(?ms)^      name: (Shared_[^\r\n]+).*?^      internalID: (\d+)')){$taskAtlasSprites[$m.Groups[1].Value]=$m.Groups[2].Value}
$taskSuffix=@{'1:1'='Btn_128';'1:2'='Capsule_128x256';'1:3'='Gauge_128x384';'1:4'='TallBanner_128x512';'2:3'='CharCard_256x384';'2:1'='Capsule_256x128';'3:1'='Capsule_384x128';'4:1'='Capsule_512x128';'3:2'='SkillCard_384x256'}
$taskRGB=@{'A'=@(1.0,0.9882352941176471,0.9254901960784314);'B'=@(1.0,0.8745098039215686,0.4901960784313725);'C'=@(0.1490196078431373,0.2196078431372549,0.3333333333333333);'Frame'=@(0.9254901960784314,0.7333333333333333,0.2549019607843137)}
$taskAssignments=@();$taskTargetIds=@{}
foreach($image in $taskImages){
    $name=$taskNames[$image.go];$path=Panel-Path $image.go
    if('GameInfoPanel' -notin $path -and 'PlayerTurnPanel' -notin $path){continue}
    $role=if($name -match '_Frame(?: |$)'){'Frame'}elseif($name -match '^Img_([ABC])_.*_BG(?: |$)'){$Matches[1]}else{$null}
    if(!$role){continue}
    $rect=$taskRects[$taskByGO[$image.go]];$ratio=$rect.width/$rect.height;$shape=$null
    foreach($key in $taskSuffix.Keys){$parts=$key -split ':';if([Math]::Abs($ratio-[double]$parts[0]/[double]$parts[1]) -lt .0001){$shape=$key;break}}
    if(!$shape){throw ('Unsupported panel ratio: '+$name)}
    $suffix=if($role -eq 'Frame' -and $shape -eq '1:1'){'Btn_128_B'}else{$taskSuffix[$shape]}
    $spriteName="Shared_${role}_$suffix";$spriteId=$taskAtlasSprites[$spriteName]
    if(!$spriteId){throw ('Atlas sprite missing: '+$spriteName)}
    $color=$taskRGB[$role];$alpha=[regex]::Match($image.block,'m_Color: \{r: [^,]+, g: [^,]+, b: [^,]+, a: ([^}]+)\}').Groups[1].Value
    $type=if($suffix -eq 'Btn_128_B'){1}else{0}
    $block=[regex]::Replace($image.block,'(?m)^  m_Sprite: [^\r\n]+',"  m_Sprite: {fileID: $spriteId, guid: $taskGuid, type: 3}")
    $block=[regex]::Replace($block,'(?m)^  m_Color: [^\r\n]+',"  m_Color: {r: $($color[0]), g: $($color[1]), b: $($color[2]), a: $alpha}")
    $block=[regex]::Replace($block,'(?m)^  m_Type: \d+',"  m_Type: $type")
    $taskChanged[$image.id]=$block
    if($name -match ' \d+:\d+$' -and !$name.EndsWith($shape)){$taskChanged[$image.go]=[regex]::Replace($taskBlocks[$image.go],'(?m)^(  m_Name: .* )\d+:\d+\r?$','$1'+$shape);$taskNames[$image.go]=[regex]::Replace($name,'\d+:\d+$',$shape)}
    $path[0]=$taskNames[$image.go]
    $entry=[pscustomobject]@{imageID=$image.id;rectID=$taskByGO[$image.go];path=($path[($path.Count-1)..0] -join '/');role=$role;ratio=$shape;width=$rect.width;height=$rect.height;sprite=$spriteName;fileID=$spriteId;guid=$taskGuid;type=$type;color=$color}
    $taskAssignments+=$entry;$taskTargetIds[$image.id]=$entry
}
if($taskAssignments.Count -ne 30){throw 'Expected thirty panel background/frame assignments'}
$taskResult=[regex]::Replace($taskText,$taskBlockPattern,[Text.RegularExpressions.MatchEvaluator]{param($m) $id=$m.Groups[2].Value;if($taskChanged.ContainsKey($id)){return $taskChanged[$id]};return $m.Value})
foreach($m in [regex]::Matches($taskResult,$taskBlockPattern)){
    $id=$m.Groups[2].Value
    if(!$taskChanged.ContainsKey($id) -and $m.Value -ne $taskBlocks[$id]){throw 'Unrequested prefab block changed'}
}
[IO.File]::WriteAllText($taskPrefab,$taskResult,$taskUtf8)
$taskPrefabGuid=[regex]::Match([IO.File]::ReadAllText($taskPrefab+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value
foreach($scene in Get-ChildItem -LiteralPath (Join-Path $taskRepo 'Assets/1. Scenes') -Filter '*.unity' -File){
    $text=[IO.File]::ReadAllText($scene.FullName)
    $after=[regex]::Replace($text,'(?m)^    - target: \{fileID: (\d+), guid: '+$taskPrefabGuid+', type: 3\}\r?\n      propertyPath: ([^\r\n]+)\r?\n      value: ([^\r\n]*)\r?\n      objectReference: [^\r\n]+',[Text.RegularExpressions.MatchEvaluator]{param($m)
        $id=$m.Groups[1].Value;$property=$m.Groups[2].Value;$block=$m.Value
        if($taskTargetIds.ContainsKey($id)){
            $entry=$taskTargetIds[$id]
            if($property -eq 'm_Sprite'){return [regex]::Replace($block,'objectReference: [^\r\n]+',"objectReference: {fileID: $($entry.fileID), guid: $taskGuid, type: 3}")}
            if($property -eq 'm_Type'){return [regex]::Replace($block,'(?m)^      value: [^\r\n]*',"      value: $($entry.type)")}
            if($property -match '^m_Color\.[rgb]$'){$n=@{'m_Color.r'=0;'m_Color.g'=1;'m_Color.b'=2}[$property];return [regex]::Replace($block,'(?m)^      value: [^\r\n]*',"      value: $($entry.color[$n])")}
        }
        if($taskRectChanges.ContainsKey($id) -and $property -eq 'm_SizeDelta.y'){return [regex]::Replace($block,'(?m)^      value: [^\r\n]*','      value: 100')}
        return $block
    })
    if($after -ne $text){[IO.File]::WriteAllText($scene.FullName,$after,$taskUtf8)}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'game-panels-assignments.json'),($taskAssignments | ConvertTo-Json -Depth 5)+"`n",$taskUtf8)
Write-Output ('Connected '+$taskAssignments.Count+' backgrounds/frames; TurnEnd group/hit area/background/frame use 400x100. Atlas unchanged.')
