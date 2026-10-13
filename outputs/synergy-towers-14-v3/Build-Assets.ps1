$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskJobs=Get-Content -Encoding UTF8 -Raw -LiteralPath (Join-Path $PSScriptRoot 'prompts.json') | ConvertFrom-Json
$taskAssetDir=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/SynergyCube/Concepts_v3'
[void][IO.Directory]::CreateDirectory($taskAssetDir)
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'png'))
$taskFolderMeta=$taskAssetDir+'.meta'
if(!(Test-Path -LiteralPath $taskFolderMeta)){
    [IO.File]::WriteAllText($taskFolderMeta,("fileFormatVersion: 2`nguid: "+[Guid]::NewGuid().ToString('N')+"`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"),[Text.UTF8Encoding]::new($false))
}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
public static class Synergy14Pack {
    public static void Pack(string[] sources,string[] singles,string[] names,string[] materials,string atlasPath,string previewPath) {
        using(var atlas=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        using(var ag=Graphics.FromImage(atlas))
        using(var preview=new Bitmap(1024,1320,PixelFormat.Format32bppArgb))
        using(var pg=Graphics.FromImage(preview))
        using(var title=new Font("Malgun Gothic",19,FontStyle.Bold))
        using(var font=new Font("Malgun Gothic",14,FontStyle.Bold))
        using(var small=new Font("Malgun Gothic",11))
        using(var fmt=new StringFormat()) {
            ag.Clear(Color.Transparent);ag.CompositingMode=CompositingMode.SourceCopy;
            pg.Clear(Color.FromArgb(255,29,33,44));fmt.Alignment=StringAlignment.Center;
            pg.DrawString("시너지 큐브 타워 · 뇨르드 포함 14종",title,Brushes.Ivory,new RectangleF(0,8,1024,36),fmt);
            pg.DrawString("2026-10-13 기획 조합 · 정적 정면 초상",small,Brushes.LightSkyBlue,new RectangleF(0,48,1024,28),fmt);
            pg.InterpolationMode=InterpolationMode.HighQualityBicubic;
            for(int i=0;i<sources.Length;i++) using(var src=new Bitmap(sources[i]))
            using(var tile=new Bitmap(256,256,PixelFormat.Format32bppArgb))
            using(var tg=Graphics.FromImage(tile)) using(var attr=new ImageAttributes()) {
                bool alpha=false,visible=false;
                for(int y=0;y<src.Height;y+=8)for(int x=0;x<src.Width;x+=8){int a=src.GetPixel(x,y).A;alpha|=a==0;visible|=a>240;}
                if(!alpha||!visible)throw new Exception("Alpha validation failed: "+sources[i]);
                tg.Clear(Color.Transparent);tg.CompositingMode=CompositingMode.SourceCopy;
                tg.InterpolationMode=InterpolationMode.HighQualityBicubic;tg.PixelOffsetMode=PixelOffsetMode.HighQuality;attr.SetWrapMode(WrapMode.TileFlipXY);
                double scale=Math.Min(224.0/src.Width,224.0/src.Height);int w=(int)Math.Round(src.Width*scale),h=(int)Math.Round(src.Height*scale);
                tg.DrawImage(src,new Rectangle((256-w)/2,(256-h)/2,w,h),0,0,src.Width,src.Height,GraphicsUnit.Pixel,attr);
                tile.Save(singles[i],ImageFormat.Png);ag.DrawImageUnscaled(tile,(i%4)*256,(i/4)*256);
                int px=(i%4)*256,py=88+(i/4)*304;
                pg.DrawString(names[i],font,Brushes.Ivory,new RectangleF(px,py,256,28),fmt);
                pg.DrawImageUnscaled(tile,px,py+22);
                pg.DrawString(materials[i],small,Brushes.LightSkyBlue,new RectangleF(px,py+278,256,24),fmt);
            }
            pg.DrawString("뇨르드 포함 · 물 추가 조합 반영",small,Brushes.LightGray,new RectangleF(512,1118,512,36),fmt);
            atlas.Save(atlasPath,ImageFormat.Png);preview.Save(previewPath,ImageFormat.Png);
        }
    }
}
'@
$taskSources=@();$taskSingles=@();$taskNames=@();$taskMaterials=@()
foreach($taskJob in $taskJobs){
    $taskFile='Synergy_'+$taskJob.name+'_Front_v3.png'
    $taskSource=Join-Path $PSScriptRoot ('source/'+$taskFile)
    if(!(Test-Path -LiteralPath $taskSource)){throw ('Missing requested character: '+$taskJob.nameKo)}
    $taskSources+=$taskSource;$taskSingles+=Join-Path $PSScriptRoot ('png/'+$taskFile)
    $taskNames+=$taskJob.nameKo;$taskMaterials+=($taskJob.materials -join ' · ')
}
$taskAtlasFile='Synergy_14_Characters_Atlas_v3.png'
$taskAtlasPath=Join-Path $PSScriptRoot $taskAtlasFile
[Synergy14Pack]::Pack([string[]]$taskSources,[string[]]$taskSingles,[string[]]$taskNames,[string[]]$taskMaterials,$taskAtlasPath,(Join-Path $PSScriptRoot 'preview-all.png'))
$taskTemplate=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/SynergyCube/Synergy_Thor_Front_v2.png.meta'))
$taskManifest=@()
for($taskI=0;$taskI -lt $taskJobs.Count;$taskI++){
    $taskJob=$taskJobs[$taskI];$taskFile='Synergy_'+$taskJob.name+'_Front_v3.png'
    $taskTarget=Join-Path $taskAssetDir $taskFile
    Copy-Item -LiteralPath $taskSingles[$taskI] -Destination $taskTarget
    $taskMetaPath=$taskTarget+'.meta'
    if(!(Test-Path -LiteralPath $taskMetaPath)){
        $taskMeta=[regex]::Replace($taskTemplate,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
        $taskMeta=[regex]::Replace($taskMeta,'(?m)^    spriteID: [a-f0-9]+','    spriteID: '+[Guid]::NewGuid().ToString('N'))
        [IO.File]::WriteAllText($taskMetaPath,$taskMeta,[Text.UTF8Encoding]::new($false))
    }
    $taskGuid=[regex]::Match([IO.File]::ReadAllText($taskMetaPath),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value
    $taskManifest+=[ordered]@{order=$taskJob.order;name=$taskJob.name;nameKo=$taskJob.nameKo;materials=$taskJob.materials;realm=$taskJob.realm;source=('source/'+$taskFile);png=('png/'+$taskFile);unityAsset=('Assets/4. Asset/2. Tower/SynergyCube/Concepts_v3/'+$taskFile);guid=$taskGuid;fileID=21300000;atlasSprite=('Synergy_'+$taskJob.name+'_Front_v3');atlasInternalId=(61000+$taskJob.order);atlasRect=@{x=($taskI%4)*256;y=(3-[int][Math]::Floor($taskI/4))*256;width=256;height=256};mode='built-in image_gen';prompt=$taskJob.prompt}
}
Copy-Item -LiteralPath $taskAtlasPath -Destination (Join-Path $taskAssetDir $taskAtlasFile)
$taskAtlasMeta=Join-Path $taskAssetDir ($taskAtlasFile+'.meta')
if(!(Test-Path -LiteralPath $taskAtlasMeta)){
    $taskPrefix=$taskTemplate.Substring(0,$taskTemplate.IndexOf('  spriteSheet:')).Replace('spriteMode: 1','spriteMode: 2')
    $taskPrefix=[regex]::Replace($taskPrefix,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
    $taskSuffix=$taskTemplate.Substring($taskTemplate.IndexOf('  mipmapLimitGroupName:'))
    $taskSheet="  spriteSheet:`n    serializedVersion: 2`n    sprites:`n"
    foreach($taskEntry in $taskManifest){
        $taskSprite='Synergy_'+$taskEntry.name+'_Front_v3';$taskId=61000+$taskEntry.order;$taskX=$taskEntry.atlasRect.x;$taskY=$taskEntry.atlasRect.y
        $taskSheet+="    - serializedVersion: 2`n      name: $taskSprite`n      rect:`n        serializedVersion: 2`n        x: $taskX`n        y: $taskY`n        width: 256`n        height: 256`n      alignment: 0`n      pivot: {x: 0.5, y: 0.5}`n      border: {x: 0, y: 0, z: 0, w: 0}`n      customData: `n      outline: []`n      physicsShape: []`n      tessellationDetail: -1`n      bones: []`n      spriteID: "+[Guid]::NewGuid().ToString('N')+"`n      internalID: $taskId`n      vertices: []`n      indices: `n      edges: []`n      weights: []`n"
    }
    $taskSheet+="    outline: []`n    customData: `n    physicsShape: []`n    bones: []`n    spriteID: `n    internalID: 0`n    vertices: []`n    indices: `n    edges: []`n    weights: []`n    secondaryTextures: []`n    spriteCustomMetadata:`n      entries: []`n    nameFileIdTable:`n"
    foreach($taskEntry in $taskManifest){$taskSheet+='      Synergy_'+$taskEntry.name+'_Front_v3: '+(61000+$taskEntry.order)+"`n"}
    [IO.File]::WriteAllText($taskAtlasMeta,($taskPrefix+$taskSheet+$taskSuffix),[Text.UTF8Encoding]::new($false))
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'manifest.json'),($taskManifest | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$taskValidation=@('Portraits: 14','Individual PNGs: 256x256','Main content: centered inside 224x224, 16px outer margin','Atlas: 1024x1024, 4x4 grid, 14 populated cells','Import: PPU192, center pivot, Full Rect, alpha enabled, no mipmaps','All source images have transparent and visible pixels','Planning: 2026-10-13 14-character table including Njord','Runtime catalog / recipes: not changed','Unity execution: not performed')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'validation.txt'),[string[]]$taskValidation,[Text.UTF8Encoding]::new($false))
'Saved 14 portraits and one Unity atlas with preview and manifest.'
