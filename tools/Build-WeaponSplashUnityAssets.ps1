# ==============================================================================
# Build-WeaponSplashUnityAssets.ps1
# Full Unity Asset Pipeline for 6 Weapon Splash Towers (101~106):
#   1. Connect pure 64x64 metallic gray symbol orbs (Single Sprite Mode)
#   2. Remove Animator component so SpriteRenderer is never overridden to null
#   3. Fix 6-frame spritesheet TextureImporter metas (textureType: 8, platformSettings)
#   4. Activate Arrow Rain effect for E102
# ==============================================================================

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"

$weapons = @(
    @{ ID = 101; Name = "Sword";  SingleGuid = "9a070200000000000000000000000101"; Tex = "Projectile_Meteor_Sword" },
    @{ ID = 102; Name = "Bow";    SingleGuid = "9a070200000000000000000000000102"; Tex = "Projectile_Meteor_Bow" },
    @{ ID = 103; Name = "Shield"; SingleGuid = "9a070200000000000000000000000103"; Tex = "Projectile_Meteor_Shield" },
    @{ ID = 104; Name = "Spear";  SingleGuid = "9a070200000000000000000000000104"; Tex = "Projectile_Meteor_Spear" },
    @{ ID = 105; Name = "Axe";    SingleGuid = "9a070200000000000000000000000105"; Tex = "Projectile_Meteor_Axe" },
    @{ ID = 106; Name = "Hammer"; SingleGuid = "9a070200000000000000000000000106"; Tex = "Projectile_Meteor_Hammer" }
)

Write-Output "=================================================="
Write-Output "Building 6 Weapon Splash Tower Unity Assets (101..106)..."
Write-Output "=================================================="

# ------------------------------------------------------------------------------
# STEP 0: Activate Arrow Rain effect for E102
# ------------------------------------------------------------------------------
$rainSrc = Join-Path $baseDir "Assets\4. DotAsset\6. Effect\Effect_Arrow_Rain.png"
$bombDst = Join-Path $baseDir "Assets\4. DotAsset\6. Effect\Effect_Arrow_Bomb_Explosion.png"
if (Test-Path $rainSrc) {
    Copy-Item $rainSrc $bombDst -Force
    Write-Output "[Arrow Rain] Successfully synced Effect_Arrow_Rain.png -> Effect_Arrow_Bomb_Explosion.png (Preserving E102 GUID linkage)."
}

# ------------------------------------------------------------------------------
# STEP 1: Process 6 Weapon Orbs (Meta, Prefab)
# ------------------------------------------------------------------------------

foreach ($wp in $weapons) {
    $id = $wp.ID
    $wName = $wp.Name
    $texName = $wp.Tex
    $singleGuid = $wp.SingleGuid

    Write-Output "`n>>> Processing [$($id): $wName]..."

    # Strict 32-hex GUIDs for multi-sheet
    $projTexGuid  = "7a0701" + $id + "0000000000000000000" + $id + "b"
    $projAnimGuid = "7a0701" + $id + "0000000000000000000" + $id + "a"
    $projCtrlGuid = "7a0701" + $id + "0000000000000000000" + $id + "c"

    # 1. FIX MULTI-SHEET META FILE (Include textureType: 8 and platformSettings)
    $pMeta = New-Object System.Collections.Generic.List[string]
    $pMeta.Add("fileFormatVersion: 2")
    $pMeta.Add("guid: $projTexGuid")
    $pMeta.Add("TextureImporter:")
    $pMeta.Add("  internalIDToNameTable:")
    for ($f = 0; $f -lt 6; $f++) {
        $fid = "${id}10${f}"
        $pMeta.Add("  - first:")
        $pMeta.Add("      213: $fid")
        $pMeta.Add("    second: ${texName}_${f}")
    }
    $pMeta.Add("  externalObjects: {}")
    $pMeta.Add("  serializedVersion: 13")
    $pMeta.Add("  mipmaps:")
    $pMeta.Add("    mipMapMode: 0")
    $pMeta.Add("    enableMipMap: 0")
    $pMeta.Add("    sRGBTexture: 1")
    $pMeta.Add("    linearTexture: 0")
    $pMeta.Add("    fadeOut: 0")
    $pMeta.Add("    borderMipMap: 0")
    $pMeta.Add("    mipMapsPreserveCoverage: 0")
    $pMeta.Add("    alphaTestReferenceValue: 0.5")
    $pMeta.Add("    mipMapFadeDistanceStart: 1")
    $pMeta.Add("    mipMapFadeDistanceEnd: 3")
    $pMeta.Add("  bumpmap:")
    $pMeta.Add("    convertToNormalMap: 0")
    $pMeta.Add("    externalNormalMap: 0")
    $pMeta.Add("    heightScale: 0.25")
    $pMeta.Add("    normalMapFilter: 0")
    $pMeta.Add("    flipGreenChannel: 0")
    $pMeta.Add("  isReadable: 0")
    $pMeta.Add("  streamingMipmaps: 0")
    $pMeta.Add("  streamingMipmapsPriority: 0")
    $pMeta.Add("  vTOnly: 0")
    $pMeta.Add("  ignoreMipmapLimit: 0")
    $pMeta.Add("  grayScaleToAlpha: 0")
    $pMeta.Add("  generateCubemap: 6")
    $pMeta.Add("  cubemapConvolution: 0")
    $pMeta.Add("  seamlessCubemap: 0")
    $pMeta.Add("  textureFormat: 1")
    $pMeta.Add("  maxTextureSize: 2048")
    $pMeta.Add("  textureSettings:")
    $pMeta.Add("    serializedVersion: 2")
    $pMeta.Add("    filterMode: 0")
    $pMeta.Add("    aniso: 1")
    $pMeta.Add("    mipBias: 0")
    $pMeta.Add("    wrapU: 1")
    $pMeta.Add("    wrapV: 1")
    $pMeta.Add("    wrapW: 1")
    $pMeta.Add("  nPOTScale: 0")
    $pMeta.Add("  lightmap: 0")
    $pMeta.Add("  compressionQuality: 50")
    $pMeta.Add("  spriteMode: 2")
    $pMeta.Add("  spriteExtrude: 1")
    $pMeta.Add("  spriteMeshType: 1")
    $pMeta.Add("  alignment: 0")
    $pMeta.Add("  spritePivot: {x: 0.5, y: 0.5}")
    $pMeta.Add("  spritePixelsToUnits: 64")
    $pMeta.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}")
    $pMeta.Add("  spriteGenerateFallbackPhysicsShape: 1")
    $pMeta.Add("  alphaUsage: 1")
    $pMeta.Add("  alphaIsTransparency: 1")
    $pMeta.Add("  spriteTessellationDetail: -1")
    $pMeta.Add("  textureType: 8")
    $pMeta.Add("  textureShape: 1")
    $pMeta.Add("  singleChannelComponent: 0")
    $pMeta.Add("  flipbookRows: 1")
    $pMeta.Add("  flipbookColumns: 1")
    $pMeta.Add("  maxTextureSizeSet: 0")
    $pMeta.Add("  compressionQualitySet: 0")
    $pMeta.Add("  textureFormatSet: 0")
    $pMeta.Add("  ignorePngGamma: 0")
    $pMeta.Add("  applyGammaDecoding: 0")
    $pMeta.Add("  swizzle: 50462976")
    $pMeta.Add("  cookieLightType: 0")
    $pMeta.Add("  platformSettings:")
    $pMeta.Add("  - serializedVersion: 4")
    $pMeta.Add("    buildTarget: DefaultTexturePlatform")
    $pMeta.Add("    maxTextureSize: 2048")
    $pMeta.Add("    resizeAlgorithm: 0")
    $pMeta.Add("    textureFormat: -1")
    $pMeta.Add("    textureCompression: 0")
    $pMeta.Add("    compressionQuality: 50")
    $pMeta.Add("    crunchedCompression: 0")
    $pMeta.Add("    allowsAlphaSplitting: 0")
    $pMeta.Add("    overridden: 0")
    $pMeta.Add("    ignorePlatformSupport: 0")
    $pMeta.Add("    androidETC2FallbackOverride: 0")
    $pMeta.Add("    forceMaximumCompressionQuality_BC6H_BC7: 0")
    $pMeta.Add("  spriteSheet:")
    $pMeta.Add("    serializedVersion: 2")
    $pMeta.Add("    sprites:")
    for ($f = 0; $f -lt 6; $f++) {
        $xPos = $f * 128
        $hexId = "{0:d2}" -f ($id % 100)
        $sid = "7a0701" + $hexId + "000" + $f + "00000800000000000000"
        $pMeta.Add("    - serializedVersion: 2")
        $pMeta.Add("      name: ${texName}_${f}")
        $pMeta.Add("      rect:")
        $pMeta.Add("        serializedVersion: 2")
        $pMeta.Add("        x: $xPos")
        $pMeta.Add("        y: 0")
        $pMeta.Add("        width: 128")
        $pMeta.Add("        height: 128")
        $pMeta.Add("      alignment: 0")
        $pMeta.Add("      pivot: {x: 0.5, y: 0.5}")
        $pMeta.Add("      border: {x: 0, y: 0, z: 0, w: 0}")
        $pMeta.Add("      customData: ")
        $pMeta.Add("      outline: []")
        $pMeta.Add("      physicsShape: []")
        $pMeta.Add("      tessellationDetail: 0")
        $pMeta.Add("      bones: []")
        $pMeta.Add("      spriteID: $sid")
        $pMeta.Add("      internalID: ${id}10${f}")
        $pMeta.Add("      vertices: []")
        $pMeta.Add("      indices: ")
        $pMeta.Add("      edges: []")
        $pMeta.Add("      weights: []")
    }
    $pMeta.Add("    outline: []")
    $pMeta.Add("    customData: ")
    $pMeta.Add("    physicsShape: []")
    $pMeta.Add("    bones: []")
    $pMeta.Add("    spriteID: 5e97eb03825dee720800000000000000")
    $pMeta.Add("    internalID: 0")
    $pMeta.Add("    vertices: []")
    $pMeta.Add("    indices: ")
    $pMeta.Add("    edges: []")
    $pMeta.Add("    weights: []")
    $pMeta.Add("    secondaryTextures: []")
    $pMeta.Add("    spriteCustomMetadata:")
    $pMeta.Add("      entries: []")
    $pMeta.Add("    nameFileIdTable:")
    for ($f = 0; $f -lt 6; $f++) {
        $pMeta.Add("      ${texName}_${f}: ${id}10${f}")
    }
    $pMeta.Add("  mipmapLimitGroupName: ")
    $pMeta.Add("  pSDRemoveMatte: 0")
    $pMeta.Add("  userData: ")
    $pMeta.Add("  assetBundleName: ")
    $pMeta.Add("  assetBundleVariant: ")

    $projMetaPath = Join-Path $baseDir "Assets\4. DotAsset\3. Projectile\$texName.png.meta"
    [System.IO.File]::WriteAllLines($projMetaPath, $pMeta)
    Write-Output "  [1/2] Updated Spritesheet Meta: $projMetaPath"

    # 2. WRITE CLEAN PREFAB (Single Sprite, NO Animator, Pure Solid Linkage)
    $prefabPath = Join-Path $baseDir "Assets\Resources\Projectiles\$id.prefab"

    $prefabText = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &6552874700961141187
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 420065135903028965}
  - component: {fileID: 1911318586800383083}
  - component: {fileID: 283738656983380058}
  - component: {fileID: 1654271686611838669}
  - component: {fileID: 7342741669996071869}
  m_Layer: 7
  m_Name: $id
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &420065135903028965
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &1911318586800383083
SpriteRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 2100000, guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: -1018217311
  m_SortingLayer: 5
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_Sprite: {fileID: 21300000, guid: $singleGuid, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 1, y: 1}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_SpriteSortPoint: 0
--- !u!50 &283738656983380058
Rigidbody2D:
  serializedVersion: 5
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  m_BodyType: 0
  m_Simulated: 1
  m_UseFullKinematicContacts: 0
  m_UseAutoMass: 0
  m_Mass: 1
  m_LinearDamping: 0
  m_AngularDamping: 0.05
  m_GravityScale: 0
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_Interpolate: 0
  m_SleepingMode: 1
  m_CollisionDetection: 0
  m_Constraints: 0
--- !u!58 &1654271686611838669
CircleCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  m_Enabled: 1
  serializedVersion: 3
  m_Density: 1
  m_Material: {fileID: 0}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_CompositeOperation: 0
  m_CompositeOrder: 0
  m_Offset: {x: 0, y: 0}
  m_Radius: 0.04
--- !u!114 &7342741669996071869
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: aba85f42b9216844e8e0ac26fc731569, type: 3}
  m_Name: 
  m_EditorClassIdentifier: Assembly-CSharp::ProjectileHit2D_ObjectPool
  m_enemyLayer:
    serializedVersion: 2
    m_Bits: 256
  m_endLayer:
    serializedVersion: 2
    m_Bits: 512
  m_audioSource: {fileID: 0}
  m_sfxLayerMask:
    serializedVersion: 2
    m_Bits: 0
  lifeTime: 3
  IsSplash: 1
"@
    [System.IO.File]::WriteAllText($prefabPath, $prefabText)
    Write-Output "  [2/2] Saved Prefab: $prefabPath (Direct Single Sprite, NO Animator)"
}

Write-Output "`n=================================================="
Write-Output "Weapon Splash Assets (101..106) successfully rebuilt!"
Write-Output "=================================================="
