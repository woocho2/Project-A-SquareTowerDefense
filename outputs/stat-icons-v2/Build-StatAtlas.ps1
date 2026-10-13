$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskAsset = Join-Path $taskRoot 'Assets/4. Asset/5. UI/Stat_Icon_Atlas_v2.png'
$taskSource = 'C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-fd81032f-94b3-4607-bb79-73e1383106b8.png'
$taskUtf8 = New-Object Text.UTF8Encoding($false)
$taskEntries = @(
    @('Icon_Tower_AttackPower','1','공격력'),
    @('Icon_Tower_Range','5','사거리'),
    @('Icon_Tower_ActionInterval','9','행동력'),
    @('Icon_Tower_AttackCount','2','공격 횟수'),
    @('Icon_Tower_BonusAttackCount','3','추가 타격 횟수'),
    @('Icon_Tower_CriticalRate','7','치명타 확률'),
    @('Icon_Tower_CriticalDamage','8','치명타 피해'),
    @('Icon_Tower_AbilityValue','18','스킬 계수'),
    @('Icon_Tower_Mastery','19','특성'),
    @('Icon_Tower_Stack','20','스택'),
    @('Icon_Tower_AttackType','21','행동 방식'),
    @('Icon_Tower_Skill','22','스킬'),
    @('Icon_Tower_SplashRadius','6','범위 반경'),
    @('Icon_Tower_Duration','10','지속 시간'),
    @('Icon_Tower_AdaptiveStats','4','적응형 스탯'),
    @('Icon_Enemy_Health','11','적 체력'),
    @('Icon_Enemy_Defense','12','적 방어력'),
    @('Icon_Enemy_Resistance','13','적 저항력'),
    @('Icon_Enemy_MovementSpeed','14','적 이동속도'),
    @('Icon_Enemy_ActionInterval','15','적 행동력')
)
Add-Type -AssemblyName System.Drawing
$taskOriginal = New-Object Drawing.Bitmap($taskSource)
$taskAtlas = New-Object Drawing.Bitmap(1280,1024,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$taskGraphics = [Drawing.Graphics]::FromImage($taskAtlas)
$taskGraphics.Clear([Drawing.Color]::Transparent)
$taskGraphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
$taskGraphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$taskGraphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$taskAttributes = New-Object Drawing.Imaging.ImageAttributes
$taskAttributes.SetWrapMode([Drawing.Drawing2D.WrapMode]::TileFlipXY)
$taskManifest = @()
$taskBounds = @(
    @(30,30,240,250), @(288,20,278,277), @(588,30,228,256), @(824,32,301,252), @(1137,29,250,255),
    @(12,300,267,263), @(288,301,274,264), @(584,302,241,263), @(840,300,272,264), @(1170,297,192,264),
    @(25,567,263,253), @(288,562,292,260), @(584,565,280,256), @(908,565,190,256), @(1137,564,249,259),
    @(26,853,251,248), @(312,828,210,273), @(590,831,224,270), @(827,816,310,285), @(1140,824,249,277)
)
for($i=0;$i -lt 20;$i++) {
    $col=$i%5; $row=[int][Math]::Floor($i/5)
    # Content rectangles accommodate the generated sheet's uneven margins.
    # Preserve original alpha and aspect ratio; only crop, resize, and pack.
    $bounds=$taskBounds[$i];$scale=224.0/[Math]::Max($bounds[2],$bounds[3])
    $width=[int][Math]::Round($bounds[2]*$scale);$height=[int][Math]::Round($bounds[3]*$scale)
    $destination=New-Object Drawing.Rectangle(($col*256+[int][Math]::Floor((256-$width)/2)),($row*256+[int][Math]::Floor((256-$height)/2)),$width,$height)
    $taskGraphics.DrawImage($taskOriginal,$destination,$bounds[0],$bounds[1],$bounds[2],$bounds[3],[Drawing.GraphicsUnit]::Pixel,$taskAttributes)
    $taskManifest += [pscustomobject]@{name=$taskEntries[$i][0];fileID=('70000000000000000'+$taskEntries[$i][1].PadLeft(2,'0'));label=$taskEntries[$i][2];x=($col*256);y=((3-$row)*256);width=256;height=256}
}
$taskAtlas.Save($taskAsset,[Drawing.Imaging.ImageFormat]::Png)
$taskOriginal.Dispose();$taskAttributes.Dispose();$taskGraphics.Dispose()
$taskMeta = [IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/5. UI/Stat_Icon_Atlas.png.meta'))
$taskGuid = if(Test-Path -LiteralPath ($taskAsset+'.meta')){[regex]::Match([IO.File]::ReadAllText($taskAsset+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
$taskPrefix = $taskMeta.Substring(0,$taskMeta.IndexOf('  spriteSheet:'))
$taskPrefix = [regex]::Replace($taskPrefix,'(?m)^guid: \w+',"guid: $taskGuid")
$taskPrefix = $taskPrefix.Replace('textureCompression: 1','textureCompression: 0')
$taskSuffix = $taskMeta.Substring($taskMeta.IndexOf('  mipmapLimitGroupName:'))
$taskSheet = New-Object Text.StringBuilder
[void]$taskSheet.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
foreach($entry in $taskManifest){
    $spriteGuid=[guid]::NewGuid().ToString('N')
    [void]$taskSheet.AppendLine(@"
    - serializedVersion: 2
      name: $($entry.name)
      rect:
        serializedVersion: 2
        x: $($entry.x)
        y: $($entry.y)
        width: 256
        height: 256
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteGuid
      internalID: $($entry.fileID)
      vertices: []
      indices: 
      edges: []
      weights: []
"@)
}
[void]$taskSheet.AppendLine(@"
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
"@)
foreach($entry in $taskManifest){[void]$taskSheet.AppendLine("      $($entry.name): $($entry.fileID)")}
[IO.File]::WriteAllText($taskAsset+'.meta',$taskPrefix+$taskSheet.ToString()+$taskSuffix,$taskUtf8)
$taskManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Encoding UTF8
$taskPreview = New-Object Drawing.Bitmap(1000,960,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($taskPreview);$g.Clear([Drawing.Color]::FromArgb(255,26,32,44))
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$font=New-Object Drawing.Font('Malgun Gothic',15,[Drawing.FontStyle]::Bold)
$format=New-Object Drawing.StringFormat;$format.Alignment=[Drawing.StringAlignment]::Center
for($i=0;$i -lt 20;$i++){
    $col=$i%5;$row=[int][Math]::Floor($i/5);$entry=$taskManifest[$i]
    $g.DrawImage($taskAtlas,(New-Object Drawing.Rectangle(($col*200+10),($row*240),180,180)),($col*256),($row*256),256,256,[Drawing.GraphicsUnit]::Pixel)
    $rect=New-Object Drawing.RectangleF(($col*200),($row*240+186),200,45)
    $brush=if($row -eq 3){[Drawing.Brushes]::LightSkyBlue}else{[Drawing.Brushes]::Ivory}
    $g.DrawString($entry.label,$font,$brush,$rect,$format)
}
$taskPreview.Save((Join-Path $PSScriptRoot 'preview.png'),[Drawing.Imaging.ImageFormat]::Png)
$format.Dispose();$font.Dispose();$g.Dispose();$taskPreview.Dispose();$taskAtlas.Dispose()
Write-Output "Packaged 20 transparent 256x256 sprites in 1280x1024 atlas. GUID=$taskGuid"
