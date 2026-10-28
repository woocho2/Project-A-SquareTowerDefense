param([string]$Groups='Shield,Hammer,Fire,Water,Earth,Wind,Ice,Electric,Light,Dark')
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class RemainingTowerPack {
 static Bitmap Blank(int w,int h){return new Bitmap(w,h,PixelFormat.Format32bppArgb);}
 static void Draw(Graphics g,Image src,Rectangle dst,Rectangle cut){
  g.CompositingMode=CompositingMode.SourceOver;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
  using(var a=new ImageAttributes()){a.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(src,dst,cut.X,cut.Y,cut.Width,cut.Height,GraphicsUnit.Pixel,a);}
 }
 static void Caption(Graphics g,string s,int x,int y,int w,int h,int size,Color c){
  using(var f=new Font("Malgun Gothic",size,FontStyle.Bold,GraphicsUnit.Pixel))
  using(var b=new SolidBrush(c))using(var t=new StringFormat()){t.Alignment=StringAlignment.Center;t.LineAlignment=StringAlignment.Center;g.DrawString(s,f,b,new RectangleF(x,y,w,h),t);}
 }
 public static string Build(string root,string group,string groupName,string[] keys,string[] names){
  var report=new StringBuilder();
  var cells=new Bitmap[4];
  Color ink=Color.FromArgb(37,43,56);
  Color[] colors={Color.FromArgb(174,54,68),Color.FromArgb(52,94,170),Color.FromArgb(153,115,16),Color.FromArgb(115,63,157)};
  string[] roles={"광역","단일","버프","디버프"};
  using(var atlas=Blank(1024,1024)){
   for(int i=0;i<4;i++){
    string input=Path.Combine(root,"Sources",group+"_"+keys[i]+".png");
    int nativeEdge=0;
    using(var src=new Bitmap(input)){
     for(int x=0;x<src.Width;x++){if(src.GetPixel(x,0).A>32)nativeEdge++;if(src.GetPixel(x,src.Height-1).A>32)nativeEdge++;}
     for(int y=0;y<src.Height;y++){if(src.GetPixel(0,y).A>32)nativeEdge++;if(src.GetPixel(src.Width-1,y).A>32)nativeEdge++;}
     double scale=480.0/Math.Max(src.Width,src.Height);int w=(int)Math.Round(src.Width*scale),h=(int)Math.Round(src.Height*scale);
     cells[i]=Blank(512,512);
     using(var g=Graphics.FromImage(cells[i])){g.Clear(Color.Transparent);Draw(g,src,new Rectangle((512-w)/2,(512-h)/2,w,h),new Rectangle(0,0,src.Width,src.Height));}
    }
    int minX=512,minY=512,maxX=-1,maxY=-1,partial=0,visible=0;
    for(int y=0;y<512;y++)for(int x=0;x<512;x++){int a=cells[i].GetPixel(x,y).A;if(a>0){visible++;minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);if(a<255)partial++;}}
    if(visible==0||minX<16||minY<16||maxX>495||maxY>495)throw new Exception("Invalid cell/gutter "+keys[i]);
    cells[i].Save(Path.Combine(root,"Characters","Tower_"+group+"_"+keys[i]+"_Cartoon_v1.png"),ImageFormat.Png);
    using(var g=Graphics.FromImage(atlas)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(cells[i],i%2*512,i/2*512);}
    report.AppendLine(group+","+keys[i]+",512,512,"+minX+","+minY+","+maxX+","+maxY+","+partial+","+nativeEdge);
   }
   atlas.Save(Path.Combine(root,"Atlases","Tower_"+group+"_Atlas_Cartoon_v1.png"),ImageFormat.Png);
   using(var preview=Blank(2048,808))using(var g=Graphics.FromImage(preview)){
    g.Clear(Color.FromArgb(239,236,228));
    Caption(g,groupName+" 4종 · 카툰풍 시안",0,0,2048,68,36,ink);
    for(int i=0;i<4;i++){
     Caption(g,names[i]+" · "+roles[i],i*512,68,512,44,26,colors[i]);
     Draw(g,cells[i],new Rectangle(i*512,112,512,512),new Rectangle(0,0,512,512));
     using(var b=new SolidBrush(colors[i]))g.FillRectangle(b,i*512+48,632,416,3);
     Draw(g,cells[i],new Rectangle(i*512+208,650,96,96),new Rectangle(0,0,512,512));
    }
    Caption(g,"512 x 512 캐릭터 / 아래: 96px 축소 확인",0,756,2048,44,20,ink);
    preview.Save(Path.Combine(root,"Previews",group+"_4_Concept_Preview.png"),ImageFormat.Png);
   }
  }
  foreach(var c in cells)c.Dispose();
  return report.ToString();
 }
}
'@
$remainingDir=Join-Path $PSScriptRoot '.'
$remainingSpecs=Get-Content -LiteralPath (Join-Path $remainingDir 'Manifest_Prompts.json') -Encoding UTF8 -Raw | ConvertFrom-Json
$remainingReport='Group,Character,Width,Height,MinX,MinY,MaxX,MaxY,PartialAlpha,NativeEdgePixels'+[Environment]::NewLine
foreach($remainingGroup in $Groups.Split(',')){
 $remainingSet=@($remainingSpecs | Where-Object {$_.group -eq $remainingGroup} | Sort-Object index)
 if($remainingSet.Count -ne 4){throw ('Wrong group size '+$remainingGroup+': '+$remainingSet.Count)}
 $remainingReport+=[RemainingTowerPack]::Build($remainingDir,$remainingGroup,$remainingSet[0].groupName,[string[]]$remainingSet.key,[string[]]$remainingSet.name)
 Write-Output ($remainingGroup+': 4 cells + atlas + preview saved')
}
[IO.File]::WriteAllText((Join-Path $remainingDir ('Image_Validation_'+$Groups.Replace(',','_')+'.csv')),$remainingReport,(New-Object Text.UTF8Encoding($true)))
