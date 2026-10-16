$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class AlignBossPortraits {
    static readonly string[] States={"SelfHeal","Defense","Mobility","Action","Locked","Hourglass"};
    static readonly double[] Hues={0,46,198,143,0,274};
    static void Draw(Graphics g, Image image, Rectangle dest, Rectangle src) {
        g.DrawImage(image,dest,src.X,src.Y,src.Width,src.Height,GraphicsUnit.Pixel);
    }
    static Color Tint(Color c,double hue,bool gray) {
        double max=Math.Max(c.R,Math.Max(c.G,c.B))/255.0,min=Math.Min(c.R,Math.Min(c.G,c.B))/255.0;
        double saturation=gray?0:(max==0?0:(max-min)/max);
        double chroma=max*saturation,x=chroma*(1-Math.Abs((hue/60)%2-1)),m=max-chroma,r=0,g=0,b=0;
        if(hue<60){r=chroma;g=x;} else if(hue<120){r=x;g=chroma;} else if(hue<180){g=chroma;b=x;}
        else if(hue<240){g=x;b=chroma;} else if(hue<300){r=x;b=chroma;} else {r=chroma;b=x;}
        return Color.FromArgb(c.A,(int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));
    }
    public static void Run(string folder,string root) {
        var log=new StringBuilder();
        using(var canonical=new Bitmap(Path.Combine(folder,"canonical-reference.png")))
        using(var nativeMask=new Bitmap(Path.Combine(folder,"equipment-mask-generated.png")))
        using(var mask=new Bitmap(1024,512,PixelFormat.Format32bppArgb))
        using(var overview=new Bitmap(840,1216,PixelFormat.Format32bppArgb))
        using(var og=Graphics.FromImage(overview))
        using(var title=new Font("Segoe UI",18,FontStyle.Bold))
        using(var label=new Font("Segoe UI",10,FontStyle.Bold)) {
            using(var mg=Graphics.FromImage(mask)){mg.InterpolationMode=InterpolationMode.HighQualityBilinear;Draw(mg,nativeMask,new Rectangle(0,0,1024,512),new Rectangle(0,0,nativeMask.Width,nativeMask.Height));}
            // Generated masks are semantic guides. Expand the guide to tolerate its
            // slight registration drift, then select exact original red pixels.
            var distance=new int[1024*512];
            for(int y=0;y<512;y++)for(int x=0;x<1024;x++)distance[y*1024+x]=mask.GetPixel(x,y).R>100?0:10000;
            for(int y=0;y<512;y++)for(int x=0;x<1024;x++) {
                int p=y*1024+x;
                if(x>0)distance[p]=Math.Min(distance[p],distance[p-1]+1);
                if(y>0)distance[p]=Math.Min(distance[p],distance[p-1024]+1);
            }
            for(int y=511;y>=0;y--)for(int x=1023;x>=0;x--) {
                int p=y*1024+x;
                if(x<1023)distance[p]=Math.Min(distance[p],distance[p+1]+1);
                if(y<511)distance[p]=Math.Min(distance[p],distance[p+1024]+1);
            }
            og.Clear(Color.FromArgb(229,232,237));
            int bossIndex=0;
            foreach(string boss in new[]{"Brunhild","Loki"}) {
                using(var atlas=new Bitmap(1536,1024,PixelFormat.Format32bppArgb))
                using(var portraits=new Bitmap(1536,1024,PixelFormat.Format32bppArgb))
                using(var refinedMask=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                using(var master=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
                    using(var g=Graphics.FromImage(master))Draw(g,canonical,new Rectangle(0,0,512,512),new Rectangle(bossIndex*512,0,512,512));
                    var selected=new bool[512*512];int count=0;
                    for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                        Color c=master.GetPixel(x,y);double hue=c.GetHue();
                        bool red=c.A>0&&c.R>28&&c.GetSaturation()>.4&&(hue<17||hue>340)&&c.R>c.G*1.55&&c.R>c.B*1.2;
                        bool near=red&&distance[y*1024+x+bossIndex*512]<=48;
                        selected[y*512+x]=near;
                        if(near)count++;
                        refinedMask.SetPixel(x,y,near?Color.White:Color.Black);
                    }
                    if(count<1000)throw new Exception("Missing equipment color selection: "+boss);
                    master.Save(Path.Combine(folder,boss+"-canonical.png"),ImageFormat.Png);
                    refinedMask.Save(Path.Combine(folder,boss+"-equipment-mask.png"),ImageFormat.Png);
                    og.DrawString("FALLEN "+boss.ToUpperInvariant()+" / ALIGNED",title,Brushes.DarkSlateGray,18,bossIndex*608+8);
                    for(int i=0;i<6;i++) {
                        using(var portrait=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                        using(var tile=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                        using(var reference=new Bitmap(Path.Combine(folder,"templates/Enemy_Valkyrie_Face_"+States[i]+"_v1.png")))
                        using(var bg=new Bitmap(Path.Combine(folder,"templates/Layers/Background_"+States[i]+".png")))
                        using(var decor=new Bitmap(Path.Combine(folder,"templates/Layers/Decoration_"+States[i]+".png"))) {
                            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                                Color c=master.GetPixel(x,y);
                                Color result=i>0&&selected[y*512+x]?Tint(c,Hues[i],i==4):c;
                                portrait.SetPixel(x,y,result);
                                if(result.A!=c.A)throw new Exception("Portrait silhouette changed.");
                                if(!selected[y*512+x]&&result.ToArgb()!=c.ToArgb())throw new Exception("Face/hair geometry or pixels changed.");
                            }
                            using(var g=Graphics.FromImage(tile)) {
                                Draw(g,bg,new Rectangle(0,0,512,512),new Rectangle(0,0,512,512));
                                Draw(g,portrait,new Rectangle(0,0,512,512),new Rectangle(0,0,512,512));
                                Draw(g,decor,new Rectangle(0,0,512,512),new Rectangle(0,0,512,512));
                            }
                            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                                Color c=tile.GetPixel(x,y),r=reference.GetPixel(x,y);
                                double dx=x+.5-256,dy=y+.5-64,d=Math.Sqrt(dx*dx+dy*dy);
                                if(d<=37)c=r;
                                else if(d<38){double a=38-d;c=Color.FromArgb(255,(int)Math.Round(r.R*a+c.R*(1-a)),(int)Math.Round(r.G*a+c.G*(1-a)),(int)Math.Round(r.B*a+c.B*(1-a)));}
                                tile.SetPixel(x,y,r.A==0?Color.Transparent:Color.FromArgb(r.A,c.R,c.G,c.B));
                            }
                            using(var g=Graphics.FromImage(atlas))Draw(g,tile,new Rectangle(i%3*512,i/3*512,512,512),new Rectangle(0,0,512,512));
                            using(var g=Graphics.FromImage(portraits))Draw(g,portrait,new Rectangle(i%3*512,i/3*512,512,512),new Rectangle(0,0,512,512));
                            int px=18+i%3*274,py=bossIndex*608+44+i/3*278;
                            Draw(og,tile,new Rectangle(px,py,256,256),new Rectangle(0,0,512,512));
                            og.DrawString(States[i],label,Brushes.DarkSlateGray,px,py+256);
                            log.AppendLine("PASS: "+boss+"/"+States[i]+": exact canonical alpha and non-accent RGBA; identical face, eyes, chin, hair and shoulder positions.");
                        }
                    }
                    atlas.Save(Path.Combine(folder,"Enemy_Fallen"+boss+"_States_Atlas_v1.png"),ImageFormat.Png);
                    portraits.Save(Path.Combine(folder,boss+"-portraits-packed.png"),ImageFormat.Png);
                    log.AppendLine(boss+": one unchanged canonical self-heal portrait for all six states; recolored equipment pixels="+count+".");
                }
                bossIndex++;
            }
            overview.Save(Path.Combine(folder,"preview.png"),ImageFormat.Png);
        }
        File.WriteAllText(Path.Combine(folder,"verification.txt"),log.ToString(),new UTF8Encoding(false));
        Console.Write(log.ToString());
    }
}
'@
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
[AlignBossPortraits]::Run($PSScriptRoot,$root)
