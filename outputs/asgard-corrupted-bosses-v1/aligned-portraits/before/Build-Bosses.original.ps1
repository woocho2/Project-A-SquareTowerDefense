$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
public static class CorruptedAsgardBosses {
    static readonly string[] Keys={"SelfHeal","Defense","Mobility","Action","Locked","Hourglass"};
    static readonly string[] Labels={"SELF HEAL / RED","DEFENSE / YELLOW","MOBILITY / SKY BLUE","ACTION / GREEN","LOCKED / GRAY","HOURGLASS / PURPLE"};
    static Bitmap Extract(Bitmap source,int cell) {
        int w=source.Width/3,h=source.Height/2,cropHeight=h-Math.Max(2,h/50);
        var colors=new Color[w*cropHeight];var labels=new int[colors.Length];var counts=new List<int>();counts.Add(0);
        for(int y=0;y<cropHeight;y++)for(int x=0;x<w;x++)colors[y*w+x]=source.GetPixel(cell%3*w+x,cell/3*h+y);
        int label=0;var queue=new Queue<int>();
        for(int i=0;i<colors.Length;i++)if(colors[i].A>=32&&labels[i]==0) {
            label++;counts.Add(0);labels[i]=label;queue.Enqueue(i);
            while(queue.Count>0){int p=queue.Dequeue(),x=p%w,y=p/w;counts[label]++;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                    int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=cropHeight)continue;int n=yy*w+xx;
                    if(labels[n]==0&&colors[n].A>=32){labels[n]=label;queue.Enqueue(n);}
                }
            }
        }
        int largest=0;for(int i=1;i<counts.Count;i++)if(counts[i]>counts[largest])largest=i;
        if(largest==0||counts[largest]<w*cropHeight*.12)throw new Exception("Missing native boss portrait.");
        var keep=new bool[colors.Length];
        for(int y=0;y<cropHeight;y++)for(int x=0;x<w;x++)if(labels[y*w+x]==largest)
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++) {
                int xx=x+dx,yy=y+dy;if(xx>=0&&xx<w&&yy>=0&&yy<cropHeight)keep[yy*w+xx]=true;
            }
        var result=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        for(int y=0;y<cropHeight;y++)for(int x=0;x<w;x++)if(keep[y*w+x])result.SetPixel(x,y,colors[y*w+x]);
        return result;
    }
    static Rectangle Bounds(Bitmap bitmap) {
        int left=bitmap.Width,top=bitmap.Height,right=-1,bottom=-1;
        for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++)if(bitmap.GetPixel(x,y).A>=128) {
            left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);
        }
        return Rectangle.FromLTRB(left,top,right+1,bottom+1);
    }
    public static void Build(string folder,string projectRoot) {
        string valkyrie=Path.Combine(projectRoot,"Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateFaces_v1");
        var report=new StringBuilder();
        using(var outerSource=new Bitmap(Path.Combine(valkyrie,"Enemy_Valkyrie_Face_SelfHeal_v1.png")))
        using(var overview=new Bitmap(840,1216,PixelFormat.Format32bppArgb))using(var og=Graphics.FromImage(overview))
        using(var heading=new Font("Segoe UI",18,FontStyle.Bold))using(var labelFont=new Font("Segoe UI",10,FontStyle.Bold)) {
            og.Clear(Color.FromArgb(255,229,232,237));
            int bossIndex=0;
            foreach(string boss in new[]{"Brunhild","Loki"}) {
                using(var native=new Bitmap(Path.Combine(folder,"source/"+boss+".png")))
                using(var atlas=new Bitmap(1536,1024,PixelFormat.Format32bppArgb))
                using(var portraitAtlas=new Bitmap(1536,1024,PixelFormat.Format32bppArgb)) {
                    var portraits=new Bitmap[6];var union=Rectangle.Empty;
                    try {
                        for(int i=0;i<6;i++){portraits[i]=Extract(native,i);union=i==0?Bounds(portraits[i]):Rectangle.Union(union,Bounds(portraits[i]));}
                        double scale=Math.Min(452.0/union.Width,460.0/union.Height);
                        int width=(int)Math.Round(union.Width*scale),height=(int)Math.Round(union.Height*scale);
                        // One shared crop and one placement per boss keep all six color variants aligned.
                        var target=new Rectangle((512-width)/2,492-height,width,height);
                        report.AppendLine(boss+": shared native crop="+union+"; portrait target="+target+".");
                        og.DrawString("FALLEN "+boss.ToUpperInvariant(),heading,Brushes.DarkSlateGray,18,bossIndex*608+8);
                        for(int i=0;i<6;i++) {
                            using(var portrait=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                            using(var composite=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                            using(var reference=new Bitmap(Path.Combine(valkyrie,"Enemy_Valkyrie_Face_"+Keys[i]+"_v1.png")))
                            using(var bg=new Bitmap(Path.Combine(valkyrie,"Layers/Background_"+Keys[i]+".png")))
                            using(var decor=new Bitmap(Path.Combine(valkyrie,"Layers/Decoration_"+Keys[i]+".png"))) {
                                using(var g=Graphics.FromImage(portrait))using(var attrs=new ImageAttributes()) {
                                    g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                                    g.PixelOffsetMode=PixelOffsetMode.HighQuality;attrs.SetWrapMode(WrapMode.TileFlipXY);
                                    g.DrawImage(portraits[i],target,union.X,union.Y,union.Width,union.Height,GraphicsUnit.Pixel,attrs);
                                }
                                using(var g=Graphics.FromImage(composite)) {
                                    // Explicit pixel rectangles ignore the source PNG's differing DPI metadata.
                                    g.DrawImage(bg,new Rectangle(0,0,512,512),0,0,512,512,GraphicsUnit.Pixel);
                                    g.DrawImage(portrait,new Rectangle(0,0,512,512),0,0,512,512,GraphicsUnit.Pixel);
                                    g.DrawImage(decor,new Rectangle(0,0,512,512),0,0,512,512,GraphicsUnit.Pixel);
                                }
                                // Copy only the original round badge, never the old portrait around it.
                                for(int y=25;y<103;y++)for(int x=217;x<295;x++) {
                                    double dx=x+.5-256,dy=y+.5-64,distance=Math.Sqrt(dx*dx+dy*dy);
                                    if(distance>38)continue;
                                    Color original=reference.GetPixel(x,y),under=composite.GetPixel(x,y);
                                    double a=Math.Max(0,Math.Min(1,38-distance));
                                    if(distance<=37)composite.SetPixel(x,y,original);
                                    else composite.SetPixel(x,y,Color.FromArgb(255,(int)Math.Round(original.R*a+under.R*(1-a)),(int)Math.Round(original.G*a+under.G*(1-a)),(int)Math.Round(original.B*a+under.B*(1-a))));
                                }
                                for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                                    Color c=composite.GetPixel(x,y);int alpha=outerSource.GetPixel(x,y).A;
                                    composite.SetPixel(x,y,alpha==0?Color.Transparent:Color.FromArgb(alpha,c.R,c.G,c.B));
                                    if(composite.GetPixel(x,y).A!=outerSource.GetPixel(x,y).A)throw new Exception("Boss tile outer mask changed.");
                                    if(composite.GetPixel(x,y).A!=outerSource.GetPixel(y,x).A)throw new Exception("Asymmetric outer mask.");
                                }
                                if(Bounds(composite)!=new Rectangle(14,14,484,484))throw new Exception("Boss frame size differs from normal enemies.");
                                for(int y=27;y<101;y++)for(int x=219;x<293;x++)if(Math.Pow(x+.5-256,2)+Math.Pow(y+.5-64,2)<=37*37&&composite.GetPixel(x,y).ToArgb()!=reference.GetPixel(x,y).ToArgb())throw new Exception("Original badge core changed.");
                                using(var g=Graphics.FromImage(atlas))g.DrawImageUnscaled(composite,i%3*512,i/3*512);
                                using(var g=Graphics.FromImage(portraitAtlas))g.DrawImageUnscaled(portrait,i%3*512,i/3*512);
                                int px=18+i%3*274,py=bossIndex*608+44+i/3*278;
                                og.DrawImage(composite,new Rectangle(px,py,256,256),0,0,512,512,GraphicsUnit.Pixel);
                                og.DrawString(Labels[i],labelFont,Brushes.DarkSlateGray,px,py+256);
                                report.AppendLine(boss+"/"+Keys[i]+": outer (14,14,484,484); original decoration/background/badge reused.");
                            }
                        }
                        atlas.Save(Path.Combine(folder,"Enemy_Fallen"+boss+"_States_Atlas_v1.png"),ImageFormat.Png);
                        portraitAtlas.Save(Path.Combine(folder,boss+"-portraits-packed.png"),ImageFormat.Png);
                    } finally {foreach(var bitmap in portraits)if(bitmap!=null)bitmap.Dispose();}
                }
                bossIndex++;
            }
            overview.Save(Path.Combine(folder,"overview.png"),ImageFormat.Png);
        }
        report.AppendLine("Native imagegen produced the dark portraits and all six color versions; packing/compositing only uses System.Drawing.");
        report.AppendLine("All 12 state faces share the normal enemy outer alpha and original badges. Unity was not launched; prefab and runtime unchanged.");
        File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
    }
}
'@
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[CorruptedAsgardBosses]::Build($PSScriptRoot,$projectRoot)
