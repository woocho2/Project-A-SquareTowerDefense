$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskGenerated='C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81'
$taskNames=@('Viking','Berserker','Skald','Executioner')
$taskFiles=@('exec-5f816861-08e1-423b-98e3-9d6ddf408219.png','exec-956cbd53-250f-4310-a69a-03eb79883e38.png','exec-b39e057c-6263-4506-9557-0ac3bef9dffb.png','exec-431e7804-9ef5-42da-ba74-ffc43898cb03.png')
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'source'))
$taskSources=@();for($i=0;$i -lt 4;$i++){$taskSource=Join-Path $PSScriptRoot ('source/'+$taskNames[$i]+'.png');Copy-Item -LiteralPath (Join-Path $taskGenerated $taskFiles[$i]) -Destination $taskSource;$taskSources+=$taskSource}
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class AxeConceptPreview
{
    static Bitmap Fit(string path)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var source=new Bitmap(path))using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes())
        {
            int l=source.Width,t=source.Height,r=-1,b=-1,m=Math.Max(2,source.Width/50);
            for(int y=m;y<source.Height-m;y++)for(int x=m;x<source.Width-m;x++)
            {if(source.GetPixel(x,y).A<220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
            if(r<l)throw new Exception("Empty concept");var crop=Rectangle.FromLTRB(l,t,r+1,b+1);
            double scale=Math.Min(248.0/crop.Width,242.0/crop.Height);int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);
            g.Clear(Color.FromArgb(0,0,0,0));g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(source,new Rectangle((256-w)/2,244-h,w,h),crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
        }
        return result;
    }
    public static void Build(string[] sources,string tilePath,string framePath,string outputDir)
    {
        using(var tile=new Bitmap(tilePath))using(var frameAtlas=new Bitmap(framePath))
        using(var preview=new Bitmap(1024,664,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
        using(var title=new Font("Malgun Gothic",16,FontStyle.Bold))using(var small=new Font("Malgun Gothic",12))using(var format=new StringFormat())
        {
            g.Clear(Color.FromArgb(255,29,33,44));format.Alignment=StringAlignment.Center;
            string[] names={"레드 · 바이킹","블루 · 버서커","화이트 · 스칼드","블랙 · 집행관"};
            string[] skills={"단두대 · 광역 강타","쪼개기 · 치명타 강화","광분/광전사 · 아군 강화","선고/처형 · 즉사 기준"};
            for(int i=0;i<4;i++)using(var character=Fit(sources[i]))
            {
                g.DrawString(names[i],title,Brushes.Ivory,new RectangleF(i*256,2,256,32),format);
                g.DrawImageUnscaled(tile,i*256,38);g.DrawImage(frameAtlas,new Rectangle(i*256,38,256,256),0,0,256,256,GraphicsUnit.Pixel);g.DrawImageUnscaled(character,i*256,38);
                g.DrawString(skills[i],small,Brushes.LightSkyBlue,new RectangleF(i*256,304,256,32),format);
                g.DrawImageUnscaled(character,i*256,392);
            }
            g.DrawString("캐릭터 원본 레이어 · 테두리는 별도",title,Brushes.LightGray,new RectangleF(0,351,1024,32),format);
            preview.Save(Path.Combine(outputDir,"preview.png"),ImageFormat.Png);
        }
        Console.WriteLine("Saved four source concepts and a framed comparison preview outside Assets.");
    }
}
'@
[AxeConceptPreview]::Build($taskSources,(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'),(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png'),$PSScriptRoot)
