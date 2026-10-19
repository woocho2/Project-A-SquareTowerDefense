param([string]$GeneratedSource)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$ui=Join-Path $root 'Assets/4. Asset/5. UI'
$destination=Join-Path $ui 'UI_Action_Atlas.png'
if((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath "$destination.meta")) { throw 'New action atlas already exists.' }
Add-Type -AssemblyName System.Drawing
$helper=Get-Content -LiteralPath (Join-Path $root 'outputs/color-upgrade-symbols/Pack.ps1') -Raw
$match=[regex]::Match($helper,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@")
if(-not $match.Success) { throw 'Missing packing helper.' }
$code=$match.Groups[1].Value.Replace('UpgradeSymbolPacking','ActionIconPacking').Replace('int left=(index%2)*source.Width/2, top=(index/2)*source.Height/2;','int left=(index%3)*source.Width/3, top=(index/3)*source.Height/3;').Replace('int w=source.Width/2,h=source.Height/2;','int w=source.Width/3,h=source.Height/3;')
$code=$code.Replace('var cell=new Bitmap(512,512,PixelFormat.Format32bppArgb);',@'
int dw=448,dh=448;
            if(index==0||index==1||index==6||index==7) {
                int sw=x1-x0+1,sh=y1-y0+1;
                dw=(int)Math.Round(sw*448.0/Math.Max(sw,sh));
                dh=(int)Math.Round(sh*448.0/Math.Max(sw,sh));
            }
            var cell=new Bitmap(512,512,PixelFormat.Format32bppArgb);
'@).Replace('new Rectangle(32,32,448,448)','new Rectangle((512-dw)/2,(512-dh)/2,dw,dh)')
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$sourcePath=Join-Path $PSScriptRoot 'source.png'
Copy-Item -LiteralPath $GeneratedSource -Destination $sourcePath
$names=@('Symbol_GameSpeed','Symbol_Menu','ColorUpgrade_Splash','ColorUpgrade_Single','ColorUpgrade_Buff','ColorUpgrade_Debuff','Symbol_CreateTower','Symbol_TierUpgrade','Symbol_UpgradePlus')
$source=[Drawing.Bitmap]::new($sourcePath)
$atlas=[Drawing.Bitmap]::new(1536,1536,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics=[Drawing.Graphics]::FromImage($atlas)
$graphics.Clear([Drawing.Color]::Transparent)
$graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$records=@()
try {
    for($i=0;$i -lt 9;$i++) {
        $cell=[ActionIconPacking]::Cell($source,$i)
        try {
            $stats=[ActionIconPacking]::Inspect($cell)
            if($stats[0] -lt 30 -or $stats[1] -lt 30 -or ($stats[0]+$stats[2]) -gt 482 -or ($stats[1]+$stats[3]) -gt 482) { throw "Invalid padding: $($names[$i])" }
            $column=$i%3;$row=[int][Math]::Floor($i/3)
            $graphics.DrawImageUnscaled($cell,$column*512,$row*512)
            $records+=@{name=$names[$i];fileID=21300001+$i;spriteID=[Guid]::NewGuid().ToString('N');x=$column*512;y=(2-$row)*512;width=512;height=512;bounds=$stats[0..3]}
        } finally { $cell.Dispose() }
    }
    $graphics.Dispose();$atlas.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
} finally { $source.Dispose();$atlas.Dispose() }
$guid=[Guid]::NewGuid().ToString('N')
$template=Get-Content -LiteralPath (Join-Path $ui 'Tower_Card_Attack_Atlas.png.meta') -Raw
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
$table=($records | ForEach-Object { "      $($_.name): $($_.fileID)" }) -join "`n"
$metadata=$header+@"
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
  userData: Nine shared pastel cartoon UI actions - 512px cells and centered pivots
  assetBundleName:
  assetBundleVariant:
"@
$encoding=[Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText("$destination.meta",$metadata+"`n",$encoding)
$report=@{atlas=$destination;guid=$guid;sha256=(Get-FileHash -LiteralPath $destination).Hash;size=@(1536,1536);cellSize=@(512,512);sprites=$records;generationMode='built-in imagegen';source=$sourcePath}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'atlas.json'),($report | ConvertTo-Json -Depth 7),$encoding)
$report | ConvertTo-Json -Depth 7
