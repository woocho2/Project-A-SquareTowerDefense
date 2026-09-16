Add-Type -AssemblyName System.Drawing

$bmp = New-Object System.Drawing.Bitmap("C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\.user_uploaded\media_1789519807848.png")

# Let's find for each Y the minX and maxX of the gold pixels
$rows = @()
for ($y = 52; $y -le 398; $y++) {
    $rowMin = 999; $rowMax = -1
    for ($x = 64; $x -le 446; $x++) {
        $c = $bmp.GetPixel($x, $y)
        if (($c.R - $c.B -gt 30) -and ($c.G - $c.B -gt 15) -and ($c.B -lt 160)) {
            if ($x -lt $rowMin) { $rowMin = $x }
            if ($x -gt $rowMax) { $rowMax = $x }
        }
    }
    if ($rowMax -ge 0) {
        $rows += [pscustomobject]@{ Y = $y; MinX = $rowMin; MaxX = $rowMax; MidX = ($rowMin + $rowMax)/2.0; Width = ($rowMax - $rowMin) }
    }
}

# Print key rows (every 25 rows, and where sudden width or midX jumps occur)
Write-Output "Total rows detected: $($rows.Count)"
for ($i = 0; $i -lt $rows.Count; $i += 15) {
    $r = $rows[$i]
    Write-Output "Y=$($r.Y): MinX=$($r.MinX), MaxX=$($r.MaxX), MidX=$($r.MidX), W=$($r.Width)"
}
$bmp.Dispose()
