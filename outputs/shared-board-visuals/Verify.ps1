$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$common=Join-Path $repo 'Assets/4. Asset/0. Common'
$manifest=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$report=[Collections.Generic.List[string]]::new()
function Check([bool]$condition,[string]$message){if(!$condition){throw $message};$report.Add('PASS: '+$message)}
function Block([string]$text,[long]$id){return [regex]::Match($text,'(?ms)^--- !u!\d+ &'+$id+'\r?\n.*?(?=^--- !u!|\z)').Value}
function PathText([string]$text){return [regex]::Match($text,'(?ms)^  m_pathGridPositions:\r?\n(.*?)(?=^  \w)').Groups[1].Value.Replace("`r",'')}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.IO;
public static class SharedImageCheck {
 public static string Check(string oldPath,string newPath){
  using(var old=new Bitmap(oldPath))using(var now=new Bitmap(newPath)){
   if(old.Size!=now.Size)throw new Exception("Dimensions changed");
   int min=255,max=0,l=now.Width,t=now.Height,r=-1,b=-1,edge=0;
   bool frame=Path.GetFileName(newPath).Contains("Decoration");
   int requiredMin=frame?150:200;
   int referenceArea=0,actualArea=0;
   for(int y=0;y<now.Height;y++)for(int x=0;x<now.Width;x++){
    var c=now.GetPixel(x,y);var previous=old.GetPixel(x,y);
    bool core=previous.A>0;
    for(int dy=-1;dy<=1&&core;dy++)for(int dx=-1;dx<=1;dx++){
     int xx=x+dx,yy=y+dy;if(xx<0||xx>=old.Width||yy<0||yy>=old.Height||old.GetPixel(xx,yy).A==0){core=false;break;}
    }
    if(!frame&&c.A!=(core?255:previous.A))throw new Exception("Expected solid background interior and original antialiased boundary");
    if((core?255:previous.A)>=128)referenceArea++;
    if(c.A>=128)actualArea++;
    if(c.A==0)continue;
    if(c.R!=c.G||c.R!=c.B||c.R<requiredMin||c.R>255)throw new Exception("Unexpected neutral RGB range");
    min=Math.Min(min,c.R);max=Math.Max(max,c.R);l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
    if(c.A!=255)edge++;
   }
   if(min!=requiredMin||max!=255)throw new Exception("Incorrect grayscale extrema");
   if(frame&&(actualArea>referenceArea*1.01||actualArea<referenceArea*.97))throw new Exception("Frame area no longer matches original proportions");
   if(r-l!=b-t||l!=t||now.Width-1-r!=now.Height-1-b)throw new Exception("Shared silhouette is not square");
   return Path.GetFileName(newPath)+": RGB "+requiredMin+"..255, "+(frame?"matched original-width smooth contours":"solid interior/original antialiased boundary")+", square bounds "+l+","+t+","+(r-l+1)+","+(b-t+1)+", partial-alpha pixels="+edge;
  }
 }
 public static long Pixels(string path){using(var b=new Bitmap(path))return (long)b.Width*b.Height;}
}
'@
foreach($kind in @('Background','Decoration')){
 $original=Join-Path $PSScriptRoot "before/Assets/4. Asset/4. Enemy/Enemy_${kind}.png"
 $actual=Join-Path $common "Board_${kind}.png"
 $report.Add('PASS: '+[SharedImageCheck]::Check($original,$actual))
 $meta=[IO.File]::ReadAllText($actual+'.meta')
 Check ($meta.Contains('  spriteMode: 1') -and $meta.Contains('  textureType: 8') -and $meta.Contains('  spritePixelsToUnits: 384')) "$kind uses one full-rect sprite at 384 PPU"
}
$settingsPath=Join-Path $repo 'Assets/Resources/SharedBoardVisuals.asset'
$settings=[IO.File]::ReadAllText($settingsPath)
Check (([regex]::Matches($settings,'(?m)^  - realm:')).Count -eq 10) 'All ten stage color entries present'
Check (([regex]::Matches($settings,'(?m)^  - tier:')).Count -eq 6) 'All six tower tier color entries present'
Check (([regex]::Matches($settings,'(?m)^  - state:')).Count -eq 6) 'All six enemy state defaults present'
Check (([regex]::Matches($settings,'(?m)^  - appearance:')).Count -eq 3) 'All three enemy appearances available for optional overrides'
$metaGuids=@{}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'Assets') -Filter '*.meta' -File -Recurse){
 $id=[regex]::Match([IO.File]::ReadAllText($file.FullName),'(?m)^guid: (\w+)').Groups[1].Value
 if($id){$metaGuids[$id]=$file.FullName}
}
$consumers=@($settingsPath)+@(Get-ChildItem -LiteralPath $common -File | Where-Object {$_.Extension -in @('.asset','.mat')} | ForEach-Object {$_.FullName})
foreach($file in $consumers){foreach($match in [regex]::Matches([IO.File]::ReadAllText($file),'guid: ([a-f0-9]{32})')){
 $id=$match.Groups[1].Value;if($id.StartsWith('0000000000000000')){continue};Check ($metaGuids.ContainsKey($id)) ('Resolved common/config dependency '+$id)
}}
$active=@(Get-ChildItem -LiteralPath (Join-Path $repo 'Assets') -File -Recurse | Where-Object {$_.Extension -in @('.asset','.prefab','.unity','.mat','.cs') -and $_.FullName -notmatch '[\\/]_Recovery[\\/]'})
$combined=($active | ForEach-Object {[IO.File]::ReadAllText($_.FullName)}) -join "`n"
foreach($path in $manifest.retired){
 $retired=Join-Path $PSScriptRoot ('retired/'+$path.Substring($repo.Length+1))
 $id=[regex]::Match([IO.File]::ReadAllText($retired+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value
 Check (!$combined.Contains($id)) ('No active references to retired '+[IO.Path]::GetFileName($path))
}
$scene=[IO.File]::ReadAllText((Join-Path $repo 'Assets/1. Scenes/3. Stage1.unity'))
$oldScene=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'before/Assets/1. Scenes/3. Stage1.unity'))
Check ((PathText $scene) -eq (PathText $oldScene)) 'Authored path indices and coordinates retained exactly'
$ids=@([regex]::Matches($scene,'(?m)^--- !u!\d+ &(-?\d+)') | ForEach-Object {$_.Groups[1].Value})
Check (($ids | Select-Object -Unique).Count -eq $ids.Count) 'Scene has no duplicate object IDs'
foreach($id in @(1535280681,2102427781,1768091552,561276725)){
 $before=Block $oldScene $id;$now=Block $scene $id
 $oldCells=@([regex]::Matches($before,'(?m)^  - first: (\{[^}]+\})') | ForEach-Object {$_.Groups[1].Value})
 $nowCells=@([regex]::Matches($now,'(?m)^  - first: (\{[^}]+\})') | ForEach-Object {$_.Groups[1].Value})
 Check ((($oldCells | Sort-Object) -join '|') -eq (($nowCells | Sort-Object) -join '|')) ('Tilemap '+$id+' occupied cells unchanged')
}
$pathMatches=[regex]::Matches((PathText $scene),'\{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}')
$arrow=Block $scene 1900009003
$arrows=[regex]::Matches($arrow,'(?ms)  - first: \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}.*?m_TileMatrixIndex: (\d+)')
Check ($arrows.Count -eq $pathMatches.Count-1) 'One shared arrow per path cell except final visit'
$byCell=@{}
foreach($a in $arrows){$byCell[($a.Groups[1].Value+','+$a.Groups[2].Value+','+$a.Groups[3].Value)]=$a}
$matrixText=[regex]::Match($arrow,'(?ms)^  m_TileMatrixArray:.*?(?=^  m_TileColorArray:)').Value
$matrices=[regex]::Matches($matrixText,'(?ms)^  - m_RefCount:.*?(?=^  - m_RefCount:|\z)')
for($i=0;$i -lt $pathMatches.Count-1;$i++){
 $p=$pathMatches[$i];$next=$pathMatches[$i+1]
 $key=$p.Groups[1].Value+','+$p.Groups[2].Value+','+$p.Groups[3].Value
 $a=$byCell[$key]
 if($null -eq $a){throw ('Missing arrow at '+$key)}
 $dx=[int]$next.Groups[1].Value-[int]$p.Groups[1].Value;$dy=[int]$next.Groups[2].Value-[int]$p.Groups[2].Value
 $matrix=$matrices[[int]$a.Groups[4].Value].Value
 $actualX=[double]::Parse([regex]::Match($matrix,'(?m)^      e00: ([^\r\n]+)').Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
 $actualY=[double]::Parse([regex]::Match($matrix,'(?m)^      e10: ([^\r\n]+)').Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
 $length=[math]::Sqrt($dx*$dx+$dy*$dy)
 Check ([math]::Abs($actualX-$dx/$length) -lt 0.0001 -and [math]::Abs($actualY-$dy/$length) -lt 0.0001) ('Path arrow '+$i+' points toward next authored cell')
}
$towerPath=Join-Path $repo 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
$tower=[IO.File]::ReadAllText($towerPath);$oldTower=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'before/Assets/2. Prefab/2. Tower/TowerBase.prefab'))
Check (([regex]::Matches($tower,'(?m)^  m_LocalScale: .*').Value -join '|') -eq ([regex]::Matches($oldTower,'(?m)^  m_LocalScale: .*').Value -join '|')) 'All tower transform scales retained'
Check ($tower.Contains('guid: '+$manifest.settingsGuid) -and !$tower.Contains('m_backgroundSprites:') -and !$tower.Contains('m_decorationSprites:')) 'Tower prefab uses shared palette, with colored sprite arrays removed'
foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'Assets/2. Prefab/3. Enemy') -Filter '*.prefab' -Recurse){
 $text=[IO.File]::ReadAllText($file.FullName)
 if($text.Contains('m_appearance:')){Check ($text.Contains('guid: '+$manifest.settingsGuid)) ('Explicit shared palette reference on '+$file.Name)}
}
$mask=$metaGuids['f6dd47a2f3ee40e0afa808a7900031b1'] -replace '\.meta$',''
Check ((Get-FileHash -LiteralPath $mask).Hash -eq (Get-FileHash -LiteralPath (Join-Path $repo 'outputs/enemy-color-mask-fix/Enemy_Valkyrie_EquipmentMask.png')).Hash) 'Corrected Valkyrie equipment mask retained byte-for-byte'
$oldPixels=0L
foreach($path in $manifest.retired){if($path.EndsWith('.png')){$oldPixels+=[SharedImageCheck]::Pixels((Join-Path $PSScriptRoot ('retired/'+$path.Substring($repo.Length+1))))}}
$oldPixels+=2L*512*512
$newPixels=2L*512*512+256*256
$report.Add(('INFO: replaced set RGBA32 pixel payload estimate: '+($oldPixels*4/1MB).ToString('0.00')+' MiB -> '+($newPixels*4/1MB).ToString('0.00')+' MiB. Not a measured GPU-memory claim; excludes mipmaps/compression and does not assume every old theme was loaded.'))
$report.Add('Unity was not launched. Recovery snapshots were intentionally excluded from active-reference migration and validation.')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'verification.txt'),$report,[Text.UTF8Encoding]::new($false))
$report | Select-Object -Last 12
