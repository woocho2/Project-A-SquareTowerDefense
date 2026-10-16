$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Collections.Generic;
public static class OriginalFrameShading {
 static int RemoveSpecks(int[] shades,int[] alpha,int w,int h,int low,int high,int maxCount) {
  bool[] seen=new bool[w*h];int removed=0;
  for(int start=0;start<shades.Length;start++) {
   if(seen[start]||alpha[start]==0||shades[start]<low||shades[start]>high)continue;
   var pixels=new List<int>();var queue=new Queue<int>();queue.Enqueue(start);seen[start]=true;
   while(queue.Count>0){int p=queue.Dequeue();pixels.Add(p);int x=p%w,y=p/w;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
     int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;int q=yy*w+xx;
     if(seen[q]||alpha[q]==0||shades[q]<low||shades[q]>high)continue;seen[q]=true;queue.Enqueue(q);
    }
   }
   if(pixels.Count<=maxCount)foreach(int p in pixels){shades[p]=207;removed++;}
  }
  return removed;
 }
 public static void Build(string source,string destination) {
  using(var input=new Bitmap(source))using(var output=new Bitmap(input.Width,input.Height,PixelFormat.Format32bppArgb)) {
   int w=input.Width,h=input.Height;int[] alpha=new int[w*h],shades=new int[w*h];double[] median=new double[w*h];
   for(int y=0;y<h;y++)for(int x=0;x<w;x++){var c=input.GetPixel(x,y);alpha[y*w+x]=c.A;shades[y*w+x]=c.R;}
   // The four large white corner faces stay. Separate bright flecks and broken highlight fragments go.
   int bright=RemoveSpecks(shades,alpha,w,h,240,255,1000);
   int dark=RemoveSpecks(shades,alpha,w,h,0,175,24);
   // Reuse the original top edge's paint-band profile on the four straight edges.
   // Only RGB changes: both the outside and the inside alpha edges remain byte-identical.
   for(int d=0;d<24;d++) {
    int[] samples=new int[176];int count=0;
    for(int t=168;t<344;t++){int p=(14+d)*w+t;if(alpha[p]>0)samples[count++]=shades[p];}
    if(count==0)continue;Array.Sort(samples,0,count);int value=samples[count/2];
    for(int t=148;t<364;t++) {
     int[] positions={(14+d)*w+t,t*w+497-d,(497-d)*w+511-t,(511-t)*w+14+d};
     foreach(int p in positions)if(alpha[p]>0)shades[p]=value;
    }
   }
   Console.WriteLine("Removed isolated shading pixels: bright="+bright+", dark="+dark);
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    int i=y*w+x;if(alpha[i]==0)continue;
    int[] window=new int[9];int count=0;
    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
     int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
     int p=yy*w+xx;if(alpha[p]==0)continue;window[count++]=shades[p];
    }
    Array.Sort(window,0,count);median[i]=window[count/2];
   }
   // A subpixel-scale color-only filter removes chipped threshold bands. Alpha is never filtered.
   const double sigma=0.65;
   for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
    int i=y*w+x;if(alpha[i]==0){output.SetPixel(x,y,input.GetPixel(x,y));continue;}
    double sum=0,weight=0;
    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++) {
     int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
     int j=yy*w+xx;if(alpha[j]==0)continue;
     double k=Math.Exp(-(dx*dx+dy*dy)/(2*sigma*sigma))*alpha[j]/255.0;
     sum+=median[j]*k;weight+=k;
    }
    int v=Math.Max(150,Math.Min(255,(int)Math.Round(sum/weight)));
    output.SetPixel(x,y,Color.FromArgb(alpha[i],v,v,v));
   }
   output.Save(destination,ImageFormat.Png);
  }
 }
 public static string Verify(string source,string destination) {
  using(var before=new Bitmap(source))using(var after=new Bitmap(destination)) {
   if(before.Size!=after.Size)throw new Exception("Dimensions changed");int changed=0,min=255,max=0;
   int[] shades=new int[after.Width*after.Height],alpha=new int[after.Width*after.Height];
   for(int y=0;y<before.Height;y++)for(int x=0;x<before.Width;x++) {
    var a=before.GetPixel(x,y);var b=after.GetPixel(x,y);
    shades[y*after.Width+x]=b.R;alpha[y*after.Width+x]=b.A;
    if(a.A!=b.A)throw new Exception("Design silhouette or thickness changed: alpha mismatch");
    if(a.A==0&&a.ToArgb()!=b.ToArgb())throw new Exception("Transparent pixels changed");
    if(b.A==0)continue;
    if(b.R!=b.G||b.R!=b.B||b.R<150||b.R>255)throw new Exception("Expected neutral 150..255");
    min=Math.Min(min,b.R);max=Math.Max(max,b.R);if(a.R!=b.R)changed++;
   }
   if(min!=150||max!=255)throw new Exception("Shading extrema changed");
   if(RemoveSpecks(shades,alpha,after.Width,after.Height,240,255,1000)!=0)throw new Exception("Isolated bright flecks remain");
   return "PASS: exact original alpha at every pixel, unchanged dimensions/silhouette/thickness, grayscale 150..255, no isolated bright flecks. Color-only refined pixels: "+changed;
  }
 }
 public static void Compare(string beforePath,string afterPath,string output) {
  using(var before=new Bitmap(beforePath))using(var after=new Bitmap(afterPath))
  using(var canvas=new Bitmap(1024,550,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
  using(var font=new Font("Segoe UI",14,FontStyle.Bold)) {
   g.Clear(Color.FromArgb(255,251,232));g.DrawString("ORIGINAL",font,Brushes.DimGray,12,5);g.DrawString("SAME DESIGN / SHADING ONLY",font,Brushes.DimGray,524,5);
   for(int i=0;i<2;i++)using(var b=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
    for(int y=0;y<512;y++)for(int x=0;x<512;x++){var c=(i==0?before:after).GetPixel(x,y);b.SetPixel(x,y,Color.FromArgb(c.A,c.R*228/255,c.G*171/255,c.B*45/255));}
    using(var fill=new SolidBrush(Color.FromArgb(32,48,72)))g.FillRectangle(fill,i*512+20,54,472,472);
    g.DrawImageUnscaled(b,i*512,36);
   }
   canvas.Save(output,ImageFormat.Png);
  }
 }
}
'@
$source=Join-Path $PSScriptRoot 'before.png'
$output=Join-Path $PSScriptRoot 'Board_Decoration_Refined.png'
[OriginalFrameShading]::Build($source,$output)
$result=[OriginalFrameShading]::Verify($source,$output)
[OriginalFrameShading]::Compare($source,$output,(Join-Path $PSScriptRoot 'shading-only-comparison.png'))
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),$result,[Text.UTF8Encoding]::new($false))
$result
