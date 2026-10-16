param([ValidateSet('All','Background','Decoration')][string]$Kind='All')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
# Rebuild pixels from the preserved originals, without rerunning the one-time reference migration.
$source=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Migrate.ps1'))
$code=[regex]::Match($source,"(?s)Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'\r?\n(.*?)\r?\n'@").Groups[1].Value
if(!$code){throw 'Image authoring code not found'}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition $code
foreach($imageKind in @('Background','Decoration')){
 if($Kind -ne 'All' -and $Kind -ne $imageKind){continue}
 $original=Join-Path $PSScriptRoot "before/Assets/4. Asset/4. Enemy/Enemy_${imageKind}.png"
 $destination=Join-Path $repo "Assets/4. Asset/0. Common/Board_${imageKind}.png"
 [CommonBoardArt]::Normalize($original,$destination,($imageKind -eq 'Decoration'))
 if($imageKind -eq 'Decoration'){
  $refine=Join-Path $PSScriptRoot '../shared-decoration-cleanup/Match-OriginalCurves.ps1'
  if(Test-Path -LiteralPath $refine){
   & powershell -NoProfile -ExecutionPolicy Bypass -File $refine
   if($LASTEXITCODE -ne 0){throw 'Decoration shading refinement failed'}
   Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../shared-decoration-cleanup/Board_Decoration_Matched.png') -Destination $destination -Force
  }
 }
}
Write-Output 'Rebuilt selected common images: background 200..255, decoration 150..255; alpha policy preserved.'
