$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$settings=[IO.File]::ReadAllText((Join-Path $repo 'Assets/Resources/SharedBoardVisuals.asset'))
$common=Join-Path $repo 'Assets/4. Asset/0. Common'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.IO;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Text.RegularExpressions;using System.Globalization;
public static class SharedBoardPreview {
 static Color ColorOf(string text,string property){
  var m=Regex.Match(text,property+@": \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}");
  if(!m.Success)throw new Exception("Missing color "+property);
  int[] c=new int[4];for(int i=0;i<4;i++)c[i]=(int)Math.Round(255*double.Parse(m.Groups[i+1].Value,CultureInfo.InvariantCulture));return Color.FromArgb(c[3],c[0],c[1],c[2]);
 }
 static Bitmap Tint(Bitmap image,Color color){
  var result=new Bitmap(image.Width,image.Height,PixelFormat.Format32bppArgb);
  for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){var p=image.GetPixel(x,y);result.SetPixel(x,y,Color.FromArgb(p.A*color.A/255,p.R*color.R/255,p.G*color.G/255,p.B*color.B/255));}
  return result;
 }
 static void Draw(Graphics g,Bitmap bg,Bitmap decoration,Rectangle rect,Color b,Color d){
  using(var first=Tint(bg,b))using(var last=Tint(decoration,d)){g.DrawImage(first,rect);g.DrawImage(last,rect);}
 }
 public static void Build(string common,string settings,string portraitPath,string maskPath,string output){
  using(var bg=new Bitmap(Path.Combine(common,"Board_Background.png")))using(var dec=new Bitmap(Path.Combine(common,"Board_Decoration.png")))
  using(var arrow=new Bitmap(Path.Combine(common,"Board_Arrow.png")))using(var portrait=new Bitmap(portraitPath))using(var mask=new Bitmap(maskPath))
  using(var canvas=new Bitmap(1280,780,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
  using(var font=new Font("Segoe UI",11,FontStyle.Bold)){
   g.Clear(Color.FromArgb(245,243,239));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   g.DrawString("SHARED BACKGROUND / DECORATION / ROTATED ARROW - OFFLINE PREVIEW",font,Brushes.DimGray,12,10);
   var stages=Regex.Matches(settings,@"(?ms)^  - realm: \d+\r?\n(.*?)(?=^  - realm:|^  m_tiers:)");
   for(int i=0;i<stages.Count;i++){
    string s=stages[i].Groups[1].Value;int x=i*128;
    g.DrawString(Regex.Match(s,@"displayName: ([^\r\n]+)").Groups[1].Value,font,Brushes.DimGray,x+4,42);
    string spawn=Regex.Match(s,@"(?s)towerSpawn:(.*?)    path:").Groups[1].Value;
    string path=Regex.Match(s,@"(?s)    path:(.*?)    arrow:").Groups[1].Value;
    Draw(g,bg,dec,new Rectangle(x+4,70,120,120),ColorOf(spawn,"background"),ColorOf(spawn,"decoration"));
    Draw(g,bg,dec,new Rectangle(x+4,200,120,120),ColorOf(path,"background"),ColorOf(path,"decoration"));
    using(var colored=Tint(arrow,ColorOf(s,"arrow")))g.DrawImage(colored,new Rectangle(x+4,200,120,120));
   }
   g.DrawString("TOWER TIERS - SAME FRAME AND BACKGROUND",font,Brushes.DimGray,12,340);
   var tiers=Regex.Matches(settings,@"(?ms)^  - tier: \d+\r?\n(.*?)(?=^  - tier:|^  m_towers:)");
   for(int i=0;i<tiers.Count;i++){
    string s=tiers[i].Groups[1].Value;int x=i*168;
    Draw(g,bg,dec,new Rectangle(x+12,370,144,144),ColorOf(s,"background"),ColorOf(s,"decoration"));
    g.DrawString(Regex.Match(s,@"displayName: ([^\r\n]+)").Groups[1].Value,font,Brushes.DimGray,x+12,518);
   }
   g.DrawString("ENEMY STATE COLORS - ORIGINAL PORTRAIT AND MASK",font,Brushes.DimGray,12,552);
   var states=Regex.Matches(settings,@"(?ms)^  - state: (\d+)\r?\n(.*?)(?=^  - state:|^  m_enemies:)");
   string[] names={"Self heal","Defense","Action","Mobility","Locked","Hourglass"};
   for(int i=0;i<states.Count;i++){
    string s=states[i].Groups[2].Value;int x=i*168;
    using(var tile=Tint(bg,ColorOf(s,"background")))using(var p=new Bitmap(portrait.Width,portrait.Height,PixelFormat.Format32bppArgb))
    using(var frame=Tint(dec,ColorOf(s,"decoration"))){
     Color e=ColorOf(s,"equipment");
     for(int y=0;y<p.Height;y++)for(int xx=0;xx<p.Width;xx++){var c=portrait.GetPixel(xx,y);p.SetPixel(xx,y,mask.GetPixel(xx,y).R>=128?Color.FromArgb(c.A,c.R*e.R/255,c.G*e.G/255,c.B*e.B/255):c);}
     using(var tg=Graphics.FromImage(tile)){tg.DrawImageUnscaled(p,0,0);tg.DrawImageUnscaled(frame,0,0);}
     g.DrawImage(tile,new Rectangle(x+12,580,144,144));g.DrawString(names[i],font,Brushes.DimGray,x+12,728);
    }
   }
   canvas.Save(output,ImageFormat.Png);
  }
 }
}
'@
function ResolveGuid([string]$guid){
 foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'Assets') -Filter '*.meta' -File -Recurse){
  if([IO.File]::ReadAllText($file.FullName) -match ('(?m)^guid: '+$guid+'\r?$')){return ($file.FullName -replace '\.meta$','')}
 }
 throw ('Unresolved preview texture '+$guid)
}
$portrait=ResolveGuid '4c589c7b375143148a6c2ca4aa862c4f'
$mask=ResolveGuid 'f6dd47a2f3ee40e0afa808a7900031b1'
[SharedBoardPreview]::Build($common,$settings,$portrait,$mask,(Join-Path $PSScriptRoot 'preview.png'))
Write-Output 'Saved shared asset composition preview.'
