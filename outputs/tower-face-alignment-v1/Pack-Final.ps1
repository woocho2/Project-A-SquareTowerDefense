$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
public static class FixedFaceAtlas
{
    // These anchors describe the FACE, including the chin under beard/mask.
    // They deliberately do not describe the outer helmet or hair silhouette.
    static int[][] face={
        new[]{149,394,503,593,478},new[]{136,377,502,592,477},
        new[]{142,366,501,542,435},new[]{144,375,498,544,443},
        new[]{164,325,466,536,410},new[]{148,408,470,602,493},
        new[]{148,305,478,486,378},new[]{126,306,474,531,400}
    };
    static double Map(double dest,double[] d,double[] s)
    {
        for(int i=0;i<d.Length-1;i++)if(dest<=d[i+1])return s[i]+(dest-d[i])/(d[i+1]-d[i])*(s[i+1]-s[i]);
        return s[s.Length-1];
    }
    static Color Sample(Bitmap b,double sx,double sy)
    {
        int x=(int)Math.Floor(sx),y=(int)Math.Floor(sy);double fx=sx-x,fy=sy-y;
        double a=0,r=0,g=0,blue=0;
        for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++)
        {
            if(x+dx<0||x+dx>=b.Width||y+dy<0||y+dy>=b.Height)continue;
            var c=b.GetPixel(x+dx,y+dy);double w=(dx==0?1-fx:fx)*(dy==0?1-fy:fy)*c.A;
            a+=w;r+=w*c.R;g+=w*c.G;blue+=w*c.B;
        }
        if(a<0.5)return Color.Transparent;
        return Color.FromArgb((int)Math.Round(a),(int)Math.Round(r/a),(int)Math.Round(g/a),(int)Math.Round(blue/a));
    }
    static Bitmap Normalize(Bitmap source,int slot,int index)
    {
        int cell=source.Width/2,ox=slot%2*cell,oy=slot/2*cell;
        var crop=source.Clone(new Rectangle(ox,oy,cell,cell),PixelFormat.Format32bppArgb);
        int l=cell,t=cell,r=0,b=0;
        for(int y=5;y<cell-5;y++)for(int x=5;x<cell-5;x++){
            if(crop.GetPixel(x,y).A<200)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);b=Math.Max(b,y);
        }
        int[] f=face[index];
        double[] sx={l,f[0],f[2],r},dx={3,44,212,253};
        // Preserve the ordinary scale below the chin: do not stretch a few
        // outline pixels into a long fake neck merely to fill the canvas.
        double lower=Math.Min(249,212+Math.Max(1,b-f[3])*98.0/(f[3]-f[1]));
        double[] sy={t,f[1],f[4],f[3],Math.Max(b,f[3]+1)},dy={3,114,160,212,lower};
        var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
        for(int y=3;y<=Math.Floor(lower);y++)for(int x=3;x<=253;x++)result.SetPixel(x,y,Sample(crop,Map(x,dx,sx),Map(y,dy,sy)));
        crop.Dispose();return result;
    }
    public static void Build(string output)
    {
        var chars=new Bitmap[8];
        using(var sword=new Bitmap(Path.Combine(output,"source-sword.png")))using(var axe=new Bitmap(Path.Combine(output,"source-axe.png")))
        using(var frames=new Bitmap(Path.Combine(output,"Tower_Frames_Atlas_v1.png")))
        {
            for(int k=0;k<2;k++)using(var atlas=new Bitmap(512,512,PixelFormat.Format32bppArgb))
            {
                for(int i=0;i<4;i++){
                    chars[k*4+i]=Normalize(k==0?sword:axe,i,k*4+i);
                    for(int y=0;y<256;y++)for(int x=0;x<256;x++)atlas.SetPixel(i%2*256+x,i/2*256+y,chars[k*4+i].GetPixel(x,y));
                }
                atlas.Save(Path.Combine(output,k==0?"Tower_Sword_Characters_Atlas_v2.png":"Tower_Axe_Characters_Atlas_v1.png"),ImageFormat.Png);
            }
            using(var preview=new Bitmap(1024,656,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var fmt=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,29,33,44));fmt.Alignment=StringAlignment.Center;
                string[] names={"히어로","검객","팔라딘","데스나이트","바이킹","버서커","스칼드","집행관"};
                for(int i=0;i<8;i++){
                    int x=i%4*256,y=40+i/4*300;
                    g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(x,y-36,256,30),fmt);
                    g.DrawImage(frames,new Rectangle(x,y,256,256),0,0,256,256,GraphicsUnit.Pixel);
                    g.DrawImageUnscaled(chars[i],x,y);
                }
                g.DrawString("공통 얼굴 168×98 · 같은 눈 높이 · 머리/장식은 별도 기준",font,Brushes.LightSkyBlue,new RectangleF(0,612,1024,32),fmt);
                preview.Save(Path.Combine(output,"preview.png"),ImageFormat.Png);
            }
            using(var preview=new Bitmap(1024,600,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var guide=new Pen(Color.FromArgb(220,50,255,130),1))
            {
                g.Clear(Color.FromArgb(255,29,33,44));
                for(int i=0;i<8;i++){
                    int x=i%4*256,y=i/4*300;
                    g.DrawImageUnscaled(chars[i],x,y);g.DrawRectangle(guide,x+44,y+114,168,98);g.DrawLine(guide,x+44,y+160,x+212,y+160);
                }
                preview.Save(Path.Combine(output,"face-guide-preview.png"),ImageFormat.Png);
            }
        }
        foreach(var c in chars)c.Dispose();
        Console.WriteLine("8 faces: fixed x44..212, y114..212, eye baseline y160. No gameplay or Unity changes.");
    }
}
'@
[FixedFaceAtlas]::Build($PSScriptRoot)
