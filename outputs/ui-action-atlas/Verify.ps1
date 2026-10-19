$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
Add-Type -AssemblyName System.Drawing
$helper=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Consolidate.ps1') -Raw
$code=[regex]::Match($helper,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@").Groups[1].Value
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
function Sprites([string]$path) {
    $meta=[IO.File]::ReadAllText($path)
    $sprites=@()
    foreach($block in [regex]::Matches($meta,'(?ms)^    - serializedVersion: 2\r?\n.*?(?=^    - serializedVersion: 2|^    outline:|\z)')) {
        $values=@{}
        foreach($key in @('name','x','y','width','height','spriteID','internalID')) {
            $values[$key]=[regex]::Match($block.Value,'(?m)^\s+'+$key+': ([^\r\n]+)').Groups[1].Value.Trim()
        }
        $sprites+=$values
    }
    return $sprites
}
$report=@{retainedSprites=@();atlasCounts=@{};removedReferences=0;menuImages=0;unityLaunched=$false}
foreach($name in @('Tower_Card_Icon_Atlas_v1.png','UI_Icon_Atlas.png')) {
    $current=Join-Path $ui $name
    $old=Join-Path $PSScriptRoot ('before/Assets/4. Asset/5. UI/'+$name)
    $beforeSprites=Sprites "$old.meta";$afterSprites=Sprites "$current.meta"
    $expected=if($name -eq 'UI_Icon_Atlas.png') {4} else {6}
    if($afterSprites.Count -ne $expected) {throw "Wrong retained count: $name"}
    $report.atlasCounts[$name]=$afterSprites.Count
    $beforeGuid=[regex]::Match([IO.File]::ReadAllText("$old.meta"),'(?m)^guid: ([0-9a-f]+)').Groups[1].Value
    $afterGuid=[regex]::Match([IO.File]::ReadAllText("$current.meta"),'(?m)^guid: ([0-9a-f]+)').Groups[1].Value
    if($beforeGuid -ne $afterGuid) {throw "Changed retained atlas GUID: $name"}
    $a=[Drawing.Bitmap]::new($old);$b=[Drawing.Bitmap]::new($current)
    try {
        foreach($sprite in $afterSprites) {
            $prior=@($beforeSprites | Where-Object {$_.name -eq $sprite.name})[0]
            if($prior.internalID -cne $sprite.internalID -or $prior.spriteID -cne $sprite.spriteID) {throw "Changed retained sprite ID: $($sprite.name)"}
            $different=[LosslessUiPacking]::Differences($a,$b,[int]$prior.x,($a.Height-[int]$prior.y-512),[int]$sprite.x,($b.Height-[int]$sprite.y-512),512,512)
            if($different -ne 0) {throw "Changed saved PNG pixels: $($sprite.name)"}
            $report.retainedSprites+=@{name=$sprite.name;differentPixels=$different;idsPreserved=$true}
        }
    } finally {$a.Dispose();$b.Dispose()}
}
$action=Join-Path $ui 'UI_Action_Atlas.png'
$sprites=Sprites "$action.meta"
if($sprites.Count -ne 9 -or @($sprites.internalID | Select-Object -Unique).Count -ne 9) {throw 'Expected nine distinct new sprites.'}
$bitmap=[Drawing.Bitmap]::new($action)
try {
    if($bitmap.Width -ne 1536 -or $bitmap.Height -ne 1536) {throw 'Wrong new atlas dimensions.'}
    foreach($sprite in $sprites) {
        if($sprite.width -ne '512' -or $sprite.height -ne '512') {throw 'Wrong cell dimensions.'}
        $x=[int]$sprite.x;$top=1536-[int]$sprite.y-512
        for($i=0;$i -lt 512;$i++) {
            if($bitmap.GetPixel($x+$i,$top).A -ne 0 -or $bitmap.GetPixel($x+$i,$top+511).A -ne 0 -or $bitmap.GetPixel($x,$top+$i).A -ne 0 -or $bitmap.GetPixel($x+511,$top+$i).A -ne 0) {throw "Sprite touches cell boundary: $($sprite.name)"}
        }
    }
} finally {$bitmap.Dispose()}
$report.atlasCounts['UI_Action_Atlas.png']=9
$canvas=[IO.File]::ReadAllText((Join-Path $root 'Assets/2. Prefab/Canvas.prefab'))
if([regex]::Matches($canvas,'m_Name: Img_Option_Menu').Count -ne 1 -or $canvas -match 'm_Name: Img_Option_(Top|Middle|Bottom)') {throw 'Expected one combined menu image.'}
$report.menuImages=1
$bad='d6b123b8dff94d30b9e9b3dcfa6379b7|dba8c17960b957f42bf1753d04fb77e4|355c07fb26724b29ac98ee594f2122dc|10c28f78066e426fbf103d7981e38406'
$extensions=@('.prefab','.unity','.asset','.mat','.controller','.overrideController','.anim','.playable','.spriteatlas','.spriteatlasv2','.cs')
foreach($file in Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Recurse -File | Where-Object {$extensions -contains $_.Extension}) {
    if([IO.File]::ReadAllText($file.FullName) -match $bad) {throw "Stale asset reference: $($file.FullName)"}
}
$encoding=[Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report | ConvertTo-Json -Depth 7),$encoding)
$report | ConvertTo-Json -Depth 7
