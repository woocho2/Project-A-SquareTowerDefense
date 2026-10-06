$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class EmblemAtlasPack {
 static void Quality(Graphics g){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;}
 static Bitmap Tint(Bitmap b,Color color){
  var r=new Bitmap(b.Width,b.Height,PixelFormat.Format32bppArgb);
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)r.SetPixel(x,y,Color.FromArgb(b.GetPixel(x,y).A,color.R,color.G,color.B));
  return r;
 }
 static Bitmap Sword(Bitmap source){
  int left=source.Width,top=source.Height,right=-1,bottom=-1;
  for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)if(source.GetPixel(x,y).A>=128){
   left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);
  }
  if(right<left)throw new Exception("Sword alpha is empty");
  left=Math.Max(0,left-3);top=Math.Max(0,top-3);right=Math.Min(source.Width-1,right+3);bottom=Math.Min(source.Height-1,bottom+3);
  int w=right-left+1,h=bottom-top+1;float s=Math.Min(160f/w,180f/h);
  using(var small=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(small)){Quality(g);g.CompositingMode=CompositingMode.SourceCopy;
    g.DrawImage(source,new RectangleF(128-w*s/2,128-h*s/2,w*s,h*s),new RectangleF(left,top,w,h),GraphicsUnit.Pixel);
   }
   return Tint(small,Color.White);
  }
 }
 public static string Run(string root,string project){
  string[] names={"Sword","Bow","Shield","Spear","Axe","Hammer","Fire","Ice","Electricity","Wind","Earth","Light","Dark"};
  string[] tiers={"Bronze","Silver","Gold","Mithril","Diamond"};
  var report=new StringBuilder();
  using(var source=new Bitmap(root+"/AI_Sword_Source.png"))using(var sword=Sword(source))
   sword.Save(root+"/individual/01_Sword_White_256.png",ImageFormat.Png);
  using(var atlas=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
  using(var preview=new Bitmap(1120,1420,PixelFormat.Format32bppArgb))
  using(var ga=Graphics.FromImage(atlas))
  using(var gp=Graphics.FromImage(preview))
  using(var font=new Font("Arial",13))
  using(var gold=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/Gold.png")){
   ga.Clear(Color.Transparent);ga.CompositingMode=CompositingMode.SourceCopy;
   gp.Clear(Color.FromArgb(238,240,244));Quality(gp);
   for(int i=0;i<names.Length;i++){
    string name=names[i],file=root+"/individual/"+(i+1).ToString("D2")+"_"+name+"_White_256.png";
    using(var mask=new Bitmap(file)){
     if(mask.Width!=256||mask.Height!=256)throw new Exception("Unexpected size: "+name);
     int nonWhite=0,visible=0,border=0;
     for(int y=0;y<256;y++)for(int x=0;x<256;x++){
      Color p=mask.GetPixel(x,y);
      if(p.A>0){visible++;if(p.R!=255||p.G!=255||p.B!=255)nonWhite++;if(x==0||x==255||y==0||y==255)border++;}
     }
     if(nonWhite!=0||visible==0||border!=0)throw new Exception("White mask invalid: "+name);
     int col=i%4,row=i/4;
     // Exact ARGB copy avoids GDI+ alpha rounding while packing cells.
     for(int y=0;y<256;y++)for(int x=0;x<256;x++)atlas.SetPixel(col*256+x,row*256+y,mask.GetPixel(x,y));
     int px=col*280+12,py=row*350+30;
     gp.DrawString((i+1).ToString("D2")+" "+name,font,Brushes.Black,px,py-24);
     using(var navy=new SolidBrush(Color.FromArgb(25,35,51)))gp.FillRectangle(navy,px,py,256,220);
     gp.DrawImage(mask,new Rectangle(px+28,py+8,200,200));
     using(var red=Tint(mask,ColorTranslator.FromHtml("#E25D62")))
     using(var tile=new Bitmap(280,280,PixelFormat.Format32bppArgb)){
      using(var g=Graphics.FromImage(tile)){
       g.Clear(Color.White);Quality(g);g.DrawImage(red,new Rectangle(44,44,192,192));
       g.DrawImage(gold,new RectangleF(12+128-128*1.06f,12+128-128*1.045f+2.46f,256*1.06f,256*1.045f));
      }
      gp.DrawImage(tile,new Rectangle(px+94,py+232,68,68));
     }
     report.AppendLine((i+1)+" "+name+": 256x256; visible RGB white; transparent border; atlas top-origin=("+(col*256)+","+(row*256)+")");
     foreach(string t in tiers){
      using(var raw=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/"+t+".png"))
      using(var tier=new Bitmap(256,256,PixelFormat.Format32bppArgb))
      using(var icon=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
       using(var g=Graphics.FromImage(tier)){Quality(g);g.DrawImage(raw,new RectangleF(128-128*1.06f,128-128*1.045f+2.46f,256*1.06f,256*1.045f));}
       using(var g=Graphics.FromImage(icon)){Quality(g);g.DrawImage(mask,new Rectangle(32,32,192,192));}
       int hidden=0;for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(icon.GetPixel(x,y).A>=128&&tier.GetPixel(x,y).A>32)hidden++;
       report.AppendLine("  "+t+": hidden opaque icon pixels="+hidden);
       if(hidden!=0)throw new Exception("Frame overlaps "+name+"/"+t);
      }
     }
    }
   }
   atlas.Save(root+"/Tower_Emblem_White_Atlas_1024.png",ImageFormat.Png);
   preview.Save(root+"/Tower_Emblem_Atlas_Preview.png",ImageFormat.Png);
  }
  using(var atlas=new Bitmap(root+"/Tower_Emblem_White_Atlas_1024.png")){
   for(int i=0;i<names.Length;i++)using(var src=new Bitmap(root+"/individual/"+(i+1).ToString("D2")+"_"+names[i]+"_White_256.png")){
    int sx=(i%4)*256,sy=(i/4)*256;
    for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(src.GetPixel(x,y).ToArgb()!=atlas.GetPixel(sx+x,sy+y).ToArgb())throw new Exception("Atlas pixel mismatch "+names[i]);
   }
   for(int i=13;i<16;i++)for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(atlas.GetPixel((i%4)*256+x,(i/4)*256+y).A!=0)throw new Exception("Unused slot not empty");
  }
  report.AppendLine("Atlas: 1024x1024 RGBA; 13 cells pixel-identical to individual PNGs; 3 unused cells fully transparent.");
  return report.ToString();
 }
}
'@
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[EmblemAtlasPack]::Run($PSScriptRoot,$projectRoot)
