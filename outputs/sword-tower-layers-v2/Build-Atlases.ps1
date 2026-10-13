$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskAssetDir=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts'
$taskOld=Join-Path $taskRoot 'outputs/sword-tower-layers-v1/source/Tower_Sword_Characters_And_Frames_Atlas_v1.png'
$taskGenerated='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81'
$taskSources=@('exec-15861999-a5c6-47bb-ae86-0574e6f4d00a.png','exec-c46380ac-c7af-44f2-820b-5a1e4aca18f7.png','exec-d8dda0a1-3cb4-43f0-a2bb-096ab8e084f9.png','exec-62f0b866-d956-47ac-9413-582eac49f7d9.png') | ForEach-Object {Join-Path $taskGenerated $_}
$taskCharacters=Join-Path $taskAssetDir 'Tower_Sword_Characters_Atlas_v2.png'
$taskFrames=Join-Path $taskAssetDir 'Tower_Frames_Atlas_v1.png'
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class SeparateSwordAtlases
{
    static Bitmap Slice(Bitmap atlas,int index)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)result.SetPixel(x,y,atlas.GetPixel((index%3)*256+x,(index/3)*256+y));
        return result;
    }
    static Bitmap Fit(string path,bool frame)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var source=new Bitmap(path))using(var g=Graphics.FromImage(result))using(var attrs=new ImageAttributes())
        {
            int l=source.Width,t=source.Height,r=-1,b=-1,m=Math.Max(2,source.Width/100);
            for(int y=m;y<source.Height-m;y++)for(int x=m;x<source.Width-m;x++)
            {if(source.GetPixel(x,y).A<220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
            if(r<l)throw new Exception("No opaque character pixels");
            var crop=Rectangle.FromLTRB(l,t,r+1,b+1);Rectangle dest;
            if(frame)dest=new Rectangle(6,5,244,243);
            else{double scale=224.0/Math.Max(crop.Width,crop.Height);int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);dest=new Rectangle((256-w)/2,240-h,w,h);}
            g.Clear(Color.FromArgb(0,0,0,0));g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(source,dest,crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attrs);
        }
        return result;
    }
    static void Pack(Bitmap[] layers,int columns,string path)
    {
        using(var atlas=new Bitmap(columns*256,((layers.Length+columns-1)/columns)*256,PixelFormat.Format32bppArgb))
        {
            for(int i=0;i<layers.Length;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)atlas.SetPixel((i%columns)*256+x,(i/columns)*256+y,layers[i].GetPixel(x,y));
            atlas.Save(path,ImageFormat.Png);
        }
        using(var saved=new Bitmap(path))for(int i=0;i<layers.Length;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            if(saved.GetPixel((i%columns)*256+x,(i/columns)*256+y).ToArgb()!=layers[i].GetPixel(x,y).ToArgb())throw new Exception("Atlas RGBA round trip mismatch");
    }
    static void Compose(Graphics g,Bitmap tile,Bitmap frame,Bitmap character,int x,int y)
    {g.DrawImageUnscaled(tile,x,y);g.DrawImageUnscaled(frame,x,y);g.DrawImageUnscaled(character,x,y);}
    public static void Build(string oldPath,string[] sources,string characterPath,string framePath,string tilePath,string outputDir)
    {
        using(var old=new Bitmap(oldPath))using(var tile=new Bitmap(tilePath))
        {
            var characters=new[]{Fit(sources[0],false),Fit(sources[1],false),Slice(old,2),Fit(sources[2],false)};
            var frames=new Bitmap[6];for(int i=0;i<5;i++)frames[i]=Slice(old,4+i);frames[5]=Fit(sources[3],true);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {var p=frames[5].GetPixel(x,y);frames[5].SetPixel(x,y,Color.FromArgb(Math.Min(p.A,tile.GetPixel(x,y).A),p.R,p.G,p.B));}
            Pack(characters,2,characterPath);Pack(frames,3,framePath);
            for(int i=0;i<5;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                if(frames[i].GetPixel(x,y).ToArgb()!=old.GetPixel(((4+i)%3)*256+x,((4+i)/3)*256+y).ToArgb())throw new Exception("Existing frame pixels changed");
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(characters[2].GetPixel(x,y).ToArgb()!=old.GetPixel(512+x,y).ToArgb())throw new Exception("Paladin pixels changed");
            for(int i=0;i<6;i++)if(frames[i].GetPixel(128,128).A!=0)throw new Exception("Nontransparent frame center");
            for(int i=0;i<4;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                if(characters[i].GetPixel(x,y).A>0&&(x<16||x>=240||y<16||y>=240))throw new Exception("Character outside inner tile area");
            int white=0;for(int y=165;y<234;y++)for(int x=60;x<195;x++)
            {var p=characters[3].GetPixel(x,y);if(p.A>220&&p.R>185&&p.G>185&&p.B>=p.R)white++;}
            if(white<2000)throw new Exception("Death knight skin is not pale enough");
            string[] names={"히어로","검객","팔라딘","데스나이트"};string[] tiers={"브론즈","실버","골드","미스릴","다이아","시너지 · 화이트 + 골드"};
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var small=new Font("Malgun Gothic",12))using(var fmt=new StringFormat())
            {
                fmt.Alignment=StringAlignment.Center;
                using(var preview=new Bitmap(1024,640,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));
                    g.DrawString("기존",font,Brushes.LightGray,new RectangleF(0,2,1024,30),fmt);
                    g.DrawString("수정 · 얼굴 확대 / 떠 있는 검 제거 / 창백한 피부",font,Brushes.LightSkyBlue,new RectangleF(0,320,1024,30),fmt);
                    for(int i=0;i<4;i++)using(var before=Slice(old,i))
                    {Compose(g,tile,frames[0],before,i*256,34);Compose(g,tile,frames[0],characters[i],i*256,352);g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(i*256,610,256,28),fmt);}
                    preview.Save(Path.Combine(outputDir,"preview-before-after.png"),ImageFormat.Png);
                }
                using(var preview=new Bitmap(1536,300,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));for(int i=0;i<6;i++){g.DrawImageUnscaled(tile,i*256,2);g.DrawImageUnscaled(frames[i],i*256,2);g.DrawString(tiers[i],font,Brushes.Ivory,new RectangleF(i*256,266,256,28),fmt);}
                    preview.Save(Path.Combine(outputDir,"preview-frames.png"),ImageFormat.Png);
                }
                using(var preview=new Bitmap(1280,1350,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));for(int t=0;t<5;t++)g.DrawString(tiers[t],font,Brushes.Ivory,new RectangleF(t*256,8,256,30),fmt);
                    for(int c=0;c<4;c++){int top=42+c*326;for(int t=0;t<5;t++){Compose(g,tile,frames[t],characters[c],t*256,top);g.DrawString(((t+1)*1000+(c+1)*100+1).ToString(),small,Brushes.LightSkyBlue,new RectangleF(t*256,top+254,256,26),fmt);}g.DrawString(names[c],font,Brushes.Ivory,new RectangleF(0,top+283,1280,30),fmt);}
                    preview.Save(Path.Combine(outputDir,"preview-20.png"),ImageFormat.Png);
                }
                using(var preview=new Bitmap(1024,294,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));for(int i=0;i<4;i++){Compose(g,tile,frames[0],characters[i],i*256,2);g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(i*256,262,256,30),fmt);}
                    preview.Save(Path.Combine(outputDir,"preview-characters.png"),ImageFormat.Png);
                }
            }
            foreach(var p in characters)p.Dispose();foreach(var p in frames)p.Dispose();
            Console.WriteLine("PASS: 4 character slices and 6 separate frame slices; existing paladin and five frames preserved byte-for-byte; transparent frame centers; all characters contained; pale death knight skin; exact PNG RGBA round trips.");
        }
    }
}
'@
[SeparateSwordAtlases]::Build($taskOld,$taskSources,$taskCharacters,$taskFrames,(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'),$PSScriptRoot)
function Write-AtlasMeta([string]$taskPath,[string[]]$taskNames,[int[]]$taskIds,[int]$taskColumns){
    $taskTemplate=[IO.File]::ReadAllText((Join-Path $taskAssetDir 'Tower_Bronze_Sword_Target_Front_v3.png.meta'))
    $taskExisting=if(Test-Path -LiteralPath ($taskPath+'.meta')){[IO.File]::ReadAllText($taskPath+'.meta')}else{''}
    $taskGuid=if($taskExisting){[regex]::Match($taskExisting,'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
    $taskPrefix=[regex]::Replace($taskTemplate.Substring(0,$taskTemplate.IndexOf('  spriteSheet:')),'(?m)^guid: \w+',"guid: $taskGuid").Replace('spriteMode: 1','spriteMode: 2')
    $taskRows=[int][Math]::Ceiling($taskNames.Length/[double]$taskColumns)
    $taskSheet=[Text.StringBuilder]::new();[void]$taskSheet.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
    $taskEntries=@()
    for($i=0;$i -lt $taskNames.Length;$i++){
        $taskX=($i%$taskColumns)*256;$taskY=($taskRows-1-[int][Math]::Floor($i/$taskColumns))*256
        $taskSpriteGuid=[guid]::NewGuid().ToString('N')
        if($taskExisting){$taskMatch=[regex]::Match($taskExisting,'(?s)name: '+$taskNames[$i]+'\s.*?spriteID: (\w+)');if($taskMatch.Success){$taskSpriteGuid=$taskMatch.Groups[1].Value}}
        [void]$taskSheet.AppendLine(@"
    - serializedVersion: 2
      name: $($taskNames[$i])
      rect:
        serializedVersion: 2
        x: $taskX
        y: $taskY
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
      spriteID: $taskSpriteGuid
      internalID: $($taskIds[$i])
      vertices: []
      indices: 
      edges: []
      weights: []
"@)
        $taskEntries+=[pscustomobject]@{name=$taskNames[$i];fileID=$taskIds[$i];x=$taskX;y=$taskY;width=256;height=256}
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
    for($i=0;$i -lt $taskNames.Length;$i++){[void]$taskSheet.AppendLine("      $($taskNames[$i]): $($taskIds[$i])")}
    [IO.File]::WriteAllText($taskPath+'.meta',$taskPrefix+$taskSheet.ToString()+$taskTemplate.Substring($taskTemplate.IndexOf('  mipmapLimitGroupName:')),[Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{asset=$taskPath;guid=$taskGuid;columns=$taskColumns;rows=$taskRows;sprites=$taskEntries}
}
$taskCharacterManifest=Write-AtlasMeta $taskCharacters @('Tower_Sword_Hero','Tower_Sword_Swordsman','Tower_Sword_Paladin','Tower_Sword_DeathKnight') @(42001,42002,42003,42004) 2
$taskFrameManifest=Write-AtlasMeta $taskFrames @('Tower_Frame_Bronze','Tower_Frame_Silver','Tower_Frame_Gold','Tower_Frame_Mithril','Tower_Frame_Diamond','Tower_Frame_Synergy') @(42011,42012,42013,42014,42015,42016) 3
@($taskCharacterManifest,$taskFrameManifest) | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Encoding UTF8
Write-Output ('Characters: '+$taskCharacters+'; GUID='+$taskCharacterManifest.guid+'; 512x512.')
Write-Output ('Frames: '+$taskFrames+'; GUID='+$taskFrameManifest.guid+'; 768x512.')
