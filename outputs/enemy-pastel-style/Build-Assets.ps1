$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class EnemyPastelArt {
 static Point[] Points(params int[] values) {var p=new Point[values.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Point(values[i*2],values[i*2+1]);return p;}
 static bool Inside(int x,int y,Point[] p) {bool hit=false;for(int i=0,j=p.Length-1;i<p.Length;j=i++)if((p[i].Y>y)!=(p[j].Y>y)&&x<(p[j].X-p[i].X)*(y-p[i].Y)/(double)(p[j].Y-p[i].Y)+p[i].X)hit=!hit;return hit;}
 static readonly Point[] Band=Points(157,220,233,220,254,207,277,219,355,220,355,234,278,235,257,252,236,235,157,235);
 static readonly Point[] Wing=Points(128,173,133,192,151,210,158,222,161,239,154,256,144,266,138,253,123,253,116,242,124,241,119,229,111,215,119,216,117,195);
 static readonly Point[] Body=Points(221,376,257,383,291,374,311,392,316,415,302,441,333,474,338,493,182,493,188,474,212,443,198,420,208,399);
 static readonly Point[] Shoulder=Points(117,396,138,402,158,413,174,435,183,457,177,465,154,461,106,489,47,492,62,476,74,455,92,417);
 static bool Equipment(int character,int x,int y,Color c,bool oldMask) {
  if(c.A==0)return false;
  // Bosses retain their original equipment masks and every unmasked original pixel.
  if(character!=0)return oldMask;
  int max=Math.Max(c.R,Math.Max(c.G,c.B)),min=Math.Min(c.R,Math.Min(c.G,c.B));
  bool neutral=max-min<=8&&c.B-c.R<=1&&c.B-c.G<=2;
  return neutral&&(Inside(x,y,Band)||Inside(x,y,Wing)||Inside(512-x,y,Wing)
    ||Inside(x,y,Body)||Inside(x,y,Shoulder)||Inside(512-x,y,Shoulder));
 }
 static int GrayPaint(Color c) {
  double v=Math.Max(c.R,Math.Max(c.G,c.B))/255.0;
  return (int)Math.Round(188+56*v);
 }
 static void CloseEquipmentMask(Bitmap mask,Bitmap original,Bitmap portrait) {
  const int radius=3;
  var dilated=new bool[512*512];
  for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
   bool white=false;
   for(int dy=-radius;dy<=radius&&!white;dy++)for(int dx=-radius;dx<=radius;dx++) {
    int xx=x+dx,yy=y+dy;if(xx>=0&&xx<512&&yy>=0&&yy<512&&mask.GetPixel(xx,yy).R>=128){white=true;break;}
   }
   dilated[y*512+x]=white;
  }
  for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
   bool white=true;
   for(int dy=-radius;dy<=radius&&white;dy++)for(int dx=-radius;dx<=radius;dx++) {
    int xx=x+dx,yy=y+dy;if(xx<0||xx>=512||yy<0||yy>=512||!dilated[yy*512+xx]){white=false;break;}
   }
   var c=original.GetPixel(x,y);white=white&&c.A>0;
   int value=white?GrayPaint(c):0;
   mask.SetPixel(x,y,white?Color.White:Color.Black);
   portrait.SetPixel(x,y,white?Color.FromArgb(c.A,value,value,value):c);
  }
 }
 static Bitmap Sized(string path) {
  using(var original=new Bitmap(path)) {
   if(original.Width==512&&original.Height==512)return new Bitmap(path);
   var result=new Bitmap(512,512,PixelFormat.Format32bppArgb);
   using(var g=Graphics.FromImage(result)) {
    g.CompositingMode=CompositingMode.SourceCopy;
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    g.DrawImage(original,new Rectangle(0,0,512,512));
   }
   return result;
  }
 }
 public static void Build(string work) {
  string[] names={"Valkyrie","Brunhild","Loki"};
  string before=Path.Combine(work,"before"),output=Path.Combine(work,"assets");Directory.CreateDirectory(output);
  for(int character=0;character<3;character++) {
   string name=names[character];
   string source=character==0?Path.Combine(work,"valkyrie-simple-generated.png"):Path.Combine(before,"Asgard/Enemy_"+name+"_Character.png");
   using(var original=Sized(source))
   using(var oldMask=new Bitmap(Path.Combine(before,"Asgard/Enemy_"+name+"_EquipmentMask.png")))
   using(var portrait=new Bitmap(512,512,PixelFormat.Format32bppArgb))
   using(var mask=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
    int maskCount=0;
    for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
     Color c=original.GetPixel(x,y);
     bool gear=Equipment(character,x,y,c,character!=0&&oldMask.GetPixel(x,y).R>=128);
     int value=gear?GrayPaint(c):0;
     portrait.SetPixel(x,y,gear?Color.FromArgb(c.A,value,value,value):c);
     mask.SetPixel(x,y,gear?Color.White:Color.Black);
     if(gear)maskCount++;
    }
    if(character==0)CloseEquipmentMask(mask,original,portrait);
    if(maskCount<5000)throw new Exception("Insufficient equipment selection: "+name);
    portrait.Save(Path.Combine(output,"Enemy_"+name+"_Character.png"),ImageFormat.Png);
    mask.Save(Path.Combine(output,"Enemy_"+name+"_EquipmentMask.png"),ImageFormat.Png);
   }
  }
  using(var original=new Bitmap(Path.Combine(before,"Enemy_Decoration.png")))
  using(var frame=new Bitmap(512,512,PixelFormat.Format32bppArgb))
  using(var oldBackground=new Bitmap(Path.Combine(before,"Enemy_Background.png")))
  using(var background=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
   for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
    Color f=original.GetPixel(x,y);int v=f.R<180?198:f.R<230?224:246;
    frame.SetPixel(x,y,f.A==0?Color.FromArgb(0,0,0,0):Color.FromArgb(f.A,v,v,v));
    int bg=(int)Math.Round(252-y/511.0*8);
    background.SetPixel(x,y,Color.FromArgb(oldBackground.GetPixel(x,y).A,bg,bg,bg));
   }
   frame.Save(Path.Combine(output,"Enemy_Decoration.png"),ImageFormat.Png);
   background.Save(Path.Combine(output,"Enemy_Background.png"),ImageFormat.Png);
  }
 }
 public static void Preview(string work) {
  string[] names={"Valkyrie","Brunhild","Loki"};
  string[] states={"SelfHeal","Defense","Action","Mobility","Locked","Hourglass"};
  string[] tint={"#E6A9B5","#E6D392","#B5D6B7","#ACD5E8","#EEECE7","#D1BDE3"};
  string[] bg={"#FAEDF0","#FAF5E6","#EEF7EE","#ECF5FA","#F8F7F4","#F4EEF9"};
  string assets=Path.Combine(work,"assets");
  using(var canvas=new Bitmap(1200,726,PixelFormat.Format32bppArgb))
  using(var g=Graphics.FromImage(canvas))using(var font=new Font("Segoe UI",10,FontStyle.Bold))
  using(var frame=new Bitmap(Path.Combine(assets,"Enemy_Decoration.png")))
  using(var background=new Bitmap(Path.Combine(assets,"Enemy_Background.png")))
  using(var badges=new Bitmap(Path.Combine(work,"before/Enemy_State_Badges_Atlas.png"))) {
   g.Clear(Color.FromArgb(243,242,239));
   for(int character=0;character<3;character++)
   using(var portrait=new Bitmap(Path.Combine(assets,"Enemy_"+names[character]+"_Character.png")))
   using(var mask=new Bitmap(Path.Combine(assets,"Enemy_"+names[character]+"_EquipmentMask.png"))) {
    g.DrawString(names[character],font,Brushes.DimGray,8,character*242+3);
    for(int state=0;state<6;state++)
    using(var tile=new Bitmap(512,512,PixelFormat.Format32bppArgb))
    using(var coloredFrame=new Bitmap(512,512,PixelFormat.Format32bppArgb))
    using(var coloredPortrait=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
     Color target=ColorTranslator.FromHtml(tint[state]),back=ColorTranslator.FromHtml(bg[state]);
     for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
      Color p=portrait.GetPixel(x,y),f=frame.GetPixel(x,y),b=background.GetPixel(x,y);
      coloredPortrait.SetPixel(x,y,mask.GetPixel(x,y).R>=128?Color.FromArgb(p.A,p.R*target.R/255,p.G*target.G/255,p.B*target.B/255):p);
      coloredFrame.SetPixel(x,y,Color.FromArgb(f.A,f.R*target.R/255,f.G*target.G/255,f.B*target.B/255));
      tile.SetPixel(x,y,Color.FromArgb(b.A,b.R*back.R/255,b.G*back.G/255,b.B*back.B/255));
     }
     using(var tg=Graphics.FromImage(tile)) {
      tg.DrawImageUnscaled(coloredPortrait,0,0);tg.DrawImageUnscaled(coloredFrame,0,0);
      tg.DrawImage(badges,new Rectangle(192,0,128,128),new Rectangle(state%3*128,state/3*128,128,128),GraphicsUnit.Pixel);
     }
     // Shader clips all layers with the shared outer background alpha.
     for(int y=0;y<512;y++)for(int x=0;x<512;x++) {var c=tile.GetPixel(x,y);tile.SetPixel(x,y,Color.FromArgb(background.GetPixel(x,y).A,c.R,c.G,c.B));}
     tile.Save(Path.Combine(work,names[character]+"_"+states[state]+".png"),ImageFormat.Png);
     g.DrawImage(tile,new Rectangle(state*200+8,character*242+26,184,184));
     g.DrawString(states[state],font,Brushes.DimGray,state*200+8,character*242+214);
    }
   }
   canvas.Save(Path.Combine(work,"preview.png"),ImageFormat.Png);
  }
 }
}
'@
[EnemyPastelArt]::Build($PSScriptRoot)
[EnemyPastelArt]::Preview($PSScriptRoot)
Write-Output 'Built staged portraits, equipment masks, shared neutral layers and 18 CPU previews. Assets not overwritten yet.'
