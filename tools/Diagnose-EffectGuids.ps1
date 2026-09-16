$ids = @(210, 211, 212, 213)
$names = @{ 210 = "Wind"; 211 = "Earth"; 212 = "Light"; 213 = "Dark" }

foreach ($id in $ids) {
    $name = $names[$id]
    Write-Output "=== Diagnosing E$id ($name) ==="
    
    # 1. Texture meta
    $texMetaPath = "Assets/4. DotAsset/6. Effect/Effect_${name}_Target_Hit.png.meta"
    if (Test-Path $texMetaPath) {
        $texMeta = (Get-Content $texMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value
        Write-Output "  Tex Meta GUID: $texMeta"
    } else {
        Write-Output "  Tex Meta NOT FOUND: $texMetaPath"
    }
    
    # 2. Anim meta
    $animMetaPath = "Assets/10.Animation/Effect/E${id}.anim.meta"
    if (Test-Path $animMetaPath) {
        $animMeta = (Get-Content $animMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value
        Write-Output "  Anim Meta GUID: $animMeta"
    } else {
        Write-Output "  Anim Meta NOT FOUND: $animMetaPath"
    }
    
    # 3. Controller
    $ctrlPath = "Assets/10.Animation/Effect/E${id}.controller"
    $ctrlMetaPath = "$ctrlPath.meta"
    if (Test-Path $ctrlMetaPath) {
        $ctrlMeta = (Get-Content $ctrlMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value
        Write-Output "  Ctrl Meta GUID: $ctrlMeta"
    }
    if (Test-Path $ctrlPath) {
        $motionGuid = (Get-Content $ctrlPath | Select-String "m_Motion:.*guid:\s*([0-9a-zA-Z]+)").Matches.Groups[1].Value
        Write-Output "  Ctrl -> Motion GUID: $motionGuid (Matches AnimMeta: $($motionGuid -eq $animMeta))"
    }
    
    # 4. Prefab
    $prefabPath = "Assets/2. Prefab/4. Effect/E${id}.prefab"
    $prefabMetaPath = "$prefabPath.meta"
    if (Test-Path $prefabMetaPath) {
        $prefabMeta = (Get-Content $prefabMetaPath | Select-String "^guid:\s*(\S+)").Matches.Groups[1].Value
        Write-Output "  Prefab Meta GUID: $prefabMeta"
    }
    if (Test-Path $prefabPath) {
        $ctrlInPrefab = (Get-Content $prefabPath | Select-String "m_Controller:.*guid:\s*([0-9a-zA-Z]+)").Matches.Groups[1].Value
        Write-Output "  Prefab -> Ctrl GUID: $ctrlInPrefab (Matches CtrlMeta: $($ctrlInPrefab -eq $ctrlMeta))"
        $spriteInPrefab = (Get-Content $prefabPath | Select-String "m_Sprite:.*guid:\s*([0-9a-zA-Z]+)").Matches.Groups[1].Value
        Write-Output "  Prefab -> Sprite GUID: $spriteInPrefab (Matches TexMeta: $($spriteInPrefab -eq $texMeta))"
    }
    
    # 5. EffectLibrary
    $libPath = "Assets/2. Prefab/4. Effect/EffectLibrary.asset"
    $libContent = Get-Content $libPath -Raw
    if ($libContent -match "- effectID: $id\s+effectPrefab:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-zA-Z]+)") {
        $libGuid = $matches[1]
        Write-Output "  EffectLibrary -> Prefab GUID: $libGuid (Matches PrefabMeta: $($libGuid -eq $prefabMeta))"
    } else {
        Write-Output "  EffectLibrary entry for $id NOT FOUND or doesn't match pattern"
    }
}
