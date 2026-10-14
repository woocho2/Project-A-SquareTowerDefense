$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class SpawnMatchedTowerPreview {
    static Rectangle Bounds(Bitmap b) {
        int l=b.Width,t=b.Height,r=-1,bot=-1;
        // Exclude the extraction's last scanlines, which are outside the intended bust crop.
        for(int y=0;y<b.Height-12;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=128){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Color Recolor(Color c,Color tone) {
        if(c.A==0)return Color.Transparent;
        double value=Math.Max(c.R,Math.Max(c.G,c.B))/255.0;
        double baseValue=Math.Max(tone.R,Math.Max(tone.G,tone.B))/255.0;
        double saturation=Math.Max(0,Math.Min(1,c.GetSaturation()*(tone.GetSaturation()/.8)));
        // Keep the original metal's luminance relief and hue-independent highlight placement.
        double v=Math.Max(0,Math.Min(1,value*(.93+.07*baseValue)));
        double chroma=v*saturation,h= tone.GetHue()/60.0;
        double xx=chroma*(1-Math.Abs(h%2-1)),m=v-chroma,r=0,g=0,b=0;
        if(h<1){r=chroma;g=xx;}else if(h<2){r=xx;g=chroma;}else if(h<3){g=chroma;b=xx;}else if(h<4){g=xx;b=chroma;}else if(h<5){r=xx;b=chroma;}else{r=chroma;b=xx;}
        return Color.FromArgb(c.A,(int)Math.Round((r+m)*255),(int)Math.Round((g+m)*255),(int)Math.Round((b+m)*255));
    }
    public static void Build(string folder) {
        string[] names={"BRONZE","SILVER","GOLD","MITHRIL","DIAMOND"};
        Color[] metals={Color.FromArgb(193,118,56),Color.FromArgb(180,192,207),Color.FromArgb(236,181,38),Color.FromArgb(41,185,195),Color.FromArgb(123,207,247)};
        Color[] backgrounds={Color.FromArgb(255,242,228),Color.FromArgb(242,245,249),Color.FromArgb(255,249,221),Color.FromArgb(234,250,247),Color.FromArgb(237,248,255)};
        using(var source=new Bitmap(Path.Combine(folder,"Source_Spawn_Decoration256.png")))
        using(var outer=new Bitmap(Path.Combine(folder,"../tower-square-frames-v1/outer-mask256.png")))
        using(var character=new Bitmap(Path.Combine(folder,"character-cutout.png")))
        using(var charCanvas=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var sheet=new Bitmap(1536,1024,PixelFormat.Format32bppArgb)) {
            Rectangle crop=Bounds(character);
            double scale=Math.Min(196.0/crop.Width,196.0/crop.Height);
            int cw=(int)Math.Round(crop.Width*scale),ch=(int)Math.Round(crop.Height*scale);
            var placement=new Rectangle((256-cw)/2,230-ch,cw,ch);
            using(var g=Graphics.FromImage(charCanvas)) {
                g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                using(var attrs=new ImageAttributes()) {attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(character,placement,crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attrs);}
            }
            charCanvas.Save(Path.Combine(folder,"Character_Layer256.png"),ImageFormat.Png);
            var report=new StringBuilder();report.AppendLine("Decoration source: actual Tile_Path_Overlay_Asgard_FRAME sprite, x512,y0,w256,h256.");
            report.AppendLine("Same 256x256 canvas and original decoration coordinates. No translation or scaling of decoration pixels.");
            report.AppendLine("The original corner plates AND original thin connecting edge are preserved; no redesigned L-shaped caps.");
            using(var sg=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",22,FontStyle.Bold))using(var small=new Font("Malgun Gothic",16))using(var fmt=new StringFormat()) {
                fmt.Alignment=StringAlignment.Center;sg.Clear(Color.FromArgb(255,252,249,242));sg.InterpolationMode=InterpolationMode.NearestNeighbor;sg.PixelOffsetMode=PixelOffsetMode.Half;
                for(int i=0;i<5;i++)using(var decor=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var bg=new Bitmap(256,256,PixelFormat.Format32bppArgb))using(var tile=new Bitmap(256,256,PixelFormat.Format32bppArgb)) {
                    int differentAlpha=0;
                    for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                        Color c=source.GetPixel(x,y);Color tinted=i==2?c:Recolor(c,metals[i]);decor.SetPixel(x,y,tinted);
                        if(tinted.A!=c.A)differentAlpha++;
                        Color baseColor=backgrounds[i];int a=outer.GetPixel(x,y).A;
                        bg.SetPixel(x,y,Color.FromArgb(a,baseColor.R,baseColor.G,baseColor.B));
                    }
                    if(differentAlpha!=0)throw new Exception("Original decoration alpha changed.");
                    if(i==2)for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(decor.GetPixel(x,y).ToArgb()!=source.GetPixel(x,y).ToArgb())throw new Exception("Gold source pixels changed.");
                    using(var g=Graphics.FromImage(tile)){g.DrawImageUnscaled(bg,0,0);g.DrawImageUnscaled(charCanvas,0,0);g.DrawImageUnscaled(decor,0,0);}
                    tile.Save(Path.Combine(folder,names[i]+"_Preview256.png"),ImageFormat.Png);
                    decor.Save(Path.Combine(folder,names[i]+"_Decoration256.png"),ImageFormat.Png);
                    bg.Save(Path.Combine(folder,names[i]+"_Background256.png"),ImageFormat.Png);
                    int px=i<3?96+i*480:336+(i-3)*480,py=i<3?36:536;
                    sg.DrawImage(tile,new Rectangle(px,py,384,384),0,0,256,256,GraphicsUnit.Pixel);
                    using(var brush=new SolidBrush(metals[i]))sg.DrawString(names[i],font,brush,new RectangleF(px,py+396,384,44),fmt);
                    report.AppendLine(names[i]+": decoration alpha differences=0; identical placement, geometry, thickness and character layer.");
                }
                sg.DrawString("기존 스폰 타일 장식 원본 배치 · 색상만 변경",small,Brushes.DimGray,new RectangleF(0,980,1536,32),fmt);
            }
            sheet.Save(Path.Combine(folder,"Tower_Tier_Actual_Spawn_Decoration_Concept_v6.png"),ImageFormat.Png);
            report.AppendLine("GOLD decoration: complete RGBA equality with original spawn decoration verified.");
            report.AppendLine("Background: common square outer mask; portrait cutout: native imagegen; compositing/recolor: System.Drawing.");
            report.AppendLine("Preview only. Assets and runtime unchanged. No Unity launch.");
            File.WriteAllText(Path.Combine(folder,"verification.txt"),report.ToString(),new UTF8Encoding(false));Console.Write(report.ToString());
        }
    }
}
'@
[SpawnMatchedTowerPreview]::Build($PSScriptRoot)
