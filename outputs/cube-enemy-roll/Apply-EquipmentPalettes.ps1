$ErrorActionPreference = 'Stop'
$projectDir = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Write-TaskFile([string]$path, [string]$text) { [IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8) }
function Ensure-Meta([string]$path, [string]$kind) {
    $meta = $path + '.meta'
    if (Test-Path -LiteralPath $meta) { return ([regex]::Match([IO.File]::ReadAllText($meta), 'guid: ([a-f0-9]+)')).Groups[1].Value }
    $guid = [guid]::NewGuid().ToString('N')
    $folder = if ($kind -eq 'Folder') { "folderAsset: yes`n" } else { '' }
    $importer = if ($kind -eq 'Folder') { 'DefaultImporter' } else { $kind + 'Importer' }
    Write-TaskFile $meta "fileFormatVersion: 2`nguid: $guid`n${folder}${importer}:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
    return $guid
}
$shaderPath = Join-Path $projectDir 'Assets/3. Scripts/03. Enemy/EnemyEquipmentPalette.shader'
$shaderGuid = Ensure-Meta $shaderPath 'Shader'
$materialDir = Join-Path $projectDir 'Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/EquipmentPalettes'
$variantDir = Join-Path $projectDir 'Assets/2. Prefab/3. Enemy/VisualVariants'
foreach ($dir in @($materialDir,$variantDir)) {
    [void](New-Item -ItemType Directory -Force -Path $dir)
    [void](Ensure-Meta $dir 'Folder')
}
$prefabDir = Join-Path $projectDir 'Assets/2. Prefab/3. Enemy'
$palettes = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'equipment-palettes.json') -Raw | ConvertFrom-Json
$manifest = @()
foreach ($palette in $palettes) {
    $matPath = Join-Path $materialDir ('Valkyrie_' + $palette.key + '.mat')
    $matGuid = Ensure-Meta $matPath 'NativeFormat'
    $armor = $palette.armor; $gem = $palette.gem
    $mat = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Valkyrie_$($palette.key)
  m_Shader: {fileID: 4800000, guid: $shaderGuid, type: 3}
  m_Parent: {fileID: 0}
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _MainTex:
        m_Texture: {fileID: 0}
        m_Scale: {x: 1, y: 1}
        m_Offset: {x: 0, y: 0}
    m_Ints: []
    m_Floats:
    - _CornerRadius: 8
    - _SquareHead: 1
    - _FaceIndex: 0
    - PixelSnap: 0
    m_Colors:
    - _ArmorColor: {r: $($armor[0]), g: $($armor[1]), b: $($armor[2]), a: 1}
    - _GemColor: {r: $($gem[0]), g: $($gem[1]), b: $($gem[2]), a: 1}
    - _Color: {r: 1, g: 1, b: 1, a: 1}
    - _SpriteRect: {r: 0, g: 256, b: 256, a: 256}
    - _HeadRect: {r: 16, g: 16, b: 224, a: 224}
  m_BuildTextureStacks: []
"@
    Write-TaskFile $matPath ($mat + "`n")
    if ($palette.prefab) {
        $prefabPath = Join-Path $prefabDir $palette.prefab
        $text = [IO.File]::ReadAllText($prefabPath)
        $text = [regex]::Replace($text, '(m_Materials:\r?\n  - )\{fileID: 2100000, guid: [a-f0-9]+, type: 2\}', ('$1{fileID: 2100000, guid: '+$matGuid+', type: 2}'))
        $text = [regex]::Replace($text, '(?m)^  m_Color: \{[^\r\n]+\}', '  m_Color: {r: 1, g: 1, b: 1, a: 1}')
        if ($text -notmatch 'm_useEquipmentPalette:') { $text = $text.Replace('  m_faceScale:', "  m_useEquipmentPalette: 1`n  m_faceScale:") }
        Write-TaskFile $prefabPath $text
    } else {
        $prefabPath = Join-Path $variantDir ($palette.variant + '.prefab')
        $baseGuid = ([regex]::Match([IO.File]::ReadAllText((Join-Path $prefabDir '1. Enemy_normal.prefab.meta')), 'guid: ([a-f0-9]+)')).Groups[1].Value
        $variant = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1001 &901820001
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {fileID: 0}
    m_Modifications:
    - target: {fileID: 140337874124788210, guid: $baseGuid, type: 3}
      propertyPath: m_Name
      value: $($palette.variant)
      objectReference: {fileID: 0}
    - target: {fileID: 8437019901037981175, guid: $baseGuid, type: 3}
      propertyPath: m_Materials.Array.data[0]
      value:
      objectReference: {fileID: 2100000, guid: $matGuid, type: 2}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: $baseGuid, type: 3}
--- !u!4 &901820002 stripped
Transform:
  m_CorrespondingSourceObject: {fileID: 7072637282772444813, guid: $baseGuid, type: 3}
  m_PrefabInstance: {fileID: 901820001}
  m_PrefabAsset: {fileID: 0}
"@
        Write-TaskFile $prefabPath ($variant + "`n")
        [void](Ensure-Meta $prefabPath 'Prefab')
    }
    $manifest += [pscustomobject]@{key=$palette.key;label=$palette.label;material=$matPath;materialGuid=$matGuid;prefab=$prefabPath;visualOnly=[bool]$palette.variant}
}
Write-TaskFile (Join-Path $PSScriptRoot 'equipment-manifest.json') ($manifest | ConvertTo-Json -Depth 4)
Write-Output 'Applied 5 regular monster palettes; existing boss equipment palettes preserve distinct boss colors.'
Write-Output 'Action and GroupHeal are visual variants of Normal. No new abilities or wave/pool types are implemented.'
