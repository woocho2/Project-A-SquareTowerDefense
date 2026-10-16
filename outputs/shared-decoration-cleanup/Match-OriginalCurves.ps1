$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;
public static class MatchedSharedFrame {
 static GraphicsPath Round(float x,float y,float w,float r) {
  var p=new GraphicsPath();float d=r*2;
  p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);
  p.AddArc(x+w-d,y+w-d,d,d,0,90);p.AddArc(x,y+w-d,d,d,90,90);p.CloseFigure();return p;
 }
 static GraphicsPath Plate() {
  var p=new GraphicsPath();p.StartFigure();
  // Fitted to the ORIGINAL 28..143 corner face, not the rejected enlarged 79px elbow.
  p.AddLine(144,28,66,28);p.AddBezier(66,28,45.013f,28,28,45.013f,28,66);
  p.AddLine(28,66,28,144);p.AddLine(28,144,46,144);
  p.AddBezier(46,144,53.18f,144,59,135.046f,59,124);
  p.AddLine(59,124,59,92);p.AddBezier(59,92,59,73.775f,73.775f,59,92,59);
  p.AddLine(92,59,124,59);p.AddBezier(124,59,135.046f,59,144,53.18f,144,46);
  p.CloseFigure();return p;
 }
 static GraphicsPath Opening() {
  using(var quarter=new GraphicsPath()) {
   // Original half-alpha contour samples: x~156 at y34..48, x~72 at y88..124.
   quarter.AddLine(256,34,157,34);quarter.AddLine(157,34,157,46);
   quarter.AddBezier(157,46,157,60.359f,142.225f,72,124,72);
   quarter.AddLine(124,72,92,72);quarter.AddBezier(92,72,80.954f,72,72,80.954f,72,92);
   quarter.AddLine(72,92,72,124);quarter.AddBezier(72,124,72,142.225f,60.359f,157,46,157);
   quarter.AddLine(46,157,34,157);quarter.AddLine(34,157,34,256);
   var result=new GraphicsPath();
   for(int i=0;i<4;i++)using(var piece=(GraphicsPath)quarter.Clone())using(var m=new Matrix()) {
    m.RotateAt(-90*i,new PointF(256,256));piece.Transform(m);result.AddPath(piece,true);
   }
   result.CloseFigure();return result;
  }
 }
 static SolidBrush Gray(int v){return new SolidBrush(Color.FromArgb(v,v,v));}
 public static void Build(string backgroundPath,string destination) {
  const int scale=4;
  using(var large=new Bitmap(512*scale,512*scale,PixelFormat.Format32bppArgb))
  using(var output=new Bitmap(512,512,PixelFormat.Format32bppArgb))
  using(var mask=new Bitmap(backgroundPath))
  using(var g=Graphics.FromImage(large))
  using(var outer=Round(14,14,484,52))using(var opening=Opening()) {
   g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(scale,scale);
   using(var paint=Gray(207))g.FillPath(paint,outer);
   for(int corner=0;corner<4;corner++)using(var plate=Plate())using(var m=new Matrix()) {
    m.RotateAt(90*corner,new PointF(256,256));plate.Transform(m);
    using(var paint=Gray(255))g.FillPath(paint,plate);
    using(var pen=new Pen(Color.FromArgb(150,150,150),3)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,plate);}
   }
   // One continuous inner stroke, including all convex/concave bends; no raster threshold bands.
   using(var shadow=new Pen(Color.FromArgb(150,150,150),8)){shadow.LineJoin=LineJoin.Round;g.DrawPath(shadow,opening);}
   g.CompositingMode=CompositingMode.SourceCopy;using(var clear=new SolidBrush(Color.Transparent))g.FillPath(clear,opening);
   g.CompositingMode=CompositingMode.SourceOver;
   using(var border=new Pen(Color.FromArgb(150,150,150),8)){border.LineJoin=LineJoin.Round;g.DrawPath(border,outer);}
   using(var small=Graphics.FromImage(output)) {
    small.CompositingMode=CompositingMode.SourceCopy;small.InterpolationMode=InterpolationMode.HighQualityBicubic;small.PixelOffsetMode=PixelOffsetMode.HighQuality;
    small.DrawImage(large,new Rectangle(0,0,512,512));
   }
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    var c=output.GetPixel(x,y);int alpha=Math.Min(c.A,mask.GetPixel(x,y).A);
    int v=Math.Max(150,Math.Min(255,(int)c.R));
    output.SetPixel(x,y,alpha==0?Color.FromArgb(0,0,0,0):Color.FromArgb(alpha,v,v,v));
   }
   // One master quadrant removes scan-conversion differences between mirrored antialiased curves.
   for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
    var c=output.GetPixel(x,y);
    output.SetPixel(511-y,x,c);output.SetPixel(511-x,511-y,c);output.SetPixel(y,511-x,c);
   }
   output.Save(destination,ImageFormat.Png);
  }
 }
 public static string Verify(string path){
  using(var b=new Bitmap(path)){
   int l=512,t=512,r=-1,bottom=-1,min=255,max=0;
   for(int y=0;y<512;y++)for(int x=0;x<512;x++){
    var c=b.GetPixel(x,y);if(c.A==0)continue;
    if(c.R!=c.G||c.R!=c.B||c.R<150||c.R>255)throw new Exception("Expected grayscale 150..255");
    min=Math.Min(min,c.R);max=Math.Max(max,c.R);l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bottom=Math.Max(bottom,y);
    if(x>160&&x<352&&y>160&&y<352)throw new Exception("Interior opening was filled");
    var rotated=b.GetPixel(511-y,x);
    if(Math.Abs(c.A-rotated.A)>5)throw new Exception("Asymmetric silhouette");
   }
   if(l!=14||t!=14||r!=497||bottom!=497||min!=150||max!=255)throw new Exception("Frame size/range changed");
   return "PASS: 512x512, centered 484x484 outer bounds, grayscale 150..255, empty center, rotationally symmetric silhouette.";
  }
 }
 public static string VerifyOriginalScale(string originalPath,string actualPath) {
  using(var old=new Bitmap(originalPath))using(var now=new Bitmap(actualPath)) {
   int before=0,after=0,oldCorner=0,newCorner=0,oldWidth=0,newWidth=0;
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    bool a=old.GetPixel(x,y).A>=128,b=now.GetPixel(x,y).A>=128;
    if(a){before++;if(x<160&&y<160)oldCorner++;}
    if(b){after++;if(x<160&&y<160)newCorner++;}
    if(y==256&&x<256){if(a)oldWidth++;if(b)newWidth++;}
   }
   if(oldWidth!=newWidth||newWidth!=20)throw new Exception("Original straight-edge thickness changed");
   if(after>before*1.01||after<before*.97||newCorner>oldCorner*1.01||newCorner<oldCorner*.97)throw new Exception("Corner/frame proportions changed");
   return "PASS: original straight-edge thickness "+oldWidth+"px retained; opaque area "+before+" -> "+after+"; corner area "+oldCorner+" -> "+newCorner+" (no enlargement).";
  }
 }
 public static void Compare(string beforePath,string afterPath,string output){
  using(var before=new Bitmap(beforePath))using(var after=new Bitmap(afterPath))
  using(var canvas=new Bitmap(1024,550,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
  using(var font=new Font("Segoe UI",14,FontStyle.Bold)){
   g.Clear(Color.FromArgb(255,251,232));g.DrawString("BEFORE",font,Brushes.DimGray,12,5);g.DrawString("SMOOTH SHADING",font,Brushes.DimGray,524,5);
   for(int i=0;i<2;i++)using(var b=new Bitmap(512,512,PixelFormat.Format32bppArgb)){
    for(int y=0;y<512;y++)for(int x=0;x<512;x++){var c=(i==0?before:after).GetPixel(x,y);b.SetPixel(x,y,Color.FromArgb(c.A,c.R*228/255,c.G*171/255,c.B*45/255));}
    using(var fill=new SolidBrush(Color.FromArgb(32,48,72)))g.FillRectangle(fill,i*512+20,54,472,472);
    g.DrawImageUnscaled(b,i*512,36);
   }
   canvas.Save(output,ImageFormat.Png);
  }
 }
}
'@
$background=Join-Path $repo 'Assets/4. Asset/0. Common/Board_Background.png'
$staged=Join-Path $PSScriptRoot 'Board_Decoration_Matched.png'
[MatchedSharedFrame]::Build($background,$staged)
$result=[MatchedSharedFrame]::Verify($staged)
$scaleResult=[MatchedSharedFrame]::VerifyOriginalScale((Join-Path $PSScriptRoot 'before.png'),$staged)
[MatchedSharedFrame]::Compare((Join-Path $PSScriptRoot 'before.png'),$staged,(Join-Path $PSScriptRoot 'matched-comparison.png'))
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'matched-verification.txt'),@($result,$scaleResult),[Text.UTF8Encoding]::new($false))
Write-Output $result
Write-Output $scaleResult
