$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class HatShapeComparison
{
    public static void Build(string sourcePath,string outputPath)
    {
        using(var source=new Bitmap(sourcePath))using(var preview=new Bitmap(660,400,PixelFormat.Format32bppArgb))
        {
            int w=source.Width,h=source.Height;var opaque=new bool[w*h];var seen=new bool[w*h];
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)opaque[y*w+x]=source.GetPixel(x,y).A>127;
            var components=new List<Tuple<int,Rectangle>>();
            for(int start=0;start<opaque.Length;start++)
            {
                if(!opaque[start]||seen[start])continue;
                int count=0,l=w,t=h,r=0,b=0;var queue=new Queue<int>();queue.Enqueue(start);seen[start]=true;
                while(queue.Count>0)
                {
                    int p=queue.Dequeue(),x=p%w,y=p/w;count++;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
                    foreach(int n in new[]{x>0?p-1:-1,x<w-1?p+1:-1,y>0?p-w:-1,y<h-1?p+w:-1})
                    {if(n<0||seen[n]||!opaque[n])continue;seen[n]=true;queue.Enqueue(n);}
                }
                if(count>10000)components.Add(Tuple.Create(count,Rectangle.FromLTRB(l,t,r+1,b+1)));
            }
            var regions=components.OrderByDescending(c=>c.Item1).Take(2).OrderBy(c=>c.Item2.X).Select(c=>c.Item2).ToArray();
            if(regions.Length!=2)
            {
                // The generated hats touch, so split the two portraits at their shared seam.
                int seam=(int)Math.Round(w*.532);regions=new Rectangle[2];
                for(int i=0;i<2;i++)
                {
                    int start=i==0?0:seam,end=i==0?seam:w,l=end,t=h,r=-1,b=-1;
                    for(int y=0;y<h;y++)for(int x=start;x<end;x++)
                    {if(!opaque[y*w+x])continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);}
                    if(r<l)throw new Exception("Empty comparison portrait");regions[i]=Rectangle.FromLTRB(l,t,r+1,b+1);
                }
            }
            using(var g=Graphics.FromImage(preview))using(var pen=new Pen(Color.FromArgb(135,155,175),1))
            using(var font=new Font("Malgun Gothic",13,FontStyle.Bold))using(var format=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,28,33,43));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                pen.DashStyle=DashStyle.Dash;format.Alignment=StringAlignment.Center;
                string[] titles={"A · 네모 캔버스 안에 담기","B · 외곽까지 네모로 채우기"};
                for(int i=0;i<2;i++)
                {
                    var canvas=new Rectangle(20+i*330,45,280,280);g.DrawRectangle(pen,canvas);
                    var src=regions[i];double scale=254.0/Math.Max(src.Width,src.Height);
                    int dw=(int)Math.Round(src.Width*scale),dh=(int)Math.Round(src.Height*scale);
                    var dest=new Rectangle(canvas.X+(280-dw)/2,canvas.Y+(280-dh)/2,dw,dh);
                    g.DrawImage(source,dest,src.X,src.Y,src.Width,src.Height,GraphicsUnit.Pixel);
                    g.DrawString(titles[i],font,Brushes.Ivory,new RectangleF(i*330,342,330,32),format);
                }
                g.DrawString("점선은 캔버스 경계 · 게임 에셋의 장식이 아님",font,Brushes.LightSkyBlue,new RectangleF(0,5,660,32),format);
            }
            preview.Save(outputPath,ImageFormat.Png);
            Console.WriteLine("Saved two portrait interpretations within equal square preview canvases; actual game assets unchanged.");
        }
    }
}
'@
[HatShapeComparison]::Build('C:/Users/user/.codex/generated_images/01a113e3-050d-77a2-9b09-b97ca0dcaa81/exec-176ea4cf-e6ed-4d61-b272-029053027e4b.png',(Join-Path $PSScriptRoot 'preview.png'))
