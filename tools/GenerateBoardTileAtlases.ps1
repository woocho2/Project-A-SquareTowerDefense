param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

# The nine 192px slices deliberately retain the original Asgard board geometry.
# Per-realm files are source images; PackBoardTileAtlas.ps1 produces the single
# runtime atlas and reconnects all RuleTiles after these sources are refreshed.
Add-Type -AssemblyName System.Drawing

$assetRoot = Join-Path $ProjectRoot 'Assets/4. Asset/1. BackGround'
$ruleRoot = Join-Path $assetRoot 'Tiles/BoardFrame'
$sourcePath = Join-Path $assetRoot 'Tile_BoardFrame_Atlas.png'
$sourceMetaPath = "$sourcePath.meta"
$rulePath = Join-Path $ruleRoot 'BoardFrame_RuleTile.asset'
$sourceGuid = 'b41f7a29e3014a5cb820f1883de4c71a'

function ColorFromHex([string]$hex) {
    return [System.Drawing.ColorTranslator]::FromHtml("#$hex")
}

function MixColor([System.Drawing.Color]$a, [System.Drawing.Color]$b, [double]$t, [int]$alpha) {
    $t = [Math]::Max(0, [Math]::Min(1, $t))
    return [System.Drawing.Color]::FromArgb(
        $alpha,
        [int][Math]::Round($a.R + ($b.R - $a.R) * $t),
        [int][Math]::Round($a.G + ($b.G - $a.G) * $t),
        [int][Math]::Round($a.B + ($b.B - $a.B) * $t))
}

$realms = @(
    @{ Name='Alfheim';    Guid='5918173407a34a53a121d6820d0b13ce'; RuleGuid='5811341551cd4df6bc58a432b4a7187b'; Paper='D4AA7B'; Shade='947252'; Accent='4E8F47'; Shine='B4D979' },
    @{ Name='Vanaheim';   Guid='839a8751d9ef426a99b45ecde55a3ba2'; RuleGuid='83e61bf4a9024eabbfb2d46a4c30c83a'; Paper='48A574'; Shade='276E58'; Accent='2B7954'; Shine='CAE878' },
    @{ Name='Ragnarok';   Guid='a6f61fb0c0b44c3fa4c9c4ebbd3d7fae'; RuleGuid='0e497cfa05fc4f01a3cfef229cc7ab78'; Paper='DDD8D2'; Shade='93877B'; Accent='A57D55'; Shine='F0D9AA' },
    @{ Name='Midgard';    Guid='976828f3c16c49d087cb96bb536ddaaa'; RuleGuid='63e7f69f6d784997a464b539744a0991'; Paper='2585CD'; Shade='155583'; Accent='1564A8'; Shine='6CDBF5' },
    @{ Name='Jotunheim';  Guid='9fcb767026394a8f93be5739ac24276d'; RuleGuid='90594fe35c39494285e9355ba7e55348'; Paper='E0EBFA'; Shade='778DB6'; Accent='4A78BD'; Shine='C5E8FF' },
    @{ Name='Nidavellir'; Guid='b88b744cf763440a89b21bac24d84b57'; RuleGuid='86d44a8433b648fba60dc63bbb528165'; Paper='5A626D'; Shade='343C45'; Accent='B5853C'; Shine='FFE19A' },
    @{ Name='Niflheim';   Guid='d5da311e8812496495b5e4229892feea'; RuleGuid='3830b4eaf6ec44e3a5bcbfa4d0d6cc31'; Paper='E3F5FA'; Shade='93BAC6'; Accent='59ACCA'; Shine='D1F6FF' },
    @{ Name='Muspelheim'; Guid='b06071343f564bb6a1b10c8568404647'; RuleGuid='b67a1f8fe0684de394df61d33de76542'; Paper='B65041'; Shade='82382E'; Accent='DE7532'; Shine='FFC069' },
    @{ Name='Hel';        Guid='f1a123520b3b4726bb33e7c89dc7473b'; RuleGuid='ec701d8aa0d3431a9e1c00b86d847a83'; Paper='2B636C'; Shade='19444A'; Accent='45ABA7'; Shine='A0E8DC' }
)

$source = [System.Drawing.Bitmap]::new($sourcePath)
try {
    # The original centre slice was entirely transparent, while the RuleTile
    # referenced a removed sprite. Fill this one slice without changing the rim.
    $paper = ColorFromHex 'F7F3EC'
    $seam = ColorFromHex 'ECE5D9'
    for ($y = 192; $y -lt 384; $y++) {
        for ($x = 192; $x -lt 384; $x++) {
            $source.SetPixel($x, $y, $(if ($x -ge 382 -or $y -ge 382) { $seam } else { $paper }))
        }
    }

    foreach ($realm in $realms) {
        $palettePaper = ColorFromHex $realm.Paper
        $paletteShade = ColorFromHex $realm.Shade
        $paletteAccent = ColorFromHex $realm.Accent
        $paletteShine = ColorFromHex $realm.Shine
        $variant = [System.Drawing.Bitmap]::new($source.Width, $source.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            for ($y = 0; $y -lt $source.Height; $y++) {
                for ($x = 0; $x -lt $source.Width; $x++) {
                    $c = $source.GetPixel($x, $y)
                    if ($c.A -eq 0) { continue }
                    $isGold = ($c.R - $c.G -ge 12) -and ($c.G - $c.B -ge 20) -and ($c.R -gt 150)
                    if ($isGold) {
                        $t = ($c.B - 32) / 165.0
                        $newColor = MixColor $paletteAccent $paletteShine $t $c.A
                    } else {
                        $t = ($c.R - 150) / 97.0
                        $newColor = MixColor $paletteShade $palettePaper $t $c.A
                    }
                    $variant.SetPixel($x, $y, $newColor)
                }
            }
            $variant.Save((Join-Path $assetRoot "Tile_BoardFrame_$($realm.Name)_Atlas.png"), [System.Drawing.Imaging.ImageFormat]::Png)
        } finally {
            $variant.Dispose()
        }

        # Clone the exact sprite rects / PPU so a 192px slice remains a 1.5-unit grid cell.
        $meta = [System.IO.File]::ReadAllText($sourceMetaPath)
        $meta = $meta.Replace($sourceGuid, $realm.Guid)
        $meta = $meta.Replace('Tile_Frame_', "Tile_Board_$($realm.Name)_")
        [System.IO.File]::WriteAllText((Join-Path $assetRoot "Tile_BoardFrame_$($realm.Name)_Atlas.png.meta"), $meta, [System.Text.UTF8Encoding]::new($false))

        $rule = [System.IO.File]::ReadAllText($rulePath)
        $rule = $rule.Replace('BoardFrame_RuleTile', "BoardFrame_$($realm.Name)_RuleTile")
        $rule = $rule.Replace($sourceGuid, $realm.Guid)
        [System.IO.File]::WriteAllText((Join-Path $ruleRoot "BoardFrame_$($realm.Name)_RuleTile.asset"), $rule, [System.Text.UTF8Encoding]::new($false))
        $ruleMeta = [System.IO.File]::ReadAllText("$rulePath.meta")
        $ruleMeta = $ruleMeta.Replace('c72e9014b4014d5e8901f1993ef5d82b', $realm.RuleGuid)
        [System.IO.File]::WriteAllText((Join-Path $ruleRoot "BoardFrame_$($realm.Name)_RuleTile.asset.meta"), $ruleMeta, [System.Text.UTF8Encoding]::new($false))
    }
} finally {
    # Save the Asgard centre last so it remains the unchanged palette master.
    $temporaryPath = Join-Path $assetRoot 'Tile_BoardFrame_Atlas.pending.png'
    $source.Save($temporaryPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $source.Dispose()
    Move-Item -LiteralPath $temporaryPath -Destination $sourcePath -Force
}

& (Join-Path $PSScriptRoot 'PackBoardTileAtlas.ps1') -ProjectRoot $ProjectRoot
