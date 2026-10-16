$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;
public static class CleanSharedFrame {
 static GraphicsPath Round(float x,float y,float w,float r) {
  var p=new GraphicsPath();float d=r*2;
  p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);
  p.AddArc(x+w-d,y+w-d,d,d,0,90);p.AddArc(x,y+w-d,d,d,90,90);p.CloseFigure();return p;
 }
 static GraphicsPath Plate() {
  var p=new GraphicsPath();p.StartFigure();
  p.AddLine(148,27,66,27);p.AddBezier(66,27,44.5f,27,27,44.5f,27,66);
  p.AddLine(27,66,27,148);p.AddLine(27,148,44,148);
  p.AddBezier(44,148,63.3f,148,79,132.3f,79,113);
  p.AddLine(79,113,79,102);p.AddBezier(79,102,79,89.3f,89.3f,79,102,79);
  p.AddLine(102,79,113,79);p.AddBezier(113,79,132.3f,79,148,63.3f,148,44);
  p.CloseFigure();return p;
 }
 static GraphicsPath Opening() {
  var p=new GraphicsPath();p.StartFigure();
  p.AddLine(156,34,356,34);p.AddLine(356,34,356,44);
  p.AddBezier(356,44,356,67.2f,374.8f,86,398,86);p.AddLine(398,86,410,86);
  p.AddBezier(410,86,418.8f,86,426,93.2f,426,102);p.AddLine(426,102,426,114);
  p.AddBezier(426,114,426,137.2f,444.8f,156,468,156);p.AddLine(468,156,478,156);
  p.AddLine(478,156,478,356);p.AddLine(478,356,468,356);
  p.AddBezier(468,356,444.8f,356,426,374.8f,426,398);p.AddLine(426,398,426,410);
  p.AddBezier(426,410,426,418.8f,418.8f,426,410,426);p.AddLine(410,426,398,426);
  p.AddBezier(398,426,374.8f,426,356,444.8f,356,468);p.AddLine(356,468,356,478);
  p.AddLine(356,478,156,478);p.AddLine(156,478,156,468);
  p.AddBezier(156,468,156,444.8f,137.2f,426,114,426);p.AddLine(114,426,102,426);
  p.AddBezier(102,426,93.2f,426,86,418.8f,86,410);p.AddLine(86,410,86,398);
  p.AddBezier(86,398,86,374.8f,67.2f,356,44,356);p.AddLine(44,356,34,356);
  p.AddLine(34,356,34,156);p.AddLine(34,156,44,156);
  p.AddBezier(44,156,67.2f,156,86,137.2f,86,114);p.AddLine(86,114,86,102);
  p.AddBezier(86,102,86,93.2f,93.2f,86,102,86);p.AddLine(102,86,114,86);
  p.AddBezier(114,86,137.2f,86,156,67.2f,156,44);p.CloseFigure();return p;
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
   using(var paint=Gray(225))g.FillPath(paint,outer);
   using(var highlight=Round(18,18,476,48))using(var pen=new Pen(Color.White,2))g.DrawPath(pen,highlight);
   for(int corner=0;corner<4;corner++)using(var plate=Plate())using(var m=new Matrix()) {
    m.RotateAt(90*corner,new PointF(256,256));plate.Transform(m);
    using(var paint=Gray(255))g.FillPath(paint,plate);
    using(var pen=new Pen(Color.FromArgb(190,190,190),3)){pen.LineJoin=LineJoin.Round;g.DrawPath(pen,plate);}
   }
   // One continuous inner stroke, including all convex/concave bends; no raster threshold bands.
   using(var shadow=new Pen(Color.FromArgb(150,150,150),4)){shadow.LineJoin=LineJoin.Round;g.DrawPath(shadow,opening);}
   g.CompositingMode=CompositingMode.SourceCopy;using(var clear=new SolidBrush(Color.Transparent))g.FillPath(clear,opening);
   g.CompositingMode=CompositingMode.SourceOver;
   using(var border=new Pen(Color.FromArgb(150,150,150),3)){border.LineJoin=LineJoin.Round;g.DrawPath(border,outer);}
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
$staged=Join-Path $PSScriptRoot 'Board_Decoration.png'
[CleanSharedFrame]::Build($background,$staged)
$result=[CleanSharedFrame]::Verify($staged)
[CleanSharedFrame]::Compare((Join-Path $PSScriptRoot 'before.png'),$staged,(Join-Path $PSScriptRoot 'comparison.png'))
Write-Output $result
