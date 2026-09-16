# ==============================================================================
# Fix-TargetHitEffectsGuids.ps1
# Fixes broken GUID chains for Target Hit Effects: E210, E211, E212, E213
# 1. Sets valid 32-char hex GUIDs on E<ID>.prefab.meta
# 2. Updates EffectLibrary.asset with the valid prefab GUIDs
# 3. Connects E<ID>.controller m_Motion to the real E<ID>.anim.meta GUID
# 4. Connects E<ID>.prefab m_Controller to the real E<ID>.controller.meta GUID
# ==============================================================================

$ids = @(210, 211, 212, 213)
$names = @{ 210 = "Wind"; 211 = "Earth"; 212 = "Light"; 213 = "Dark" }

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"
$libPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\EffectLibrary.asset"
$libContent = [System.IO.File]::ReadAllText($libPath)

foreach ($id in $ids) {
    $name = $names[$id]
    Write-Output "Processing E$id ($name)..."

    $animMetaPath = Join-Path $baseDir "Assets\10.Animation\Effect\E${id}.anim.meta"
    $animGuid = (Get-Content $animMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value

    $ctrlPath = Join-Path $baseDir "Assets\10.Animation\Effect\E${id}.controller"
    $ctrlMetaPath = "$ctrlPath.meta"
    $ctrlGuid = (Get-Content $ctrlMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value

    $prefabPath = Join-Path $baseDir "Assets\2. Prefab\4. Effect\E${id}.prefab"
    $prefabMetaPath = "$prefabPath.meta"
    $newPrefabGuid = ("7a0702" + $id).PadRight(28, '0') + "${id}f"
    if ($newPrefabGuid.Length -ne 32 -or $newPrefabGuid -notmatch '^[0-9a-fA-F]{32}$') {
        throw "Invalid GUID generated: $newPrefabGuid (Length: $($newPrefabGuid.Length))"
    }

    # 1. Update E<ID>.controller -> set m_Motion guid to actual animGuid
    if (Test-Path $ctrlPath) {
        $ctrlText = [System.IO.File]::ReadAllText($ctrlPath)
        $ctrlText = [System.Text.RegularExpressions.Regex]::Replace(
            $ctrlText,
            "m_Motion:\s*\{fileID:\s*7400000,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*2\}",
            "m_Motion: {fileID: 7400000, guid: $animGuid, type: 2}"
        )
        [System.IO.File]::WriteAllText($ctrlPath, $ctrlText)
        Write-Output "  -> Updated $ctrlPath (m_Motion GUID: $animGuid)"
    }

    # 2. Update E<ID>.prefab.meta -> set valid 32-char hex guid
    if (Test-Path $prefabMetaPath) {
        $metaText = [System.IO.File]::ReadAllText($prefabMetaPath)
        $metaText = [System.Text.RegularExpressions.Regex]::Replace(
            $metaText,
            "guid:\s*[0-9a-zA-Z]+",
            "guid: $newPrefabGuid"
        )
        [System.IO.File]::WriteAllText($prefabMetaPath, $metaText)
        Write-Output "  -> Updated $prefabMetaPath (GUID: $newPrefabGuid)"
    }

    # 3. Update E<ID>.prefab -> set m_Controller guid to actual ctrlGuid
    if (Test-Path $prefabPath) {
        $prefabText = [System.IO.File]::ReadAllText($prefabPath)
        $prefabText = [System.Text.RegularExpressions.Regex]::Replace(
            $prefabText,
            "m_Controller:\s*\{fileID:\s*9100000,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*2\}",
            "m_Controller: {fileID: 9100000, guid: $ctrlGuid, type: 2}"
        )
        [System.IO.File]::WriteAllText($prefabPath, $prefabText)
        Write-Output "  -> Updated $prefabPath (m_Controller GUID: $ctrlGuid)"
    }

    # 4. Update EffectLibrary.asset entry for this id
    $pattern = "(- effectID: $id\r?\n\s*effectPrefab:\s*\{fileID:\s*1142942343058428718,\s*guid:\s*)[0-9a-zA-Z]+(,\s*type:\s*3\})"
    $replacement = "`${1}$newPrefabGuid`${2}"
    $libContent = [System.Text.RegularExpressions.Regex]::Replace($libContent, $pattern, $replacement)
}

[System.IO.File]::ReadAllText($libPath) | Out-Null
[System.IO.File]::WriteAllText($libPath, $libContent)
Write-Output "EffectLibrary.asset updated successfully!"
