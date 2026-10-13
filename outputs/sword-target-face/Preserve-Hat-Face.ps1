$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskOriginalPath=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v3.png'
$taskEditedPath=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Satgat_Front_v1.png'
Add-Type -AssemblyName System.Drawing
$taskOriginal=New-Object Drawing.Bitmap($taskOriginalPath)
$taskStream=New-Object IO.MemoryStream(,[IO.File]::ReadAllBytes($taskEditedPath))
$taskEdited=New-Object Drawing.Bitmap($taskStream)
for($y=130;$y -lt 256;$y++){for($x=0;$x -lt 256;$x++){$taskEdited.SetPixel($x,$y,$taskOriginal.GetPixel($x,$y))}}
$taskEdited.Save($taskEditedPath,[Drawing.Imaging.ImageFormat]::Png)
$taskOriginal.Dispose();$taskEdited.Dispose();$taskStream.Dispose()
Write-Output 'Preserved all original face/scar/expression pixels and lower hair from row 130 downward.'
