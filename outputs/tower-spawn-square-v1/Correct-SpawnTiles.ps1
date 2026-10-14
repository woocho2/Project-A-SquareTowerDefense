$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class SquareSpawnTiles {
    static Rectangle Bounds(Bitmap b,Rectangle rect,int threshold) {
        int l=256,t=256,r=-1,bot=-1;
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(b.GetPixel(rect.X+x,rect.Y+y).A>=threshold) {
            l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);
        }
        if(r<l)throw new Exception("Empty source tile.");
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Color Sample(Bitmap b,Rectangle rect,double x,double y) {
        int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double r=0,g=0,bl=0,total=0;
        for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++) {
            int xx=Math.Max(0,Math.Min(255,ix+dx)),yy=Math.Max(0,Math.Min(255,iy+dy));
            Color c=b.GetPixel(rect.X+xx,rect.Y+yy);
            double w=(dx==0?1-(x-ix):x-ix)*(dy==0?1-(y-iy):y-iy)*c.A/255.0;
            total+=w;r+=c.R*w;g+=c.G*w;bl+=c.B*w;
        }
        if(total>.001)return Color.FromArgb(255,(int)Math.Round(r/total),(int)Math.Round(g/total),(int)Math.Round(bl/total));
        int ax=(int)Math.Round(x),ay=(int)Math.Round(y);
        for(int radius=1;radius<=32;radius++) {
            Color found=Color.Empty;double best=double.MaxValue;
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++) {
                if(Math.Abs(dx)!=radius&&Math.Abs(dy)!=radius)continue;
                int xx=ax+dx,yy=ay+dy;if(xx<0||xx>=256||yy<0||yy>=256)continue;
                Color c=b.GetPixel(rect.X+xx,rect.Y+yy);if(c.A<200)continue;
                double d=(xx-x)*(xx-x)+(yy-y)*(yy-y);if(d<best){best=d;found=c;}
            }
            if(!found.IsEmpty)return Color.FromArgb(255,found.R,found.G,found.B);
        }
        throw new Exception("No color near source edge.");
    }
    public static void Build(string folder) {
        string input=Path.Combine(folder,"before/Tile_Floor_Atlas.png");
        string meta=File.ReadAllText(input+".meta");
        var matches=Regex.Matches(meta,@"name: (Tile_TowerSpawn_\w+)\r?\n\s+rect:\r?\n\s+serializedVersion: \d+\r?\n\s+x: (\d+)\r?\n\s+y: (\d+)\r?\n\s+width: (\d+)\r?\n\s+height: (\d+)");
        if(matches.Count!=10)throw new Exception("Expected 10 spawn sprites; found "+matches.Count);
        using(var original=new Bitmap(input))using(var corrected=original.Clone(new Rectangle(0,0,original.Width,original.Height),PixelFormat.Format32bppArgb))
        using(var mask=new Bitmap(Path.Combine(folder,"../tower-square-frames-v1/outer-mask256.png")))
        using(var frames=new Bitmap(Path.Combine(folder,"../tower-square-frames-v1/Tower_Frames_Atlas_v1.png"))) {
            var rects=new Rectangle[10];var names=new string[10];var selected=new bool[original.Width,original.Height];
            var report=new StringBuilder();
            report.AppendLine("Source: Tile_Floor_Atlas.png; only 10 Tile_TowerSpawn_* sprite cells edited.");
            report.AppendLine("Atlas: "+original.Width+"x"+original.Height+"; sprite cells: 256x256 (unchanged).");
            report.AppendLine("Target drawn bounds: x=7,y=7,width=242,height=242; center=(128,128) on pixel edges.");
            report.AppendLine("Exact same alpha mask as corrected tower frames; rounded corner radius 28px.");
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                int a=mask.GetPixel(x,y).A;
                if(a!=mask.GetPixel(y,x).A||a!=mask.GetPixel(255-x,y).A||a!=mask.GetPixel(x,255-y).A)throw new Exception("Non-symmetric reference mask.");
                if(a!=frames.GetPixel(x,y).A)throw new Exception("Frame reference does not match mask.");
            }
            for(int i=0;i<10;i++) {
                var m=matches[i];names[i]=m.Groups[1].Value;
                int x0=int.Parse(m.Groups[2].Value),y0=int.Parse(m.Groups[3].Value),w=int.Parse(m.Groups[4].Value),h=int.Parse(m.Groups[5].Value);
                if(w!=256||h!=256)throw new Exception("Unexpected sprite rect size.");
                rects[i]=new Rectangle(x0,original.Height-y0-h,w,h);var rect=rects[i];
                var before=Bounds(original,rect,128);
                for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                    selected[rect.X+x,rect.Y+y]=true;
                    int a=mask.GetPixel(x,y).A;
                    Color c=Color.Transparent;
                    if(a>0) {
                        double sx=before.X+(x+.5-7)*before.Width/242.0-.5;
                        double sy=before.Y+(y+.5-7)*before.Height/242.0-.5;
                        Color rgb=Sample(original,rect,sx,sy);c=Color.FromArgb(a,rgb.R,rgb.G,rgb.B);
                    }
                    corrected.SetPixel(rect.X+x,rect.Y+y,c);
                }
                var after=Bounds(corrected,rect,128);
                if(after!=new Rectangle(7,7,242,242))throw new Exception("Incorrect visible bounds: "+names[i]);
                if(Bounds(corrected,rect,1)!=after)throw new Exception("Stray low-alpha pixels.");
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                    if(corrected.GetPixel(rect.X+x,rect.Y+y).A!=mask.GetPixel(x,y).A)throw new Exception("Incorrect alpha mask.");
                using(var tile=corrected.Clone(rect,PixelFormat.Format32bppArgb))tile.Save(Path.Combine(folder,names[i]+".png"),ImageFormat.Png);
                report.AppendLine(names[i]+": before="+before+"; after="+after+"; shared alpha verified.");
            }
            int untouched=0;
            for(int y=0;y<original.Height;y++)for(int x=0;x<original.Width;x++)if(!selected[x,y]) {
                if(original.GetPixel(x,y).ToArgb()!=corrected.GetPixel(x,y).ToArgb())throw new Exception("Changed pixel outside spawn sprites.");untouched++;
            }
            corrected.Save(Path.Combine(folder,"Tile_Floor_Atlas.png"),ImageFormat.Png);
            using(var p=new Bitmap(1280,1132,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(p))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var small=new Font("Segoe UI",12)) {
                g.Clear(Color.FromArgb(255,42,47,58));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawString("보정 전 · 실제 그림 244×242",font,Brushes.White,12,8);
                g.DrawString("보정 후 · 전부 242×242 · 중앙 정렬 · 동일한 둥근 모서리",font,Brushes.White,12,564);
                for(int i=0;i<10;i++) {
                    int px=i%5*256,py=i/5*256;var rect=rects[i];
                    g.DrawImage(original,new Rectangle(px,40+py,256,256),rect.X,rect.Y,256,256,GraphicsUnit.Pixel);
                    g.DrawImage(corrected,new Rectangle(px,596+py,256,256),rect.X,rect.Y,256,256,GraphicsUnit.Pixel);
                    string name=names[i].Replace("Tile_TowerSpawn_","");
                    g.DrawString(name,small,Brushes.LightSkyBlue,px+12,40+py+232);
                    g.DrawString(name,small,Brushes.LightSkyBlue,px+12,596+py+232);
                }
                p.Save(Path.Combine(folder,"comparison.png"),ImageFormat.Png);
            }
            using(var p=new Bitmap(512,552,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(p))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold)) {
                g.Clear(Color.FromArgb(255,42,47,58));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
                g.DrawString("스폰 타일 + 타워 프레임 · 외곽 일치",font,Brushes.White,12,8);
                var rect=rects[0];g.DrawImage(corrected,new Rectangle(0,40,512,512),rect.X,rect.Y,256,256,GraphicsUnit.Pixel);
                g.DrawImage(frames,new Rectangle(0,40,512,512),0,0,256,256,GraphicsUnit.Pixel);
                p.Save(Path.Combine(folder,"frame-overlay.png"),ImageFormat.Png);
            }
            report.AppendLine("Untouched pixels outside spawn cells: "+untouched+"; exact RGBA equality verified.");
            report.AppendLine("All 10 masks are identical; square bounds verified at alpha>=1 and >=128; horizontal/vertical/90-degree geometry symmetry verified.");
            report.AppendLine("Original colors/shading resampled; color gradients need not be symmetric. Metadata/IDs and runtime code unchanged. No Unity launch.");
            File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
        }
    }
}
'@
[SquareSpawnTiles]::Build($PSScriptRoot)
