param([switch]$AllowPartial)
$ErrorActionPreference = 'Stop'
$projectDir = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$utf8 = New-Object System.Text.UTF8Encoding($false)
function Write-TaskFile([string]$path,[string]$text) { [IO.File]::WriteAllText($path,$text,$utf8) }
function Guid-For([string]$path,[string]$kind) {
    $meta=$path+'.meta'
    if(Test-Path -LiteralPath $meta) { return [regex]::Match([IO.File]::ReadAllText($meta),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value }
    $guid=[guid]::NewGuid().ToString('N')
    $folder=if($kind -eq 'Folder'){"folderAsset: yes`n"}else{''}
    $importer=if($kind -eq 'Folder'){'DefaultImporter'}elseif($kind -eq 'Script'){'MonoImporter'}else{$kind+'Importer'}
    Write-TaskFile $meta "fileFormatVersion: 2`nguid: $guid`n${folder}${importer}:`n  externalObjects: {}`n  userData:`n  assetBundleName:`n  assetBundleVariant:`n"
    return $guid
}
function Ensure-TaskDirectory([string]$relative) {
    $path=Join-Path $projectDir $relative
    [void](New-Item -ItemType Directory -Force -Path $path)
    if($relative.StartsWith('Assets/')) { [void](Guid-For $path 'Folder') }
    return $path
}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class SynergySpriteImport
{
    public static Bitmap Normalize(Bitmap source,Rectangle region)
    {
        int minX=region.Right,minY=region.Bottom,maxX=-1,maxY=-1,clear=0;
        for(int y=region.Top;y<region.Bottom;y++) for(int x=region.Left;x<region.Right;x++) {
            var c=source.GetPixel(x,y);
            if(c.A<8)clear++;
            if(c.A>16){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
        }
        if(maxX<minX || clear<region.Width*region.Height/100)throw new Exception("Empty sprite or missing native transparency");
        // Find the largest fully filled rectangle of the head texture. Cropping
        // puts every view on four straight edges without painting a box behind it.
        int[] heights=new int[region.Width],stack=new int[region.Width+1];
        int bestArea=0;Rectangle solid=Rectangle.Empty;
        for(int y=0;y<region.Height;y++){
            for(int x=0;x<region.Width;x++)heights[x]=source.GetPixel(region.Left+x,region.Top+y).A>=250?heights[x]+1:0;
            int count=0;
            for(int x=0;x<=region.Width;x++){
                int h=x==region.Width?0:heights[x];
                while(count>0 && heights[stack[count-1]]>h){
                    int height=heights[stack[--count]],left=count==0?0:stack[count-1]+1,width=x-left,area=height*width;
                    if(area>bestArea){bestArea=area;solid=new Rectangle(region.Left+left,region.Top+y-height+1,width,height);}
                }
                if(x<region.Width)stack[count++]=x;
            }
        }
        if(bestArea<(maxX-minX+1)*(maxY-minY+1)*0.4)throw new Exception("Square head texture has too little solid area");
        minX=solid.Left;minY=solid.Top;maxX=solid.Right-1;maxY=solid.Bottom-1;
        int inset=Math.Max(1,Math.Min(maxX-minX+1,maxY-minY+1)/200);
        minX+=inset;minY+=inset;maxX-=inset;maxY-=inset;
        var output=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(output))using(var attributes=new ImageAttributes()){
            g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(source,new Rectangle(16,16,224,224),minX,minY,maxX-minX+1,maxY-minY+1,GraphicsUnit.Pixel,attributes);
        }
        return output;
    }
    public static void Front(string source,string output)
    {
        using(var input=new Bitmap(source))using(var image=Normalize(input,new Rectangle(0,0,input.Width,input.Height)))image.Save(output,ImageFormat.Png);
    }
    public static void Atlas(string source,string output,string faceDir)
    {
        string[] names={"Front","Left","Right","Back","Top","Bottom"};
        using(var input=new Bitmap(source))using(var atlas=new Bitmap(768,512,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(atlas)){
            g.CompositingMode=CompositingMode.SourceCopy;
            for(int i=0;i<6;i++){
                int column=i%3,row=i/3;
                int x0=column*input.Width/3,x1=(column+1)*input.Width/3;
                int y0=row*input.Height/2,y1=(row+1)*input.Height/2;
                using(var cell=Normalize(input,new Rectangle(x0,y0,x1-x0,y1-y0))){
                    g.DrawImageUnscaled(cell,column*256,row*256);
                    cell.Save(System.IO.Path.Combine(faceDir,names[i]+".png"),ImageFormat.Png);
                }
            }
            atlas.Save(output,ImageFormat.Png);
        }
    }
    public static void Gallery(string[] images,string[] names,string[] floors,string[] frames,string output)
    {
        int columns=5,rows=(images.Length+columns-1)/columns;
        using(var canvas=new Bitmap(1280,rows*300+70,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
        using(var font=new Font("Malgun Gothic",15,FontStyle.Bold)){
            g.Clear(ColorTranslator.FromHtml("#19212C"));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.DrawString("시너지 타워 · 정적 캐릭터 / 스폰 타일 크기",font,Brushes.White,20,18);
            for(int i=0;i<images.Length;i++){
                int x=16+(i%columns)*252,y=60+(i/columns)*300;
                if(System.IO.File.Exists(floors[i]))using(var floor=new Bitmap(floors[i]))g.DrawImage(floor,new Rectangle(x,y,224,224));
                if(System.IO.File.Exists(frames[i]))using(var frame=new Bitmap(frames[i]))g.DrawImage(frame,new Rectangle(x,y,224,224));
                using(var image=new Bitmap(images[i]))using(var clip=new GraphicsPath()){
                    float left=x+14,top=y+14,side=196,diameter=14;
                    clip.AddArc(left,top,diameter,diameter,180,90);
                    clip.AddArc(left+side-diameter,top,diameter,diameter,270,90);
                    clip.AddArc(left+side-diameter,top+side-diameter,diameter,diameter,0,90);
                    clip.AddArc(left,top+side-diameter,diameter,diameter,90,90);clip.CloseFigure();
                    var state=g.Save();g.SetClip(clip);g.DrawImage(image,new Rectangle(x,y,224,224));g.Restore(state);
                }
                g.DrawString(names[i],font,Brushes.White,x+8,y+237);
            }
            canvas.Save(output,ImageFormat.Png);
        }
    }
}
'@
$characters=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'characters.json') -Raw | ConvertFrom-Json
$frontSources=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'front-sources.json') -Raw | ConvertFrom-Json
$bossSources=@()
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'boss-sources.json')) { $bossSources=Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'boss-sources.json') -Raw | ConvertFrom-Json }
if(!$AllowPartial -and $frontSources.Count -ne 13) { throw 'All thirteen synergy character sources are required.' }
$frontDir=Ensure-TaskDirectory 'Assets/4. Asset/2. Tower/SynergyCube'
$bossDir=Ensure-TaskDirectory 'Assets/4. Asset/4. Enemy/BossCube'
$bossPrefabDir=Ensure-TaskDirectory 'Assets/2. Prefab/3. Enemy/BossCharacters'
$rawDir=Ensure-TaskDirectory 'outputs/synergy-cube-characters-v1/source'
$facePreviewDir=Ensure-TaskDirectory 'outputs/synergy-cube-characters-v1/boss-faces'
$template=[IO.File]::ReadAllText((Join-Path $frontDir 'Synergy_Odin_Front_v2.png.meta'))
$catalogPath=Join-Path $projectDir 'Assets/Resources/SynergyCharacterCatalog.asset'
$catalogGuid=Guid-For $catalogPath 'NativeFormat'
$scriptGuid=Guid-For (Join-Path $projectDir 'Assets/3. Scripts/01. Tower/SynergyCharacterCatalog.cs') 'Script'
$entries=@();$assets=@();$galleryPaths=@();$galleryNames=@();$galleryFloors=@();$galleryFrames=@();$prompts=@()
foreach($character in $characters){
    $source=$frontSources | Where-Object key -eq $character.key | Select-Object -First 1
    if(!$source){continue}
    $path=Join-Path $frontDir ('Synergy_'+$character.key+'_Front_v2.png')
    [SynergySpriteImport]::Front($source.path,$path)
    Copy-Item -LiteralPath $source.path -Destination (Join-Path $rawDir ($character.key+'_Front.png'))
    $guid=if(Test-Path -LiteralPath ($path+'.meta')){[regex]::Match([IO.File]::ReadAllText($path+'.meta'),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
    $meta=[regex]::Replace($template,'(?m)^guid: [a-f0-9]+',"guid: $guid")
    $meta=[regex]::Replace($meta,'(?m)^    spriteID: [a-f0-9]+',('    spriteID: '+[guid]::NewGuid().ToString('N')))
    Write-TaskFile ($path+'.meta') $meta
    $entries+="  - towerID: $($character.id)`n    characterName: $($character.name)`n    front: {fileID: 21300000, guid: $guid, type: 3}`n"
    $assets+=[pscustomobject]@{key=$character.key;name=$character.name;towerID=$character.id;realm=$character.realm;front=$path;guid=$guid;static=$true}
    $galleryPaths+=$path;$galleryNames+=$character.name
    $galleryFloors+=(Join-Path $PSScriptRoot ('tiles/'+$character.realm+'-tower.png'))
    $galleryFrames+=(Join-Path $projectDir ('outputs/realm-tile-layers/preview/'+$character.realm+'-frame.png'))
    $prompts+=[pscustomobject]@{key=$source.key;kind='synergy-front';prompt=$source.prompt}
}
$catalog=@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: SynergyCharacterCatalog
  m_EditorClassIdentifier: Assembly-CSharp::SynergyCharacterCatalog
  m_entries:
"@
Write-TaskFile $catalogPath ($catalog+"`n"+($entries -join ''))
$towerPrefab=Join-Path $projectDir 'Assets/2. Prefab/2. Tower/TowerBase.prefab'
$tower=[IO.File]::ReadAllText($towerPrefab)
if($tower -notmatch 'm_synergyCharacters:') { $tower=$tower.Replace('  m_splashColor:',"  m_synergyCharacters: {fileID: 11400000, guid: $catalogGuid, type: 2}`n  m_synergyCanvasWorldSize: 1.3333334`n  m_splashColor:") }
$roundedGuid=Guid-For (Join-Path $projectDir 'Assets/Resources/CharacterRounded.mat') 'NativeFormat'
if($tower -notmatch 'm_synergyCharacterMaterial:') { $tower=$tower.Replace('  m_synergyCanvasWorldSize:',"  m_synergyCharacterMaterial: {fileID: 2100000, guid: $roundedGuid, type: 2}`n  m_synergyCornerRadius: 8`n  m_synergyCanvasWorldSize:") }
Write-TaskFile $towerPrefab $tower
$faces=@('Front','Left','Right','Back','Top','Bottom');$bossAssets=@();$normalAssets=@()
function Connect-CubeFaces([string]$prefab,[string]$guid,[bool]$equipmentPalette) {
    $prefab=[regex]::Replace($prefab,'(?m)^  m_Sprite: \{fileID: [^\r\n]+\}',"  m_Sprite: {fileID: 1001, guid: $guid, type: 3}")
    $prefab=[regex]::Replace($prefab,'(?m)^  m_Color: \{[^\r\n]+\}','  m_Color: {r: 1, g: 1, b: 1, a: 1}')
    if(!$equipmentPalette) {
        $prefab=[regex]::Replace($prefab,'(m_Materials:\r?\n  - )\{fileID: [^\r\n]+\}',('${1}'+"{fileID: 2100000, guid: $roundedGuid, type: 2}"))
        $prefab=$prefab.Replace('  m_useEquipmentPalette: 1','  m_useEquipmentPalette: 0')
    }
    $fields=@('m_front','m_side','m_right','m_back','m_top','m_bottom')
    for($i=0;$i -lt 6;$i++){
        $field=$fields[$i];$id=1001+$i;$line="  ${field}: {fileID: $id, guid: $guid, type: 3}"
        if($prefab -match "(?m)^  ${field}:"){$prefab=[regex]::Replace($prefab,"(?m)^  ${field}: [^\r\n]+",$line)}else{$prefab=$prefab.Replace('  m_back:',($line+"`n  m_back:"))}
        $rectField=if($field -eq 'm_side'){'m_sideRect'}else{$field+'Rect'}
        $rect="  ${rectField}:`n    serializedVersion: 2`n    x: 16`n    y: 16`n    width: 224`n    height: 224"
        $rectPattern="(?m)^  ${rectField}:\r?\n(?:    [^\r\n]*\r?\n){4}    [^\r\n]*"
        if($field -eq 'm_right'){
            $prefab=[regex]::Replace($prefab,$rectPattern+'\r?\n?','')
            $prefab=$prefab.Replace('  m_backRect:',($rect+"`n  m_backRect:"))
        }elseif($prefab -match "(?m)^  ${rectField}:"){$prefab=[regex]::Replace($prefab,$rectPattern,$rect)}else{$prefab+="`n"+$rect+"`n"}
    }
    if($prefab -notmatch '(?m)^  m_cornerRadius:') { $prefab=$prefab.Replace('  m_faceShade:',"  m_cornerRadius: 8`n  m_faceShade:") }
    return $prefab.TrimEnd()+"`n"
}
foreach($source in $bossSources){
    $character=$characters | Where-Object key -eq $source.key | Select-Object -First 1
    $normal=$source.key -eq 'Valkyrie'
    $path=if($normal){Join-Path $projectDir 'Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/Enemy_Cube_FallenValkyrie_Faces_v4.png'}else{Join-Path $bossDir ('Boss_'+$source.key+'_Faces_v2.png')}
    $faceDir=Join-Path $facePreviewDir $source.key
    [void](New-Item -ItemType Directory -Force -Path $faceDir)
    [SynergySpriteImport]::Atlas($source.path,$path,$faceDir)
    Copy-Item -LiteralPath $source.path -Destination (Join-Path $rawDir ($source.key+'_BossFaces.png'))
    $guid=if(Test-Path -LiteralPath ($path+'.meta')){[regex]::Match([IO.File]::ReadAllText($path+'.meta'),'(?m)^guid: ([a-f0-9]+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
    $meta=[regex]::Replace($template,'(?m)^guid: [a-f0-9]+',"guid: $guid")
    $meta=$meta.Replace('  spriteMode: 1','  spriteMode: 2')
    $idTable="  internalIDToNameTable:`n";$sprites="    sprites:`n";$nameTable="    nameFileIdTable:`n"
    for($i=0;$i -lt 6;$i++){
        $name=$source.key+'_'+$faces[$i];$id=1001+$i;$x=($i%3)*256;$y=if($i -lt 3){256}else{0};$spriteGuid=[guid]::NewGuid().ToString('N')
        $idTable+="  - first:`n      213: $id`n    second: $name`n"
        $nameTable+="      ${name}: $id`n"
        $sprites+=@"
    - serializedVersion: 2
      name: $name
      rect:
        serializedVersion: 2
        x: $x
        y: $y
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
      internalID: $id
      vertices: []
      indices:
      edges: []
      weights: []
"@+"`n"
    }
    $meta=$meta.Replace("  internalIDToNameTable: []",$idTable.TrimEnd())
    $meta=$meta.Replace('    sprites: []',$sprites.TrimEnd())
    $meta=$meta.Replace('    nameFileIdTable: {}',$nameTable.TrimEnd())
    Write-TaskFile ($path+'.meta') $meta
    if($normal){
        foreach($prefabFile in @('1. Enemy_normal.prefab','2. Enemy_speed.prefab','3. Enemy_depend.prefab')){
            $prefabPath=Join-Path $projectDir ('Assets/2. Prefab/3. Enemy/'+$prefabFile)
            Write-TaskFile $prefabPath (Connect-CubeFaces ([IO.File]::ReadAllText($prefabPath)) $guid $true)
        }
        $normalAssets+=[pscustomobject]@{key='Valkyrie';atlas=$path;guid=$guid;faces=$faces;columns=3;rows=2;cellSize=256;rolling=$true;equipmentPalette=$true}
        $prompts+=[pscustomobject]@{key=$source.key;kind='square-valkyrie-six-faces';prompt=$source.prompt}
        continue
    }
    $prefabFile=if($source.key -eq 'Brunhild'){'4. Enemy_middleBoss.prefab'}else{'5. Enemy_boss.prefab'}
    $prefabPath=Join-Path $projectDir ('Assets/2. Prefab/3. Enemy/'+$prefabFile)
    $prefab=Connect-CubeFaces ([IO.File]::ReadAllText($prefabPath)) $guid $false
    if($source.key -eq 'Loki' -or $source.key -eq 'Brunhild'){Write-TaskFile $prefabPath $prefab}
    $bossName=if($source.key -eq 'Loki' -or $source.key -eq 'Brunhild'){'타락한 '+$character.name}else{$character.name}
    $namedPrefab=Join-Path $bossPrefabDir ('EnemyBoss_'+$source.key+'.prefab')
    $prefab=$prefab.Replace(('  m_Name: '+[IO.Path]::GetFileNameWithoutExtension($prefabFile)),('  m_Name: EnemyBoss_'+$source.key))
    Write-TaskFile $namedPrefab $prefab
    [void](Guid-For $namedPrefab 'Prefab')
    $bossAssets+=[pscustomobject]@{key=$source.key;name=$bossName;atlas=$path;guid=$guid;prefab=$namedPrefab;faces=$faces;columns=3;rows=2;cellSize=256;rolling=$true}
    $prompts+=[pscustomobject]@{key=$source.key;kind='boss-six-faces';prompt=$source.prompt}
}
[SynergySpriteImport]::Gallery($galleryPaths,$galleryNames,$galleryFloors,$galleryFrames,(Join-Path $PSScriptRoot 'preview.png'))
Write-TaskFile (Join-Path $PSScriptRoot 'manifest.json') ([pscustomobject]@{generator='built-in image_gen';synergy=$assets;bosses=$bossAssets;normalEnemy=$normalAssets;pixelsPerUnit=192;synergyCanvasWorldSize=1.3333334;bossSourceLayout='Front Left Right / Back Top Bottom'}|ConvertTo-Json -Depth 8)
Write-TaskFile (Join-Path $PSScriptRoot 'prompts.json') ($prompts|ConvertTo-Json -Depth 5)
Write-Output ("Imported "+$assets.Count+" static synergy characters and "+$bossAssets.Count+" six-face rolling bosses.")
