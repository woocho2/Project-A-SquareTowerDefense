$ErrorActionPreference = 'Stop'
$backupRoot = Join-Path $PSScriptRoot 'submission-backup'
$manifest = Get-Content (Join-Path $backupRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$git = (Get-ChildItem "$env:LOCALAPPDATA/GitHubDesktop/app-*/resources/app/git/cmd/git.exe" | Sort-Object FullName -Descending | Select-Object -First 1).FullName
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    $badBackup = @($manifest.Files | Where-Object {
        $file = Join-Path $backupRoot $_.Path
        !(Test-Path -LiteralPath $file) -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $_.Hash
    })
    $protectedChanges = @($manifest.Protected | Where-Object {
        !(Test-Path -LiteralPath $_.Path) -or (Get-FileHash -LiteralPath $_.Path -Algorithm SHA256).Hash -ne $_.Hash
    })
    & $git diff --quiet HEAD -- $manifest.RestoredFromHead
    $matchesHead = $LASTEXITCODE -eq 0
    $head = (& $git rev-parse HEAD).Trim()
    $effects = @($manifest.RestoredFromHead | Where-Object { $_.EndsWith('.prefab') })
    $clips = @($manifest.RestoredFromHead | Where-Object { $_.EndsWith('.anim') })
    $badEffects = @($effects | Where-Object {
        $text = Get-Content -LiteralPath $_ -Raw -Encoding UTF8
        $text -notmatch 'guid: e0facfe5cc7365d469d252cf7019ef98' -or $text -notmatch 'guid: 3c2ef45af8816d443ac4d1e3a1a9778a'
    })
    $badClips = @($clips | Where-Object {
        (Get-Content -LiteralPath $_ -Raw -Encoding UTF8) -notmatch 'attribute: m_Sprite\r?\n    path: *\r?\n'
    })
    $scene = Get-Content 'Assets/1. Scenes/3. Stage1.unity' -Raw -Encoding UTF8
    $newIds = @('1287103','238421516','266728057','464247216','474818962','490017124','662574885','1060905246','1457479799','1618822198','1624481550','1838845911','1903353676','2097060043')
    $enabledNew = @($newIds | Where-Object {
        $go = [regex]::Match($scene, '(?ms)^--- !u!1 &' + $_ + '\r?\n(.*?)(?=^--- !u!|\z)').Value
        !$go -or $go -notmatch 'm_IsActive: 0'
    })
    $report = [ordered]@{
        VerifiedAt = (Get-Date).ToString('o')
        Head = $head
        HeadUnchanged = $head -eq $manifest.Head
        BackupFileCount = $manifest.Files.Count
        BackupHashFailures = @($badBackup | ForEach-Object { $_.Path })
        ProtectedFileCount = $manifest.Protected.Count
        ProtectedChanges = @($protectedChanges | ForEach-Object { $_.Path })
        RestoredAssetCount = $manifest.RestoredFromHead.Count
        RestoredAssetsMatchHead = $matchesHead
        EffectPrefabsWithOldComponents = $effects.Count - $badEffects.Count
        InvalidEffectPrefabs = @($badEffects)
        TargetClipsWithRootSpriteCurve = $clips.Count - $badClips.Count
        InvalidTargetClips = @($badClips)
        NewManagersStillDisabled = $enabledNew.Count -eq 0
        RuntimeVerified = @('Wave 1 spawned 8 enemies', 'Tower creation button', 'Turn progression and enemy movement', 'Tower attacks, enemy deaths and gold rewards; reached Wave 2', 'Tower info character portrait and name', 'Options open and continue', 'No red Console errors in final play run')
        RuntimeLimitations = @('No exhaustive test of all 28 projectile/effect types', 'Other info panels outside final acceptance per user clarification', 'Existing GameManager DontDestroyOnLoad warning remains', 'No standalone build or full-stage clear test')
    }
    $report | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $backupRoot 'verification.json') -Encoding UTF8
    $manifest.RestoredFromHead | Set-Content (Join-Path $backupRoot 'reverted-files.txt') -Encoding UTF8
    $report | ConvertTo-Json -Depth 6
    if ($badBackup.Count -or $protectedChanges.Count -or !$matchesHead -or $badEffects.Count -or $badClips.Count -or $enabledNew.Count -or $head -ne $manifest.Head) { throw 'Verification failed; inspect verification.json.' }
}
finally { Pop-Location }
