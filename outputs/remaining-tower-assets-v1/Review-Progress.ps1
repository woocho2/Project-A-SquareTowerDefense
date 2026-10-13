$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskJobs=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'prompts.json') -Raw | ConvertFrom-Json
$taskAvailable=@($taskJobs | Where-Object {Test-Path -LiteralPath (Join-Path $PSScriptRoot ('source/Tower_'+$_.group+'_'+$_.role+'_'+$_.name+'.png'))})
$taskRows=[int][Math]::Ceiling($taskAvailable.Count/4.0)
$taskImage=New-Object Drawing.Bitmap(896,([Math]::Max(1,$taskRows)*246))
$taskGraphics=[Drawing.Graphics]::FromImage($taskImage)
$taskGraphics.Clear([Drawing.Color]::FromArgb(255,29,33,44))
$taskGraphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskFont=New-Object Drawing.Font('Malgun Gothic',12,[Drawing.FontStyle]::Bold)
$taskFormat=New-Object Drawing.StringFormat
$taskFormat.Alignment=[Drawing.StringAlignment]::Center
for($taskI=0;$taskI -lt $taskAvailable.Count;$taskI++){
    $taskJob=$taskAvailable[$taskI]
    $taskFile=Join-Path $PSScriptRoot ('source/Tower_'+$taskJob.group+'_'+$taskJob.role+'_'+$taskJob.name+'.png')
    $taskSource=[Drawing.Bitmap]::FromFile($taskFile)
    $taskX=($taskI%4)*224;$taskY=[int][Math]::Floor($taskI/4)*246
    $taskGraphics.DrawString(($taskJob.groupKo+' · '+$taskJob.nameKo),$taskFont,[Drawing.Brushes]::Ivory,[Drawing.RectangleF]::new($taskX,$taskY,224,30),$taskFormat)
    $taskGraphics.DrawImage($taskSource,[Drawing.Rectangle]::new(($taskX+12),($taskY+32),200,200))
    $taskSource.Dispose()
}
$taskImage.Save((Join-Path $PSScriptRoot 'review-progress.png'),[Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose();$taskImage.Dispose();$taskFont.Dispose();$taskFormat.Dispose()
Write-Output ('Saved review of '+$taskAvailable.Count+' generated portraits.')
