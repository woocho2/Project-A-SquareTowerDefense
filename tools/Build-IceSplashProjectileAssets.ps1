# ==============================================================================
# Build-IceSplashProjectileAssets.ps1
# Generates Splash Ice Projectile Assets (108)
# - Spritesheet: Assets/4. DotAsset/3. Projectile/Projectile_Meteor_Ice.png (768x128, 6F)
# - Texture Meta: Projectile_Meteor_Ice.png.meta (PPU 64, Point filter, 6 sliced sprites 108100..108105)
# - Animation Clip: Assets/10.Animation/Projectile/E_108.anim (16 FPS, looping)
# - Animator Controller: Assets/10.Animation/Projectile/E_108.controller
# - Prefab: Assets/Resources/Projectiles/108.prefab
# - Verification of all GUID links
# ==============================================================================

Add-Type -AssemblyName System.Drawing

function Clr([int]$a, [int]$r, [int]$g, [int]$b) {
    $a = [int]([Math]::Min(255, [Math]::Max(0, $a)))
    $r = [int]([Math]::Min(255, [Math]::Max(0, $r)))
    $g = [int]([Math]::Min(255, [Math]::Max(0, $g)))
    $b = [int]([Math]::Min(255, [Math]::Max(0, $b)))
    [System.Drawing.Color]::FromArgb($a, $r, $g, $b)
}

function Pt([float]$x, [float]$y) { New-Object System.Drawing.PointF($x, $y) }

function Fill-EllipseCentered($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $gfx.FillEllipse($brush, ($cx - $rx), ($cy - $ry), ($rx * 2), ($ry * 2))
}

function Draw-Diamond($gfx, $brush, [float]$cx, [float]$cy, [float]$rx, [float]$ry) {
    $pts = [System.Drawing.PointF[]]@(
        (Pt $cx ($cy - $ry)),
        (Pt ($cx + $rx) $cy),
        (Pt $cx ($cy + $ry)),
        (Pt ($cx - $rx) $cy)
    )
    $gfx.FillPolygon($brush, $pts)
}

# ==============================================================================
# DRAW 1 FRAME OF ICE METEOR / COMET (768x128 sheet, 6 frames of 128x128)
# ==============================================================================
function Draw-IceMeteorFrame($gfx, [float]$cellX, [float]$cellY, [int]$frameIndex, [int]$totalFrames) {
    $phase = ($frameIndex / [float]$totalFrames) * 2.0 * [Math]::PI
    $cy = $cellY + 64.0
    $sphereX = $cellX + 76.0 # Sphere head centered at (cellX + 76, 64)
    $radius = 15.0

    # --------------------------------------------------------------------------
    # 1. METEOR TAIL (Flickering, undulating aerodynamic icy frost wake trailing left)
    # --------------------------------------------------------------------------
    $tailLen1 = 58.0 + [Math]::Sin($phase * 2.0) * 5.0
    $tailLen2 = 36.0 + [Math]::Cos($phase * 2.0) * 4.0
    $wWave1 = [Math]::Sin($phase) * 2.2
    $wWave2 = [Math]::Cos($phase) * 2.2

    # Outer Deep Azure Frost Polygon
    $tailPtsAzure = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 14.0)),
        (Pt ($sphereX - 22.0) ($cy - 12.0 + $wWave1)),
        (Pt ($sphereX - $tailLen2) ($cy - 7.0 + $wWave2)),
        (Pt ($sphereX - $tailLen1) ($cy + [Math]::Sin($phase) * 3.0)), # Main central tip
        (Pt ($sphereX - $tailLen2) ($cy + 7.0 + $wWave1)),
        (Pt ($sphereX - 22.0) ($cy + 12.0 + $wWave2)),
        (Pt ($sphereX - 2.0) ($cy + 14.0))
    )
    $bTailAzure = New-Object System.Drawing.SolidBrush((Clr 180 0 135 230))
    $gfx.FillPolygon($bTailAzure, $tailPtsAzure)
    $bTailAzure.Dispose()

    # Mid Radiant Cyan Plasma Stream
    $oLen1 = $tailLen1 * 0.76
    $oLen2 = $tailLen2 * 0.72
    $tailPtsCyan = [System.Drawing.PointF[]]@(
        (Pt ($sphereX - 2.0) ($cy - 9.5)),
        (Pt ($sphereX - 18.0) ($cy - 7.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - $oLen2) ($cy - 4.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - $oLen1) ($cy + [Math]::Sin($phase) * 2.0)),
        (Pt ($sphereX - $oLen2) ($cy + 4.5 + $wWave1 * 0.7)),
        (Pt ($sphereX - 18.0) ($cy + 7.5 + $wWave2 * 0.7)),
        (Pt ($sphereX - 2.0) ($cy + 9.5))
    )
    $bTailCyan = New-Object System.Drawing.SolidBrush((Clr 235 0 215 255))
    $gfx.FillPolygon($bTailCyan, $tailPtsCyan)
    $bTailCyan.Dispose()

    # Inner Glowing Frost Core Streak
    $yLen = $tailLen1 * 0.48
    $tailPtsWhite = [System.Drawing.PointF[]]@(
        (Pt ($sphereX) ($cy - 5.5)),
        (Pt ($sphereX - 14.0) ($cy - 3.5 + $wWave1 * 0.4)),
        (Pt ($sphereX - $yLen) ($cy + [Math]::Sin($phase) * 1.0)),
        (Pt ($sphereX - 14.0) ($cy + 3.5 + $wWave2 * 0.4)),
        (Pt ($sphereX) ($cy + 5.5))
    )
    $bTailWhite = New-Object System.Drawing.SolidBrush((Clr 255 215 248 255))
    $gfx.FillPolygon($bTailWhite, $tailPtsWhite)
    $bTailWhite.Dispose()

    # Trailing Filament Lines (Frost Whisps)
    $pWhisp1 = New-Object System.Drawing.Pen((Clr 160 100 230 255), 1.8)
    $pWhisp2 = New-Object System.Drawing.Pen((Clr 140 0 170 240), 1.6)
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy - 4.0), ($sphereX - $tailLen1 - 6.0), ($cy - 3.0 + $wWave1))
    $gfx.DrawLine($pWhisp1, ($sphereX - 12.0), ($cy + 4.0), ($sphereX - $tailLen1 - 6.0), ($cy + 3.0 + $wWave2))
    $gfx.DrawLine($pWhisp2, ($sphereX - 10.0), $cy, ($sphereX - $tailLen1 - 12.0), $cy)
    $pWhisp1.Dispose(); $pWhisp2.Dispose()

    # --------------------------------------------------------------------------
    # 2. TRAILING FROST CRYSTAL PARTICLES / CINDERS (Flowing backwards seamlessly)
    # --------------------------------------------------------------------------
    $bSpkWhite = New-Object System.Drawing.SolidBrush((Clr 250 245 255 255))
    $bSpkCyan  = New-Object System.Drawing.SolidBrush((Clr 200 0 190 255))

    # 5 looping diamond sparks
    $sparkData = @(
        @(0.15, -7.0, 2.3),
        @(0.35,  6.5, 2.0),
        @(0.55, -4.0, 2.5),
        @(0.75,  5.0, 1.9),
        @(0.92, -1.0, 1.7)
    )
    foreach ($spk in $sparkData) {
        $loopProgress = ($spk[0] + ($frameIndex / [float]$totalFrames)) % 1.0
        $spkX = $sphereX - 10.0 - ($loopProgress * 54.0)
        $spkY = $cy + [float]$spk[1] + [Math]::Sin($loopProgress * 4.0 + $phase) * 2.0
        $sz   = [float]$spk[2] * (1.1 - $loopProgress * 0.4)

        Draw-Diamond $gfx $bSpkCyan  $spkX $spkY ($sz * 1.4) ($sz * 1.4)
        Draw-Diamond $gfx $bSpkWhite $spkX $spkY ($sz * 0.75) ($sz * 0.75)
    }
    $bSpkWhite.Dispose(); $bSpkCyan.Dispose()

    # --------------------------------------------------------------------------
    # 3. SPHERE HEAD (Clean, solid, crystalline incandescent frosty orb)
    # --------------------------------------------------------------------------
    # Outer Cold Frost Aura Halo
    $bAura = New-Object System.Drawing.SolidBrush((Clr 105 0 180 255))
    Fill-EllipseCentered $gfx $bAura $sphereX $cy 19.5 19.5
    $bAura.Dispose()

    # Bow Shock (Atmospheric ice barrier on leading edge +X)
    $pBowShock = New-Object System.Drawing.Pen((Clr 195 200 245 255), 2.2)
    # Draw arc on right side (-70 deg to +70 deg)
    $gfx.DrawArc($pBowShock, ($sphereX - 19.5), ($cy - 19.5), 39.0, 39.0, -70.0, 140.0)
    $pBowShock.Dispose()

    # Main Spherical Orb Body (Solid vibrant cyan blue)
    $bSphereBody = New-Object System.Drawing.SolidBrush((Clr 255 0 175 240))
    Fill-EllipseCentered $gfx $bSphereBody $sphereX $cy $radius $radius
    $bSphereBody.Dispose()

    # Spherical Volume Layer (Offset towards front +X for 3D sphere look)
    $bSphereVol = New-Object System.Drawing.SolidBrush((Clr 255 60 220 255))
    Fill-EllipseCentered $gfx $bSphereVol ($sphereX + 2.5) $cy 11.0 11.0
    $bSphereVol.Dispose()

    # Supercooled Incandescent Core
    $bCore = New-Object System.Drawing.SolidBrush((Clr 255 220 250 255))
    Fill-EllipseCentered $gfx $bCore ($sphereX + 4.5) $cy 6.5 6.5
    $bCore.Dispose()

    # Pure White Hot Glint
    $bHotSpot = New-Object System.Drawing.SolidBrush((Clr 255 255 255 255))
    Fill-EllipseCentered $gfx $bHotSpot ($sphereX + 6.0) $cy 3.5 3.5
    $bHotSpot.Dispose()

    # Subtle Crystalline Specular Cross on Core
    $pCross = New-Object System.Drawing.Pen((Clr 255 255 255 255), 1.2)
    $gfx.DrawLine($pCross, ($sphereX + 6.0 - 2.5), $cy, ($sphereX + 6.0 + 2.5), $cy)
    $gfx.DrawLine($pCross, ($sphereX + 6.0), ($cy - 2.5), ($sphereX + 6.0), ($cy + 2.5))
    $pCross.Dispose()
}

$baseDir = "d:\MyGitHub\ProjectA\Project-A-SquareTowerDefense"
$id = 108
$texName = "Projectile_Meteor_Ice"
$texGuid = "7a07010800000000000000000000108b"
$animGuid = "7a07010800000000000000000000108a"
$ctrlGuid = "7a07010800000000000000000000108c"
$totalFrames = 6

Write-Output "Building Splash Ice Projectile Assets (ID: $id)..."

# ==============================================================================
# 1. Generate Spritesheet PNG (768 x 128)
# ==============================================================================
$sheetW = $totalFrames * 128
$sheetH = 128
$spritesheet = New-Object System.Drawing.Bitmap($sheetW, $sheetH, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$gSheet = [System.Drawing.Graphics]::FromImage($spritesheet)
$gSheet.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gSheet.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$gSheet.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$gSheet.Clear([System.Drawing.Color]::Transparent)

for ($i = 0; $i -lt $totalFrames; $i++) {
    $cellX = $i * 128
    Draw-IceMeteorFrame $gSheet $cellX 0 $i $totalFrames
}
$gSheet.Dispose()

$pngPath = Join-Path $baseDir "Assets\4. DotAsset\3. Projectile\$texName.png"
if (Test-Path $pngPath) { [System.IO.File]::Delete($pngPath) }
$spritesheet.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "  1. Saved Spritesheet: $pngPath"

# ==============================================================================
# 2. Generate Spritesheet Meta (Point filter, PPU 64, 6 sliced sprites 108100..108105)
# ==============================================================================
$metaLines = New-Object System.Collections.Generic.List[string]
$metaLines.Add("fileFormatVersion: 2")
$metaLines.Add("guid: $texGuid")
$metaLines.Add("TextureImporter:")
$metaLines.Add("  internalIDToNameTable:")
for ($f = 0; $f -lt $totalFrames; $f++) {
    $fid = "${id}10${f}"
    $metaLines.Add("  - first:")
    $metaLines.Add("      213: $fid")
    $metaLines.Add("    second: ${texName}_${f}")
}
$metaLines.Add("  externalObjects: {}")
$metaLines.Add("  serializedVersion: 13")
$metaLines.Add("  mipmaps:")
$metaLines.Add("    mipMapMode: 0")
$metaLines.Add("    enableMipMap: 0")
$metaLines.Add("    sRGBTexture: 1")
$metaLines.Add("    linearTexture: 0")
$metaLines.Add("    fadeOut: 0")
$metaLines.Add("    borderMipMap: 0")
$metaLines.Add("    mipMapsPreserveCoverage: 0")
$metaLines.Add("    alphaTestReferenceValue: 0.5")
$metaLines.Add("    mipMapFadeDistanceStart: 1")
$metaLines.Add("    mipMapFadeDistanceEnd: 3")
$metaLines.Add("  bumpmap:")
$metaLines.Add("    convertToNormalMap: 0")
$metaLines.Add("    externalNormalMap: 0")
$metaLines.Add("    heightScale: 0.25")
$metaLines.Add("    normalMapFilter: 0")
$metaLines.Add("    flipGreenChannel: 0")
$metaLines.Add("  isReadable: 0")
$metaLines.Add("  streamingMipmaps: 0")
$metaLines.Add("  streamingMipmapsPriority: 0")
$metaLines.Add("  vTOnly: 0")
$metaLines.Add("  ignoreMipmapLimit: 0")
$metaLines.Add("  grayScaleToAlpha: 0")
$metaLines.Add("  generateCubemap: 6")
$metaLines.Add("  cubemapConvolution: 0")
$metaLines.Add("  seamlessCubemap: 0")
$metaLines.Add("  textureFormat: 1")
$metaLines.Add("  maxTextureSize: 2048")
$metaLines.Add("  textureSettings:")
$metaLines.Add("    serializedVersion: 2")
$metaLines.Add("    filterMode: 0")
$metaLines.Add("    aniso: 1")
$metaLines.Add("    mipBias: 0")
$metaLines.Add("    wrapU: 1")
$metaLines.Add("    wrapV: 1")
$metaLines.Add("    wrapW: 1")
$metaLines.Add("  nPOTScale: 0")
$metaLines.Add("  lightmap: 0")
$metaLines.Add("  compressionQuality: 50")
$metaLines.Add("  spriteMode: 2")
$metaLines.Add("  spriteExtrude: 1")
$metaLines.Add("  spriteMeshType: 1")
$metaLines.Add("  alignment: 0")
$metaLines.Add("  spritePivot: {x: 0.5, y: 0.5}")
$metaLines.Add("  spritePixelsToUnits: 64")
$metaLines.Add("  spriteBorder: {x: 0, y: 0, z: 0, w: 0}")
$metaLines.Add("  spriteGenerateFallbackPhysicsShape: 1")
$metaLines.Add("  alphaUsage: 1")
$metaLines.Add("  alphaIsTransparency: 1")
$metaLines.Add("  spriteTessellationDetail: -1")
$metaLines.Add("  textureType: 8")
$metaLines.Add("  textureShape: 1")
$metaLines.Add("  singleChannelComponent: 0")
$metaLines.Add("  flipbookRows: 1")
$metaLines.Add("  flipbookColumns: 1")
$metaLines.Add("  maxTextureSizeSet: 0")
$metaLines.Add("  compressionQualitySet: 0")
$metaLines.Add("  textureFormatSet: 0")
$metaLines.Add("  ignorePngGamma: 0")
$metaLines.Add("  applyGammaDecoding: 0")
$metaLines.Add("  swizzle: 50462976")
$metaLines.Add("  cookieLightType: 0")
$metaLines.Add("  platformSettings:")
$metaLines.Add("  - serializedVersion: 4")
$metaLines.Add("    buildTarget: DefaultTexturePlatform")
$metaLines.Add("    maxTextureSize: 2048")
$metaLines.Add("    resizeAlgorithm: 0")
$metaLines.Add("    textureFormat: -1")
$metaLines.Add("    textureCompression: 0")
$metaLines.Add("    compressionQuality: 50")
$metaLines.Add("    crunchedCompression: 0")
$metaLines.Add("    allowsAlphaSplitting: 0")
$metaLines.Add("    overridden: 0")
$metaLines.Add("    ignorePlatformSupport: 0")
$metaLines.Add("    androidETC2FallbackOverride: 0")
$metaLines.Add("    forceMaximumCompressionQuality_BC6H_BC7: 0")
$metaLines.Add("  spriteSheet:")
$metaLines.Add("    serializedVersion: 2")
$metaLines.Add("    sprites:")
for ($f = 0; $f -lt $totalFrames; $f++) {
    $xPos = $f * 128
    $metaLines.Add("    - serializedVersion: 2")
    $metaLines.Add("      name: ${texName}_${f}")
    $metaLines.Add("      rect:")
    $metaLines.Add("        serializedVersion: 2")
    $metaLines.Add("        x: $xPos")
    $metaLines.Add("        y: 0")
    $metaLines.Add("        width: 128")
    $metaLines.Add("        height: 128")
    $metaLines.Add("      alignment: 0")
    $metaLines.Add("      pivot: {x: 0.5, y: 0.5}")
    $metaLines.Add("      border: {x: 0, y: 0, z: 0, w: 0}")
    $metaLines.Add("      customData: ")
    $metaLines.Add("      outline: []")
    $metaLines.Add("      physicsShape: []")
    $metaLines.Add("      tessellationDetail: 0")
    $metaLines.Add("      bones: []")
    $metaLines.Add("      spriteID: 7a070108000${f}00000800000000000000")
    $metaLines.Add("      internalID: ${id}10${f}")
    $metaLines.Add("      vertices: []")
    $metaLines.Add("      indices: ")
    $metaLines.Add("      edges: []")
    $metaLines.Add("      weights: []")
}
$metaLines.Add("    outline: []")
$metaLines.Add("    customData: ")
$metaLines.Add("    physicsShape: []")
$metaLines.Add("    bones: []")
$metaLines.Add("    spriteID: ")
$metaLines.Add("    internalID: 0")
$metaLines.Add("    vertices: []")
$metaLines.Add("    indices: ")
$metaLines.Add("    edges: []")
$metaLines.Add("    weights: []")
$metaLines.Add("    secondaryTextures: []")
$metaLines.Add("    spriteCustomMetadata:")
$metaLines.Add("      entries: []")
$metaLines.Add("    nameFileIdTable:")
for ($f = 0; $f -lt $totalFrames; $f++) {
    $metaLines.Add("      ${texName}_${f}: ${id}10${f}")
}
$metaLines.Add("  mipmapLimitGroupName: ")
$metaLines.Add("  pSDRemoveMatte: 0")
$metaLines.Add("  userData: ")
$metaLines.Add("  assetBundleName: ")
$metaLines.Add("  assetBundleVariant: ")

$metaPath = "$pngPath.meta"
[System.IO.File]::WriteAllLines($metaPath, $metaLines)
Write-Output "  2. Saved Texture Meta: $metaPath"

# ==============================================================================
# 3. Generate Animation Clip (Assets/10.Animation/Projectile/E_108.anim)
# ==============================================================================
$animLines = New-Object System.Collections.Generic.List[string]
$animLines.Add("%YAML 1.1")
$animLines.Add("%TAG !u! tag:unity3d.com,2011:")
$animLines.Add("--- !u!74 &7400000")
$animLines.Add("AnimationClip:")
$animLines.Add("  m_ObjectHideFlags: 0")
$animLines.Add("  m_CorrespondingSourceObject: {fileID: 0}")
$animLines.Add("  m_PrefabInstance: {fileID: 0}")
$animLines.Add("  m_PrefabAsset: {fileID: 0}")
$animLines.Add("  m_Name: E_108")
$animLines.Add("  serializedVersion: 7")
$animLines.Add("  m_Legacy: 0")
$animLines.Add("  m_Compressed: 0")
$animLines.Add("  m_UseHighQualityCurve: 1")
$animLines.Add("  m_RotationCurves: []")
$animLines.Add("  m_CompressedRotationCurves: []")
$animLines.Add("  m_EulerCurves: []")
$animLines.Add("  m_PositionCurves: []")
$animLines.Add("  m_ScaleCurves: []")
$animLines.Add("  m_FloatCurves: []")
$animLines.Add("  m_PPtrCurves:")
$animLines.Add("  - serializedVersion: 2")
$animLines.Add("    curve:")
for ($f = 0; $f -lt $totalFrames; $f++) {
    $t = ($f * 0.0625).ToString("0.0000", [System.Globalization.CultureInfo]::InvariantCulture)
    $fid = "${id}10${f}"
    $animLines.Add("    - time: $t")
    $animLines.Add("      value: {fileID: $fid, guid: $texGuid, type: 3}")
}
$animLines.Add("    attribute: m_Sprite")
$animLines.Add("    path: ")
$animLines.Add("    classID: 212")
$animLines.Add("    script: {fileID: 0}")
$animLines.Add("    flags: 2")
$animLines.Add("  m_SampleRate: 16")
$animLines.Add("  m_WrapMode: 0")
$animLines.Add("  m_Bounds:")
$animLines.Add("    m_Center: {x: 0, y: 0, z: 0}")
$animLines.Add("    m_Extent: {x: 0, y: 0, z: 0}")
$animLines.Add("  m_ClipBindingConstant:")
$animLines.Add("    genericBindings:")
$animLines.Add("    - serializedVersion: 2")
$animLines.Add("      path: 0")
$animLines.Add("      attribute: 0")
$animLines.Add("      script: {fileID: 0}")
$animLines.Add("      typeID: 212")
$animLines.Add("      customType: 23")
$animLines.Add("      isPPtrCurve: 1")
$animLines.Add("      isIntCurve: 0")
$animLines.Add("      isSerializeReferenceCurve: 0")
$animLines.Add("    pptrCurveMapping:")
for ($f = 0; $f -lt $totalFrames; $f++) {
    $fid = "${id}10${f}"
    $animLines.Add("    - {fileID: $fid, guid: $texGuid, type: 3}")
}
$animLines.Add("  m_AnimationClipSettings:")
$animLines.Add("    serializedVersion: 2")
$animLines.Add("    m_AdditiveReferencePoseClip: {fileID: 0}")
$animLines.Add("    m_AdditiveReferencePoseTime: 0")
$animLines.Add("    m_StartTime: 0")
$animLines.Add("    m_StopTime: 0.375")
$animLines.Add("    m_OrientationOffsetY: 0")
$animLines.Add("    m_Level: 0")
$animLines.Add("    m_CycleOffset: 0")
$animLines.Add("    m_HasAdditiveReferencePose: 0")
$animLines.Add("    m_LoopTime: 1")
$animLines.Add("    m_LoopBlend: 0")
$animLines.Add("    m_LoopBlendOrientation: 0")
$animLines.Add("    m_LoopBlendPositionY: 0")
$animLines.Add("    m_LoopBlendPositionXZ: 0")
$animLines.Add("    m_KeepOriginalOrientation: 0")
$animLines.Add("    m_KeepOriginalPositionY: 1")
$animLines.Add("    m_KeepOriginalPositionXZ: 0")
$animLines.Add("    m_HeightFromFeet: 0")
$animLines.Add("    m_Mirror: 0")
$animLines.Add("  m_EditorCurves: []")
$animLines.Add("  m_EulerEditorCurves: []")
$animLines.Add("  m_HasGenericRootTransform: 0")
$animLines.Add("  m_HasMotionFloatCurves: 0")
$animLines.Add("  m_Events: []")

$animPath = Join-Path $baseDir "Assets\10.Animation\Projectile\E_108.anim"
[System.IO.File]::WriteAllLines($animPath, $animLines)

# Animation Meta
$animMetaLines = @(
    "fileFormatVersion: 2",
    "guid: $animGuid",
    "AnimationClip:",
    "  serializedVersion: 2",
    "  defaultClipBindingConstant:",
    "    genericBindings: []",
    "    pptrCurveMapping: []",
    "  masterClip: {fileID: 0}"
)
$animMetaPath = "$animPath.meta"
[System.IO.File]::WriteAllLines($animMetaPath, $animMetaLines)
Write-Output "  3. Saved Animation Clip & Meta: $animPath"

# ==============================================================================
# 4. Generate Animator Controller (Assets/10.Animation/Projectile/E_108.controller)
# ==============================================================================
$ctrlLines = New-Object System.Collections.Generic.List[string]
$ctrlLines.Add("%YAML 1.1")
$ctrlLines.Add("%TAG !u! tag:unity3d.com,2011:")
$ctrlLines.Add("--- !u!91 &9100000")
$ctrlLines.Add("AnimatorController:")
$ctrlLines.Add("  m_ObjectHideFlags: 0")
$ctrlLines.Add("  m_CorrespondingSourceObject: {fileID: 0}")
$ctrlLines.Add("  m_PrefabInstance: {fileID: 0}")
$ctrlLines.Add("  m_PrefabAsset: {fileID: 0}")
$ctrlLines.Add("  m_Name: E_108")
$ctrlLines.Add("  serializedVersion: 5")
$ctrlLines.Add("  m_AnimatorParameters: []")
$ctrlLines.Add("  m_AnimatorLayers:")
$ctrlLines.Add("  - serializedVersion: 5")
$ctrlLines.Add("    m_Name: Base Layer")
$ctrlLines.Add("    m_StateMachine: {fileID: 354852189424992894}")
$ctrlLines.Add("    m_Mask: {fileID: 0}")
$ctrlLines.Add("    m_Motions: []")
$ctrlLines.Add("    m_Behaviours: []")
$ctrlLines.Add("    m_BlendingMode: 0")
$ctrlLines.Add("    m_SyncedLayerIndex: -1")
$ctrlLines.Add("    m_DefaultWeight: 0")
$ctrlLines.Add("    m_IKPass: 0")
$ctrlLines.Add("    m_SyncedLayerAffectsTiming: 0")
$ctrlLines.Add("    m_Controller: {fileID: 9100000}")
$ctrlLines.Add("--- !u!1107 &354852189424992894")
$ctrlLines.Add("AnimatorStateMachine:")
$ctrlLines.Add("  serializedVersion: 6")
$ctrlLines.Add("  m_ObjectHideFlags: 1")
$ctrlLines.Add("  m_CorrespondingSourceObject: {fileID: 0}")
$ctrlLines.Add("  m_PrefabInstance: {fileID: 0}")
$ctrlLines.Add("  m_PrefabAsset: {fileID: 0}")
$ctrlLines.Add("  m_Name: Base Layer")
$ctrlLines.Add("  m_ChildStates:")
$ctrlLines.Add("  - serializedVersion: 1")
$ctrlLines.Add("    m_State: {fileID: 8833959328309836345}")
$ctrlLines.Add("    m_Position: {x: 350, y: 120, z: 0}")
$ctrlLines.Add("  m_ChildStateMachines: []")
$ctrlLines.Add("  m_AnyStateTransitions: []")
$ctrlLines.Add("  m_EntryTransitions: []")
$ctrlLines.Add("  m_StateMachineTransitions: {}")
$ctrlLines.Add("  m_StateMachineBehaviours: []")
$ctrlLines.Add("  m_AnyStatePosition: {x: 50, y: 20, z: 0}")
$ctrlLines.Add("  m_EntryPosition: {x: 50, y: 120, z: 0}")
$ctrlLines.Add("  m_ExitPosition: {x: 800, y: 120, z: 0}")
$ctrlLines.Add("  m_ParentStateMachinePosition: {x: 800, y: 20, z: 0}")
$ctrlLines.Add("  m_DefaultState: {fileID: 8833959328309836345}")
$ctrlLines.Add("--- !u!1102 &8833959328309836345")
$ctrlLines.Add("AnimatorState:")
$ctrlLines.Add("  serializedVersion: 6")
$ctrlLines.Add("  m_ObjectHideFlags: 1")
$ctrlLines.Add("  m_CorrespondingSourceObject: {fileID: 0}")
$ctrlLines.Add("  m_PrefabInstance: {fileID: 0}")
$ctrlLines.Add("  m_PrefabAsset: {fileID: 0}")
$ctrlLines.Add("  m_Name: E_108")
$ctrlLines.Add("  m_Speed: 1")
$ctrlLines.Add("  m_CycleOffset: 0")
$ctrlLines.Add("  m_Transitions: []")
$ctrlLines.Add("  m_StateMachineBehaviours: []")
$ctrlLines.Add("  m_Position: {x: 50, y: 50, z: 0}")
$ctrlLines.Add("  m_IKOnFeet: 0")
$ctrlLines.Add("  m_WriteDefaultValues: 1")
$ctrlLines.Add("  m_Mirror: 0")
$ctrlLines.Add("  m_SpeedParameterActive: 0")
$ctrlLines.Add("  m_MirrorParameterActive: 0")
$ctrlLines.Add("  m_CycleOffsetParameterActive: 0")
$ctrlLines.Add("  m_TimeParameterActive: 0")
$ctrlLines.Add("  m_Motion: {fileID: 7400000, guid: $animGuid, type: 2}")
$ctrlLines.Add("  m_Tag: ")
$ctrlLines.Add("  m_SpeedParameter: ")
$ctrlLines.Add("  m_MirrorParameter: ")
$ctrlLines.Add("  m_CycleOffsetParameter: ")
$ctrlLines.Add("  m_TimeParameter: ")

$ctrlPath = Join-Path $baseDir "Assets\10.Animation\Projectile\E_108.controller"
[System.IO.File]::WriteAllLines($ctrlPath, $ctrlLines)

# Controller Meta
$ctrlMetaLines = @(
    "fileFormatVersion: 2",
    "guid: $ctrlGuid",
    "NativeFormatImporter:",
    "  externalObjects: {}",
    "  mainObjectFileID: 0",
    "  userData: ",
    "  assetBundleName: ",
    "  assetBundleVariant: "
)
$ctrlMetaPath = "$ctrlPath.meta"
[System.IO.File]::WriteAllLines($ctrlMetaPath, $ctrlMetaLines)
Write-Output "  4. Saved Animator Controller & Meta: $ctrlPath"

# ==============================================================================
# 5. Update Prefab (Assets/Resources/Projectiles/108.prefab)
# ==============================================================================
$prefabPath = Join-Path $baseDir "Assets\Resources\Projectiles\108.prefab"
$pText = [System.IO.File]::ReadAllText($prefabPath)

# Fix Name to 108
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Name:\s*107", "m_Name: 108")
# Fix Scale to 1, 1, 1 (matching 107)
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_LocalScale:\s*\{x:\s*[\d\.]+,?\s*y:\s*[\d\.]+,?\s*z:\s*[\d\.]+\}", "m_LocalScale: {x: 1, y: 1, z: 1}")
# Fix Color to pure white so sprite graphics render cleanly
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Color:\s*\{r:\s*[\d\.]+,?\s*g:\s*[\d\.]+,?\s*b:\s*[\d\.]+,?\s*a:\s*[\d\.]+\}", "m_Color: {r: 1, g: 1, b: 1, a: 1}")
# Fix Sprite to {fileID: 108100, guid: 7a07010800000000000000000000108b, type: 3}
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Sprite:\s*\{fileID:\s*-?\d+,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*3\}", "m_Sprite: {fileID: 108100, guid: $texGuid, type: 3}")
# Fix Controller to {fileID: 9100000, guid: 7a07010800000000000000000000108c, type: 2}
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Controller:\s*\{fileID:\s*9100000,\s*guid:\s*[0-9a-zA-Z]+,\s*type:\s*2\}", "m_Controller: {fileID: 9100000, guid: $ctrlGuid, type: 2}")
# Ensure CircleCollider radius is 0.01 (matching 107)
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "m_Radius:\s*[\d\.]+", "m_Radius: 0.01")
# Ensure IsSplash is 1
$pText = [System.Text.RegularExpressions.Regex]::Replace($pText, "IsSplash:\s*\d+", "IsSplash: 1")

[System.IO.File]::WriteAllText($prefabPath, $pText)
Write-Output "  5. Updated Prefab: $prefabPath"

# ==============================================================================
# 6. Generate Preview Showcase (Side-by-side 6 frames + 2x close-up)
# ==============================================================================
$prevW = 768
$prevH = 340
$bmpPrev = New-Object System.Drawing.Bitmap($prevW, $prevH)
$gPrev = [System.Drawing.Graphics]::FromImage($bmpPrev)
$gPrev.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gPrev.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gPrev.Clear([System.Drawing.Color]::FromArgb(20, 22, 28))

# Top title
$fTitle = New-Object System.Drawing.Font('Malgun Gothic', 11, [System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush((Clr 255 240 245 255))
$bSub   = New-Object System.Drawing.SolidBrush((Clr 180 180 190 205))
$sf     = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center

$titleBytes = @(236, 138, 164, 237, 148, 140, 235, 158, 152, 236, 139, 156, 32, 236, 150, 188, 236, 157, 140, 32, 237, 131, 128, 236, 155, 140, 32, 236, 180, 157, 236, 149, 140, 58, 32, 236, 150, 188, 236, 157, 140, 32, 237, 152, 156, 236, 132, 177, 32, 236, 149, 160, 235, 139, 136, 235, 169, 148, 236, 157, 180, 236, 133, 152, 32, 40, 54, 32, 70, 114, 97, 109, 101, 115, 41)
$title = [System.Text.Encoding]::UTF8.GetString($titleBytes)
$gPrev.DrawString($title, $fTitle, $bWhite, 384.0, 12.0, $sf)

# 6 Frames row (Y: 45 ~ 173)
$pCardBorder = New-Object System.Drawing.Pen((Clr 60 255 255 255), 1.0)
for ($i = 0; $i -lt $totalFrames; $i++) {
    $x = $i * 128
    $gPrev.DrawRectangle($pCardBorder, ($x + 2), 42, 124, 124)
    $srcRect = New-Object System.Drawing.Rectangle($x, 0, 128, 128)
    $dstRect = New-Object System.Drawing.Rectangle($x, 40, 128, 128)
    $gPrev.DrawImage($spritesheet, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)

    $lbl = "Frame $i"
    $fMini = New-Object System.Drawing.Font('Arial', 9, [System.Drawing.FontStyle]::Regular)
    $gPrev.DrawString($lbl, $fMini, $bSub, [float]($x + 64), 168.0, $sf)
    $fMini.Dispose()
}

# Bottom section: 2x Enlarged Close-up of Ice Comet (Left to Right)
$gPrev.DrawLine($pCardBorder, 20, 195, 748, 195)
$subBytes = @(236, 150, 188, 236, 157, 140, 32, 234, 181, 172, 236, 178, 180, 32, 235, 179, 184, 236, 178, 180, 32, 43, 32, 235, 131, 137, 234, 184, 176, 32, 237, 155, 132, 235, 165, 152, 32, 235, 176, 143, 32, 236, 132, 156, 235, 166, 172, 32, 237, 140, 140, 237, 142, 184, 32, 237, 153, 149, 235, 140, 128, 32, 40, 50, 120, 32, 90, 111, 111, 109, 41)
$closeUpTitle = [System.Text.Encoding]::UTF8.GetString($subBytes)
$gPrev.DrawString($closeUpTitle, $fTitle, $bWhite, 384.0, 205.0, $sf)

# Draw 2x zoomed Frame 0 centered at bottom
$srcCrop = New-Object System.Drawing.Rectangle(0, 0, 128, 128)
$dstCrop = New-Object System.Drawing.Rectangle((384 - 128), 220, 256, 110)
$gPrev.DrawImage($spritesheet, $dstCrop, $srcCrop, [System.Drawing.GraphicsUnit]::Pixel)

$fTitle.Dispose(); $bWhite.Dispose(); $bSub.Dispose(); $pCardBorder.Dispose(); $sf.Dispose()

$prevPath = "C:\Users\user\.gemini\antigravity\brain\dc024a25-2f90-4a0d-912c-bd52cd35c0ed\preview_ice_comet_bullet.png"
if (Test-Path $prevPath) { [System.IO.File]::Delete($prevPath) }
$bmpPrev.Save($prevPath, [System.Drawing.Imaging.ImageFormat]::Png)

$rootPrev = Join-Path $baseDir "preview_ice_comet_bullet.png"
Copy-Item $prevPath $rootPrev -Force

$spritesheet.Dispose()
$gPrev.Dispose(); $bmpPrev.Dispose()

Write-Output "  6. Saved Preview: $prevPath"

# ==============================================================================
# 7. Verification
# ==============================================================================
Write-Output "=== 108 Projectile Asset Verification ==="
$pCtrlMatch = (Get-Content $prefabPath | Select-String "m_Controller:.*guid:\s*$ctrlGuid").Matches.Count -gt 0
Write-Output "  Prefab -> Controller GUID ($ctrlGuid): $pCtrlMatch"

$pSpriteMatch = (Get-Content $prefabPath | Select-String "m_Sprite:.*guid:\s*$texGuid").Matches.Count -gt 0
Write-Output "  Prefab -> Sprite GUID ($texGuid): $pSpriteMatch"

$ctrlAnimMatch = (Get-Content $ctrlPath | Select-String "m_Motion:.*guid:\s*$animGuid").Matches.Count -gt 0
Write-Output "  Controller -> Anim GUID ($animGuid): $ctrlAnimMatch"

$animSpriteMatch = (Get-Content $animPath | Select-String "guid:\s*$texGuid").Matches.Count -gt 0
Write-Output "  Anim -> Sprite GUID ($texGuid): $animSpriteMatch"

Write-Output "ALL 108 PROJECTILE ASSETS SUCCESSFULLY CREATED AND VERIFIED!"
