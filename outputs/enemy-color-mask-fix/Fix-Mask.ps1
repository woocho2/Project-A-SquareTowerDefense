$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetRoot=Join-Path $repoRoot 'Assets/4. Asset/4. Enemy'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class ValkyrieEquipmentMask {
 // Full visible equipment contours in the unchanged 512px portrait coordinates.
 // No brightness threshold: highlights, shadows and feather subdivisions all belong to the equipment.
 static PointF[] Points(params float[] xy) {
  var p=new PointF[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new PointF(xy[i*2],xy[i*2+1]);return p;
 }
 static readonly PointF[][] Contours={
  // Headband and centre jewel, extending behind the two wing clips.
  Points(157,219,174,222,198,223,224,221,245,216,257,205,273,222,300,223,324,223,351,220,
    351,234,323,237,299,237,276,234,260,252,245,235,224,233,200,237,177,236,158,233),
  // Left wing clip: all three feathers and their connector, not the surrounding hair.
  Points(128,172,131,181,140,191,149,198,156,206,162,216,165,225,165,235,161,242,
    157,246,154,255,144,262,141,254,139,249,132,249,124,246,119,240,117,235,
    125,234,118,228,114,221,112,211,113,201,119,207,126,211,124,202,123,192,124,182),
  // Right wing clip, traced separately because the artwork is not perfectly mirrored.
  Points(383,172,387,183,388,196,386,209,391,207,398,201,400,210,399,219,395,227,
    387,234,394,235,391,242,385,248,374,250,374,260,365,257,359,251,356,245,
    349,239,347,229,349,217,355,207,364,199,374,190,380,182),
  // Collar, cloak and central brooch. Side contours follow the hair locks in front.
  Points(220,376,228,375,236,379,246,382,259,384,271,381,284,378,295,375,303,377,
    306,382,304,389,303,396,310,396,316,400,308,408,304,417,305,427,311,435,322,440,
    335,443,334,450,329,455,318,455,325,462,334,466,344,466,343,475,338,483,
    342,486,349,486,352,494,171,494,178,487,177,481,184,477,187,471,189,463,
    187,458,182,456,190,456,199,452,192,449,189,441,195,443,205,436,211,427,
    212,417,209,409,205,404,209,400,216,397,214,389,214,382),
  // Left shoulder and sleeve, with the overlying hair excluded along the right edge.
  Points(119,394,132,395,145,399,158,405,170,413,177,425,178,435,175,445,164,447,
    163,456,160,466,162,478,171,484,166,490,169,494,32,494,40,484,50,476,60,469,
    66,455,72,440,78,423,88,414,103,407,112,404,113,400),
  // Right shoulder and sleeve.
  Points(394,394,382,395,369,399,354,406,344,413,338,423,336,435,338,444,352,448,
    354,454,355,462,353,469,347,475,341,480,348,487,354,494,487,494,480,483,471,475,
    459,467,451,451,446,437,439,422,427,414,412,407,407,403,405,399)
 };
 public static void Build(string root,string work) {
  string actual=Path.Combine(root,"Asgard/Enemy_Valkyrie_EquipmentMask.png");
  using(var portrait=new Bitmap(Path.Combine(root,"Asgard/Enemy_Valkyrie_Character.png")))
  using(var source=new Bitmap(Path.Combine(work,"../enemy-pastel-style/valkyrie-simple-generated.png")))
  using(var guide=new Bitmap(512,512,PixelFormat.Format32bppArgb))
  using(var mask=new Bitmap(512,512,PixelFormat.Format32bppArgb))
  using(var path=new GraphicsPath(FillMode.Winding)) {
   using(var sg=Graphics.FromImage(guide)) {
    sg.CompositingMode=CompositingMode.SourceCopy;sg.InterpolationMode=InterpolationMode.HighQualityBicubic;
    sg.PixelOffsetMode=PixelOffsetMode.HighQuality;sg.DrawImage(source,new Rectangle(0,0,512,512));
   }
   foreach(var contour in Contours)path.AddPolygon(contour);
   using(var g=Graphics.FromImage(mask)) {
    g.Clear(Color.Black);g.SmoothingMode=SmoothingMode.None;g.FillPath(Brushes.White,path);
   }
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    var c=guide.GetPixel(x,y);
    bool skin=c.R>c.B+15&&c.R>c.G+10;
    // The contour excludes hair. Do not classify by gray/purple brightness:
    // shaded metal is also purple-gray, so color thresholds create speckled holes.
    if(portrait.GetPixel(x,y).A==0||skin)mask.SetPixel(x,y,Color.Black);
   }
   mask.Save(Path.Combine(work,"Enemy_Valkyrie_EquipmentMask.png"),ImageFormat.Png);
  }
 }
 public static string Verify(string work) {
  using(var mask=new Bitmap(Path.Combine(work,"Enemy_Valkyrie_EquipmentMask.png"))) {
   var equipment=Points(130,220,150,237,180,228,258,224,375,218,350,226,230,402,288,400,260,438,250,470,130,430,390,430,80,480,436,480);
   var protectedPoints=Points(260,160,190,280,160,380,189,430,320,420,350,470,256,320,256,355);
   foreach(var p in equipment)if(mask.GetPixel((int)p.X,(int)p.Y).R!=255)throw new Exception("Equipment missing at "+p);
   foreach(var p in protectedPoints)if(mask.GetPixel((int)p.X,(int)p.Y).R!=0)throw new Exception("Hair/face masked at "+p);
   int selected=0;
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    var c=mask.GetPixel(x,y);
    if(c.A!=255||c.R!=c.G||c.R!=c.B||(c.R!=0&&c.R!=255))throw new Exception("Expected opaque binary data mask");
    if(c.R==255)selected++;
   }
   return "PASS: 512x512 binary equipment mask, equipment sample points covered and face/hair sample points excluded. Mask pixels: "+selected;
  }
 }
 public static void Preview(string root,string work) {
  var match=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(Path.Combine(root,"Asgard/EnemyCube_Mobility.mat")),
   @"_EquipmentColor: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)");
  if(!match.Success)throw new Exception("Cannot read current preview material color");
  int[] tint=new int[3];for(int i=0;i<3;i++)tint[i]=(int)Math.Round(255*double.Parse(match.Groups[i+1].Value,System.Globalization.CultureInfo.InvariantCulture));
  using(var portrait=new Bitmap(Path.Combine(root,"Asgard/Enemy_Valkyrie_Character.png")))
  using(var oldMask=new Bitmap(Path.Combine(work,"before/Enemy_Valkyrie_EquipmentMask.png")))
  using(var mask=new Bitmap(Path.Combine(work,"Enemy_Valkyrie_EquipmentMask.png")))
  using(var canvas=new Bitmap(1024,550,PixelFormat.Format32bppArgb))
  using(var g=Graphics.FromImage(canvas))using(var font=new Font("Segoe UI",13,FontStyle.Bold)) {
   g.Clear(Color.FromArgb(245,242,237));
   g.DrawString("BEFORE",font,Brushes.DimGray,12,6);g.DrawString("EQUIPMENT MASK FIX",font,Brushes.DimGray,524,6);
   for(int which=0;which<2;which++)using(var colored=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
    for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
     var p=portrait.GetPixel(x,y);bool hit=(which==0?oldMask:mask).GetPixel(x,y).R>=128;
     colored.SetPixel(x,y,hit?Color.FromArgb(p.A,p.R*tint[0]/255,p.G*tint[1]/255,p.B*tint[2]/255):p);
    }
    g.DrawImageUnscaled(colored,which*512,34);
   }
   canvas.Save(Path.Combine(work,"mask-comparison.png"),ImageFormat.Png);
  }
 }
}
'@
[ValkyrieEquipmentMask]::Build($assetRoot,$PSScriptRoot)
[ValkyrieEquipmentMask]::Preview($assetRoot,$PSScriptRoot)
[ValkyrieEquipmentMask]::Verify($PSScriptRoot)
Write-Output 'Built mask and preview; actual asset has not been overwritten.'
