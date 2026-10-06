$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class DiamondLowerWingPack {
 static Color Mix(Color old,Color edit,double weight){
  double oa=old.A*(1-weight),ea=edit.A*weight,a=oa+ea;
  if(a<.5)return Color.FromArgb(0,0,0,0);
  return Color.FromArgb((int)Math.Round(a),(int)Math.Round((old.R*oa+edit.R*ea)/a),(int)Math.Round((old.G*oa+edit.G*ea)/a),(int)Math.Round((old.B*oa+edit.B*ea)/a));
 }
 static int Uncovered(Bitmap b,Bitmap tile,bool right){
  int count=0;
  for(int y=222;y<=246;y++)for(int x=right?230:6;x<=(right?249:25);x++){
   if(tile.GetPixel(x,y).A<192)continue;
   int sx=(int)Math.Round(128+(x+.5-128)/1.06-.5);
   int sy=(int)Math.Round(128+(y+.5-128-2.46)/1.045-.5);
   int a=sx<0||sx>=256||sy<0||sy>=256?0:b.GetPixel(sx,sy).A;
   if(a<64)count++;
  }
  return count;
 }
 public static string Run(string root){
  using(var old=new Bitmap(root+"/Diamond_Before.png"))
  using(var ai=new Bitmap(root+"/AI_Source.png"))
  using(var edit=new Bitmap(256,256,PixelFormat.Format32bppArgb))
  using(var dst=new Bitmap(256,256,PixelFormat.Format32bppArgb))
  using(var tile=new Bitmap(root+"/Asgard_Spawn_Reference.png")){
   using(var g=Graphics.FromImage(edit)){
    g.CompositingMode=CompositingMode.SourceCopy;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(ai,new Rectangle(0,0,256,256));
   }
   int changed=0,protectedChanges=0;
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){
    Color a=old.GetPixel(x,y);
    bool allowed=y>=205&&(x<81||x>174);
    double wy=Math.Max(0,Math.Min(1,(y-205)/8.0));
    double wx=x<81?Math.Max(0,Math.Min(1,(80-x)/8.0)):Math.Max(0,Math.Min(1,(x-175)/8.0));
    Color result=allowed?Mix(a,edit.GetPixel(x,y),wx*wy):a;
    dst.SetPixel(x,y,result);
    if(a.ToArgb()!=result.ToArgb()){changed++;if(!allowed)protectedChanges++;}
   }
   if(protectedChanges!=0)throw new Exception("Protected pixels changed");
   int bl=Uncovered(old,tile,false),br=Uncovered(old,tile,true),al=Uncovered(dst,tile,false),ar=Uncovered(dst,tile,true);
   if(al>=bl||ar>=br)throw new Exception("Corner coverage did not improve on BOTH sides");
   if(dst.GetPixel(127,127).A!=0)throw new Exception("Opaque center");
   dst.Save(root+"/Diamond_After.png",ImageFormat.Png);
   using(var comparison=new Bitmap(600,320,PixelFormat.Format32bppArgb))
   using(var g=Graphics.FromImage(comparison))
   using(var font=new Font("Arial",12)){
    g.Clear(Color.FromArgb(235,231,219));
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    for(int i=0;i<2;i++){
     int ox=i*300+22,oy=38;
     g.DrawString(i==0?"Before":"After - lower feather corners",font,Brushes.Black,i*300+16,10);
     g.DrawImage(tile,ox,oy,256,256);
     g.DrawImage(i==0?old:dst,new RectangleF((float)(ox+128-128*1.06),(float)(oy+128-128*1.045+2.46),(float)(256*1.06),(float)(256*1.045)));
    }
    comparison.Save(root+"/Before_After_On_Spawn.png",ImageFormat.Png);
   }
   return "changedPixels="+changed+"; protectedChanges="+protectedChanges+"; clearly exposed corner pixels at current prefab scale: left "+bl+" -> "+al+", right "+br+" -> "+ar+"; output=256x256 RGBA";
  }
 }
}
'@
[DiamondLowerWingPack]::Run($PSScriptRoot)
