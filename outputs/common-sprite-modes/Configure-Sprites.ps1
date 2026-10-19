$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$common=[IO.Path]::GetFullPath((Join-Path $root 'Assets/4. Asset/0. Common'))
$backup=Join-Path $PSScriptRoot 'before'
if(Test-Path -LiteralPath $backup){throw 'Sprite metadata backup already exists'}
New-Item -ItemType Directory -Path $backup | Out-Null
$utf8=[Text.UTF8Encoding]::new($false)
$definitions=@(
    @{name='Board_Background';size=512;border=80;ppu=384},
    @{name='Board_Decoration';size=512;border=160;ppu=384},
    @{name='Board_Arrow';size=256;border=96;ppu=192}
)
function Stable-Guid([string]$value) {
    $md5=[Security.Cryptography.MD5]::Create()
    try {return ([BitConverter]::ToString($md5.ComputeHash([Text.Encoding]::UTF8.GetBytes($value)))).Replace('-','').ToLowerInvariant()} finally {$md5.Dispose()}
}
$report=@()
foreach($definition in $definitions) {
    $name=$definition.name;$size=$definition.size
    $png=Join-Path $common ($name+'.png')
    $metaPath=$png+'.meta'
    if(-not [IO.Path]::GetFullPath($metaPath).StartsWith($common+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Metadata target outside Common'}
    $original=[IO.File]::ReadAllText($metaPath)
    if($original -notmatch '(?m)^  spriteMode: 1\r?$' -or $original -notmatch '    sprites: \[\]'){throw "Unexpected original sprite configuration: $name"}
    $pngHash=(Get-FileHash -LiteralPath $png).Hash
    $guid=[regex]::Match($original,'(?m)^guid: (\w+)').Groups[1].Value
    $oldSpriteID=[regex]::Match($original,'(?m)^    spriteID: (\w+)').Groups[1].Value
    $meta=$original.Replace('  spriteMode: 1','  spriteMode: 2')
    $blocks=@()
    $entries=@()
    foreach($mode in 'Single','Sliced') {
        $spriteName=$name+'_'+$mode
        $fileID=if($mode -eq 'Single'){21300000}else{21300001}
        $spriteID=if($mode -eq 'Single'){$oldSpriteID}else{Stable-Guid ($guid+'/'+$spriteName)}
        $border=if($mode -eq 'Single'){0}else{$definition.border}
        $blocks+=@"
    - serializedVersion: 2
      name: $spriteName
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: $size
        height: $size
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: $border, y: $border, z: $border, w: $border}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteID
      internalID: $fileID
      vertices: []
      indices:
      edges: []
      weights: []
"@
        $entries+=@{name=$spriteName;fileID=$fileID;border=$border;size=$size;spriteID=$spriteID}
    }
    $meta=$meta.Replace('    sprites: []',("    sprites:`n"+($blocks -join "`n")))
    $table=($entries | ForEach-Object {"      $($_.name): $($_.fileID)"}) -join "`n"
    $meta=$meta.Replace('    nameFileIdTable: {}',"    nameFileIdTable:`n$table")
    if($meta -notmatch "spritePixelsToUnits: $($definition.ppu)"){throw 'Pixels-per-unit unexpectedly changed'}
    if(([regex]::Matches($meta,'(?m)^      name: ')).Count -ne 2){throw 'Expected exactly two sprite definitions'}
    if([regex]::Match($meta,'(?m)^guid: (\w+)').Groups[1].Value -ne $guid){throw 'Asset GUID changed'}
    Copy-Item -LiteralPath $metaPath -Destination (Join-Path $backup ($name+'.png.meta.txt'))
    [IO.File]::WriteAllText($metaPath,$meta,$utf8)
    if((Get-FileHash -LiteralPath $png).Hash -ne $pngHash){throw 'Source PNG changed'}
    $report+=@{asset=('Assets/4. Asset/0. Common/'+$name+'.png');guid=$guid;pngSha256=$pngHash;pngUnchanged=$true;existingSpriteFileIDPreserved=21300000;pixelsPerUnit=$definition.ppu;sprites=$entries}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),(ConvertTo-Json -InputObject @($report) -Depth 6),$utf8)
$report | ForEach-Object {"$($_.asset): Single + Sliced; PNG/GUID/existing fileID preserved."}
