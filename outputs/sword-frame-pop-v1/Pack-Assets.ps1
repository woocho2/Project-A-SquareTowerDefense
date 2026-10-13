$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskDir=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts'
$taskGenerated='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81'
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class FramePopPack
{
    static Bitmap Resize(Bitmap source,Rectangle crop,Rectangle dest)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes())
        {
            g.Clear(Color.FromArgb(0,0,0,0));g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(source,dest,crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
        }
        return result;
    }
    static Rectangle Bounds(Bitmap source)
    {
        int l=source.Width,t=source.Height,r=0,b=0,m=source.Width/100;
        for(int y=m;y<source.Height-m;y++)for(int x=m;x<source.Width-m;x++)
        {if(source.GetPixel(x,y).A<220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
        return Rectangle.FromLTRB(l,t,r+1,b+1);
    }
    static void Copy(Bitmap source,Bitmap target,int ox,int oy)
    {for(int y=0;y<256;y++)for(int x=0;x<256;x++)target.SetPixel(ox+x,oy+y,source.GetPixel(x,y));}
    public static void Build(string originalFramePath,string originalCharacterPath,string generatedFramePath,string generatedCharacterPath,string paladinPath,string synergyPath,string outputDir,string tilePath)
    {
        using(var oldFrames=new Bitmap(originalFramePath))using(var oldCharacters=new Bitmap(originalCharacterPath))
        using(var generatedFrames=new Bitmap(generatedFramePath))using(var generatedCharacters=new Bitmap(generatedCharacterPath))
        using(var paladin=new Bitmap(paladinPath))using(var synergy=new Bitmap(synergyPath))
        using(var frameAtlas=new Bitmap(768,512,PixelFormat.Format32bppArgb))using(var characterAtlas=new Bitmap(512,512,PixelFormat.Format32bppArgb))
        using(var tile=new Bitmap(tilePath))
        {
            var frames=new Bitmap[6];var characters=new Bitmap[4];
            int fw=generatedFrames.Width/3,fh=generatedFrames.Height/2;
            for(int i=0;i<6;i++)
            {
                int margin=Math.Max(2,fw/50);
                if(i<5)frames[i]=Resize(generatedFrames,new Rectangle((i%3)*fw+margin,(i/3)*fh+margin,fw-2*margin,fh-2*margin),new Rectangle(6,5,244,243));
                else frames[i]=Resize(synergy,Bounds(synergy),new Rectangle(6,5,244,243));
                // Use the existing plain-frame transparency, removing generated background/floor pixels.
                // All six frames now occupy the same outer shape and central window.
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    var rgb=frames[i].GetPixel(x,y);int alpha=oldFrames.GetPixel(512+x,y).A;
                    frames[i].SetPixel(x,y,Color.FromArgb(alpha,rgb.R,rgb.G,rgb.B));
                }
                Copy(frames[i],frameAtlas,(i%3)*256,(i/3)*256);
            }
            int cw=generatedCharacters.Width/2,ch=generatedCharacters.Height/2;
            for(int i=0;i<4;i++)
            {
                if(i==2)
                {
                    var crop=Bounds(paladin);double scale=Math.Min(240.0/crop.Width,232.0/crop.Height);
                    int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);
                    characters[i]=Resize(paladin,crop,new Rectangle((256-w)/2,240-h,w,h));
                }
                else characters[i]=Resize(generatedCharacters,new Rectangle((i%2)*cw,(i/2)*ch,cw,ch),new Rectangle(0,0,256,256));
                // Keep tiny transparent separation between neighboring atlas cells.
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(x<3||x>252||y<3||y>252)characters[i].SetPixel(x,y,Color.FromArgb(0,0,0,0));
                Copy(characters[i],characterAtlas,(i%2)*256,(i/2)*256);
            }
            frameAtlas.Save(Path.Combine(outputDir,"Tower_Frames_Atlas_v1.png"),ImageFormat.Png);
            characterAtlas.Save(Path.Combine(outputDir,"Tower_Sword_Characters_Atlas_v2.png"),ImageFormat.Png);
            using(var font=new Font("Malgun Gothic",15,FontStyle.Bold))using(var format=new StringFormat())
            {
                format.Alignment=StringAlignment.Center;
                using(var preview=new Bitmap(1024,604,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));string[] names={"히어로","검객","팔라딘","데스나이트"};
                    for(int i=0;i<4;i++)
                    {
                        var src=new Rectangle((i%2)*256,(i/2)*256,256,256);
                        g.DrawImageUnscaled(tile,i*256,0);g.DrawImage(oldFrames,new Rectangle(i*256,0,256,256),0,0,256,256,GraphicsUnit.Pixel);g.DrawImage(oldCharacters,new Rectangle(i*256,0,256,256),src.X,src.Y,256,256,GraphicsUnit.Pixel);
                        g.DrawImageUnscaled(tile,i*256,318);g.DrawImageUnscaled(frames[0],i*256,318);g.DrawImageUnscaled(characters[i],i*256,318);
                        g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(i*256,574,256,28),format);
                    }
                    g.DrawString("기존",font,Brushes.LightGray,new RectangleF(0,256,1024,28),format);
                    g.DrawString("두꺼운 테두리 · 투구 겹침 · 팔라딘 비율 수정",font,Brushes.LightSkyBlue,new RectangleF(0,287,1024,28),format);
                    preview.Save(Path.Combine(outputDir,"preview-before-after.png"),ImageFormat.Png);
                }
                using(var preview=new Bitmap(1536,290,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(255,29,33,44));string[] names={"브론즈","실버","골드","미스릴","다이아","시너지 · 흰색 + 금색"};
                    for(int i=0;i<6;i++){g.DrawImageUnscaled(tile,i*256,0);g.DrawImageUnscaled(frames[i],i*256,0);g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(i*256,260,256,28),format);}
                    preview.Save(Path.Combine(outputDir,"preview-frames.png"),ImageFormat.Png);
                }
            }
            foreach(var item in frames)item.Dispose();foreach(var item in characters)item.Dispose();
        }
        Console.WriteLine("Saved asset previews and two staged atlases; no Unity or gameplay code executed.");
    }
}
'@
[FramePopPack]::Build((Join-Path $PSScriptRoot 'before/Tower_Frames_Atlas_v1.png'),(Join-Path $PSScriptRoot 'before/Tower_Sword_Characters_Atlas_v2.png'),(Join-Path $taskGenerated 'exec-423e7848-9b1c-4a05-b3f7-ce74bf769eea.png'),(Join-Path $taskGenerated 'exec-0363dae1-ae31-4970-aa77-57f7d3ae9dd7.png'),(Join-Path $taskGenerated 'exec-3a663789-7bed-4f17-9ed0-a990d0ca49e3.png'),(Join-Path $taskGenerated 'exec-9dd8a34d-9e4b-455c-a126-94c048a72673.png'),$PSScriptRoot,(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'))
