$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Get-Location).Path
$backup = $PSScriptRoot
$pngPath = 'Assets/4. Asset/4. Enemy/TwoTone/Hel_HelQueen.png'
$metaPath = $pngPath + '.meta'
$appearancePath = 'Assets/4. Asset/4. Enemy/Enemy_Data/HelQueen_Appearance.asset'
$sourcePath = 'Assets/4. Asset/2. Tower/TowerCharacter/00_Synergy_4_Hel.png'
$originals = @($pngPath, $metaPath, $appearancePath, ($appearancePath + '.meta'))
$manifest = @()
foreach ($path in $originals) {
    $source = Join-Path $root $path
    $destination = Join-Path $backup $path
    if (Test-Path -LiteralPath $destination) { throw "Backup already exists: $destination" }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $hash) { throw 'Backup integrity check failed' }
    $manifest += [pscustomobject]@{Path=$path; SHA256=$hash}
}
[pscustomobject]@{Files=$manifest} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8

$metaBefore = [IO.File]::ReadAllText((Join-Path $root $metaPath))
$guid = [regex]::Match($metaBefore,'(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
if ($guid -ne '49c649b661e04a4194d2dc38b42b3625') { throw 'Unexpected source GUID' }
if ($metaBefore -notmatch '(?m)^  spritePixelsToUnits: 2048\r?$') { throw 'Unexpected PPU' }
if ([regex]::Matches($metaBefore,'(?m)^      name: ').Count -ne 2) { throw 'Expected exactly Full and Face' }

# Copy the already approved tower PNG byte-for-byte; no resize or redraw.
$outputPath = Join-Path $backup 'approved-2048.png'
$image = New-Object Drawing.Bitmap((Join-Path $root $sourcePath))
try { if ($image.Width -ne 2048 -or $image.Height -ne 2048) { throw 'Tower source must be 2048 square' } } finally { $image.Dispose() }
Copy-Item -LiteralPath (Join-Path $root $sourcePath) -Destination $outputPath

$eyeSource = @(1208,254)
$eyeFinal = @($eyeSource[0], $eyeSource[1])
$faceX = $eyeFinal[0] - 128
$faceTop = $eyeFinal[1] - 128
$faceY = 2048 - $faceTop - 256
if ($faceX -lt 0 -or $faceTop -lt 0 -or ($faceX+256) -gt 2048 -or ($faceTop+256) -gt 2048) { throw 'Face bounds invalid' }

$metaAfter = $metaBefore
$slices = @(
    [pscustomobject]@{Name='Hel_HelQueen_Full';ID='4600000000000261';X=0;Y=0;Width=2048;Height=2048},
    [pscustomobject]@{Name='Hel_HelQueen_Face';ID='4600000000000262';X=$faceX;Y=$faceY;Width=256;Height=256}
)
foreach ($slice in $slices) {
    $pattern = '(?ms)^    - serializedVersion: 2\r?\n      name: ' + [regex]::Escape($slice.Name) + '\r?\n.*?(?=^    - serializedVersion: 2\r?$|^    outline:|\z)'
    $matches = [regex]::Matches($metaAfter,$pattern)
    if ($matches.Count -ne 1) { throw "Missing sprite definition: $($slice.Name)" }
    $match = $matches[0]
    if ($match.Value -notmatch ('(?m)^      internalID: ' + $slice.ID + '\r?$')) { throw 'Unexpected sprite ID' }
    $block = $match.Value
    foreach ($field in @('X','Y','Width','Height')) {
        $key = $field.ToLowerInvariant()
        $fieldPattern = '(?m)^        ' + $key + ': [^\r\n]+'
        if ([regex]::Matches($block,$fieldPattern).Count -ne 1) { throw "Invalid rect field: $key" }
        $block = [regex]::Replace($block,$fieldPattern,'        ' + $key + ': ' + $slice.$field)
    }
    $metaAfter = $metaAfter.Substring(0,$match.Index) + $block + $metaAfter.Substring($match.Index+$match.Length)
}
$rectFieldPattern = '(?m)^        (x|y|width|height): [^\r\n]+'
if ([regex]::Replace($metaBefore,$rectFieldPattern,'RECT') -cne [regex]::Replace($metaAfter,$rectFieldPattern,'RECT')) { throw 'Metadata changes outside slice rectangles' }

$appearance = [IO.File]::ReadAllText((Join-Path $root $appearancePath))
foreach ($slice in $slices) {
    if ($appearance -notmatch ('fileID: ' + $slice.ID + ', guid: ' + $guid + ', type: 3')) { throw 'Appearance does not link to existing sprite ID' }
}
foreach ($entry in $manifest) {
    if ((Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Concurrent edit: $($entry.Path)" }
}
$metaBytes = [IO.File]::ReadAllBytes((Join-Path $root $metaPath))
$hasBom = $metaBytes.Length -ge 3 -and $metaBytes[0] -eq 239 -and $metaBytes[1] -eq 187 -and $metaBytes[2] -eq 191
$encoding = New-Object Text.UTF8Encoding($hasBom)
Copy-Item -LiteralPath $outputPath -Destination (Join-Path $root $pngPath) -Force
[IO.File]::WriteAllText((Join-Path $root $metaPath),$metaAfter,$encoding)

$final = New-Object Drawing.Bitmap((Join-Path $root $pngPath))
try {
    if ($final.Width -ne 2048 -or $final.Height -ne 2048) { throw 'Saved PNG size mismatch' }
    $cornerAlpha = @($final.GetPixel(0,0).A,$final.GetPixel(2047,0).A,$final.GetPixel(0,2047).A,$final.GetPixel(2047,2047).A)
    if (($cornerAlpha | Measure-Object -Maximum).Maximum -ne 0) { throw 'Expected transparent corners' }
    $faceRect = New-Object Drawing.Rectangle($faceX,$faceTop,256,256)
    $face = $final.Clone($faceRect,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try { $face.Save((Join-Path $backup 'face-slice-preview.png'),[Drawing.Imaging.ImageFormat]::Png) } finally { $face.Dispose() }
} finally { $final.Dispose() }
if ([IO.File]::ReadAllText((Join-Path $root $metaPath)) -cne $metaAfter) { throw 'Metadata write verification failed' }
foreach ($entry in $manifest | Where-Object {$_.Path -like '*.asset*'}) {
    if ((Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) { throw 'Appearance reference asset changed' }
}
$report = [ordered]@{
    ApprovedPreview=$sourcePath
    Destination=$pngPath
    SourceDimensions=@(2048,2048)
    SavedDimensions=@(2048,2048)
    Method='Existing approved 2048px tower PNG copied byte-for-byte; no resize, redraw, color changes or sharpening'
    PPU=2048
    Guid=$guid
    Slices=$slices
    CharacterLeftEyeSourceTopLeft=$eyeSource
    CharacterLeftEyeFinalTopLeft=$eyeFinal
    CornerAlpha=$cornerAlpha
    SpriteNamesIDsGuidAndAppearanceReferencesPreserved=$true
    MetadataChangesLimitedToSliceRects=$true
    UnityPlayTested=$false
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'file-verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 6
