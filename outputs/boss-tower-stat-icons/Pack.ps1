$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
$specs=@(
    @{kind='Boss';file='Boss_Stat_Icon_Atlas.png';source='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-7b72d059-f9e1-4610-a5c0-7164bf325a69.png';columns=3;rows=2;names=@('BossStat_Defense','BossStat_Resistance','BossStat_Movement','BossStat_DamageTaken','BossStat_Healing','BossStat_Weight')},
    @{kind='AttackType';file='Tower_AttackType_Icon_Atlas.png';source='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-4653b3f9-e68f-43d6-8c7b-ea06e2453bc4.png';columns=2;rows=2;names=@('AttackType_Splash','AttackType_Single','AttackType_Buff','AttackType_Debuff')}
)
foreach($spec in $specs) {foreach($suffix in @('','.meta')) {if(Test-Path -LiteralPath (Join-Path $ui ($spec.file+$suffix))) {throw "Destination exists: $($spec.file+$suffix)"}}}
$protected=@{}
foreach($file in Get-ChildItem -LiteralPath $ui -File) {$protected[$file.FullName]=(Get-FileHash -LiteralPath $file.FullName).Hash}
Add-Type -AssemblyName System.Drawing
$helper=Get-Content -LiteralPath (Join-Path $root 'outputs/color-upgrade-symbols/Pack.ps1') -Raw
$code=[regex]::Match($helper,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@").Groups[1].Value
if([string]::IsNullOrWhiteSpace($code)) {throw 'Packing helper is missing.'}
$code=$code.Replace('UpgradeSymbolPacking','BossTowerStatPacking').Replace('Cell(Bitmap source, int index)','Cell(Bitmap source, int index, int columns, int rows)')
$code=$code.Replace('int left=(index%2)*source.Width/2, top=(index/2)*source.Height/2;','int cx=index%columns,cy=index/columns;int left=cx*source.Width/columns,top=cy*source.Height/rows;').Replace('int w=source.Width/2,h=source.Height/2;','int w=(cx+1)*source.Width/columns-left,h=(cy+1)*source.Height/rows-top;')
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$template=[IO.File]::ReadAllText((Join-Path $ui 'Tower_Card_Icon_Atlas_v1.png.meta'))
$encoding=[Text.UTF8Encoding]::new($false)
$reports=@()
foreach($spec in $specs) {
    $localSource=Join-Path $PSScriptRoot ("source-$($spec.kind).png")
    Copy-Item -LiteralPath $spec.source -Destination $localSource
    $source=[Drawing.Bitmap]::new($localSource)
    $atlas=[Drawing.Bitmap]::new(($spec.columns*512),($spec.rows*512),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics=[Drawing.Graphics]::FromImage($atlas)
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
    $records=@()
    try {
        for($i=0;$i -lt $spec.names.Count;$i++) {
            $cell=[BossTowerStatPacking]::Cell($source,$i,$spec.columns,$spec.rows)
            try {
                $stats=[BossTowerStatPacking]::Inspect($cell)
                if($stats[0] -lt 30 -or $stats[1] -lt 30 -or ($stats[0]+$stats[2]) -gt 482 -or ($stats[1]+$stats[3]) -gt 482) {throw "Invalid sprite padding: $($spec.names[$i])"}
                for($p=0;$p -lt 512;$p++) {if($cell.GetPixel($p,0).A -ne 0 -or $cell.GetPixel($p,511).A -ne 0 -or $cell.GetPixel(0,$p).A -ne 0 -or $cell.GetPixel(511,$p).A -ne 0) {throw 'Sprite crosses its cell boundary.'}}
                $column=$i%$spec.columns;$row=[int][Math]::Floor($i/$spec.columns)
                $graphics.DrawImageUnscaled($cell,$column*512,$row*512)
                $records+=@{name=$spec.names[$i];fileID=21300001+$i;spriteID=[Guid]::NewGuid().ToString('N');x=$column*512;y=($spec.rows-1-$row)*512;width=512;height=512;bounds=$stats[0..3]}
            } finally {$cell.Dispose()}
        }
        $graphics.Dispose()
        $destination=Join-Path $ui $spec.file
        $atlas.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$source.Dispose();$atlas.Dispose()}
    $guid=[Guid]::NewGuid().ToString('N')
    $header=($template -split '  spriteSheet:',2)[0] -replace '(?m)^guid: .*$',"guid: $guid"
    $blocks=@()
    foreach($entry in $records) {
        $blocks+=@"
    - serializedVersion: 2
      name: $($entry.name)
      rect:
        serializedVersion: 2
        x: $($entry.x)
        y: $($entry.y)
        width: 512
        height: 512
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $($entry.spriteID)
      internalID: $($entry.fileID)
      vertices: []
      indices:
      edges: []
      weights: []
"@
    }
    $table=($records | ForEach-Object {"      $($_.name): $($_.fileID)"}) -join "`n"
    $meta=$header+@"
  spriteSheet:
    serializedVersion: 2
    sprites:
$($blocks -join "`n")
    outline: []
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    secondaryTextures: []
    nameFileIdTable:
$table
  userData: Pastel cartoon $($spec.kind) stat icons - 512px cells, 32px transparent padding
  assetBundleName:
  assetBundleVariant:
"@
    [IO.File]::WriteAllText("$destination.meta",$meta+"`n",$encoding)
    if([regex]::Matches($meta,'(?m)^      name: ').Count -ne $spec.names.Count) {throw 'Wrong slice count.'}
    $reports+=@{file=$spec.file;size=@(($spec.columns*512),($spec.rows*512));guid=$guid;sha256=(Get-FileHash -LiteralPath $destination).Hash;sprites=$records;generationMode='built-in imagegen'}
}
foreach($entry in $protected.GetEnumerator()) {if((Get-FileHash -LiteralPath $entry.Key).Hash -ne $entry.Value) {throw "Existing asset changed: $($entry.Key)"}}
$summary=@{atlases=$reports;newSpriteCount=10;existingAssetsUnchanged=$true;skillIconsCreated=0;unityLaunched=$false}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($summary | ConvertTo-Json -Depth 8),$encoding)
$reports | ForEach-Object {[PSCustomObject]@{file=$_.file;size=($_.size -join 'x');count=$_.sprites.Count}} | ConvertTo-Json
