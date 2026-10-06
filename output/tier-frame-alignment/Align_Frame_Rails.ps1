$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
public static class TierRailAlign {
 static double Map(double value,double[] dst,double[] src){
  int i=0;while(i<dst.Length-2&&value>dst[i+1])i++;
  double t=(value-dst[i])/(dst[i+1]-dst[i]);return src[i]+t*(src[i+1]-src[i]);
 }
 static Color Sample(Bitmap b,double x,double y){
  x=Math.Max(0,Math.Min(255,x));y=Math.Max(0,Math.Min(255,y));
  int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y),x1=Math.Min(255,x0+1),y1=Math.Min(255,y0+1);
  double fx=x-x0,fy=y-y0;
  Color[] colors={b.GetPixel(x0,y0),b.GetPixel(x1,y0),b.GetPixel(x0,y1),b.GetPixel(x1,y1)};
  double[] w={(1-fx)*(1-fy),fx*(1-fy),(1-fx)*fy,fx*fy};
  double a=0,r=0,g=0,blue=0;
  for(int i=0;i<4;i++){double aw=colors[i].A*w[i];a+=aw;r+=colors[i].R*aw;g+=colors[i].G*aw;blue+=colors[i].B*aw;}
  if(a<.5)return Color.FromArgb(0,0,0,0);
  return Color.FromArgb((int)Math.Round(a),(int)Math.Round(r/a),(int)Math.Round(g/a),(int)Math.Round(blue/a));
 }
 static int[] Edges(Bitmap b,int y){
  int lo=-1,li=-1,ri=-1,ro=-1;
  for(int x=0;x<64;x++)if(b.GetPixel(x,y).A>=128){if(lo<0)lo=x;li=x+1;}
  for(int x=192;x<256;x++)if(b.GetPixel(x,y).A>=128){if(ri<0)ri=x;ro=x+1;}
  return new int[]{lo,li,ri,ro};
 }
 public static string Run(string sourcePath,string outPath,int topInner,int bottomInner,int wingStart){
 using(var src=new Bitmap(sourcePath))
 using(var dst=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
  var side=Edges(src,100);
  double[] targetX={0,11,23,127.5,233,245,256};
  double[] targetY={0,5,17,235,247,256};
  double[] sourceY={0,5,topInner,bottomInner,247,256};
  int wingChanges=0,changes=0;
  for(int y=0;y<256;y++)for(int x=0;x<256;x++){
   var old=src.GetPixel(x,y);
   bool protectedWing=y>=wingStart&&(x<80||x>175);
   Color result=old;
   if(!protectedWing){
    double sx=x,sy=y;
    double sideWeight=y<wingStart-4?1:Math.Max(0,(wingStart-y)/4.0);
    if(x>=80&&x<=175)sideWeight=1;
    int[] row=side;
    int rowY=Math.Max(80,Math.Min(132,y));
    if(y>=80&&y<=132)row=Edges(src,rowY);
    double[] sourceX={0,row[0],row[1],127.5,row[2],row[3],256};
    double mx=Map(x+.5,targetX,sourceX)-.5;
    sx=x+(mx-x)*sideWeight;
    double verticalWeight=1;
    if(y>=wingStart){
     verticalWeight=Math.Min(1,Math.Min((x-79)/7.0,(176-x)/7.0));
     verticalWeight=Math.Max(0,verticalWeight);
    }
    double my=Map(y+.5,targetY,sourceY)-.5;
    sy=y+(my-y)*verticalWeight;
    if(Math.Abs(sx-x)>.00001||Math.Abs(sy-y)>.00001)result=Sample(src,sx,sy);
   }
   dst.SetPixel(x,y,result);
   if(result.ToArgb()!=old.ToArgb()){changes++;if(protectedWing)wingChanges++;}
  }
  if(wingChanges!=0)throw new Exception("Wing pixels changed");
  dst.Save(outPath,ImageFormat.Png);
  string resultReport="changed="+changes+"; protectedWingChanges="+wingChanges;
  foreach(int y in new int[]{80,100,128}){var e=Edges(dst,y);resultReport+="; y"+y+" rails="+String.Join(",",e);}
  return resultReport;
 }
 }
 public static void Compare(string backup,string output,string outPath){
 using(var dst=new Bitmap(1280,512,PixelFormat.Format32bppArgb))
 using(var g=Graphics.FromImage(dst)){
  g.Clear(Color.FromArgb(231,227,214));
  string[] names={"Bronze","Silver","Gold","Mithril","Diamond"};
  for(int i=0;i<5;i++){
   using(var before=new Bitmap(backup+"/"+names[i]+".png"))g.DrawImageUnscaled(before,i*256,0);
   using(var after=new Bitmap(output+"/"+names[i]+".png"))g.DrawImageUnscaled(after,i*256,256);
  }
  dst.Save(outPath,ImageFormat.Png);
 }
 }
}
'@
$alignmentRoot=$PSScriptRoot
$backup=Join-Path $alignmentRoot 'backups\before-0e5d0a23-9f0b-4752-b06c-033175dd149a'
$staged=Join-Path $alignmentRoot 'aligned'
New-Item -ItemType Directory -Path $staged -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $backup 'Bronze.png') -Destination (Join-Path $staged 'Bronze.png')
Write-Output ('Silver: '+[TierRailAlign]::Run((Join-Path $backup 'Silver.png'),(Join-Path $staged 'Silver.png'),17,235,147))
Write-Output ('Gold: '+[TierRailAlign]::Run((Join-Path $backup 'Gold.png'),(Join-Path $staged 'Gold.png'),17,234,139))
Write-Output ('Mithril: '+[TierRailAlign]::Run((Join-Path $backup 'Mithril.png'),(Join-Path $staged 'Mithril.png'),18,233,138))
Write-Output ('Diamond: '+[TierRailAlign]::Run((Join-Path $backup 'Diamond.png'),(Join-Path $staged 'Diamond.png'),17,236,137))
[TierRailAlign]::Compare($backup,$staged,(Join-Path $alignmentRoot 'Before_After_Frame_Alignment.png'))

