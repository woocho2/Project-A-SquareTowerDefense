param([string]$GeneratedSource)
$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$destination=Join-Path $root 'Assets/4. Asset/5. UI/UI_Upgrade_Plus.png'
if((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath "$destination.meta")) { throw 'Destination already exists.' }
Add-Type -AssemblyName System.Drawing
# Reuse the atlas packing helper with a single full-image source cell.
$packing=Get-Content -LiteralPath (Join-Path $root 'outputs/color-upgrade-symbols/Pack.ps1') -Raw
$match=[regex]::Match($packing,"(?s)-TypeDefinition @'\r?\n(.*?)\r?\n'@")
if(-not $match.Success) { throw 'Packing helper source missing.' }
$code=$match.Groups[1].Value.Replace('UpgradeSymbolPacking','UpgradePlusPacking').Replace('int left=(index%2)*source.Width/2, top=(index/2)*source.Height/2;','int left=0, top=0;').Replace('int w=source.Width/2,h=source.Height/2;','int w=source.Width,h=source.Height;')
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
$sourcePath=Join-Path $PSScriptRoot 'source.png'
Copy-Item -LiteralPath $GeneratedSource -Destination $sourcePath
$source=[Drawing.Bitmap]::new($sourcePath)
try {
    $cell=[UpgradePlusPacking]::Cell($source,0)
    try {
        $stats=[UpgradePlusPacking]::Inspect($cell)
        if($stats[0] -lt 30 -or $stats[1] -lt 30 -or ($stats[0]+$stats[2]) -gt 482 -or ($stats[1]+$stats[3]) -gt 482) { throw 'Padding validation failed.' }
        $cell.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
    } finally { $cell.Dispose() }
} finally { $source.Dispose() }
$template=Get-Content -LiteralPath (Join-Path $root 'Assets/4. Asset/5. UI/UI_Color_Upgrade_Atlas.png.meta') -Raw
$guid=[Guid]::NewGuid().ToString('N')
$header=(($template -split '  spriteSheet:',2)[0] -replace '(?m)^guid: .*$',"guid: $guid").Replace('  spriteMode: 2','  spriteMode: 1')
$meta=$header+@"
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    secondaryTextures: []
    nameFileIdTable: {}
  userData: Gold upgrade plus button - 512px sprite, 32px transparent padding
  assetBundleName:
  assetBundleVariant:
"@
$encoding=[Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText("$destination.meta",$meta+"`n",$encoding)
$report=@{asset=$destination;size=@(512,512);bounds=$stats[0..3];guid=$guid;sha256=(Get-FileHash -LiteralPath $destination).Hash;spriteMode='Single';pivot=@(0.5,0.5);pixelsPerUnit=100;generationMode='built-in imagegen'}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.json'),($report | ConvertTo-Json -Depth 4),$encoding)
$report | ConvertTo-Json -Depth 4
