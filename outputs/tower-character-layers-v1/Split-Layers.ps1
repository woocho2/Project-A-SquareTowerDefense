$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class TowerLayerPack
{
    static Rectangle Bounds(Bitmap b,Rectangle region)
    {
        int l=region.Right,t=region.Bottom,r=region.X,bot=region.Y;
        for(int y=region.Y+2;y<region.Bottom-2;y++)for(int x=region.X+2;x<region.Right-2;x++){
            if(b.GetPixel(x,y).A<200)continue;l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);
        }
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static byte[,] Labels(Bitmap mask,Rectangle region)
    {
        int w=region.Width,h=region.Height;var labels=new byte[w,h];var queue=new Queue<Point>();
        for(int y=0;y<h;y++)for(int x=0;x<w;x++){
            var c=mask.GetPixel(region.X+x,region.Y+y);if(c.A<100)continue;
            if(c.G>100&&c.G>c.R+60&&c.G>c.B+60)labels[x,y]=1;
            else if(c.R>100&&c.B>100&&c.R>c.G+60&&c.B>c.G+60)labels[x,y]=2;
            if(labels[x,y]!=0)queue.Enqueue(new Point(x,y));
        }
        while(queue.Count>0){var p=queue.Dequeue();int[] xx={p.X-1,p.X+1,p.X,p.X};int[] yy={p.Y,p.Y,p.Y-1,p.Y+1};
            for(int i=0;i<4;i++){int x=xx[i],y=yy[i];if(x<0||y<0||x>=w||y>=h||labels[x,y]!=0)continue;labels[x,y]=labels[p.X,p.Y];queue.Enqueue(new Point(x,y));}}
        return labels;
    }
    static bool FaceCorrection(Color c,int index,int x,int y)
    {
        if(index==3&&x>=60&&x<=196&&y>=132&&y<=199&&c.R>c.G+15&&c.B>c.G+25)return true;
        if(index==4&&x>=40&&x<=218&&y>=211)return true;
        if(index<4&&x>=60&&x<=196&&y>=146&&y<=211)return true;
        if(index==4&&x>=56&&x<=200&&y>=151&&y<=228)return true;
        if(index==5&&x>=48&&x<=208&&y>=146&&y<=212){
            bool hair=c.R<105&&c.G<105&&c.B<105;
            bool paint=c.B>150&&c.B>c.R+50;
            bool skin=c.R>180&&c.G>110&&c.B>85&&c.G-c.B<75;
            return hair||paint||skin;
        }
        if(index==6&&x>=60&&x<=196&&y>=142&&y<=173)return true;
        if(index==7&&x>=60&&x<=196&&y>=148&&y<=177)return true;
        return false;
    }
    public static void Build(string output,string rimSource)
    {
        var baseLayers=new Bitmap[2];var topLayers=new Bitmap[2];
        string[] weapon={"Sword","Axe"};
        using(var frames=new Bitmap(Path.Combine(output,"before/Frames.png")))using(var rims=new Bitmap(rimSource))
        {
            rims.Save(Path.Combine(output,"Tower_Frame_Rims_Atlas_v1.png"),ImageFormat.Png);
            for(int k=0;k<2;k++)using(var source=new Bitmap(Path.Combine(output,"before/"+weapon[k]+".png")))
            using(var mask=new Bitmap(Path.Combine(output,k==0?"sword-mask.png":"axe-mask.png")))
            {
                baseLayers[k]=new Bitmap(512,512,PixelFormat.Format32bppArgb);topLayers[k]=new Bitmap(512,512,PixelFormat.Format32bppArgb);
                int cell=mask.Width/2;
                for(int i=0;i<4;i++){
                    var sb=Bounds(source,new Rectangle(i%2*256,i/2*256,256,256));
                    var mr=new Rectangle(i%2*cell,i/2*cell,cell,cell);var mb=Bounds(mask,mr);var labels=Labels(mask,mr);
                    int back=0,front=0;
                    for(int y=0;y<256;y++)for(int x=0;x<256;x++){
                        int ax=i%2*256+x,ay=i/2*256+y;var c=source.GetPixel(ax,ay);if(c.A==0)continue;
                        int mx=(int)Math.Round(mb.X-mr.X+(ax-sb.X)*(mb.Width-1.0)/(sb.Width-1));
                        int my=(int)Math.Round(mb.Y-mr.Y+(ay-sb.Y)*(mb.Height-1.0)/(sb.Height-1));
                        mx=Math.Max(0,Math.Min(cell-1,mx));my=Math.Max(0,Math.Min(cell-1,my));
                        bool isBase=labels[mx,my]==1||FaceCorrection(c,k*4+i,x,y);
                        (isBase?baseLayers[k]:topLayers[k]).SetPixel(ax,ay,c);if(isBase)back++;else front++;
                    }
                    if(back<3000||front<1500)throw new Exception("Empty layer in "+weapon[k]+" "+i);
                    Console.WriteLine(weapon[k]+" "+i+": base "+back+", front "+front+" pixels");
                }
                // Partition validation: each original non-transparent pixel belongs to exactly one layer.
                for(int y=0;y<512;y++)for(int x=0;x<512;x++){
                    var s=source.GetPixel(x,y);var a=baseLayers[k].GetPixel(x,y);var b=topLayers[k].GetPixel(x,y);
                    if(a.A>0&&b.A>0)throw new Exception("Overlapping partition");
                    var c=a.A>0?a:b;if(s.A>0&&c.ToArgb()!=s.ToArgb())throw new Exception("Original RGBA changed");
                    if(s.A==0&&c.A!=0)throw new Exception("Background introduced");
                }
                baseLayers[k].Save(Path.Combine(output,"Tower_"+weapon[k]+"_FaceHair_Unclipped_Atlas_v1.png"),ImageFormat.Png);
                // Preclip only the rear layer to the shared frame opening.
                // This prevents hair/beard tips peeking beyond the outer frame,
                // without requiring a new runtime mask component.
                int clipped=0;
                for(int y=0;y<512;y++)for(int x=0;x<512;x++){
                    var c=baseLayers[k].GetPixel(x,y);if(c.A==0)continue;
                    int open=Math.Max(0,frames.GetPixel(x%256,y%256).A-rims.GetPixel(x%256,y%256).A);
                    int alpha=(c.A*open+127)/255;
                    if(alpha!=c.A){clipped++;baseLayers[k].SetPixel(x,y,Color.FromArgb(alpha,c.R,c.G,c.B));}
                }
                Console.WriteLine(weapon[k]+": "+clipped+" rear-layer pixels masked by the common frame opening; unclipped copy archived.");
                baseLayers[k].Save(Path.Combine(output,"Tower_"+weapon[k]+"_FaceHair_Atlas_v1.png"),ImageFormat.Png);
                topLayers[k].Save(Path.Combine(output,"Tower_"+weapon[k]+"_HeadgearDecor_Atlas_v1.png"),ImageFormat.Png);
            }
            using(var preview=new Bitmap(1024,648,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var fmt=new StringFormat())
            {
                fmt.Alignment=StringAlignment.Center;g.Clear(Color.FromArgb(255,29,33,44));
                string[] names={"히어로","검객","팔라딘","데스나이트","바이킹","버서커","스칼드","집행관"};
                for(int i=0;i<8;i++){
                    int k=i/4,j=i%4,x=j*256,y=k*300+36;var cr=new Rectangle(j%2*256,j/2*256,256,256);
                    g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(x,y-34,256,30),fmt);
                    g.DrawImage(frames,new Rectangle(x,y,256,256),0,0,256,256,GraphicsUnit.Pixel);
                    g.DrawImage(baseLayers[k],new Rectangle(x,y,256,256),cr.X,cr.Y,256,256,GraphicsUnit.Pixel);
                    g.DrawImage(rims,new Rectangle(x,y,256,256),0,0,256,256,GraphicsUnit.Pixel);
                    g.DrawImage(topLayers[k],new Rectangle(x,y,256,256),cr.X,cr.Y,256,256,GraphicsUnit.Pixel);
                }
                g.DrawString("흰 배경 → 얼굴·머리카락 → 테두리 → 투구·장식",font,Brushes.LightSkyBlue,new RectangleF(0,609,1024,32),fmt);
                preview.Save(Path.Combine(output,"preview.png"),ImageFormat.Png);
            }
            using(var preview=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview)){
                g.Clear(Color.FromArgb(255,52,57,67));
                for(int k=0;k<2;k++){g.DrawImageUnscaled(baseLayers[k],0,k*512);g.DrawImageUnscaled(topLayers[k],512,k*512);}
                preview.Save(Path.Combine(output,"layers-preview.png"),ImageFormat.Png);
            }
        }
        foreach(var b in baseLayers)b.Dispose();foreach(var b in topLayers)b.Dispose();
        Console.WriteLine("Original RGBA preserved in all 8 partitions. No Unity or runtime changes.");
    }
}
'@
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[TowerLayerPack]::Build($PSScriptRoot,(Join-Path $taskRoot 'outputs/tower-face-alignment-v1/before/Tower_Frames_Atlas_v1.png'))
