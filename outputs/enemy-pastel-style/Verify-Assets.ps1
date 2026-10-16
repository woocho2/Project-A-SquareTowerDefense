$ErrorActionPreference = 'Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetRoot=Join-Path $repoRoot 'Assets/4. Asset/4. Enemy'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
public static class EnemyPastelVerify {
 public static string CheckPortrait(string originalPath,string actualPath,string maskPath,bool originalFixed) {
  using(var actual=new Bitmap(actualPath))using(var mask=new Bitmap(maskPath))
  using(var original=new Bitmap(originalPath)) {
   if(actual.Width!=512||actual.Height!=512||mask.Width!=512||mask.Height!=512)throw new Exception("Texture dimensions changed");
   int white=0;
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    var c=actual.GetPixel(x,y);var m=mask.GetPixel(x,y);
    if(m.R!=m.G||m.G!=m.B||(m.R!=0&&m.R!=255))throw new Exception("Nonbinary equipment mask");
    if(m.R==255) {
     white++;if(c.A==0)throw new Exception("Mask outside character alpha");
     if(c.R!=c.G||c.G!=c.B||c.R<188||c.R>244)throw new Exception("Expected soft grayscale equipment base");
    }
    if(originalFixed) {
     if(c.A!=original.GetPixel(x,y).A)throw new Exception("Boss character moved");
     if(m.R==0&&c.ToArgb()!=original.GetPixel(x,y).ToArgb())throw new Exception("Unmasked boss face/hair/metal changed");
    }
    if(!originalFixed&&x>=170&&x<=340&&y>=270&&y<=370&&m.R!=0)throw new Exception("Valkyrie face accidentally masked");
    if(!originalFixed&&x>=170&&x<=340&&y>=80&&y<=200&&m.R!=0)throw new Exception("Valkyrie upper hair accidentally masked");
   }
   if(white<5000)throw new Exception("Missing equipment mask");
   return "PASS: "+actualPath+" / valid neutral equipment, binary mask and protected face/hair; mask pixels="+white;
  }
 }
 public static string CheckLayer(string originalPath,string actualPath) {
  using(var original=new Bitmap(originalPath))using(var actual=new Bitmap(actualPath)) {
   if(actual.Width!=original.Width||actual.Height!=original.Height)throw new Exception("Common layer dimensions changed");
   for(int y=0;y<actual.Height;y++)for(int x=0;x<actual.Width;x++) {
    var c=actual.GetPixel(x,y);
    if(c.A!=original.GetPixel(x,y).A)throw new Exception("Shared outline alpha changed");
    if(c.A>0&&(c.R!=c.G||c.G!=c.B))throw new Exception("Common base is not grayscale");
   }
   return "PASS: "+actualPath+" / exact original alpha and neutral grayscale RGB";
  }
 }
}
'@
$report=[Collections.Generic.List[string]]::new()
foreach($name in @('Valkyrie','Brunhild','Loki')) {
    $portrait=Join-Path $assetRoot "Asgard/Enemy_${name}_Character.png"
    $mask=Join-Path $assetRoot "Asgard/Enemy_${name}_EquipmentMask.png"
    $original=Join-Path $PSScriptRoot "before/Asgard/Enemy_${name}_Character.png"
    $report.Add([EnemyPastelVerify]::CheckPortrait($original,$portrait,$mask,($name -ne 'Valkyrie')))
}
foreach($name in @('Enemy_Decoration.png','Enemy_Background.png')) {
    $report.Add([EnemyPastelVerify]::CheckLayer((Join-Path $PSScriptRoot "before/$name"),(Join-Path $assetRoot $name)))
}
foreach($oldMeta in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'before') -Filter '*.meta' -Recurse) {
    $relative=$oldMeta.FullName.Substring((Join-Path $PSScriptRoot 'before').Length+1)
    $actual=Join-Path $assetRoot $relative
    if((Get-FileHash -LiteralPath $actual).Hash -ne (Get-FileHash -LiteralPath $oldMeta.FullName).Hash) {throw ('Metadata changed: '+$relative)}
}
$states=@{}
foreach($mat in Get-ChildItem -LiteralPath (Join-Path $assetRoot 'Asgard') -Filter '*.mat') {
    $state=($mat.BaseName -split '_')[-1]
    $text=[IO.File]::ReadAllText($mat.FullName)
    $color=[regex]::Match($text,'(?m)^    - _EquipmentColor: (.*)').Groups[1].Value
    if($states.ContainsKey($state) -and $states[$state] -ne $color) {throw ('Inconsistent state palette: '+$state)}
    $states[$state]=$color
}
if($states.Count -ne 6) {throw 'Expected six common state colors'}
$report.Add('PASS: all enemy texture/material GUIDs and sprite metadata retained; six pastel colors consistent across 18 materials.')
$protected=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'protected-hashes.json') -Raw | ConvertFrom-Json
foreach($entry in $protected) {
    if((Get-FileHash -LiteralPath $entry.path).Hash -ne $entry.hash) {throw ('Protected tower/prefab/movement asset changed: '+$entry.path)}
}
$report.Add('PASS: all tower assets, enemy prefabs, cube mesh visual and movement script hashes unchanged. Unity was not launched.')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'verification.txt'),$report,[Text.UTF8Encoding]::new($false))
$report | Write-Output
