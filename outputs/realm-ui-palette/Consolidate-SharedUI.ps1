$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskUi=Join-Path $taskRepo 'Assets/4. Asset/5. UI'
$taskUtf8=New-Object System.Text.UTF8Encoding($false)
function Write-UIFile([string]$path,[string]$value){[IO.File]::WriteAllText($path,$value,$taskUtf8)}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class SharedUIBake {
    public static void Bake(string[] paths,string output){
        using(var first=new Bitmap(paths[0]))using(var atlas=new Bitmap(first.Width*2,first.Height*2,PixelFormat.Format32bppArgb))
        using(var graphics=Graphics.FromImage(atlas)){
            graphics.CompositingMode=System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            for(int layer=0;layer<4;layer++)using(var input=new Bitmap(paths[layer])){
                var pixels=input.Clone(new Rectangle(0,0,input.Width,input.Height),PixelFormat.Format32bppArgb);
                var data=pixels.LockBits(new Rectangle(0,0,pixels.Width,pixels.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
                var bytes=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
                long[] histogram=new long[256];long count=0;
                for(int p=0;p<bytes.Length;p+=4)if(bytes[p+3]>=160){histogram[(int)Math.Round(.114*bytes[p]+.587*bytes[p+1]+.299*bytes[p+2])]++;count++;}
                long sum=0;int anchor=1;for(int n=1;n<256;n++){sum+=histogram[n];if(sum>=count/2){anchor=n;break;}}
                for(int p=0;p<bytes.Length;p+=4){double luma=.114*bytes[p]+.587*bytes[p+1]+.299*bytes[p+2];
                    double shade=1+(luma/anchor-1)*(layer==3?.8:.12);
                    byte gray=(byte)Math.Max(0,Math.Min(255,Math.Round(255*shade)));bytes[p]=bytes[p+1]=bytes[p+2]=gray;
                }
                Marshal.Copy(bytes,0,data.Scan0,bytes.Length);pixels.UnlockBits(data);
                graphics.DrawImageUnscaled(pixels,(layer%2)*first.Width,(layer/2)*first.Height);pixels.Dispose();
            }
            atlas.Save(output,ImageFormat.Png);
        }
    }
}
'@
$taskShared=Join-Path $taskUi 'Shared_UI_Atlas.png'
$taskExisting=Test-Path -LiteralPath ($taskShared+'.meta')
$taskLayers=@('A','B','C','Frame');$taskNewGuid=if($taskExisting){[regex]::Match([IO.File]::ReadAllText($taskShared+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
$taskSources=@($taskLayers | ForEach-Object {Join-Path $taskUi "Themes/Asgard/Asgard_Atlas_$_.png"})
$taskInput=New-Object Drawing.Bitmap($taskSources[0]);$taskWidth=$taskInput.Width;$taskHeight=$taskInput.Height;$taskInput.Dispose()
if(!$taskExisting){[SharedUIBake]::Bake($taskSources,$taskShared)}
$taskSprites=@();$taskByKey=@{};$taskRefs=@{};$taskDelete=@()
$taskSpritePattern='(?ms)^    - serializedVersion: 2\r?\n      name: ([^\r\n]+)\r?\n      rect:.*?(?=^    - serializedVersion: 2|^    outline:)'
for($taskLayerIndex=0;$taskLayerIndex -lt 4;$taskLayerIndex++){
    $taskLayer=$taskLayers[$taskLayerIndex];$taskMeta=[IO.File]::ReadAllText($taskSources[$taskLayerIndex]+'.meta');$taskNumber=0
    foreach($taskMatch in [regex]::Matches($taskMeta,$taskSpritePattern)){
        $taskBlock=$taskMatch.Value;$taskSuffix=($taskMatch.Groups[1].Value -split '_',3)[2]
        $taskName="Shared_${taskLayer}_$taskSuffix";$taskId=2001+$taskLayerIndex*100+$taskNumber;$taskNumber++
        $taskX=[int][regex]::Match($taskBlock,'(?m)^        x: (\d+)').Groups[1].Value
        $taskY=[int][regex]::Match($taskBlock,'(?m)^        y: (\d+)').Groups[1].Value
        $taskBlock=[regex]::Replace($taskBlock,'(?m)^      name: [^\r\n]+',"      name: $taskName")
        $taskBlock=[regex]::Replace($taskBlock,'(?m)^        x: \d+',('        x: '+($taskX+($taskLayerIndex%2)*$taskWidth)))
        $taskBlock=[regex]::Replace($taskBlock,'(?m)^        y: \d+',('        y: '+($taskY+(1-[int][Math]::Floor($taskLayerIndex/2))*$taskHeight)))
        $taskBlock=[regex]::Replace($taskBlock,'(?m)^      internalID: -?\d+',"      internalID: $taskId")
        $taskBlock=[regex]::Replace($taskBlock,'(?m)^      spriteID: [a-f0-9]+',('      spriteID: '+[guid]::NewGuid().ToString('N')))
        $taskSprites+=$taskBlock
        $taskByKey[$taskLayer+'_'+$taskSuffix]=[pscustomobject]@{id=$taskId;name=$taskName;layer=$taskLayer}
    }
}
if($taskByKey.Count -ne 60){throw 'Expected all sixty native UI shapes'}
$taskMeta=[IO.File]::ReadAllText($taskSources[0]+'.meta')
$taskMeta=[regex]::Replace($taskMeta,'(?m)^guid: [a-f0-9]+',"guid: $taskNewGuid")
$taskMeta=[regex]::Replace($taskMeta,'spritePixelsToUnits: \d+','spritePixelsToUnits: 128')
$taskMeta=[regex]::Replace($taskMeta,'maxTextureSize: \d+','maxTextureSize: 4096')
$taskTable=($taskByKey.Values | Sort-Object id | ForEach-Object {"      $($_.name): $($_.id)"}) -join "`n"
$taskSheet="  spriteSheet:`n    serializedVersion: 2`n    sprites:`n"+($taskSprites -join '')+"    outline: []`n    physicsShape: []`n    bones: []`n    spriteID: $([guid]::NewGuid().ToString('N'))`n    internalID: 0`n    vertices: []`n    indices:`n    edges: []`n    weights: []`n    secondaryTextures: []`n    nameFileIdTable:`n$taskTable`n"
$taskMeta=[regex]::Replace($taskMeta,'(?ms)^  spriteSheet:.*?(?=^  mipmapLimitGroupName:)',$taskSheet)
if(!$taskExisting){Write-UIFile ($taskShared+'.meta') $taskMeta}
$taskOldAtlases=@(Get-ChildItem -LiteralPath (Join-Path $taskUi 'Themes') -Recurse -File -Filter '*_Atlas_*.png')
$taskLegacyFolder=Join-Path $taskUi 'Asgard'
if(!(Test-Path -LiteralPath $taskLegacyFolder)){$taskLegacyFolder=Join-Path $PSScriptRoot 'source/Assets/4. Asset/5. UI/Asgard'}
if(Test-Path -LiteralPath $taskLegacyFolder){$taskOldAtlases+=@(Get-ChildItem -LiteralPath $taskLegacyFolder -File -Filter '*_Atlas_*.png')}
foreach($taskAtlas in $taskOldAtlases){
    $taskLayer=($taskAtlas.BaseName -split '_Atlas_')[1]
    $taskLayer=switch($taskLayer){'Ivory'{'A'}'Gold'{'B'}'Night'{'C'}default{$taskLayer}}
    if($taskLayer -notin $taskLayers){throw 'Unknown old UI layer'}
    $taskOldMeta=[IO.File]::ReadAllText($taskAtlas.FullName+'.meta');$taskOldGuid=[regex]::Match($taskOldMeta,'(?m)^guid: (\w+)').Groups[1].Value
    $taskRefs[$taskOldGuid]=@{}
    foreach($taskMatch in [regex]::Matches($taskOldMeta,$taskSpritePattern)){
        $taskOldId=[regex]::Match($taskMatch.Value,'internalID: (-?\d+)').Groups[1].Value
        $taskSuffix=($taskMatch.Groups[1].Value -split '_',3)[2]
        $taskRefs[$taskOldGuid][$taskOldId]=$taskByKey[$taskLayer+'_'+$taskSuffix]
        if(!$taskRefs[$taskOldGuid][$taskOldId]){throw 'Missing common shape for legacy sprite'}
    }
    if($taskAtlas.FullName.StartsWith($taskUi+'\',[StringComparison]::OrdinalIgnoreCase)){$taskDelete+=$taskAtlas.FullName}
}
$taskShapes=@{'1x1'='Btn_128';'1x1_Sliced'='Btn_128_B';'1x2'='Capsule_128x256';'1x3'='Gauge_128x384';'1x4'='TallBanner_128x512';'2x3'='CharCard_256x384';'2x1'='Capsule_256x128';'3x1'='Capsule_384x128';'4x1'='Capsule_512x128';'3x2'='SkillCard_384x256'}
foreach($taskAsset in (Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'assets.json') -Raw | ConvertFrom-Json)){
    $taskParts=$taskAsset.key -split '_',2;$taskRole=$taskParts[0];$taskSuffix=$taskShapes[$taskParts[1]]
    $taskRefs[$taskAsset.guid]=@{'21300000'=$taskByKey[$taskRole+'_'+$taskSuffix]}
    $taskOldPath=Join-Path $taskRepo $taskAsset.path
    if(Test-Path -LiteralPath $taskOldPath){$taskDelete+=$taskOldPath}
}
$taskGuidFile=Join-Path $PSScriptRoot 'migration-guids.txt';Write-UIFile $taskGuidFile (($taskRefs.Keys -join "`n")+"`n")
$taskFiles=@(& rg -l -F -f $taskGuidFile (Join-Path $taskRepo 'Assets') -g '!*.meta' -g '*.prefab' -g '*.unity' -g '*.asset')
if($LASTEXITCODE -gt 1){throw 'UI reference search failed'}
$taskPalette=Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'palette.json') -Raw | ConvertFrom-Json
function RGB([string]$hex){$color=[Drawing.ColorTranslator]::FromHtml($hex);return @(($color.R/255.0),($color.G/255.0),($color.B/255.0))}
$taskPresetRGB=@(,@(1.0,1.0,1.0));foreach($taskPreset in $taskPalette){foreach($taskHex in @($taskPreset.light,$taskPreset.dark,$taskPreset.accent)){$taskPresetRGB+=,@(RGB $taskHex)}}
$taskReferences=0;$taskCustomColors=0
foreach($taskFile in $taskFiles){
    $taskText=[IO.File]::ReadAllText($taskFile);$taskManual=@()
    $taskRealmMatch=[regex]::Match($taskText,'(?m)^  currentRealm: (\d+)');$taskRealm=if($taskRealmMatch.Success){[int]$taskRealmMatch.Groups[1].Value}else{0}
    $taskText=[regex]::Replace($taskText,'\{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: [23]\}',[Text.RegularExpressions.MatchEvaluator]{param($m)
        $guid=$m.Groups[2].Value;$id=$m.Groups[1].Value
        if(!$taskRefs.ContainsKey($guid)){return $m.Value}
        $entry=$taskRefs[$guid][$id];if(!$entry){throw ('Unsupported old UI reference: '+$m.Value)}
        $script:taskReferences++;return "{fileID: $($entry.id), guid: $taskNewGuid, type: 3}"
    })
    $taskText=[regex]::Replace($taskText,'(?ms)^--- !u!114 &(\d+).*?(?=^---|\z)',[Text.RegularExpressions.MatchEvaluator]{param($m)
        $block=$m.Value;if(!$block.Contains('guid: fe87c0e1cc204ed48ad3b37840f39efc')){return $block}
        $sprite=[regex]::Match($block,'m_Sprite: \{fileID: (\d+), guid: '+$taskNewGuid)
        if(!$sprite.Success){return $block}
        $entry=$taskByKey.Values | Where-Object id -eq ([int]$sprite.Groups[1].Value) | Select-Object -First 1
        $color=[regex]::Match($block,'m_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+), a: ([^}]+)\}')
        if(!$color.Success){throw 'Image color missing'}
        $r=[double]$color.Groups[1].Value;$g=[double]$color.Groups[2].Value;$b=[double]$color.Groups[3].Value
        $isPreset=$false;foreach($preset in $taskPresetRGB){if([Math]::Abs($r-$preset[0])+[Math]::Abs($g-$preset[1])+[Math]::Abs($b-$preset[2]) -lt .0001){$isPreset=$true;break}}
        if(!$isPreset){$script:taskManual+=@("  - image: {fileID: $($m.Groups[1].Value)}`n    color: {r: $r, g: $g, b: $b, a: $($color.Groups[4].Value)}");$script:taskCustomColors++;return $block}
        $hex=switch($entry.layer){'A'{$taskPalette[$taskRealm].light}'C'{$taskPalette[$taskRealm].dark}default{$taskPalette[$taskRealm].accent}}
        $rgb=RGB $hex
        return $block.Replace($color.Value,"m_Color: {r: $($rgb[0]), g: $($rgb[1]), b: $($rgb[2]), a: $($color.Groups[4].Value)}")
    })
    if($taskText.Contains('guid: 42abc7f6d3ac4c40a467fb007ee45f49')){
        $overrides=if($taskManual.Count){"  imageColorOverrides:`n"+($taskManual -join "`n")+"`n"}else{"  imageColorOverrides: []`n"}
        $taskText=[regex]::Replace($taskText,'(?ms)^  useThemeSprites: \d+\r?\n  realmSprites:.*?(?=^  preserveOriginalAlpha:)',"  useThemeSprites: 0`n  realmSprites: []`n  useSharedAtlas: 1`n"+$overrides)
    }
    Write-UIFile $taskFile $taskText
}
$taskHits=@(& rg -l -F -f $taskGuidFile (Join-Path $taskRepo 'Assets') -g '*.prefab' -g '*.unity' -g '*.asset' -g '*.mat' -g '*.anim' -g '*.controller')
if($LASTEXITCODE -ne 1){throw ('Old UI GUID references remain: '+($taskHits -join ', '))}
$taskBytesBefore=($taskDelete | ForEach-Object {(Get-Item -LiteralPath $_).Length} | Measure-Object -Sum).Sum
foreach($taskPath in $taskDelete){
    $taskResolved=(Resolve-Path -LiteralPath $taskPath).Path
    if(!$taskResolved.StartsWith($taskUi+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe UI cleanup path'}
    Remove-Item -LiteralPath $taskResolved -Force;Remove-Item -LiteralPath ($taskResolved+'.meta') -Force
}
foreach($taskDirectory in @((Join-Path $taskUi 'Themes'),(Join-Path $taskUi 'Asgard'))){
    if(!(Test-Path -LiteralPath $taskDirectory)){continue}
    $taskResolved=(Resolve-Path -LiteralPath $taskDirectory).Path
    if(!$taskResolved.StartsWith($taskUi+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Unsafe UI folder cleanup'}
    if(Get-ChildItem -LiteralPath $taskResolved -Recurse -File | Where-Object Extension -ne '.meta'){throw 'Unexpected remaining UI files'}
    Remove-Item -LiteralPath $taskResolved -Recurse -Force;Remove-Item -LiteralPath ($taskResolved+'.meta') -Force
}
$taskResult=[pscustomobject]@{atlas=$taskShared;guid=$taskNewGuid;sprites=@($taskByKey.Values | Sort-Object id);removedTextures=$taskDelete.Count;references=$taskReferences;manualColorsPreserved=$taskCustomColors;oldPngBytes=$taskBytesBefore;newPngBytes=(Get-Item -LiteralPath $taskShared).Length}
Write-UIFile (Join-Path $PSScriptRoot 'shared-ui-manifest.json') (($taskResult | ConvertTo-Json -Depth 5)+"`n")
Write-Output ($taskResult | Select-Object -Property removedTextures,references,manualColorsPreserved,oldPngBytes,newPngBytes | ConvertTo-Json)
