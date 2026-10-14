$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
public static class SquareSpawnDecorations {
    static readonly string[] Realms={"Asgard","Alfheim","Vanaheim","Midgard","Jotunheim","Nidavellir","Niflheim","Muspelheim","Hel","Ragnarok"};
    static Rectangle SpriteRect(string meta,string realm,int imageHeight) {
        var m=Regex.Match(meta,@"name: Tile_Path_Overlay_"+realm+@"_FRAME\r?\n\s+rect:\r?\n\s+serializedVersion: \d+\r?\n\s+x: (\d+)\r?\n\s+y: (\d+)\r?\n\s+width: (\d+)\r?\n\s+height: (\d+)");
        if(!m.Success)throw new Exception("Missing FRAME sprite: "+realm);
        int x=int.Parse(m.Groups[1].Value),y=int.Parse(m.Groups[2].Value),w=int.Parse(m.Groups[3].Value),h=int.Parse(m.Groups[4].Value);
        if(w!=256||h!=256)throw new Exception("Unexpected FRAME size.");return new Rectangle(x,imageHeight-y-h,w,h);
    }
    static Rectangle Bounds(Bitmap b,Rectangle rect) {
        int l=256,t=256,r=-1,bot=-1;
        for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(b.GetPixel(rect.X+x,rect.Y+y).A>=128) {
            l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);
        }
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Color Sample(Bitmap b,Rectangle rect,double x,double y,bool solid) {
        int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);double r=0,g=0,bl=0,a=0;
        for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++) {
            int xx=Math.Max(0,Math.Min(255,ix+dx)),yy=Math.Max(0,Math.Min(255,iy+dy));Color c=b.GetPixel(rect.X+xx,rect.Y+yy);
            double w=(dx==0?1-(x-ix):x-ix)*(dy==0?1-(y-iy):y-iy)*c.A;
            a+=w;r+=c.R*w;g+=c.G*w;bl+=c.B*w;
        }
        if(a>.1)return Color.FromArgb(solid?255:(int)Math.Round(a),(int)Math.Round(r/a),(int)Math.Round(g/a),(int)Math.Round(bl/a));
        if(!solid)return Color.Transparent;
        int ax=(int)Math.Round(x),ay=(int)Math.Round(y);
        for(int radius=1;radius<=40;radius++) {
            Color found=Color.Empty;double best=double.MaxValue;
            for(int dy=-radius;dy<=radius;dy++)for(int dx=-radius;dx<=radius;dx++) {
                if(Math.Abs(dx)!=radius&&Math.Abs(dy)!=radius)continue;
                int xx=ax+dx,yy=ay+dy;if(xx<0||xx>255||yy<0||yy>255)continue;
                Color c=b.GetPixel(rect.X+xx,rect.Y+yy);if(c.A<128)continue;
                double d=(xx-x)*(xx-x)+(yy-y)*(yy-y);if(d<best){best=d;found=c;}
            }
            if(!found.IsEmpty)return Color.FromArgb(255,found.R,found.G,found.B);
        }
        throw new Exception("No source decoration color.");
    }
    static int InsetMask(int x,int y) {
        int hits=0;for(int sy=0;sy<8;sy++)for(int sx=0;sx<8;sx++) {
            double px=x+(sx+.5)/8,py=y+(sy+.5)/8;if(px<10||px>=246||py<10||py>=246)continue;
            double cx=Math.Max(35,Math.Min(221,px)),cy=Math.Max(35,Math.Min(221,py));
            if((px-cx)*(px-cx)+(py-cy)*(py-cy)<=625)hits++;
        }return (hits*255+32)/64;
    }
    public static void Build(string folder) {
        var report=new StringBuilder();report.AppendLine("10 tower-spawn FRAME decorations corrected; all other atlas sprite cells unchanged.");
        report.AppendLine("Canvas=256x256; drawn bounds=(7,7,242,242); center=(128,128); outer radius=28px.");
        using(var outer=new Bitmap(Path.Combine(folder,"../tower-square-frames-v1/outer-mask256.png")))
        using(var reference=new Bitmap(Path.Combine(folder,"before/Tile_Path_Overlay_Asgard_Atlas.png")))
        using(var shared=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var preview=new Bitmap(1280,596,PixelFormat.Format32bppArgb)) {
            var refRect=SpriteRect(File.ReadAllText(Path.Combine(folder,"before/Tile_Path_Overlay_Asgard_Atlas.png.meta")),"Asgard",reference.Height);
            var refBounds=Bounds(reference,refRect);var raw=new int[256,256];
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)raw[x,y]=Sample(reference,refRect,refBounds.X+(x+.5-7)*refBounds.Width/242.0-.5,refBounds.Y+(y+.5-7)*refBounds.Height/242.0-.5,false).A;
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                // Share symmetric geometry while retaining each realm's original lighting/colors.
                int sum=raw[x,y]+raw[255-x,y]+raw[x,255-y]+raw[255-x,255-y]+raw[y,x]+raw[255-y,x]+raw[y,255-x]+raw[255-y,255-x];
                int oa=outer.GetPixel(x,y).A;int a=Math.Min(oa,Math.Max((sum+4)/8,oa-InsetMask(x,y)));
                if(a>0)shared.SetPixel(x,y,Color.FromArgb(a,255,255,255));
            }
            if(Bounds(shared,new Rectangle(0,0,256,256))!=new Rectangle(7,7,242,242))throw new Exception("Incorrect shared geometry.");
            for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                int a=shared.GetPixel(x,y).A;
                if(a!=shared.GetPixel(y,x).A||a!=shared.GetPixel(255-x,y).A||a!=shared.GetPixel(x,255-y).A)throw new Exception("Asymmetric decoration geometry.");
                if(a>outer.GetPixel(x,y).A)throw new Exception("Decoration exceeds square tile.");
            }
            shared.Save(Path.Combine(folder,"decoration-mask256.png"),ImageFormat.Png);
            using(var g=Graphics.FromImage(preview))using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var small=new Font("Segoe UI",12)) {
                g.Clear(Color.FromArgb(255,42,47,58));g.DrawString("타워 스폰 타일 + 테마 장식 · 외곽 242×242 · 중앙 정렬",font,Brushes.White,12,8);
                for(int i=0;i<Realms.Length;i++) {
                    string realm=Realms[i],name="Tile_Path_Overlay_"+realm+"_Atlas.png",input=Path.Combine(folder,"before/"+name);
                    using(var source=new Bitmap(input))using(var corrected=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb)) {
                        var rect=SpriteRect(File.ReadAllText(input+".meta"),realm,source.Height);var bounds=Bounds(source,rect);
                        for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                            int a=shared.GetPixel(x,y).A;Color c=Color.Transparent;
                            if(a>0){var rgb=Sample(source,rect,bounds.X+(x+.5-7)*bounds.Width/242.0-.5,bounds.Y+(y+.5-7)*bounds.Height/242.0-.5,true);c=Color.FromArgb(a,rgb.R,rgb.G,rgb.B);}
                            corrected.SetPixel(rect.X+x,rect.Y+y,c);
                        }
                        if(Bounds(corrected,rect)!=new Rectangle(7,7,242,242))throw new Exception("Incorrect bounds: "+realm);
                        int unchanged=0;for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++) {
                            if(rect.Contains(x,y)){if(corrected.GetPixel(x,y).A!=shared.GetPixel(x-rect.X,y-rect.Y).A)throw new Exception("Different decoration alpha.");}
                            else{if(source.GetPixel(x,y).ToArgb()!=corrected.GetPixel(x,y).ToArgb())throw new Exception("Changed other sprite.");unchanged++;}
                        }
                        corrected.Save(Path.Combine(folder,name),ImageFormat.Png);
                        int px=i%5*256,py=40+i/5*276;
                        using(var tile=new Bitmap(Path.Combine(folder,"../tower-spawn-square-v1/Tile_TowerSpawn_"+realm+".png")))g.DrawImageUnscaled(tile,px,py);
                        g.DrawImage(corrected,new Rectangle(px,py,256,256),rect.X,rect.Y,256,256,GraphicsUnit.Pixel);
                        g.DrawString(realm,small,Brushes.LightSkyBlue,px+12,py+252);
                        report.AppendLine(realm+": before="+bounds+"; after=(7,7,242,242); other "+unchanged+" pixels unchanged.");
                    }
                }
            }
            preview.Save(Path.Combine(folder,"preview.png"),ImageFormat.Png);
        }
        report.AppendLine("All 10 FRAME alpha masks identical and symmetric horizontally, vertically, and through 90-degree rotations.");
        report.AppendLine("Existing overlay PPU=256 with tile transform 1.3333334 gives same world canvas as floor PPU=192. Metadata and tile transforms unchanged.");
        report.AppendLine("Regular portrait canvas remains at user's previous 95% display setting; no runtime code changes. No Unity launch.");
        File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
    }
}
'@
[SquareSpawnDecorations]::Build($PSScriptRoot)
