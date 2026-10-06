param()
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class RealmOverlayBuilder {
 static double L(Color c){return .2126*c.R+.7152*c.G+.0722*c.B;}
 static bool Gold(Color c){return c.R>c.B+20&&c.R>c.G+5;}
 static bool Text(int x,int y,Color c){int lo=Math.Min(c.R,Math.Min(c.G,c.B)),hi=Math.Max(c.R,Math.Max(c.G,c.B));return y>=618&&y<=664&&x%256>=28&&x%256<=228&&lo>35&&hi-lo<60;}
 static double[] Mean(List<Color> a,double q){
  int mid=(int)Math.Round(q*(a.Count-1)),half=Math.Max(1,a.Count/100),n=0;double r=0,g=0,b=0;
  for(int i=Math.Max(0,mid-half);i<=Math.Min(a.Count-1,mid+half);i++){r+=a[i].R;g+=a[i].G;b+=a[i].B;n++;}
  return new double[]{r/n,g/n,b/n};
 }
 class Palette {
  double[] levels=new double[9];double[][] colors=new double[9][];
  public Palette(List<Color> source,List<Color> target){
   if(source.Count<100||target.Count<100)throw new Exception("Insufficient palette samples");
   source.Sort((a,b)=>L(a).CompareTo(L(b)));target.Sort((a,b)=>L(a).CompareTo(L(b)));
   double[] q={0,.02,.10,.25,.5,.75,.9,.98,1};
   for(int i=0;i<q.Length;i++){var c=Mean(source,q[i]);levels[i]=.2126*c[0]+.7152*c[1]+.0722*c[2];colors[i]=Mean(target,q[i]);}
  }
  public Color Map(Color c){
   double lum=L(c);int k=0;while(k<7&&lum>levels[k+1])k++;
   double f=Math.Max(0,Math.Min(1,(lum-levels[k])/Math.Max(.001,levels[k+1]-levels[k])));
   return Color.FromArgb(c.A,
    (int)Math.Round(colors[k][0]+f*(colors[k+1][0]-colors[k][0])),
    (int)Math.Round(colors[k][1]+f*(colors[k+1][1]-colors[k][1])),
    (int)Math.Round(colors[k][2]+f*(colors[k+1][2]-colors[k][2])));
  }
 }
 public static string Build(string masterPath,string generatedPath,string outPath){
 using(var master=new Bitmap(masterPath))
 using(var generated=new Bitmap(generatedPath))
 using(var dst=new Bitmap(1024,768,PixelFormat.Format32bppArgb)){
  var sourceGold=new List<Color>();var targetGold=new List<Color>();
  var sourceDark=new List<Color>();var targetDark=new List<Color>();
  for(int y=0;y<512;y++)for(int x=0;x<1024;x++){
   var c=master.GetPixel(x,y);if(c.A<245)continue;
   int gx=Math.Min(generated.Width-1,(int)((x+.5)*generated.Width/1024));
   int gy=Math.Min(generated.Height-1,(int)((y+.5)*generated.Height/768));
   var t=generated.GetPixel(gx,gy);
   if(Gold(c)){
    sourceGold.Add(c);if(t.A>240&&L(t)>80)targetGold.Add(t);
   }else{
    sourceDark.Add(c);if(t.A>240&&L(t)<100)targetDark.Add(t);
   }
  }
  var goldPalette=new Palette(sourceGold,targetGold);var darkPalette=new Palette(sourceDark,targetDark);
  int alphaChanges=0,textChanges=0;int[] activeCounts=new int[8];long textCount=0;
  for(int y=0;y<768;y++)for(int x=0;x<1024;x++){
   var c=master.GetPixel(x,y);bool text=Text(x,y,c);var result=c;
   if(c.A>0&&!text)result=Gold(c)?goldPalette.Map(c):darkPalette.Map(c);
   dst.SetPixel(x,y,result);
   if(c.A!=result.A)alphaChanges++;
   if(text){textCount++;if(c.ToArgb()!=result.ToArgb())textChanges++;}
   if(y<512&&c.A>0&&Gold(c))activeCounts[(y/256)*4+x/256]++;
  }
  if(alphaChanges!=0||textChanges!=0)throw new Exception("Geometry or ivory text changed");
  dst.Save(outPath,ImageFormat.Png);
  return "alphaChanges="+alphaChanges+"; ivoryChanges="+textChanges+"; ivoryPixels="+textCount+"; activeMaskPixels="+String.Join(",",activeCounts)+"; AI palette samples="+targetGold.Count+","+targetDark.Count;
 }
 }
 public static void Composite(string atlasPath,string tilePath,string outPath){
 using(var atlas=new Bitmap(atlasPath))
 using(var tile=new Bitmap(tilePath))
 using(var dst=new Bitmap(1024,768,PixelFormat.Format32bppArgb))
 using(var g=Graphics.FromImage(dst)){
  g.Clear(Color.FromArgb(245,241,230));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  for(int i=0;i<12;i++){
   int x=i%4*256,y=i/4*256;var src=new Rectangle(x,y,256,256);
   g.DrawImage(tile,new Rectangle(x,y,256,256));
   g.DrawImage(atlas,i<8?src:new Rectangle(x+32,y+32,192,192),src,GraphicsUnit.Pixel);
  }
  dst.Save(outPath,ImageFormat.Png);
 }
 }
 public static void Comparison(string root,string[] names,string outPath){
 using(var dst=new Bitmap(1024,1280,PixelFormat.Format32bppArgb))
 using(var g=Graphics.FromImage(dst))
 using(var font=new Font("Arial",19,FontStyle.Bold))
 using(var ink=new SolidBrush(Color.FromArgb(45,50,55))){
  g.Clear(Color.FromArgb(245,241,230));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  for(int i=0;i<names.Length;i++){
   string name=names[i];int x=(i%2)*512,y=(i/2)*256;
   using(var atlas=new Bitmap(root+(name=="Asgard"?"/Asgard_Master.png":"/"+name+"/Tile_Path_Overlay_"+name+"_Atlas.png")))
   using(var tile=new Bitmap(root+"/references/"+name+"_Path_Reference.png")){
    g.DrawString(name,font,ink,x+16,y+10);
    var rect=new Rectangle(x+16,y+48,192,192);g.DrawImage(tile,rect);
    g.DrawImage(atlas,rect,new Rectangle(512,0,256,256),GraphicsUnit.Pixel);
    for(int j=0;j<4;j++)g.DrawImage(atlas,new Rectangle(x+228+(j%2)*140,y+78+(j/2)*70,130,46),new Rectangle(j*256+15,600,226,80),GraphicsUnit.Pixel);
   }
  }
  dst.Save(outPath,ImageFormat.Png);
 }
 }
}
'@
$realmRoot=Join-Path $PSScriptRoot ''
$plan=Get-Content -LiteralPath (Join-Path $realmRoot 'Realm_Plan.json') -Encoding UTF8 -Raw | ConvertFrom-Json
foreach($realm in $plan){
 $realmDirectory=Join-Path $realmRoot $realm.name
 New-Item -ItemType Directory -Path $realmDirectory -Force | Out-Null
 Copy-Item -LiteralPath $realm.generated -Destination (Join-Path $realmDirectory 'AI_Palette_Source.png')
 $outPath=Join-Path $realmDirectory ('Tile_Path_Overlay_'+$realm.name+'_Atlas.png')
 $validation=[RealmOverlayBuilder]::Build((Join-Path $realmRoot 'Asgard_Master.png'),$realm.generated,$outPath)
 [RealmOverlayBuilder]::Composite($outPath,(Join-Path $realmRoot ('references\'+$realm.name+'_Path_Reference.png')),(Join-Path $realmDirectory '12Sprites_Composite.png'))
 Write-Output ($realm.name+': '+$validation)
}
Copy-Item -LiteralPath 'D:\MyGitHub\ProjectA\Project-A-SquareTowerDefense\output\asgard-rounded-arrow-concept\Asgard_Path_Reference.png' -Destination (Join-Path $realmRoot 'references\Asgard_Path_Reference.png')
[RealmOverlayBuilder]::Comparison($realmRoot,@('Asgard','Vanaheim','Alfheim','Jotunheim','Midgard','Nidavellir','Niflheim','Hel','Muspelheim','Ragnarok'),(Join-Path $realmRoot 'All_Realms_Comparison.png'))

