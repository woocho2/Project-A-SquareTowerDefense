$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class WeaponPreviewPack {
 static void Quality(Graphics g) {
  g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  g.PixelOffsetMode=PixelOffsetMode.HighQuality;
 }
 static Bitmap Tint(Bitmap b, Color c) {
  var result=new Bitmap(b.Width,b.Height,PixelFormat.Format32bppArgb);
  for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)
   result.SetPixel(x,y,Color.FromArgb(b.GetPixel(x,y).A,c.R,c.G,c.B));
  return result;
 }
 static Bitmap Normalize(Bitmap source) {
  int left=source.Width,top=source.Height,right=-1,bottom=-1;
  for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)
   if(source.GetPixel(x,y).A>=128){
    left=Math.Min(left,x);right=Math.Max(right,x);
    top=Math.Min(top,y);bottom=Math.Max(bottom,y);
   }
  if(right<left)throw new Exception("Empty alpha source");
  left=Math.Max(0,left-3);top=Math.Max(0,top-3);
  right=Math.Min(source.Width-1,right+3);bottom=Math.Min(source.Height-1,bottom+3);
  int width=right-left+1,height=bottom-top+1;
  float scale=Math.Min(160f/width,180f/height);
  float dw=width*scale,dh=height*scale;
  using(var reduced=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
   using(var g=Graphics.FromImage(reduced)){
    g.CompositingMode=CompositingMode.SourceCopy;Quality(g);
    g.DrawImage(source,new RectangleF(128-dw/2,128-dh/2,dw,dh),
     new RectangleF(left,top,width,height),GraphicsUnit.Pixel);
   }
   // Neutral storage conversion only: preserve every alpha value, replace RGB with pure white.
   return Tint(reduced,Color.White);
  }
 }
 static Bitmap Compose(Bitmap mask,Color role,Bitmap tier){
  var result=new Bitmap(280,280,PixelFormat.Format32bppArgb);
  using(var icon=Tint(mask,role))using(var g=Graphics.FromImage(result)){
   g.Clear(Color.White);Quality(g);
   // Existing emblem PPU256 vs tier PPU192: emblem canvas occupies 192x192.
   g.DrawImage(icon,new Rectangle(44,44,192,192));
   // Existing TowerBase prefab scale and vertical offset, preview only.
   g.DrawImage(tier,new RectangleF(12+128-128*1.06f,12+128-128*1.045f+2.46f,
    256*1.06f,256*1.045f));
  }
  return result;
 }
 public static string Run(string root,string project){
  string[] names={"Bow","Shield","Spear","Axe","Hammer"};
  string[] tiers={"Bronze","Silver","Gold","Mithril","Diamond"};
  Color[] roles={ColorTranslator.FromHtml("#E25D62"),ColorTranslator.FromHtml("#538ACD"),
   ColorTranslator.FromHtml("#D5AD28"),Color.FromArgb(32,32,39)};
  var report=new StringBuilder();
  using(var page=new Bitmap(1600,840,PixelFormat.Format32bppArgb))
  using(var g=Graphics.FromImage(page))
  using(var font=new Font("Arial",16))
  using(var small=new Font("Arial",10))
  using(var gold=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/Gold.png")){
   g.Clear(Color.FromArgb(238,240,244));Quality(g);
   for(int i=0;i<names.Length;i++){
    string name=names[i];int col=i*320;
    using(var source=new Bitmap(root+"/AI_"+name+"_Source.png"))
    using(var mask=Normalize(source)){
     mask.Save(root+"/"+name+"_White_256.png",ImageFormat.Png);
     int transparent=0,opaque=0,nonWhite=0,border=0;
     for(int y=0;y<256;y++)for(int x=0;x<256;x++){
      Color p=mask.GetPixel(x,y);
      if(p.A==0)transparent++;else{
       if(p.A>=128)opaque++;
       if(p.R!=255||p.G!=255||p.B!=255)nonWhite++;
       if(x==0||y==0||x==255||y==255)border++;
      }
     }
     if(nonWhite!=0||border!=0||opaque==0||transparent==0)
      throw new Exception("Mask validation failed: "+name);
     report.AppendLine(name+": 256x256 RGBA; visible RGB=#FFFFFF; opaque="+opaque+
      "; transparent="+transparent+"; border visible="+border);
     g.DrawString(name,font,Brushes.Black,col+24,12);
     using(var navy=new SolidBrush(Color.FromArgb(25,35,51)))
      g.FillRectangle(navy,col+32,44,256,256);
     g.DrawImageUnscaled(mask,col+32,44);
     g.DrawString("WHITE MASK / transparent PNG",small,Brushes.Black,col+32,310);
     using(var composed=Compose(mask,roles[0],gold))
      g.DrawImageUnscaled(composed,col+20,340);
     g.DrawString("Tint preview / existing Gold frame",small,Brushes.Black,col+28,628);
     for(int c=0;c<4;c++)using(var mini=Compose(mask,roles[c],gold))
      g.DrawImage(mini,new Rectangle(col+24+c*72,666,56,56),
       new Rectangle(0,0,280,280),GraphicsUnit.Pixel);
     // Check every existing tier at current prefab scale, not only the Gold sample.
     foreach(string tierName in tiers){
      using(var raw=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/"+tierName+".png"))
      using(var tier=new Bitmap(256,256,PixelFormat.Format32bppArgb))
      using(var emblem=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
       using(var gt=Graphics.FromImage(tier)){
        Quality(gt);gt.DrawImage(raw,new RectangleF(128-128*1.06f,128-128*1.045f+2.46f,256*1.06f,256*1.045f));
       }
       using(var ge=Graphics.FromImage(emblem)){Quality(ge);ge.DrawImage(mask,new Rectangle(32,32,192,192));}
       int hidden=0;
       for(int y=0;y<256;y++)for(int x=0;x<256;x++)
        if(emblem.GetPixel(x,y).A>=128&&tier.GetPixel(x,y).A>32)hidden++;
       report.AppendLine("  "+tierName+" / frame-overlapped opaque pixels="+hidden);
      }
     }
    }
   }
   g.DrawString("Preview only. White icons use a dark backdrop for visibility; tower faces remain pure white.",
    small,Brushes.Black,24,782);
   page.Save(root+"/Weapons_5Types_Preview.png",ImageFormat.Png);
  }
  return report.ToString();
 }
}
'@
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$report=[WeaponPreviewPack]::Run($PSScriptRoot,$projectRoot)
$report
