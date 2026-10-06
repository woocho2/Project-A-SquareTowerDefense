$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class ApprovedTierPack {
 static Color Sample(Bitmap b,double x,double y,int cellLeft,int cellRight) {
  int ix=(int)Math.Floor(x),iy=(int)Math.Floor(y);
  double fx=x-ix,fy=y-iy,a=0,r=0,g=0,blue=0;
  for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){
   int px=ix+dx,py=iy+dy;
   if(px<cellLeft||px>cellRight||py<0||py>=b.Height)continue;
   Color c=b.GetPixel(px,py);
   double w=(dx==0?1-fx:fx)*(dy==0?1-fy:fy),aw=c.A*w;
   a+=aw;r+=c.R*aw;g+=c.G*aw;blue+=c.B*aw;
  }
  if(a<0.5)return Color.FromArgb(0,0,0,0);
  return Color.FromArgb((int)Math.Round(a),(int)Math.Round(r/a),(int)Math.Round(g/a),(int)Math.Round(blue/a));
 }
 public static string Pack(string sheetPath,string outDir) {
  string[] names={"Bronze","Silver","Gold","Mithril","Diamond"};
  int[] frameLeft={23,460,899,1341,1782},frameRight={424,862,1303,1744,2181};
  string report="";
  using(var src=new Bitmap(sheetPath))
  using(var comparison=new Bitmap(1280,256,PixelFormat.Format32bppArgb))
  using(var graphics=Graphics.FromImage(comparison)){
   graphics.Clear(Color.FromArgb(235,231,219));
   for(int i=0;i<5;i++){
    int cellLeft=(int)Math.Floor(src.Width*i/5.0),cellRight=(int)Math.Floor(src.Width*(i+1)/5.0)-1;
    double sx=(frameRight[i]-frameLeft[i])/234.0,sy=389.0/242.0;
    int clipped=0;
    // Whole-sprite affine placement. No separate wing masks, local rail warps, or palette edits.
    for(int y=0;y<src.Height;y++)for(int x=cellLeft;x<=cellRight;x++){
     if(src.GetPixel(x,y).A<128)continue;
     double tx=11+(x+.5-frameLeft[i])/sx,ty=5+(y+.5-140)/sy;
     if(tx<0||tx>=256||ty<0||ty>=256)clipped++;
    }
    if(clipped!=0)throw new Exception(names[i]+" has clipped opaque pixels: "+clipped);
    using(var dst=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
     for(int y=0;y<256;y++)for(int x=0;x<256;x++){
      double a=0,r=0,g=0,blue=0;
      // 4x4 premultiplied-alpha supersampling prevents dark halos when downscaling.
      for(int yy=0;yy<4;yy++)for(int xx=0;xx<4;xx++){
       double sourceX=frameLeft[i]+(x+(xx+.5)/4.0-11)*sx-.5;
       double sourceY=140+(y+(yy+.5)/4.0-5)*sy-.5;
       Color c=Sample(src,sourceX,sourceY,cellLeft,cellRight);
       a+=c.A;r+=c.R*c.A;g+=c.G*c.A;blue+=c.B*c.A;
      }
      if(a<8)dst.SetPixel(x,y,Color.FromArgb(0,0,0,0));
      else dst.SetPixel(x,y,Color.FromArgb((int)Math.Round(a/16),(int)Math.Round(r/a),(int)Math.Round(g/a),(int)Math.Round(blue/a)));
     }
     if(dst.GetPixel(127,127).A!=0)throw new Exception(names[i]+" center is not transparent");
     int opaqueEdge=0;
     for(int p=0;p<256;p++)if(dst.GetPixel(0,p).A>=128||dst.GetPixel(255,p).A>=128||dst.GetPixel(p,0).A>=128||dst.GetPixel(p,255).A>=128)opaqueEdge++;
     if(opaqueEdge!=0)throw new Exception(names[i]+" touches canvas edge");
     dst.Save(outDir+"/"+names[i]+".png",ImageFormat.Png);
     graphics.DrawImageUnscaled(dst,i*256,0);
     report+=names[i]+": 256x256 RGBA, center transparent, clippedOpaquePixels=0; target frame x=11..244, y=5..246\n";
    }
   }
   comparison.Save(outDir+"/../Applied_5Tiers_Comparison.png",ImageFormat.Png);
  }
  return report;
 }
}
'@
$staged=Join-Path $PSScriptRoot 'staged'
New-Item -ItemType Directory -Path $staged -Force | Out-Null
[ApprovedTierPack]::Pack((Join-Path $PSScriptRoot 'Wing_XSpread_5Tiers_AI_Preview.png'),$staged)
