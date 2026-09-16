Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap("C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\.user_uploaded\media_1789519807848.png")

# Find the tips and kinks
# The bolt is golden: R > 120, G > 90, B < 80, or saturation high, or darker gold than white fog
# Let's inspect points along the bolt
$minX = 999; $maxX = 0; $minY = 999; $maxY = 0

for ($y = 0; $y -lt $bmp.Height; $y += 2) {
    for ($x = 0; $x -lt $bmp.Width; $x += 2) {
        $c = $bmp.GetPixel($x, $y)
        # Background is very light (R>200, G>200, B>200)
        # The bolt is golden brown/amber: (R - B > 40) and (G - B > 25)
        if (($c.R - $c.B -gt 35) -and ($c.G - $c.B -gt 20) -and ($c.B -lt 150)) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

Write-Output "Gold bolt bounding box: X=[$minX, $maxX], Y=[$minY, $maxY]"
$bmp.Dispose()
