$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'before.json') -Raw | ConvertFrom-Json
$sources=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'sources.json') -Raw | ConvertFrom-Json
foreach($entry in $manifest) {if((Get-FileHash -LiteralPath (Join-Path $ui $entry.name)).Hash -ne $entry.sha256) {throw "Original asset changed since snapshot: $($entry.name)"}}
Add-Type -AssemblyName System.Drawing
$helper=Get-Content -LiteralPath (Join-Path $root 'outputs/color-upgrade-symbols/Pack.ps1') -Raw
$code=[regex]::Match($helper,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@").Groups[1].Value
$code=$code.Replace('UpgradeSymbolPacking','PastelCardPacking').Replace('Cell(Bitmap source, int index)','Cell(Bitmap source, int index, int columns, int rows, bool square)')
$code=$code.Replace('int left=(index%2)*source.Width/2, top=(index/2)*source.Height/2;','int cx=index%columns,cy=index/columns; int left=cx*source.Width/columns,top=cy*source.Height/rows;').Replace('int w=source.Width/2,h=source.Height/2;','int w=(cx+1)*source.Width/columns-left,h=(cy+1)*source.Height/rows-top;')
$code=$code.Replace('var cell=new Bitmap(512,512,PixelFormat.Format32bppArgb);',@'
int dw=448,dh=448;
            if(!square) {int sw=x1-x0+1,sh=y1-y0+1;dw=(int)Math.Round(sw*448.0/Math.Max(sw,sh));dh=(int)Math.Round(sh*448.0/Math.Max(sw,sh));}
            var cell=new Bitmap(512,512,PixelFormat.Format32bppArgb);
'@).Replace('new Rectangle(32,32,448,448)','new Rectangle((512-dw)/2,(512-dh)/2,dw,dh)')
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$prepared=@();$report=@()
foreach($kind in @('Attack','Stat','Emblem','Tier')) {
    $spec=$sources.$kind
    $sourcePath=Join-Path $PSScriptRoot ("source-$kind.png")
    Copy-Item -LiteralPath $spec.source -Destination $sourcePath
    $source=[Drawing.Bitmap]::new($sourcePath)
    $atlas=[Drawing.Bitmap]::new(($spec.columns*512),($spec.rows*512),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics=[Drawing.Graphics]::FromImage($atlas)
    $graphics.Clear([Drawing.Color]::Transparent)
    $graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
    $cells=@()
    $destination=Join-Path $ui $spec.file
    $meta=[IO.File]::ReadAllText("$destination.meta")
    $spriteNames=@([regex]::Matches($meta,'(?m)^      name: ([^\r\n]+)') | ForEach-Object {$_.Groups[1].Value})
    if($spriteNames.Count -ne $spec.count) {throw "Unexpected sprite count for $kind"}
    try {
        for($i=0;$i -lt $spec.count;$i++) {
            $cell=[PastelCardPacking]::Cell($source,$i,$spec.columns,$spec.rows,$spec.square)
            try {
                $stats=[PastelCardPacking]::Inspect($cell)
                if($stats[0] -lt 30 -or $stats[1] -lt 30 -or ($stats[0]+$stats[2]) -gt 482 -or ($stats[1]+$stats[3]) -gt 482) {throw "Cell padding failed: $kind $i"}
                $column=$i%$spec.columns;$row=[int][Math]::Floor($i/$spec.columns)
                $graphics.DrawImageUnscaled($cell,$column*512,$row*512)
                $cells+=@{name=$spriteNames[$i];bounds=$stats[0..3];cellSize=@(512,512)}
            } finally {$cell.Dispose()}
        }
        $graphics.Dispose()
        $stage=Join-Path $PSScriptRoot ("packed-$kind.png")
        $atlas.Save($stage,[Drawing.Imaging.ImageFormat]::Png)
    } finally {$source.Dispose();$atlas.Dispose()}
    $prepared+=@{stage=$stage;destination=$destination}
    $report+=@{kind=$kind;file=$spec.file;size=@(($spec.columns*512),($spec.rows*512));sprites=$cells;metadataUnchanged=$true;sha256=(Get-FileHash -LiteralPath $stage).Hash}
}
# Check every saved PNG and its transparent cell boundaries before replacing originals.
foreach($entry in $prepared) {
    $bitmap=[Drawing.Bitmap]::new($entry.stage)
    try {
        for($y=0;$y -lt $bitmap.Height;$y++) {for($x=0;$x -lt $bitmap.Width;$x++) {
            if(($x%512 -eq 0 -or $x%512 -eq 511 -or $y%512 -eq 0 -or $y%512 -eq 511) -and $bitmap.GetPixel($x,$y).A -ne 0) {throw 'Nontransparent cell boundary.'}
        }}
    } finally {$bitmap.Dispose()}
}
foreach($entry in $manifest) {if((Get-FileHash -LiteralPath (Join-Path $ui $entry.name)).Hash -ne $entry.sha256) {throw "Asset edited while packing: $($entry.name)"}}
foreach($entry in $prepared) {Copy-Item -LiteralPath $entry.stage -Destination $entry.destination}
foreach($entry in $manifest | Where-Object {$_.name.EndsWith('.meta')}) {if((Get-FileHash -LiteralPath (Join-Path $ui $entry.name)).Hash -ne $entry.sha256) {throw "Metadata unexpectedly changed: $($entry.name)"}}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report | ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
$report | ForEach-Object {[PSCustomObject]@{kind=$_.kind;file=$_.file;size=($_.size -join 'x');count=$_.sprites.Count;metadataUnchanged=$_.metadataUnchanged}} | ConvertTo-Json
