$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$nativePath=Join-Path $PSScriptRoot 'native-edit.png'
$generated=[Drawing.Bitmap]::new($nativePath)
$normalized=[Drawing.Bitmap]::new(256,256,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    $graphics=[Drawing.Graphics]::FromImage($normalized)
    try {
        $graphics.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.DrawImage($generated,[Drawing.Rectangle]::new(0,0,256,256))
    } finally {$graphics.Dispose()}
    $before=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'before.png'))
    $portrait=$before.Clone([Drawing.Rectangle]::new(0,0,256,256),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        # Composite only the native-generated open-eye area onto the original portrait.
        # This retains the original hat, hair, clothing, scale, alpha and all other pixels.
        $eyeRect=[Drawing.Rectangle]::new(86,151,30,25)
        for($y=$eyeRect.Top;$y -lt $eyeRect.Bottom;$y++) {
            for($x=$eyeRect.Left;$x -lt $eyeRect.Right;$x++) {
                $portrait.SetPixel($x,$y,$normalized.GetPixel($x,$y))
            }
        }
        $portrait.Save((Join-Path $PSScriptRoot 'after.png'),[Drawing.Imaging.ImageFormat]::Png)
        $atlasBefore=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'atlas-before.png'))
        $atlas=$atlasBefore.Clone([Drawing.Rectangle]::new(0,0,512,512),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for($y=0;$y -lt 256;$y++) {
                for($x=0;$x -lt 256;$x++) {$atlas.SetPixel(256+$x,$y,$portrait.GetPixel($x,$y))}
            }
            $atlas.Save((Join-Path $PSScriptRoot 'atlas-after.png'),[Drawing.Imaging.ImageFormat]::Png)
            for($y=0;$y -lt 512;$y++) {
                for($x=0;$x -lt 512;$x++) {
                    if(($x -lt 342 -or $x -ge 372 -or $y -lt 151 -or $y -ge 176) -and $atlas.GetPixel($x,$y).ToArgb() -ne $atlasBefore.GetPixel($x,$y).ToArgb()) {
                        throw 'A pixel outside the requested eye edit changed.'
                    }
                }
            }
        } finally {$atlas.Dispose();$atlasBefore.Dispose()}
    } finally {$portrait.Dispose();$before.Dispose()}
} finally {$normalized.Dispose();$generated.Dispose()}
Write-Output 'Native eye edit composited. All pixels outside the eye area retained exactly.'
$finalAtlas=Join-Path $projectRoot 'Assets/4. Asset/2. Tower/Portraits_SD_Front_v2/Tower_Sword_Front_Atlas_v2.png'
$metadataBefore=(Get-FileHash -LiteralPath ($finalAtlas+'.meta') -Algorithm SHA256).Hash
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'atlas-after.png') -Destination $finalAtlas -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'atlas-after.png') -Destination (Join-Path $projectRoot 'outputs/tower-front-portraits-v2/packed/01_Sword.png') -Force
if((Get-FileHash -LiteralPath ($finalAtlas+'.meta') -Algorithm SHA256).Hash -ne $metadataBefore) {throw 'Sprite metadata changed.'}
$previewPath=Join-Path $projectRoot 'outputs/tower-front-portraits-v2/normal-preview.png'
$previewOriginal=[Drawing.Bitmap]::new($previewPath)
$preview=$previewOriginal.Clone([Drawing.Rectangle]::new(0,0,$previewOriginal.Width,$previewOriginal.Height),[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$previewOriginal.Dispose()
$updated=[Drawing.Bitmap]::new((Join-Path $PSScriptRoot 'after.png'))
try {
    $graphics=[Drawing.Graphics]::FromImage($preview)
    try {$graphics.DrawImageUnscaled($updated,376,30)} finally {$graphics.Dispose()}
    $preview.Save((Join-Path $PSScriptRoot 'overview-updated.png'),[Drawing.Imaging.ImageFormat]::Png)
} finally {$preview.Dispose();$updated.Dispose()}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'overview-updated.png') -Destination $previewPath -Force
Write-Output 'Updated Sword atlas, staging atlas and overview. Sprite GUIDs and slices retained.'
