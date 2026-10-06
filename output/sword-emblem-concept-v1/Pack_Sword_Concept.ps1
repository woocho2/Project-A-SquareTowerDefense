$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class SwordConceptPack {
 static Bitmap Tint(Bitmap master,Color color){
  var b=new Bitmap(256,256,PixelFormat.Format32bppArgb);
  for(int y=0;y<256;y++)for(int x=0;x<256;x++)b.SetPixel(x,y,Color.FromArgb(master.GetPixel(x,y).A,color.R,color.G,color.B));
  return b;
 }
 static Bitmap TierDisplay(string path){
  var b=new Bitmap(256,256,PixelFormat.Format32bppArgb);
  using(var src=new Bitmap(path))using(var g=Graphics.FromImage(b)){
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   g.DrawImage(src,new RectangleF((float)(128-128*1.06),(float)(128-128*1.045+2.46),(float)(256*1.06),(float)(256*1.045)));
  }
  return b;
 }
 static Bitmap EmblemDisplay(Bitmap src){
  var b=new Bitmap(256,256,PixelFormat.Format32bppArgb);
  using(var g=Graphics.FromImage(b)){
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   // Existing emblem PPU256 vs tier PPU192: the emblem canvas renders as 192x192.
   g.DrawImage(src,new Rectangle(32,32,192,192));
  }
  return b;
 }
 public static string Run(string root,string project){
  string[] tiers={"Bronze","Silver","Gold","Mithril","Diamond"};
  string[] roles={"Splash - RED","Target - BLUE","Buff - YELLOW","Debuff - BLACK"};
  string[] filenames={"Sword_Red","Sword_Blue","Sword_Yellow","Sword_Black"};
  Color[] colors={ColorTranslator.FromHtml("#E25D62"),ColorTranslator.FromHtml("#538ACD"),ColorTranslator.FromHtml("#D5AD28"),Color.Black};
  using(var ai=new Bitmap(root+"/AI_Sword_Source.png"))
  using(var spawn=new Bitmap(project+"/Assets/4. Asset/1. BackGround/Tile_TowerSpawn_Atlas.png"))
  using(var neutralBase=new Bitmap(256,256,PixelFormat.Format32bppArgb))
  using(var reduced=new Bitmap(256,256,PixelFormat.Format32bppArgb))
  using(var plain=new Bitmap(1120,320,PixelFormat.Format32bppArgb))
  using(var grid=new Bitmap(1550,1560,PixelFormat.Format32bppArgb))
  using(var primary=new Bitmap(1240,390,PixelFormat.Format32bppArgb))
  using(var gp=Graphics.FromImage(plain))
  using(var gg=Graphics.FromImage(grid))
  using(var gm=Graphics.FromImage(primary))
  using(var font=new Font("Arial",12)){
   using(var g=Graphics.FromImage(reduced)){
    g.CompositingMode=CompositingMode.SourceCopy;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(ai,new Rectangle(0,0,256,256));
   }
   using(var master=Tint(reduced,Color.White))master.Save(root+"/Sword_Tint_Master_256.png",ImageFormat.Png);
   for(int y=0;y<256;y++)for(int x=0;x<256;x++)neutralBase.SetPixel(x,y,Color.FromArgb(spawn.GetPixel(x,y).A,255,253,247));
   gp.Clear(Color.FromArgb(235,231,219));gg.Clear(Color.FromArgb(235,231,219));gm.Clear(Color.FromArgb(235,231,219));
   string report="";
   for(int c=0;c<4;c++){
    using(var icon=Tint(reduced,colors[c])){
     icon.Save(root+"/"+filenames[c]+".png",ImageFormat.Png);
     int px=c*280+12;
     gp.FillRectangle(new SolidBrush(Color.FromArgb(255,253,247)),px,36,256,256);
     gp.DrawString(roles[c],font,Brushes.Black,px,10);
     gp.DrawImageUnscaled(icon,px,36);
     for(int t=0;t<5;t++){
      using(var tier=TierDisplay(project+"/Assets/4. Asset/2. Tower/1. Tier/"+tiers[t]+".png"))
      using(var emblem=EmblemDisplay(icon))
      using(var composed=new Bitmap(280,280,PixelFormat.Format32bppArgb)){
       int hidden=0,opaque=0;
       for(int y=0;y<256;y++)for(int x=0;x<256;x++){
        if(emblem.GetPixel(x,y).A>=128){opaque++;if(tier.GetPixel(x,y).A>32)hidden++;}
       }
       if(hidden!=0)throw new Exception(tiers[t]+" covers sword pixels: "+hidden);
       using(var g=Graphics.FromImage(composed)){
        g.Clear(Color.FromArgb(235,231,219));
        g.DrawImageUnscaled(neutralBase,12,12);
        g.DrawImageUnscaled(emblem,12,12);
        g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        using(var raw=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/"+tiers[t]+".png"))
         g.DrawImage(raw,new RectangleF((float)(12+128-128*1.06),(float)(12+128-128*1.045+2.46),(float)(256*1.06),(float)(256*1.045)));
       }
       int gx=t*310+12,gy=c*390+36;
       gg.DrawString(tiers[t]+" / "+roles[c],font,Brushes.Black,gx,c*390+10);
       gg.DrawImageUnscaled(composed,gx,gy);
       // Small, game-sized thumbnail: look for loss of silhouette at 56px.
       gg.DrawImage(composed,new Rectangle(gx+110,gy+284,56,56),new Rectangle(0,0,280,280),GraphicsUnit.Pixel);
       if(t==4){
        gm.DrawString(roles[c]+" / Diamond",font,Brushes.Black,c*310+12,10);
        gm.DrawImageUnscaled(composed,c*310+12,36);
        gm.DrawImage(composed,new Rectangle(c*310+122,320,56,56),new Rectangle(0,0,280,280),GraphicsUnit.Pixel);
       }
       report+=tiers[t]+" / "+roles[c]+": sword opaque pixels="+opaque+", tier-covered pixels="+hidden+"\n";
      }
     }
    }
   }
   plain.Save(root+"/Sword_4Colors_Concept.png",ImageFormat.Png);
   grid.Save(root+"/Sword_Under_All_5Tiers_Preview.png",ImageFormat.Png);
   primary.Save(root+"/Sword_Diamond_4Colors_Preview.png",ImageFormat.Png);
   return report;
  }
 }
}
'@
$project=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[SwordConceptPack]::Run($PSScriptRoot,$project)
