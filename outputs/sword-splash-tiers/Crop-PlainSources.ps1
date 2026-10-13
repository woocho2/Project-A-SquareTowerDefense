$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskImage=New-Object Drawing.Bitmap('C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-d42b0b1e-d533-4ef6-bb9c-a4601f2f0e18.png')
$taskEntries=@(@('Silver',2),@('Gold',3),@('Mithril',4),@('Diamond',5))
foreach($entry in $taskEntries){
    $index=[int]$entry[1];$col=$index%3;$row=[int][Math]::Floor($index/3)
    $left=[int][Math]::Round($col*$taskImage.Width/3);$right=[int][Math]::Round(($col+1)*$taskImage.Width/3)
    $top=[int][Math]::Round($row*$taskImage.Height/2);$bottom=[int][Math]::Round(($row+1)*$taskImage.Height/2)
    $part=$taskImage.Clone((New-Object Drawing.Rectangle($left,$top,($right-$left),($bottom-$top))),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $part.Save((Join-Path $PSScriptRoot ('Plain_'+$entry[0]+'.png')),[Drawing.Imaging.ImageFormat]::Png);$part.Dispose()
}
$taskImage.Dispose()
Write-Output 'Cropped four plain tier color references; original equipment masks supply final alpha.'
