$ErrorActionPreference = 'Stop'
$enemyCommon = 'Assets/4. Asset/0. Common'
$enemyFolder = 'Assets/4. Asset/4. Enemy'
$enemyUtf8 = New-Object Text.UTF8Encoding($false)
function Write-EnemyAsset([string]$path, [string]$text) {
    [IO.File]::WriteAllText((Join-Path (Get-Location) $path), $text, $enemyUtf8)
}
function Read-EnemyGuid([string]$path) {
    [regex]::Match([IO.File]::ReadAllText((Join-Path (Get-Location) $path)), '(?m)^guid: ([0-9a-f]{32})').Groups[1].Value
}
$enemyThinGuid = Read-EnemyGuid ($enemyCommon + '/Board_Decoration_Thin.png.meta')
$enemyIconPath = $enemyFolder + '/Enemy_Boss_Gimmick_Badge.png'
if (-not (Test-Path -LiteralPath ($enemyIconPath + '.meta'))) {
    $enemyTextureMeta = [IO.File]::ReadAllText((Join-Path (Get-Location) ($enemyFolder + '/Enemy_State_Badges_Atlas.png.meta')))
    $enemyTextureMeta = [regex]::Replace($enemyTextureMeta, '(?m)^guid: [0-9a-f]{32}', ('guid: ' + [Guid]::NewGuid().ToString('N')))
    $enemyTextureMeta = [regex]::Replace($enemyTextureMeta, '(?m)^(\s+spriteID:) [0-9a-f]{32}', ('$1 ' + [Guid]::NewGuid().ToString('N')))
    foreach ($enemySetting in @(@('enableMipMap','0','1'), @('spriteMode','0','1'), @('textureType','0','8'), @('alphaIsTransparency','0','1'), @('spritePixelsToUnits','384','256'), @('maxTextureSize','2048','256'))) {
        $enemyTextureMeta = [regex]::Replace($enemyTextureMeta, ('(?m)^(\s+' + $enemySetting[0] + ':) ' + $enemySetting[1] + '\s*$'), ('$1 ' + $enemySetting[2]))
    }
    Write-EnemyAsset ($enemyIconPath + '.meta') $enemyTextureMeta
}
$enemyIconGuid = Read-EnemyGuid ($enemyIconPath + '.meta')
foreach ($enemyName in @('Heal','Defense','Action','Mobility','Locked','Hourglass')) {
    $enemyMaterialPath = $enemyCommon + '/Enemy_State_' + $enemyName + '.mat'
    $enemyText = [IO.File]::ReadAllText((Join-Path (Get-Location) $enemyMaterialPath))
    $enemyTexturePattern = '(    - _DecorationTex:\r?\n        m_Texture: \{fileID: 2800000, guid: )[0-9a-f]{32}'
    if ([regex]::Matches($enemyText, $enemyTexturePattern).Count -ne 1) { throw ('Unexpected decoration field: ' + $enemyMaterialPath) }
    $enemyText = [regex]::Replace($enemyText, $enemyTexturePattern, { param($m) $m.Groups[1].Value + $enemyThinGuid })
    if ($enemyName -eq 'Locked') {
        $enemyText = [regex]::Replace($enemyText, '(?m)^    - _DecorationColor:.*$', '    - _DecorationColor: {r: 0.06, g: 0.06, b: 0.06, a: 1}')
    }
    if ($enemyName -eq 'Hourglass') {
        $enemyText = [regex]::Replace($enemyText, '(?m)^    - _DecorationColor:.*$', '    - _DecorationColor: {r: 0.7830189, g: 0.7830189, b: 0.7830189, a: 1}')
    }
    Write-EnemyAsset $enemyMaterialPath $enemyText
}
$enemyBossMaterialPath = $enemyCommon + '/Enemy_State_BossGimmick.mat'
if (-not (Test-Path -LiteralPath ($enemyBossMaterialPath + '.meta'))) {
    $enemyMaterialMeta = [IO.File]::ReadAllText((Join-Path (Get-Location) ($enemyCommon + '/Enemy_State_Locked.mat.meta')))
    $enemyMaterialMeta = [regex]::Replace($enemyMaterialMeta, '(?m)^guid: [0-9a-f]{32}', ('guid: ' + [Guid]::NewGuid().ToString('N')))
    Write-EnemyAsset ($enemyBossMaterialPath + '.meta') $enemyMaterialMeta
}
$enemyBossGuid = Read-EnemyGuid ($enemyBossMaterialPath + '.meta')
$enemyBossText = [IO.File]::ReadAllText((Join-Path (Get-Location) ($enemyCommon + '/Enemy_State_Locked.mat')))
$enemyBossText = $enemyBossText.Replace('m_Name: Enemy_State_Locked', 'm_Name: Enemy_State_BossGimmick')
$enemyBadgeBlock = "    - _BadgeTex:`r`n        m_Texture: {fileID: 2800000, guid: $enemyIconGuid, type: 3}`r`n        m_Scale: {x: 1, y: 1}`r`n        m_Offset: {x: 0, y: 0}`r`n"
$enemyBossText = [regex]::Replace($enemyBossText, '(?s)    - _BadgeTex:\r?\n.*?(?=    - _)', $enemyBadgeBlock)
Write-EnemyAsset $enemyBossMaterialPath $enemyBossText
$enemyPrefabPath = 'Assets/2. Prefab/2. Enemy.prefab'
$enemyPrefabText = [IO.File]::ReadAllText((Join-Path (Get-Location) $enemyPrefabPath))
$enemyBossReference = "  m_bossMechanicMaterial: {fileID: 2100000, guid: $enemyBossGuid, type: 2}"
if ($enemyPrefabText.Contains('  m_bossMechanicMaterial:')) {
    $enemyPrefabText = [regex]::Replace($enemyPrefabText, '(?m)^  m_bossMechanicMaterial:.*$', $enemyBossReference)
} else {
    if ([regex]::Matches($enemyPrefabText, '(?m)^  m_faceAssetPool:').Count -ne 1) { throw 'Unexpected enemy face pool count' }
    $enemyPrefabText = $enemyPrefabText.Replace('  m_faceAssetPool:', ($enemyBossReference + "`r`n  m_faceAssetPool:"))
}
Write-EnemyAsset $enemyPrefabPath $enemyPrefabText
[PSCustomObject]@{ ThinFrameGuid=$enemyThinGuid; BossIconGuid=$enemyIconGuid; BossMaterialGuid=$enemyBossGuid }
