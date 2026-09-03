Add-Type -AssemblyName System.Drawing

$ErrorActionPreference = 'Stop'
$assetDirectory = 'Assets/4. DotAsset/2. Tower/Parts/Tier'
$sourcePath = Join-Path $assetDirectory 'Bronze_Panel_512_Refined_V2.png'
$outputPath = Join-Path $assetDirectory 'Bronze_Panel_512_Square_V3.png'

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $sourcePath))
$output = [System.Drawing.Bitmap]::new(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

try {
    for ($y = 0; $y -lt 512; $y++) {
        for ($x = 0; $x -lt 512; $x++) {
            $output.SetPixel($x, $y, $source.GetPixel($x, $y))
        }
    }

    # New 208×208 central panel with stepped, rounded pixel corners.
    for ($y = 112; $y -le 319; $y++) {
        if ($y -le 119 -or $y -ge 312) {
            $panelLeft = 160
            $panelRight = 351
        }
        else {
            $panelLeft = 152
            $panelRight = 359
        }

        # Replace the old too-wide white opening with a continuation of each inner side bevel.
        for ($x = 124; $x -le 387; $x++) {
            if ($x -ge $panelLeft -and $x -le $panelRight) {
                $output.SetPixel($x, $y, [System.Drawing.Color]::White)
            }
            elseif ($x -lt $panelLeft) {
                $output.SetPixel($x, $y, $source.GetPixel(120, $y))
            }
            else {
                $output.SetPixel($x, $y, $source.GetPixel(391, $y))
            }
        }

        # Inner outline gives the new wider side frame a clean, deliberate edge.
        for ($x = $panelLeft - 4; $x -lt $panelLeft; $x++) {
            $base = $output.GetPixel($x, $y)
            $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($base.A, [Math]::Round($base.R * 0.58), [Math]::Round($base.G * 0.58), [Math]::Round($base.B * 0.58)))
        }
        for ($x = $panelRight + 1; $x -le $panelRight + 4; $x++) {
            $base = $output.GetPixel($x, $y)
            $output.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($base.A, [Math]::Round($base.R * 0.58), [Math]::Round($base.G * 0.58), [Math]::Round($base.B * 0.58)))
        }
    }

    $output.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $source.Dispose()
    $output.Dispose()
}
