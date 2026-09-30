param(
    [string]$SourcePath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets/4. Asset/1. BackGround/Tile_TowerSpawn_Atlas.png'),
    [string]$OutputPath = (Join-Path (Split-Path $PSScriptRoot -Parent) 'output/ui/Tile_TowerSpawn_Atlas_10Realm_preview.png')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Each existing cell uses the same seven-color pixel template. The roles are
# ordered by pixel frequency: face, rim, rim shadow, deep face shadow,
# face highlight, rim highlight, and face mid-shadow.
$palettes = [ordered]@{
    Asgard      = @('#F6F7F4', '#DBAE43', '#9A6B18', '#CCD7DB', '#FFFFFF', '#FFF1A4', '#E5ECEA')
    Alfheim     = @('#B99363', '#6F9B48', '#365A2D', '#705235', '#DEBC89', '#B7D47A', '#9D784F')
    Vanaheim    = @('#3B885A', '#B4D55C', '#668734', '#20593B', '#76B985', '#D9EFA0', '#31754D')
    Midgard     = @('#176A9C', '#43BCD3', '#1675A9', '#0C3D68', '#55B8D0', '#B5F3EA', '#145A84')
    Jotunheim   = @('#D8E4EB', '#31547F', '#17314F', '#9BAFC0', '#F7FBFF', '#789FC5', '#C2D2DC')
    Nidavellir  = @('#41464A', '#BE9957', '#6D542C', '#24282D', '#6E7678', '#E8CD91', '#34393E')
    Niflheim    = @('#F6FBFC', '#9DDAE8', '#5D91A7', '#CEE7EC', '#FFFFFF', '#E3FAFF', '#E4F3F6')
    Muspelheim  = @('#743C33', '#D98D52', '#8C4B2B', '#41241E', '#A05A4C', '#F3BE86', '#5B3029')
    Hel         = @('#202B2B', '#5DB7AC', '#286F68', '#10191A', '#3A5553', '#B4E9DC', '#192323')
    Ragnarok    = @('#C9C6BE', '#B69A67', '#6D614B', '#929590', '#EDEAE2', '#E0CEAC', '#B2B2AA')
}

function Convert-HexColor([string]$hex) {
    return [System.Drawing.Color]::FromArgb(
        255,
        [Convert]::ToInt32($hex.Substring(1, 2), 16),
        [Convert]::ToInt32($hex.Substring(3, 2), 16),
        [Convert]::ToInt32($hex.Substring(5, 2), 16)
    )
}

$source = [System.Drawing.Bitmap]::new($SourcePath)
$result = [System.Drawing.Bitmap]::new(512, 384, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
try {
    if ($source.Width -ne 384 -or $source.Height -ne 384) {
        throw "Expected a 384x384 source atlas, got $($source.Width)x$($source.Height)."
    }

    $names = @($palettes.Keys)
    for ($tile = 0; $tile -lt $names.Count; $tile++) {
        $sourceTile = if ($tile -eq 9) { 0 } else { $tile }
        $sourceX = ($sourceTile % 3) * 128
        $sourceY = [int][Math]::Floor($sourceTile / 3) * 128
        $destX = if ($tile -eq 9) { 384 } else { $sourceX }
        $destY = $sourceY

        $counts = @{}
        $transparentCount = 0
        for ($y = 0; $y -lt 128; $y++) {
            for ($x = 0; $x -lt 128; $x++) {
                $pixel = $source.GetPixel($sourceX + $x, $sourceY + $y)
                if ($pixel.A -eq 0) {
                    $transparentCount++
                }
                elseif ($pixel.A -eq 255) {
                    $key = $pixel.ToArgb()
                    if (-not $counts.ContainsKey($key)) { $counts[$key] = 0 }
                    $counts[$key]++
                }
                else {
                    throw "Unexpected semitransparent pixel in $($names[$tile])."
                }
            }
        }

        $orderedColors = @($counts.GetEnumerator() | Sort-Object -Property Value -Descending)
        $expectedCounts = @(8908, 2219, 1944, 934, 930, 819, 518)
        if ($transparentCount -ne 112 -or $orderedColors.Count -ne 7) {
            throw "Unexpected source palette or alpha mask in $($names[$tile])."
        }
        for ($role = 0; $role -lt 7; $role++) {
            if ($orderedColors[$role].Value -ne $expectedCounts[$role]) {
                throw "Pixel geometry changed in $($names[$tile]), role $role."
            }
        }

        $replacement = @{}
        for ($role = 0; $role -lt 7; $role++) {
            $replacement[$orderedColors[$role].Key] = Convert-HexColor $palettes[$names[$tile]][$role]
        }
        for ($y = 0; $y -lt 128; $y++) {
            for ($x = 0; $x -lt 128; $x++) {
                $pixel = $source.GetPixel($sourceX + $x, $sourceY + $y)
                if ($pixel.A -ne 0) {
                    $result.SetPixel($destX + $x, $destY + $y, $replacement[$pixel.ToArgb()])
                }
            }
        }
        Write-Output "$($names[$tile]): palette applied, alpha and geometry preserved"
    }

    $outputDirectory = Split-Path $OutputPath -Parent
    if (-not (Test-Path -LiteralPath $outputDirectory)) {
        New-Item -ItemType Directory -Path $outputDirectory | Out-Null
    }
    $result.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "Saved $OutputPath"
}
finally {
    $result.Dispose()
    $source.Dispose()
}
