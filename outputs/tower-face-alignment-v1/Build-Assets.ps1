$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
public static class FaceAlignmentPack
{
    static Bitmap Fit(string path)
    {
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var source=new Bitmap(path))using(var g=Graphics.FromImage(result))using(var attributes=new ImageAttributes())
        {
            int l=source.Width,t=source.Height,r=-1,b=-1,m=Math.Max(2,source.Width/50);
            for(int y=m;y<source.Height-m;y++)for(int x=m;x<source.Width-m;x++)
            {if(source.GetPixel(x,y).A<220)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
            var crop=Rectangle.FromLTRB(l,t,r+1,b+1);
            double scale=Math.Min(248.0/crop.Width,242.0/crop.Height);int w=(int)Math.Round(crop.Width*scale),h=(int)Math.Round(crop.Height*scale);
            g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attributes.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(source,new Rectangle((256-w)/2,244-h,w,h),crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
        }
        return result;
    }
    public static void Axe(string sourceDir,string outputDir)
    {
        string[] names={"Viking","Berserker","Skald","Executioner"};
        using(var atlas=new Bitmap(512,512,PixelFormat.Format32bppArgb))
        using(var preview=new Bitmap(1024,280,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
        using(var font=new Font("Arial",10))using(var line=new Pen(Color.FromArgb(180,255,60,170)))
        {
            g.Clear(Color.FromArgb(255,60,65,76));
            for(int i=0;i<4;i++)using(var character=Fit(Path.Combine(sourceDir,names[i]+".png")))
            {
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)atlas.SetPixel(i%2*256+x,i/2*256+y,character.GetPixel(x,y));
                g.DrawImageUnscaled(character,i*256,0);
                for(int y=140;y<=220;y+=20){g.DrawLine(line,i*256,y,i*256+255,y);g.DrawString(y.ToString(),font,Brushes.White,i*256+2,y);}
                g.DrawString(names[i],font,Brushes.White,i*256+65,260);
            }
            atlas.Save(Path.Combine(outputDir,"Tower_Axe_Characters_Atlas_v1.png"),ImageFormat.Png);
            preview.Save(Path.Combine(outputDir,"axe-landmarks.png"),ImageFormat.Png);
        }
    }
    public static void Preview(string swordPath,string axePath,string framePath,string outputDir)
    {
        int[][] shifts={new[]{24,24,24,24},new[]{8,34,8,20}};
        string[] names={"히어로","검객","팔라딘","데스나이트","바이킹","버서커","스칼드","집행관"};
        using(var sword=new Bitmap(swordPath))using(var axe=new Bitmap(axePath))using(var frames=new Bitmap(framePath))
        using(var preview=new Bitmap(1024,720,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
        using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var fmt=new StringFormat())
        {
            g.Clear(Color.FromArgb(255,29,33,44));fmt.Alignment=StringAlignment.Center;
            for(int row=0;row<2;row++)for(int i=0;i<4;i++)
            {
                int x=i*256,top=48+row*354;
                g.DrawString(names[row*4+i],font,Brushes.Ivory,new RectangleF(x,top-38,256,30),fmt);
                g.DrawImage(frames,new Rectangle(x,top+24,256,256),0,0,256,256,GraphicsUnit.Pixel);
                var characters=row==0?sword:axe;
                g.DrawImage(characters,new Rectangle(x,top+24-shifts[row][i],256,256),i%2*256,i/2*256,256,256,GraphicsUnit.Pixel);
            }
            g.DrawString("흰색 안쪽 · 얼굴 높이 기준 · 투구/모자는 테두리 위로",font,Brushes.LightSkyBlue,new RectangleF(0,690,1024,30),fmt);
            preview.Save(Path.Combine(outputDir,"preview.png"),ImageFormat.Png);
        }
    }
    public static void WhiteFrames(string sourcePath,string outputDir)
    {
        using(var source=new Bitmap(sourcePath))using(var result=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        {
            for(int y=0;y<512;y++)for(int x=0;x<768;x++)result.SetPixel(x,y,source.GetPixel(x,y));
            for(int i=0;i<6;i++)
            {
                int ox=i%3*256,oy=i/3*256;var seen=new bool[256,256];var queue=new System.Collections.Generic.Queue<Point>();
                queue.Enqueue(new Point(128,128));int count=0;
                while(queue.Count>0)
                {
                    var p=queue.Dequeue();int x=p.X,y=p.Y;
                    if(x<24||x>232||y<24||y>232||seen[x,y])continue;seen[x,y]=true;
                    var c=source.GetPixel(ox+x,oy+y);if(c.A>=240)continue;
                    double a=c.A/255.0;
                    result.SetPixel(ox+x,oy+y,Color.FromArgb(255,(int)Math.Round(c.R*a+255*(1-a)),(int)Math.Round(c.G*a+255*(1-a)),(int)Math.Round(c.B*a+255*(1-a))));count++;
                    queue.Enqueue(new Point(x-1,y));queue.Enqueue(new Point(x+1,y));queue.Enqueue(new Point(x,y-1));queue.Enqueue(new Point(x,y+1));
                }
                if(count<25000||count>40000)throw new Exception("Unexpected inner window mask: "+count);
                Console.WriteLine("Frame "+i+": filled "+count+" inner pixels, perimeter unchanged.");
            }
            result.Save(Path.Combine(outputDir,"Tower_Frames_Atlas_v1.png"),ImageFormat.Png);
        }
    }
    public static void Guide(string output)
    {
        using(var b=new Bitmap(512,512,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(b))
        using(var pen=new Pen(Color.DarkSlateBlue,4))using(var face=new SolidBrush(Color.FromArgb(255,252,209,173)))
        using(var eye=new SolidBrush(Color.FromArgb(255,66,29,48)))using(var font=new Font("Arial",15))
        {
            g.Clear(Color.White);
            // This is a construction diagram, not a replacement character asset.
            g.DrawRectangle(Pens.LightGray,48,48,416,416);
            using(var path=new GraphicsPath()){
                path.AddArc(88,228,48,48,180,90);path.AddArc(376,228,48,48,270,90);path.AddArc(376,376,48,48,0,90);path.AddArc(88,376,48,48,90,90);path.CloseFigure();
                g.FillPath(face,path);g.DrawPath(pen,path);
            }
            g.FillEllipse(eye,160,304,26,38);g.FillEllipse(eye,326,304,26,38);
            g.DrawLine(pen,234,380,278,380);
            g.DrawString("FIXED FACE: x44..212, y114..212",font,Brushes.Black,57,442);
            g.DrawString("Hair / helmet / ornaments above",font,Brushes.Gray,68,55);
            b.Save(output,ImageFormat.Png);
        }
    }
}
'@
[FaceAlignmentPack]::WhiteFrames((Join-Path $PSScriptRoot 'before/Tower_Frames_Atlas_v1.png'),$PSScriptRoot)
[FaceAlignmentPack]::Guide((Join-Path $PSScriptRoot 'face-size-guide.png'))
[FaceAlignmentPack]::Axe((Join-Path $taskRoot 'outputs/axe-tower-concepts-v1/source'),$PSScriptRoot)
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'Tower_Frames_Atlas_v1.png')){
    [FaceAlignmentPack]::Preview((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_Atlas_v2.png'),(Join-Path $PSScriptRoot 'Tower_Axe_Characters_Atlas_v1.png'),(Join-Path $PSScriptRoot 'Tower_Frames_Atlas_v1.png'),$PSScriptRoot)
}
