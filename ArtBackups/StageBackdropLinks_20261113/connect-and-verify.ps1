$ErrorActionPreference = 'Stop'
$root = (Get-Location).Path
$backup = $PSScriptRoot
$jobs = (Get-Content -LiteralPath 'ArtPreviews/StageBackdrops_20261113/jobs-and-prompts.json' -Raw -Encoding UTF8 | ConvertFrom-Json).jobs
$sourceGuid = 'e22ac303548879f4ea8e06bee903eaf5'
$oldReference = 'm_Sprite: {fileID: -7517352591717720445, guid: ' + $sourceGuid + ', type: 3}'
$spritePattern = 'm_Sprite: \{fileID: -?\d+, guid: [0-9a-f]{32}, type: 3\}'
$referenceMeta = [IO.File]::ReadAllText((Join-Path $root 'Assets/4. Asset/1. BackGround/Asgard_SkyBackdrop_v2_TopDown_1920x1080.png.meta'))
$referencePPU = [regex]::Match($referenceMeta, '(?m)^  spritePixelsToUnits: ([0-9.]+)').Groups[1].Value
if ($referencePPU -ne '72') { throw 'Asgard PPU changed; review the scale before proceeding' }
$plans = @()
$connections = @()
$unchanged = @('Assets/1. Scenes/3. Stage1.unity', 'Assets/1. Scenes/3. Stage1.unity.meta', 'Assets/4. Asset/1. BackGround/Asgard_SkyBackdrop_v2_TopDown_1920x1080.png', 'Assets/4. Asset/1. BackGround/Asgard_SkyBackdrop_v2_TopDown_1920x1080.png.meta')

function New-EditPlan([string]$path, [string]$before, [string]$after, [string]$kind) {
    $absolute = Join-Path $root $path
    $bytes = [IO.File]::ReadAllBytes($absolute)
    if ([IO.File]::ReadAllText($absolute) -cne $before) { throw "Concurrent edit: $path" }
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 239 -and $bytes[1] -eq 187 -and $bytes[2] -eq 191
    [pscustomobject]@{ Path=$path; Before=$before; After=$after; Kind=$kind; Bom=$hasBom; BeforeSHA256=(Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash }
}

foreach ($job in $jobs) {
    $scenePath = 'Assets/1. Scenes/' + ($job.id + 2) + '. Stage' + $job.id + '.unity'
    $metaPath = $job.destination + '.meta'
    $meta = [IO.File]::ReadAllText((Join-Path $root $metaPath))
    $guid = [regex]::Match($meta, '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
    if ($guid.Length -ne 32) { throw "Missing GUID: $metaPath" }
    if ($meta -notmatch '(?m)^  textureType: 8\r?$' -or $meta -notmatch '(?m)^  spriteMode: 2\r?$') { throw "Expected imported multiple Sprite: $metaPath" }
    $ids = [regex]::Matches($meta, '(?m)^      internalID: (-?\d+)\r?$')
    if ($ids.Count -ne 1) { throw "Expected one full image slice: $metaPath" }
    $spriteID = $ids[0].Groups[1].Value
    $spriteName = [regex]::Match($meta, '(?m)^      name: ([^\r\n]+)').Groups[1].Value
    if ($meta -notmatch ('(?m)^      ' + [regex]::Escape($spriteName) + ': ' + $spriteID + '\r?$')) { throw "Sprite ID table mismatch: $metaPath" }
    if ($meta -notmatch '(?ms)      rect:\r?\n        serializedVersion: 2\r?\n        x: 0\r?\n        y: 0\r?\n        width: 1920\r?\n        height: 1080') { throw "Unexpected sprite rectangle: $metaPath" }
    $ppuPattern = '(?m)^  spritePixelsToUnits: [^\r\n]+'
    if ([regex]::Matches($meta, $ppuPattern).Count -ne 1) { throw 'Ambiguous PPU' }
    $newMeta = [regex]::Replace($meta, $ppuPattern, '  spritePixelsToUnits: ' + $referencePPU)
    if ([regex]::Replace($meta, $ppuPattern, 'PPU') -cne [regex]::Replace($newMeta, $ppuPattern, 'PPU')) { throw 'Unexpected importer change' }
    $plans += New-EditPlan $metaPath $meta $newMeta 'PPU'

    $scene = [IO.File]::ReadAllText((Join-Path $root $scenePath))
    $go = [regex]::Match($scene, '(?ms)^--- !u!1 &1771618199\r?\n.*?(?=^--- !u!|\z)')
    if (-not $go.Success -or $go.Value -notmatch '(?m)^  m_Name: BackGround\r?$' -or $go.Value -notmatch '(?m)^  m_IsActive: 1\r?$') { throw "Missing active BackGround: $scenePath" }
    $rendererPattern = '(?ms)^--- !u!212 &1771618201\r?\n.*?(?=^--- !u!|\z)'
    $renderers = [regex]::Matches($scene, $rendererPattern)
    if ($renderers.Count -ne 1) { throw "Ambiguous background renderer: $scenePath" }
    $renderer = $renderers[0]
    if ($renderer.Value -notmatch 'm_GameObject: \{fileID: 1771618199\}' -or $renderer.Value -notmatch '(?m)^  m_Enabled: 1\r?$') { throw 'Wrong background renderer owner or state' }
    if ([regex]::Matches($renderer.Value, [regex]::Escape($oldReference)).Count -ne 1) { throw "Unexpected previous background: $scenePath" }
    $newReference = 'm_Sprite: {fileID: ' + $spriteID + ', guid: ' + $guid + ', type: 3}'
    $newRenderer = $renderer.Value.Replace($oldReference, $newReference)
    $newScene = $scene.Substring(0, $renderer.Index) + $newRenderer + $scene.Substring($renderer.Index + $renderer.Length)
    if ([regex]::Replace($scene, $spritePattern, 'SPRITE') -cne [regex]::Replace($newScene, $spritePattern, 'SPRITE')) { throw 'Unexpected non-sprite scene changes' }
    $plans += New-EditPlan $scenePath $scene $newScene 'Sprite reference'
    $unchanged += $scenePath + '.meta'
    $unchanged += $job.destination
    $connections += [pscustomobject]@{ Stage=$job.id; World=$job.world; Korean=$job.korean; Scene=$scenePath; PNG=$job.destination; Meta=$metaPath; Guid=$guid; SpriteID=$spriteID; SpriteName=$spriteName; PPU=72; WorldSize=@((1920.0/72), (1080.0/72)) }
}
if ($connections.Count -ne 9 -or $plans.Count -ne 18) { throw 'Expected nine scenes and nine importer updates' }

$unchangedRecords = @(foreach ($path in $unchanged) { [pscustomobject]@{Path=$path; SHA256=(Get-FileHash -LiteralPath (Join-Path $root $path) -Algorithm SHA256).Hash} })
$backupRecords = @()
foreach ($plan in $plans) {
    $source = Join-Path $root $plan.Path
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $plan.BeforeSHA256) { throw "File changed before backup: $($plan.Path)" }
    $destination = Join-Path $backup $plan.Path
    if (Test-Path -LiteralPath $destination) { throw "Backup already exists: $destination" }
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $plan.BeforeSHA256) { throw 'Backup verification failed' }
    $backupRecords += [pscustomobject]@{Path=$plan.Path; SHA256=$plan.BeforeSHA256; Change=$plan.Kind}
    if ($plan.Path.EndsWith('.unity')) {
        $sceneMeta = $plan.Path + '.meta'
        Copy-Item -LiteralPath (Join-Path $root $sceneMeta) -Destination (Join-Path $backup $sceneMeta)
        $backupRecords += [pscustomobject]@{Path=$sceneMeta; SHA256=(Get-FileHash -LiteralPath (Join-Path $backup $sceneMeta) -Algorithm SHA256).Hash; Change='None; scene GUID retained'}
    }
}
[pscustomobject]@{Files=$backupRecords;UnchangedFiles=$unchangedRecords} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backup 'manifest.json') -Encoding UTF8

foreach ($plan in $plans) {
    $absolute = Join-Path $root $plan.Path
    if ((Get-FileHash -LiteralPath $absolute -Algorithm SHA256).Hash -ne $plan.BeforeSHA256) { throw "File changed before writing: $($plan.Path)" }
    $encoding = New-Object Text.UTF8Encoding($plan.Bom)
    [IO.File]::WriteAllText($absolute, $plan.After, $encoding)
}

foreach ($plan in $plans) {
    if ([IO.File]::ReadAllText((Join-Path $root $plan.Path)) -cne $plan.After) { throw "Write verification failed: $($plan.Path)" }
}
foreach ($entry in $unchangedRecords) {
    if ((Get-FileHash -LiteralPath (Join-Path $root $entry.Path) -Algorithm SHA256).Hash -ne $entry.SHA256) { throw "Unexpected unrelated change: $($entry.Path)" }
}
foreach ($entry in $connections) {
    $scene = [IO.File]::ReadAllText((Join-Path $root $entry.Scene))
    $renderer = [regex]::Match($scene, '(?ms)^--- !u!212 &1771618201\r?\n.*?(?=^--- !u!|\z)').Value
    if ($renderer -notmatch ('m_Sprite: \{fileID: ' + $entry.SpriteID + ', guid: ' + $entry.Guid + ', type: 3\}')) { throw 'Final reference verification failed' }
    $meta = [IO.File]::ReadAllText((Join-Path $root $entry.Meta))
    if ($meta -notmatch '(?m)^  spritePixelsToUnits: 72\r?$') { throw 'Final PPU verification failed' }
    if ($meta -notmatch ('(?m)^      internalID: ' + $entry.SpriteID + '\r?$')) { throw 'Final sprite ID verification failed' }
}
$report = [ordered]@{
    ConnectedStageCount = 9
    Connections = $connections
    SceneChanges = 'Only BackGround SpriteRenderer.m_Sprite, one per scene'
    ImporterChanges = 'Only spritePixelsToUnits, 100 -> 72, matches existing Asgard size'
    PNGsUnchanged = $true
    Stage1Unchanged = $true
    SceneMetaGUIDsUnchanged = $true
    SpriteGUIDsAndIDsAndRectsUnchanged = $true
    GameCodeChanged = $false
    UnityPlayTested = $false
}
$report | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath (Join-Path $backup 'file-verification.json') -Encoding UTF8
$connections | Select-Object Stage,World,Scene,PPU | ConvertTo-Json
Write-Output 'Connected nine stage backgrounds; verified minimal scene/importer changes, stable GUIDs, sprite IDs and dimensions.'
