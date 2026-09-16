# ==============================================================================
# Rename-ProjectileAnimationsToE.ps1
# Renames Projectile_<ID> animations and controllers to E_<ID> in Assets/10.Animation/
# Preserves all Unity GUIDs so Prefab connections remain 100% intact.
# ==============================================================================

$animDir = "Assets/10.Animation"
$ids = @(107, 207, 208, 209, 210, 211, 212, 213)

foreach ($id in $ids) {
    $oldAnimPath = Join-Path $animDir "Projectile_${id}.anim"
    $newAnimPath = Join-Path $animDir "E_${id}.anim"
    $oldAnimMeta = "$oldAnimPath.meta"
    $newAnimMeta = "$newAnimPath.meta"

    $oldCtrlPath = Join-Path $animDir "Projectile_${id}.controller"
    $newCtrlPath = Join-Path $animDir "E_${id}.controller"
    $oldCtrlMeta = "$oldCtrlPath.meta"
    $newCtrlMeta = "$newCtrlPath.meta"

    # 1. Process Animation Clip
    if (Test-Path $oldAnimPath) {
        $content = [System.IO.File]::ReadAllText($oldAnimPath)
        $content = $content.Replace("m_Name: Projectile_${id}", "m_Name: E_${id}")
        [System.IO.File]::WriteAllText($newAnimPath, $content)
        Remove-Item $oldAnimPath -Force
        
        if (Test-Path $oldAnimMeta) {
            Move-Item -Path $oldAnimMeta -Destination $newAnimMeta -Force
        }
        Write-Output "Renamed: Projectile_${id}.anim -> E_${id}.anim"
    }

    # 2. Process Controller
    if (Test-Path $oldCtrlPath) {
        $content = [System.IO.File]::ReadAllText($oldCtrlPath)
        $content = $content.Replace("m_Name: Projectile_${id}", "m_Name: E_${id}")
        [System.IO.File]::WriteAllText($newCtrlPath, $content)
        Remove-Item $oldCtrlPath -Force
        
        if (Test-Path $oldCtrlMeta) {
            Move-Item -Path $oldCtrlMeta -Destination $newCtrlMeta -Force
        }
        Write-Output "Renamed: Projectile_${id}.controller -> E_${id}.controller"
    }
}

Write-Output "ALL PROJECTILE ANIMATIONS SUCCESSFULLY RENAMED TO E_<ID>!"
