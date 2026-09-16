# ==============================================================================
# Move-AnimationAssets.ps1
# Organizes Assets/10.Animation folder:
# - E_*.anim / .controller (and .meta) -> Assets/10.Animation/Projectile/
# - E[0-9]*.anim / .controller (and .meta) -> Assets/10.Animation/Effect/
# ==============================================================================

$baseDir = "Assets/10.Animation"
$projDir = "Assets/10.Animation/Projectile"
$effDir = "Assets/10.Animation/Effect"

if (-not (Test-Path $projDir)) {
    New-Item -ItemType Directory -Path $projDir -Force | Out-Null
}
if (-not (Test-Path $effDir)) {
    New-Item -ItemType Directory -Path $effDir -Force | Out-Null
}

$allFiles = Get-ChildItem -Path $baseDir -File

$projFiles = @()
$effFiles = @()
$otherFiles = @()

foreach ($f in $allFiles) {
    if ($f.Name -match "^Effect\.meta$" -or $f.Name -match "^Projectile\.meta$") {
        # Keep directory meta files in baseDir
        continue
    }

    if ($f.Name -match "^E_") {
        $projFiles += $f
    }
    elseif ($f.Name -match "^E\d+") {
        $effFiles += $f
    }
    else {
        $otherFiles += $f
    }
}

Write-Output "Found $($projFiles.Count) Projectile animation files."
Write-Output "Found $($effFiles.Count) Effect animation files."
if ($otherFiles.Count -gt 0) {
    Write-Output "WARNING: Found $($otherFiles.Count) unexpected other files: $($otherFiles.Name -join ', ')"
}

# 1. Move Projectile files
foreach ($f in $projFiles) {
    $dest = Join-Path $projDir $f.Name
    Move-Item -Path $f.FullName -Destination $dest -Force
}
Write-Output "Successfully moved $($projFiles.Count) files to $projDir."

# 2. Move Effect files
foreach ($f in $effFiles) {
    $dest = Join-Path $effDir $f.Name
    Move-Item -Path $f.FullName -Destination $dest -Force
}
Write-Output "Successfully moved $($effFiles.Count) files to $effDir."

# 3. Verification
$remainingFiles = Get-ChildItem -Path $baseDir -File | Where-Object { $_.Name -notmatch "^(Effect|Projectile)\.meta$" }
Write-Output "Remaining files in $baseDir (should be 0): $($remainingFiles.Count)"
if ($remainingFiles.Count -gt 0) {
    $remainingFiles | ForEach-Object { Write-Output "  Remaining: $($_.Name)" }
}

$projCount = (Get-ChildItem -Path $projDir -File).Count
$effCount = (Get-ChildItem -Path $effDir -File).Count

Write-Output "Final Verification:"
Write-Output "  Projectile folder file count: $projCount (expected 32: 8 clips, 8 controllers, 16 metas)"
Write-Output "  Effect folder file count:     $effCount (expected 108: 27 clips, 27 controllers, 54 metas)"
