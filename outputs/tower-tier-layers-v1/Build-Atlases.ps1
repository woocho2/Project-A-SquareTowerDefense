$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class TowerTierLayers {
    static Bitmap Normalize(string input) {
        var output=new Bitmap(768,512,PixelFormat.Format32bppArgb);
        using(var source=new Bitmap(input))using(var g=Graphics.FromImage(output)) {
            g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            g.DrawImage(source,new Rectangle(0,0,768,512));
        }
        return output;
    }
    static Rectangle Bounds(Bitmap mask) {
        int left=256,top=256,right=-1,bottom=-1;
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(mask.GetPixel(x,y).A>=128) {
            left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);
        }
        return Rectangle.FromLTRB(left,top,right+1,bottom+1);
    }
    public static void Build(string folder) {
        string[] tiers={"Bronze","Silver","Gold","Mithril","Diamond","Synergy"};
        using(var nativeDecor=Normalize(Path.Combine(folder,"decorations-native.png")))
        using(var nativeBackground=Normalize(Path.Combine(folder,"backgrounds-native.png")))
        using(var decorMask=new Bitmap(Path.Combine(folder,"spawn-decoration256.png")))
        using(var backgroundMask=new Bitmap(Path.Combine(folder,"spawn-background256.png")))
        using(var backgrounds=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        using(var decorations=new Bitmap(768,512,PixelFormat.Format32bppArgb))
        using(var preview=new Bitmap(1600,920,PixelFormat.Format32bppArgb))
        using(var characterAtlas=new Bitmap(Path.Combine(folder,"../../Assets/4. Asset/2. Tower/Portraits_SD_Front_v2/Tower_Sword_Front_Atlas_v2.png")))
        using(var hero=characterAtlas.Clone(new Rectangle(0,0,256,256),PixelFormat.Format32bppArgb)) {
            var expected=new Rectangle(7,7,242,242);
            if(Bounds(decorMask)!=expected||Bounds(backgroundMask)!=expected)throw new Exception("Spawn geometry differs from expected square.");
            var report=new StringBuilder();
            report.AppendLine("2 atlases, 6 sprites per atlas, 768x512, grid 3x2, cells 256x256.");
            report.AppendLine("Both layers use actual current spawn-tile alpha masks. Visible bounds=(7,7,242,242). Shared center=(128,128).");
            using(var pg=Graphics.FromImage(preview))using(var font=new Font("Segoe UI",15,FontStyle.Bold))using(var small=new Font("Segoe UI",11)) {
                pg.Clear(Color.FromArgb(255,232,235,240));
                pg.DrawString("TOWER BACKGROUND / DECORATION / PORTRAIT + FRONT DECORATION",font,Brushes.DarkSlateGray,24,14);
                for(int i=0;i<6;i++) {
                    int ox=i%3*256,oy=i/3*256;
                    using(var bg=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var deco=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var combined=new Bitmap(256,256,PixelFormat.Format32bppArgb)) {
                        for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                            int ba=backgroundMask.GetPixel(x,y).A,da=decorMask.GetPixel(x,y).A;
                            // Sample the native fill away from its generated edge so the
                            // background stays free of outlines and texture in the separate layer.
                            int sampleY=16+(int)Math.Round(Math.Max(0,Math.Min(242,y-7))*223.0/242);
                            var bc=nativeBackground.GetPixel(ox+128,oy+sampleY);var dc=nativeDecor.GetPixel(ox+x,oy+y);
                            // Native imagegen supplies the colors and shading; original sprite alpha
                            // restores exact pixel geometry and removes any generated center fill.
                            var b=ba==0?Color.Transparent:Color.FromArgb(ba,bc.R,bc.G,bc.B);
                            var d=da==0?Color.Transparent:Color.FromArgb(da,dc.R,dc.G,dc.B);
                            bg.SetPixel(x,y,b);deco.SetPixel(x,y,d);
                            backgrounds.SetPixel(ox+x,oy+y,b);decorations.SetPixel(ox+x,oy+y,d);
                            if(da>ba)throw new Exception("Decoration escapes background.");
                            if(ba!=backgroundMask.GetPixel(y,x).A||ba!=backgroundMask.GetPixel(255-x,y).A||da!=decorMask.GetPixel(y,x).A||da!=decorMask.GetPixel(255-x,y).A)throw new Exception("Asymmetric source alpha geometry.");
                        }
                        if(Bounds(bg)!=expected||Bounds(deco)!=expected||deco.GetPixel(128,128).A!=0||bg.GetPixel(128,128).A!=255)throw new Exception("Layer separation or square bounds failed.");
                        using(var g=Graphics.FromImage(combined)){g.DrawImageUnscaled(bg,0,0);g.DrawImageUnscaled(hero,0,0);g.DrawImageUnscaled(deco,0,0);}
                        int px=20+i%2*800,py=55+i/2*286;
                        pg.DrawString(tiers[i],font,Brushes.DarkSlateGray,px,py);
                        pg.DrawImageUnscaled(bg,px,py+28);pg.DrawImageUnscaled(deco,px+262,py+28);pg.DrawImageUnscaled(combined,px+524,py+28);
                        var center=bg.GetPixel(128,128);
                        report.AppendLine(tiers[i]+": identical alpha in each layer; background center RGB="+center.R+","+center.G+","+center.B+"; decoration center alpha=0.");
                    }
                }
            }
            backgrounds.Save(Path.Combine(folder,"Tower_Backgrounds_Atlas_v1.png"),ImageFormat.Png);
            decorations.Save(Path.Combine(folder,"Tower_Decorations_Atlas_v1.png"),ImageFormat.Png);
            preview.Save(Path.Combine(folder,"preview.png"),ImageFormat.Png);
            report.AppendLine("Native imagegen art normalized and packed with original masks. No source atlas, portrait, prefab or runtime code changed. Unity was not launched.");
            File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
        }
    }
}
'@
[TowerTierLayers]::Build($PSScriptRoot)
