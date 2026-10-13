$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskGenerated='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81'
$taskNames=@('Character_Hero','Character_Swordsman','Character_Paladin','Character_DeathKnight','Frame_Bronze','Frame_Silver','Frame_Gold','Frame_Mithril','Frame_Diamond')
$taskFiles=@('exec-da289fa5-b84a-4b1a-bbf7-e2212544360c.png','exec-06642b13-e4c4-4b74-aa5f-14d29b2260ea.png','exec-f0eb6d69-bb47-4cc8-b127-efa8869f8b2a.png','exec-a4e55b00-0fbb-4962-abe8-84b6dc931bc4.png','exec-d519b37d-6a93-4aa0-9a6d-1dcad7f0156f.png','exec-f48c9c25-9d77-4ee0-9423-3b007d40d0cd.png','exec-78cffb0a-f684-4f93-941c-8ae42c8f3037.png','exec-7a9568c8-0a5f-4ad7-b4d8-2aa1d9e0b55f.png','exec-08be1d6c-d4bf-4941-84b1-1a4e62dc3cb6.png')
$taskSources=@($taskFiles | ForEach-Object {Join-Path $taskGenerated $_})
$taskAsset=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_And_Frames_Atlas_v1.png'
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class SwordLayerAtlas
{
    static Rectangle Bounds(Bitmap image)
    {
        int l=image.Width,t=image.Height,r=-1,b=-1,m=Math.Max(2,image.Width/100);
        for(int y=m;y<image.Height-m;y++)for(int x=m;x<image.Width-m;x++)
        {if(image.GetPixel(x,y).A<220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
        if(r<l)throw new Exception("Missing sprite pixels");return Rectangle.FromLTRB(l,t,r+1,b+1);
    }
    static Bitmap Fit(string path,bool frame)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var original=new Bitmap(path))using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes())
        {
            Rectangle crop=Bounds(original),dest;
            if(frame)dest=new Rectangle(6,5,244,243);
            else
            {
                double scale=224.0/Math.Max(crop.Width,crop.Height);
                int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);
                dest=new Rectangle((256-w)/2,16+224-h,w,h);
            }
            g.Clear(Color.FromArgb(0,0,0,0));g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(original,dest,crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
        }
        return result;
    }
    public static void Build(string[] sources,string atlasPath,string outputDir,string tilePath)
    {
        var layers=new Bitmap[9];for(int i=0;i<9;i++)layers[i]=Fit(sources[i],i>=4);
        // Share the clean gold frame's alpha to remove the bronze source's unintended faint center.
        // Frame RGB stays image-generated; all tier frames use exactly the same transparency.
        using(var mask=(Bitmap)layers[6].Clone())using(var tile=new Bitmap(tilePath))
        using(var atlas=new Bitmap(768,768,PixelFormat.Format32bppArgb))
        {
            for(int i=4;i<9;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                Color rgb=layers[i].GetPixel(x,y),shape=mask.GetPixel(x,y);
                bool metal=shape.GetHue()<100&&(shape.R>shape.B+10||shape.GetBrightness()>.65f);
                if(!metal)rgb=shape; // Keep the navy frame inset identical across tier colors.
                int alpha=Math.Min(shape.A,tile.GetPixel(x,y).A);
                if(alpha<=8)alpha=0;
                layers[i].SetPixel(x,y,Color.FromArgb(alpha,rgb.R,rgb.G,rgb.B));
            }
            for(int i=0;i<9;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                atlas.SetPixel((i%3)*256+x,(i/3)*256+y,layers[i].GetPixel(x,y));
            atlas.Save(atlasPath,ImageFormat.Png);
            using(var preview=new Bitmap(1280,1350,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",17,FontStyle.Bold))using(var small=new Font("Malgun Gothic",13))
            using(var format=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,29,33,44));format.Alignment=StringAlignment.Center;
                string[] tiers={"브론즈","실버","골드","미스릴","다이아"};
                string[] classes={"레드 · 히어로 / 엑스칼리버","블루 · 검객 / 매화검법","화이트 · 팔라딘 / 아군 강화","블랙 · 데스나이트 / 저주"};
                for(int t=0;t<5;t++)g.DrawString(tiers[t],font,Brushes.Ivory,new RectangleF(t*256,8,256,32),format);
                for(int c=0;c<4;c++)
                {
                    int top=42+c*326;
                    for(int t=0;t<5;t++)
                    {
                        g.DrawImageUnscaled(tile,t*256,top);
                        g.DrawImageUnscaled(layers[t+4],t*256,top);
                        g.DrawImageUnscaled(layers[c],t*256,top);
                        int id=(t+1)*1000+(c+1)*100+1;
                        g.DrawString(id.ToString(),small,Brushes.LightSkyBlue,new RectangleF(t*256,top+252,256,25),format);
                    }
                    g.DrawString(classes[c],font,Brushes.Ivory,new RectangleF(0,top+283,1280,30),format);
                }
                preview.Save(Path.Combine(outputDir,"preview-20.png"),ImageFormat.Png);
            }
            using(var preview=new Bitmap(1024,568,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",15,FontStyle.Bold))using(var format=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,29,33,44));format.Alignment=StringAlignment.Center;
                string[] names={"히어로","검객","팔라딘","데스나이트"};
                for(int c=0;c<4;c++)
                {
                    g.DrawImageUnscaled(tile,c*256,8);g.DrawImageUnscaled(layers[4],c*256,8);g.DrawImageUnscaled(layers[c],c*256,8);
                    g.DrawString(names[c],font,Brushes.Ivory,new RectangleF(c*256,264,256,30),format);
                    g.DrawImageUnscaled(layers[c],c*256,302);
                }
                preview.Save(Path.Combine(outputDir,"preview-characters.png"),ImageFormat.Png);
            }
            using(var saved=new Bitmap(atlasPath))
            {
                for(int i=0;i<9;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    Color expected=layers[i].GetPixel(x,y),actual=saved.GetPixel((i%3)*256+x,(i/3)*256+y);
                    if(expected.ToArgb()!=actual.ToArgb())throw new Exception("Atlas RGBA round trip changed pixels");
                    if(i>=4&&expected.A!=layers[4].GetPixel(x,y).A)throw new Exception("Frame alpha mismatch");
                    if(i<4&&expected.A>0&&(x<16||x>=240||y<16||y>=240))throw new Exception("Character exceeds inner canvas");
                }
                for(int i=4;i<9;i++)if(layers[i].GetPixel(128,128).A!=0)throw new Exception("Frame center is not transparent");
            }
        }
        foreach(var layer in layers)layer.Dispose();
        Console.WriteLine("PASS: 9 separate 256x256 layers, 20 compositions, frame alpha equality, transparent frame centers, contained characters, exact atlas RGBA round trip.");
    }
}
'@
[SwordLayerAtlas]::Build($taskSources,$taskAsset,$PSScriptRoot,(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'))
$taskSpriteNames=@('Tower_Sword_Hero','Tower_Sword_Swordsman','Tower_Sword_Paladin','Tower_Sword_DeathKnight','Tower_Frame_Bronze','Tower_Frame_Silver','Tower_Frame_Gold','Tower_Frame_Mithril','Tower_Frame_Diamond')
$taskIds=@(42001,42002,42003,42004,42011,42012,42013,42014,42015)
$taskTemplate=[IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v3.png.meta'))
$taskExistingMeta=if(Test-Path -LiteralPath ($taskAsset+'.meta')){[IO.File]::ReadAllText($taskAsset+'.meta')}else{''}
$taskGuid=if($taskExistingMeta){[regex]::Match($taskExistingMeta,'(?m)^guid: (\w+)').Groups[1].Value}else{[guid]::NewGuid().ToString('N')}
$taskPrefix=$taskTemplate.Substring(0,$taskTemplate.IndexOf('  spriteSheet:'))
$taskPrefix=[regex]::Replace($taskPrefix,'(?m)^guid: \w+',"guid: $taskGuid").Replace('spriteMode: 1','spriteMode: 2')
$taskSheet=[Text.StringBuilder]::new()
[void]$taskSheet.AppendLine("  spriteSheet:`n    serializedVersion: 2`n    sprites:")
$taskManifest=@()
for($i=0;$i -lt 9;$i++){
    $taskX=($i%3)*256;$taskY=(2-[int][Math]::Floor($i/3))*256
    $taskSpriteGuid=[guid]::NewGuid().ToString('N')
    if($taskExistingMeta){$taskMatch=[regex]::Match($taskExistingMeta,'(?s)name: '+$taskSpriteNames[$i]+'\s.*?spriteID: (\w+)');if($taskMatch.Success){$taskSpriteGuid=$taskMatch.Groups[1].Value}}
    [void]$taskSheet.AppendLine(@"
    - serializedVersion: 2
      name: $($taskSpriteNames[$i])
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
    $taskManifest += [pscustomobject]@{name=$taskSpriteNames[$i];fileID=$taskIds[$i];x=$taskX;y=$taskY;width=256;height=256;source=$taskSources[$i];prompt=('prompts/'+$taskNames[$i]+'.txt')}
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
for($i=0;$i -lt 9;$i++){[void]$taskSheet.AppendLine("      $($taskSpriteNames[$i]): $($taskIds[$i])")}
[IO.File]::WriteAllText($taskAsset+'.meta',$taskPrefix+$taskSheet.ToString()+$taskTemplate.Substring($taskTemplate.IndexOf('  mipmapLimitGroupName:')),[Text.UTF8Encoding]::new($false))
$taskManifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Encoding UTF8
Write-Output ('Saved '+$taskAsset+'; GUID='+$taskGuid+'; 768x768, nine 256x256 sprites, PPU=192.')
