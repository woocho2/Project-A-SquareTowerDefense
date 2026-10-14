$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class TowerShapeFitReference
{
    static Rectangle Bounds(Bitmap b,int threshold)
    {
        int l=b.Width,t=b.Height,r=-1,bot=-1;for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=threshold){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static string Rect(Rectangle r){return "x="+r.X+", y="+r.Y+", width="+r.Width+", height="+r.Height;}
    public static void Build(string folder,string framePath,string fillPath)
    {
        using(var spawn=new Bitmap(Path.Combine(folder,"spawn-tile.png")))using(var frames=new Bitmap(framePath))using(var fills=new Bitmap(fillPath))
        using(var opening=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        {
            var report=new StringBuilder();report.AppendLine("Reference canvas: 256x256; PPU: 192; pivot: (0.5, 0.5); grid cell: 1.5x1.5 world units.");
            report.AppendLine("Spawn visible bounds (alpha>=128): "+Rect(Bounds(spawn,128)));
            report.AppendLine("Spawn canvas world size: "+(256.0/192).ToString("F7"));
            report.AppendLine("Current regular character/frame canvas: 1.2666667 (95%); desired exact shared canvas: 1.3333333 (100%).");
            string[] names={"Bronze","Silver","Gold","Mithril","Diamond","Synergy"};
            for(int i=0;i<6;i++)using(var rim=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var mask=new Bitmap(256,256,PixelFormat.Format32bppArgb))
            {
                int ox=i%3*256,oy=i/3*256;int count=0;
                for(int y=0;y<256;y++)for(int x=0;x<256;x++){
                    var r=frames.GetPixel(ox+x,oy+y);rim.SetPixel(x,y,r);
                    int a=Math.Max(0,fills.GetPixel(ox+x,oy+y).A-r.A);if(a>0){count++;mask.SetPixel(x,y,Color.FromArgb(a,255,255,255));}
                    if(i==0)opening.SetPixel(x,y,mask.GetPixel(x,y));
                }
                report.AppendLine(names[i]+" outer: "+Rect(Bounds(rim,128))+"; face opening: "+Rect(Bounds(mask,128))+"; opening pixels="+count);
                mask.Save(Path.Combine(folder,names[i]+"-face-opening-mask256.png"),ImageFormat.Png);
            }
            using(var mask64=new Bitmap(64,64,PixelFormat.Format32bppArgb)){
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){var c=opening.GetPixel(x*4+2,y*4+2);if(c.A>=128)mask64.SetPixel(x,y,Color.White);}
                mask64.Save(Path.Combine(folder,"face-opening-mask64.png"),ImageFormat.Png);report.AppendLine("64px reference face opening: "+Rect(Bounds(mask64,128)));
            }
            opening.Save(Path.Combine(folder,"face-opening-mask256.png"),ImageFormat.Png);
            using(var preview=new Bitmap(1536,602,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",18,FontStyle.Bold))using(var small=new Font("Malgun Gothic",12))using(var fmt=new StringFormat())
            {
                fmt.Alignment=StringAlignment.Center;g.Clear(Color.FromArgb(255,37,41,51));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                string[] titles={"스폰 타일 영역","프레임 + 캐릭터가 채울 영역","같은 크기·중심으로 정합"};
                for(int i=0;i<3;i++){
                    int ox=i*512;g.DrawString(titles[i],font,Brushes.Ivory,new RectangleF(ox,10,512,34),fmt);
                    if(i!=1)g.DrawImage(spawn,new Rectangle(ox,60,512,512),0,0,256,256,GraphicsUnit.Pixel);
                    if(i!=0){
                        using(var tint=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
                            for(int y=0;y<256;y++)for(int x=0;x<256;x++){var c=opening.GetPixel(x,y);tint.SetPixel(x,y,Color.FromArgb(c.A,255,209,170));}
                            g.DrawImage(tint,new Rectangle(ox,60,512,512),0,0,256,256,GraphicsUnit.Pixel);
                        }
                        g.DrawImage(frames,new Rectangle(ox,60,512,512),0,0,256,256,GraphicsUnit.Pixel);
                    }
                    g.DrawString("256×256 기준 · 중심 동일 · 프레임 밖으로 돌출 없음",small,Brushes.LightSkyBlue,new RectangleF(ox,571,512,30),fmt);
                }
                preview.Save(Path.Combine(folder,"layout-reference.png"),ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(folder,"measurements.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
        }
    }
}
'@
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[TowerShapeFitReference]::Build($PSScriptRoot,(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frame_Rims_Atlas_v1.png'),(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png'))
