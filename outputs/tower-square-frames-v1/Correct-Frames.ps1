$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class SquareTowerFrames
{
    const int Cell=256, OuterStart=7, OuterSize=242, InnerStart=36, InnerSize=184;
    static double Map(double v, double[] source, double[] target) {
        for(int i=0;i<target.Length-1;i++) if(v<=target[i+1])
            return source[i]+(v-target[i])/(target[i+1]-target[i])*(source[i+1]-source[i]);
        return source[source.Length-1];
    }
    static int Mask(int x,int y,int start,int size,int radius) {
        int hits=0;
        for(int sy=0;sy<8;sy++)for(int sx=0;sx<8;sx++) {
            double px=x+(sx+.5)/8,py=y+(sy+.5)/8;
            if(px<start||py<start||px>=start+size||py>=start+size)continue;
            double cx=Math.Max(start+radius,Math.Min(start+size-radius,px));
            double cy=Math.Max(start+radius,Math.Min(start+size-radius,py));
            if((px-cx)*(px-cx)+(py-cy)*(py-cy)<=radius*radius)hits++;
        }
        return (hits*255+32)/64;
    }
    // Sample only existing rim colors. Source alpha is replaced by exact geometry,
    // so transparent holes in the old rim cannot leave gaps in the new border.
    static Color Sample(Bitmap b,int ox,int oy,double px,double py) {
        int ix=(int)Math.Floor(px),iy=(int)Math.Floor(py);
        double r=0,g=0,bl=0,total=0;
        for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++) {
            int xx=Math.Max(0,Math.Min(255,ix+dx)),yy=Math.Max(0,Math.Min(255,iy+dy));
            Color c=b.GetPixel(ox+xx,oy+yy);
            double w=(dx==0?1-(px-ix):px-ix)*(dy==0?1-(py-iy):py-iy)*c.A/255.0;
            r+=c.R*w;g+=c.G*w;bl+=c.B*w;total+=w;
        }
        if(total>.02)return Color.FromArgb(255,(int)Math.Round(r/total),(int)Math.Round(g/total),(int)Math.Round(bl/total));
        int ax=Math.Max(0,Math.Min(255,(int)Math.Round(px))),ay=Math.Max(0,Math.Min(255,(int)Math.Round(py)));
        for(int radius=1;radius<=40;radius++) {
            double best=double.MaxValue;Color found=Color.Empty;
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++) {
                if(Math.Abs(dx)!=radius&&Math.Abs(dy)!=radius)continue;
                int xx=ax+dx,yy=ay+dy;if(xx<0||xx>255||yy<0||yy>255)continue;
                Color c=b.GetPixel(ox+xx,oy+yy);if(c.A<200)continue;
                double d=(xx-px)*(xx-px)+(yy-py)*(yy-py);
                if(d<best){best=d;found=c;}
            }
            if(!found.IsEmpty)return Color.FromArgb(255,found.R,found.G,found.B);
        }
        throw new Exception("No source rim color near sample.");
    }
    static Rectangle Bounds(Bitmap b,int ox,int oy,bool opening,Bitmap rim) {
        int l=256,t=256,r=-1,bot=-1;
        for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
            int a=b.GetPixel(ox+x,oy+y).A;
            if(opening)a-=rim.GetPixel(ox+x,oy+y).A;
            if(a<128)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);
        }
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static void AssertSymmetry(Bitmap b) {
        for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
            int a=b.GetPixel(x,y).A;
            if(a!=b.GetPixel(y,x).A||a!=b.GetPixel(255-x,y).A||a!=b.GetPixel(x,255-y).A)
                throw new Exception("Mask is not square/symmetric.");
        }
    }
    public static void Build(string folder) {
        string[] names={"Bronze","Silver","Gold","Mithril","Diamond","Synergy"};
        using(var source=new Bitmap(Path.Combine(folder,"before/Tower_Frame_Rims_Atlas_v1.png")))
        using(var beforeFill=new Bitmap(Path.Combine(folder,"before/Tower_Frames_Atlas_v1.png")))
        using(var fills=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        using(var rims=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        using(var outer=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var opening=new Bitmap(256,256,PixelFormat.Format32bppArgb)) {
            double[] target={0,7,36,220,249,256};
            double[] sourceX={0,6,37,219,250,256};
            double[] sourceY={0,5,31,220,247,256};
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                int oa=Mask(x,y,OuterStart,OuterSize,28),ia=Mask(x,y,InnerStart,InnerSize,20);
                outer.SetPixel(x,y,Color.FromArgb(oa,255,255,255));
                opening.SetPixel(x,y,Color.FromArgb(ia,255,255,255));
                for(int i=0;i<6;i++) {
                    int ox=i%3*256,oy=i/3*256;if(oa==0)continue;
                    int ra=oa*(255-ia)/255;
                    Color c=ia==255?Color.White:Sample(source,ox,oy,Map(x+.5,sourceX,target)-.5,Map(y+.5,sourceY,target)-.5);
                    if(ra>0)rims.SetPixel(ox+x,oy+y,Color.FromArgb(ra,c.R,c.G,c.B));
                    int fr=(c.R*(255-ia)+255*ia+127)/255;
                    int fg=(c.G*(255-ia)+255*ia+127)/255;
                    int fb=(c.B*(255-ia)+255*ia+127)/255;
                    fills.SetPixel(ox+x,oy+y,Color.FromArgb(oa,fr,fg,fb));
                }
            }
            AssertSymmetry(outer);AssertSymmetry(opening);
            var report=new StringBuilder();
            report.AppendLine("Canvas per sprite: 256x256; atlas: 768x512 (unchanged).");
            report.AppendLine("Pixel coordinates are measured from PNG top-left; alpha threshold 128.");
            report.AppendLine("Center: (128,128) on pixel edges. Outer radius: 28px; inner radius: 20px.");
            report.AppendLine("Straight border thickness: 29px on every side.");
            for(int i=0;i<6;i++) {
                int ox=i%3*256,oy=i/3*256;
                var ob=Bounds(fills,ox,oy,false,rims);var ib=Bounds(fills,ox,oy,true,rims);
                if(ob!=new Rectangle(7,7,242,242)||ib!=new Rectangle(36,36,184,184))throw new Exception("Incorrect bounds: "+names[i]);
                for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                    int a=fills.GetPixel(ox+x,oy+y).A,ra=rims.GetPixel(ox+x,oy+y).A;
                    if(a!=outer.GetPixel(x,y).A||a-ra!=opening.GetPixel(x,y).A)throw new Exception("Mismatched masks.");
                    if(rims.GetPixel(ox+x,oy+y).A!=rims.GetPixel(x,y).A)throw new Exception("Different rim geometry.");
                    if(opening.GetPixel(x,y).A==255&&fills.GetPixel(ox+x,oy+y).ToArgb()!=Color.White.ToArgb())throw new Exception("Interior is not opaque white.");
                }
                report.AppendLine(names[i]+": outer=(7,7,242,242); opening=(36,36,184,184); masks identical.");
            }
            fills.Save(Path.Combine(folder,"Tower_Frames_Atlas_v1.png"),ImageFormat.Png);
            rims.Save(Path.Combine(folder,"Tower_Frame_Rims_Atlas_v1.png"),ImageFormat.Png);
            outer.Save(Path.Combine(folder,"outer-mask256.png"),ImageFormat.Png);
            opening.Save(Path.Combine(folder,"face-opening-mask256.png"),ImageFormat.Png);
            using(var p=new Bitmap(1536,602,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(p))
            using(var font=new Font("Malgun Gothic",18,FontStyle.Bold)) {
                g.Clear(Color.FromArgb(255,40,45,56));g.DrawString("보정 전",font,Brushes.White,16,12);g.DrawString("보정 후 · 외곽 242×242 / 안쪽 184×184",font,Brushes.White,784,12);
                g.DrawImageUnscaled(beforeFill,0,52);g.DrawImageUnscaled(fills,768,52);
                g.DrawString("상하·좌우 두께 29px · 여섯 프레임 모두 같은 중심과 형태",font,Brushes.LightSkyBlue,780,564);
                p.Save(Path.Combine(folder,"comparison.png"),ImageFormat.Png);
            }
            using(var spawn=new Bitmap(Path.Combine(folder,"../tower-shape-fit-reference-v1/spawn-tile.png")))
            using(var p=new Bitmap(1024,554,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(p))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold)) {
                g.Clear(Color.FromArgb(255,40,45,56));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawString("스폰 타일 원본 · 244×242",font,Brushes.White,12,4);
                g.DrawString("정사각형 프레임 · 242×242",font,Brushes.White,524,4);
                g.DrawImage(spawn,new Rectangle(0,40,512,512),0,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(spawn,new Rectangle(512,40,512,512),0,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(fills,new Rectangle(512,40,512,512),0,0,256,256,GraphicsUnit.Pixel);
                p.Save(Path.Combine(folder,"spawn-overlay.png"),ImageFormat.Png);
            }
            report.AppendLine("Outer/opening alpha masks: 90-degree rotational symmetry and horizontal/vertical symmetry verified.");
            report.AppendLine("Six rims share exact alpha; white interiors are fully opaque; texture dimensions unchanged.");
            report.AppendLine("Spawn texture and runtime 95% scale are unchanged. Native spawn outer was 244x242, so it is not an exact square match yet.");
            File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
        }
    }
}
'@
[SquareTowerFrames]::Build($PSScriptRoot)
