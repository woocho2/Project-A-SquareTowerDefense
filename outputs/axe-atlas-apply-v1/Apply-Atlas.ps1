$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$directory = Join-Path $root 'Assets\4. Asset\2. Tower\TowerCharacter'
$oldPath = Join-Path $directory 'Tower_Axe_Front_Atlas_v2.png'
$newPath = Join-Path $directory 'Axe_atlas.png'
$source = Join-Path $root 'outputs\axe-jobs-v1\Axe_Classes_Atlas_Draft.png'
if ((Split-Path $oldPath -Parent) -ne $directory -or (Split-Path $newPath -Parent) -ne $directory) { throw 'Unexpected asset path.' }
if (Test-Path -LiteralPath $newPath) { throw 'Destination already exists; inspect before replacing.' }
$metaPath = $oldPath + '.meta'
$originalMeta = [IO.File]::ReadAllText($metaPath)
$guidValue = '38dd808fe99a439d8843a95932730424'
if ($originalMeta -notmatch ('guid: ' + $guidValue)) { throw 'Unexpected source GUID.' }
foreach ($id in 52020..52023) {
    if ($originalMeta -notmatch ('internalID: ' + $id)) { throw 'Missing existing sprite ID.' }
}
$bitmap = New-Object Drawing.Bitmap($source)
try {
    if ($bitmap.Width -ne 1024 -or $bitmap.Height -ne 1024) { throw 'Wrong atlas dimensions.' }
    for ($row=0; $row -lt 2; $row++) {
        for ($column=0; $column -lt 2; $column++) {
            for ($y=0; $y -lt 512; $y++) {
                for ($x=0; $x -lt 512; $x++) {
                    if ($x -ge 16 -and $x -lt 496 -and $y -ge 16 -and $y -lt 496) { continue }
                    if ($bitmap.GetPixel($column*512+$x,$row*512+$y).A -ne 0) { throw 'A cell has less than 16px transparent padding.' }
                }
            }
        }
    }
} finally { $bitmap.Dispose() }
$backup = Join-Path $PSScriptRoot 'before'
New-Item -ItemType Directory -Path $backup -Force | Out-Null
Copy-Item -LiteralPath $oldPath -Destination (Join-Path $backup 'Tower_Axe_Front_Atlas_v2.png')
Copy-Item -LiteralPath $metaPath -Destination (Join-Path $backup 'Tower_Axe_Front_Atlas_v2.png.meta')
Move-Item -LiteralPath $metaPath -Destination ($newPath + '.meta')
Move-Item -LiteralPath $oldPath -Destination $newPath
Copy-Item -LiteralPath $source -Destination $newPath -Force
if ([IO.File]::ReadAllText($newPath+'.meta') -cne $originalMeta) { throw 'Importer metadata changed.' }
if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $newPath).Hash) { throw 'Atlas content does not match approved draft.' }
$references = @()
foreach ($catalog in 'TowerCharacterCatalog.asset','TowerCharacterCatalog_Old.asset') {
    $path = Join-Path $root ('Assets\Resources\'+$catalog)
    $text = [IO.File]::ReadAllText($path)
    foreach ($id in 52020..52023) {
        if (-not $text.Contains(('fileID: '+$id+', guid: '+$guidValue))) { throw ('Missing catalog reference in '+$catalog) }
    }
    $references += $catalog
}
$record = [ordered]@{
    source='outputs/axe-jobs-v1/Axe_Classes_Atlas_Draft.png'
    destination='Assets/4. Asset/2. Tower/TowerCharacter/Axe_atlas.png'
    size='1024x1024';cell_size='512x512';transparent_padding_px=16
    guid=$guidValue;sprite_ids=@(52020,52021,52022,52023)
    metadata_preserved=$true;source_hash_matches=$true;catalogs_checked=$references
    game_code_changed=$false;unity_playback_checked=$false
}
$record | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'Validation.json') -Encoding UTF8
$record | ConvertTo-Json -Depth 4
