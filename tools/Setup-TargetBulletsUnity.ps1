# ==============================================================================
# Setup-TargetBulletsUnity.ps1
# Automatically configures .meta, .anim, .controller, and prefabs for 208..213
# ==============================================================================

$configs = @(
    @{ Id = 208; Name = "Icicle_Small";      FileBase = "Projectile_Icicle_Small" },
    @{ Id = 209; Name = "Zeus_Thunderbolt";  FileBase = "Projectile_Zeus_Thunderbolt" },
    @{ Id = 210; Name = "Wind_Orb";          FileBase = "Projectile_Wind_Orb" },
    @{ Id = 211; Name = "Earth_Orb";         FileBase = "Projectile_Earth_Orb" },
    @{ Id = 212; Name = "Light_Laser";       FileBase = "Projectile_Light_Laser" },
    @{ Id = 213; Name = "Dark_Orb";          FileBase = "Projectile_Dark_Orb" }
)

foreach ($cfg in $configs) {
    $id = $cfg.Id
    $fb = $cfg.FileBase
    
    $guidPng        = "7a070${id}0000000000000000000${id}b"
    $guidAnim       = "7a070${id}0000000000000000000${id}a"
    $guidController = "7a070${id}0000000000000000000${id}c"
    
    $firstSpriteId = [int]("${id}100")

    # 1. Texture Meta
    $metaContent = @"
fileFormatVersion: 2
guid: $guidPng
TextureImporter:
  internalIDToNameTable:
  - first:
      213: ${id}100
    second: ${fb}_0
  - first:
      213: ${id}101
    second: ${fb}_1
  - first:
      213: ${id}102
    second: ${fb}_2
  - first:
      213: ${id}103
    second: ${fb}_3
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: ${fb}_0
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: 128
        height: 128
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: 7a070${id}000000000800000000000000
      internalID: ${id}100
      vertices: []
      indices: 
      edges: []
      weights: []
    - serializedVersion: 2
      name: ${fb}_1
      rect:
        serializedVersion: 2
        x: 128
        y: 0
        width: 128
        height: 128
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: 7a070${id}000100000800000000000000
      internalID: ${id}101
      vertices: []
      indices: 
      edges: []
      weights: []
    - serializedVersion: 2
      name: ${fb}_2
      rect:
        serializedVersion: 2
        x: 256
        y: 0
        width: 128
        height: 128
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: 7a070${id}000200000800000000000000
      internalID: ${id}102
      vertices: []
      indices: 
      edges: []
      weights: []
    - serializedVersion: 2
      name: ${fb}_3
      rect:
        serializedVersion: 2
        x: 384
        y: 0
        width: 128
        height: 128
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: 7a070${id}000300000800000000000000
      internalID: ${id}103
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
      ${fb}_0: ${id}100
      ${fb}_1: ${id}101
      ${fb}_2: ${id}102
      ${fb}_3: ${id}103
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -Path "Assets/4. DotAsset/${fb}.png.meta" -Value $metaContent -Encoding UTF8
    Write-Output "Created Assets/4. DotAsset/${fb}.png.meta"

    # 2. Animation Clip
    $animContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Projectile_${id}
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
    - time: 0
      value: {fileID: ${id}100, guid: $guidPng, type: 3}
    - time: 0.083333336
      value: {fileID: ${id}101, guid: $guidPng, type: 3}
    - time: 0.16666667
      value: {fileID: ${id}102, guid: $guidPng, type: 3}
    - time: 0.25
      value: {fileID: ${id}103, guid: $guidPng, type: 3}
    attribute: m_Sprite
    path: 
    classID: 212
    script: {fileID: 0}
    flags: 2
  m_SampleRate: 12
  m_WrapMode: 0
  m_Bounds:
    m_Center: {x: 0, y: 0, z: 0}
    m_Extent: {x: 0, y: 0, z: 0}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {fileID: 0}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
    - {fileID: ${id}100, guid: $guidPng, type: 3}
    - {fileID: ${id}101, guid: $guidPng, type: 3}
    - {fileID: ${id}102, guid: $guidPng, type: 3}
    - {fileID: ${id}103, guid: $guidPng, type: 3}
  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {fileID: 0}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: 0.33333334
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 1
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
"@
    Set-Content -Path "Assets/10.Animation/Projectile_${id}.anim" -Value $animContent -Encoding UTF8
    
    $animMeta = @"
fileFormatVersion: 2
guid: $guidAnim
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 7400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -Path "Assets/10.Animation/Projectile_${id}.anim.meta" -Value $animMeta -Encoding UTF8
    Write-Output "Created Assets/10.Animation/Projectile_${id}.anim and meta"

    # 3. Animator Controller
    $ctrlContent = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!91 &9100000
AnimatorController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Projectile_${id}
  serializedVersion: 5
  m_AnimatorParameters: []
  m_AnimatorLayers:
  - serializedVersion: 5
    m_Name: Base Layer
    m_StateMachine: {fileID: 354852189424992894}
    m_Mask: {fileID: 0}
    m_Motions: []
    m_Behaviours: []
    m_BlendingMode: 0
    m_SyncedLayerIndex: -1
    m_DefaultWeight: 0
    m_IKPass: 0
    m_SyncedLayerAffectsTiming: 0
    m_Controller: {fileID: 9100000}
--- !u!1107 &354852189424992894
AnimatorStateMachine:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Base Layer
  m_ChildStates:
  - serializedVersion: 1
    m_State: {fileID: 8833959328309836345}
    m_Position: {x: 350, y: 120, z: 0}
  m_ChildStateMachines: []
  m_AnyStateTransitions: []
  m_EntryTransitions: []
  m_StateMachineTransitions: {}
  m_StateMachineBehaviours: []
  m_AnyStatePosition: {x: 50, y: 20, z: 0}
  m_EntryPosition: {x: 50, y: 120, z: 0}
  m_ExitPosition: {x: 800, y: 120, z: 0}
  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}
  m_DefaultState: {fileID: 8833959328309836345}
--- !u!1102 &8833959328309836345
AnimatorState:
  serializedVersion: 6
  m_ObjectHideFlags: 1
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Projectile_${id}
  m_Speed: 1
  m_CycleOffset: 0
  m_Transitions: []
  m_StateMachineBehaviours: []
  m_Position: {x: 50, y: 50, z: 0}
  m_IKOnFeet: 0
  m_WriteDefaultValues: 1
  m_Mirror: 0
  m_SpeedParameterActive: 0
  m_MirrorParameterActive: 0
  m_CycleOffsetParameterActive: 0
  m_TimeParameterActive: 0
  m_Motion: {fileID: 7400000, guid: $guidAnim, type: 2}
  m_Tag: 
  m_SpeedParameter: 
  m_MirrorParameter: 
  m_CycleOffsetParameter: 
  m_TimeParameter: 
"@
    Set-Content -Path "Assets/10.Animation/Projectile_${id}.controller" -Value $ctrlContent -Encoding UTF8
    
    $ctrlMeta = @"
fileFormatVersion: 2
guid: $guidController
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 9100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Set-Content -Path "Assets/10.Animation/Projectile_${id}.controller.meta" -Value $ctrlMeta -Encoding UTF8
    Write-Output "Created Assets/10.Animation/Projectile_${id}.controller and meta"

    # 4. Prefab Update
    $prefabContent = @"
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
  - component: {fileID: 5979125414150266392}
  m_Layer: 7
  m_Name: ${id}
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
  m_Sprite: {fileID: ${id}100, guid: $guidPng, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 0.1, y: 0.1}
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
  m_Radius: 0.06
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
  IsSplash: 0
--- !u!95 &5979125414150266392
Animator:
  serializedVersion: 7
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6552874700961141187}
  m_Enabled: 1
  m_Avatar: {fileID: 0}
  m_Controller: {fileID: 9100000, guid: $guidController, type: 2}
  m_CullingMode: 0
  m_UpdateMode: 0
  m_ApplyRootMotion: 0
  m_LinearVelocityBlending: 0
  m_StabilizeFeet: 0
  m_AnimatePhysics: 0
  m_WarningMessage: 
  m_HasTransformHierarchy: 1
  m_AllowConstantClipSamplingOptimization: 1
  m_KeepAnimatorStateOnDisable: 0
  m_WriteDefaultValuesOnDisable: 0
"@
    Set-Content -Path "Assets/Resources/Projectiles/${id}.prefab" -Value $prefabContent -Encoding UTF8
    Write-Output "Updated Assets/Resources/Projectiles/${id}.prefab"
}

Write-Output "All target projectiles 208..213 successfully configured in Unity!"
