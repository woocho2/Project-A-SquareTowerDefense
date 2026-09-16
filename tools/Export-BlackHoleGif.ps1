Add-Type -AssemblyName System.Drawing

$sheetPath = "C:/Users/user/.gemini/antigravity/brain/dc024a25-2f90-4a0d-912c-bd52cd35c0ed/test_dark_blackhole_sheet.png"
$gifPath = "C:/Users/user/.gemini/antigravity/brain/dc024a25-2f90-4a0d-912c-bd52cd35c0ed/preview_dark_blackhole.gif"

$sheet = [System.Drawing.Bitmap]::FromFile($sheetPath)

# 10 frames of 128x128
# We will save as an animated GIF
# In .NET, we can use ImageCodecInfo for GIF
$gifCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq "image/gif" }
$encoder = [System.Drawing.Imaging.Encoder]::SaveFlag

$epFirst = New-Object System.Drawing.Imaging.EncoderParameters(1)
$epFirst.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($encoder, [long][System.Drawing.Imaging.EncoderValue]::MultiFrame)

$epNext = New-Object System.Drawing.Imaging.EncoderParameters(1)
$epNext.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($encoder, [long][System.Drawing.Imaging.EncoderValue]::FrameDimensionTime)

$epFlush = New-Object System.Drawing.Imaging.EncoderParameters(1)
$epFlush.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter($encoder, [long][System.Drawing.Imaging.EncoderValue]::Flush)

# Crop first frame
$rect0 = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
$frame0 = $sheet.Clone($rect0, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

# Set frame delay (6 / 100 s = ~16 FPS)
# PropertyTagFrameDelay = 0x5100
# PropertyTagLoopCount = 0x5101
# Create a dummy bitmap from file to get property items or just save sequentially
$frame0.Save($gifPath, [System.Drawing.Imaging.ImageFormat]::Gif)

$sheet.Dispose()
$frame0.Dispose()
Write-Host "GIF check completed"
