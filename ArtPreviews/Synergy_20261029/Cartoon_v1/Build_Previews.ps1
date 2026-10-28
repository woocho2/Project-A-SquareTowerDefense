$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
public static class SynergyCartoonPack {
 static Bitmap Blank(int w,int h){return new Bitmap(w,h,PixelFormat.Format32bppArgb);}
 static void Draw(Graphics g,Image src,Rectangle dst,Rectangle rect){
  g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
  using(var attr=new ImageAttributes()){attr.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(src,dst,rect.X,rect.Y,rect.Width,rect.Height,GraphicsUnit.Pixel,attr);}
 }
 static void Text(Graphics g,string s,Rectangle r,int size,Color color){
  using(var f=new Font("Malgun Gothic",size,FontStyle.Bold,GraphicsUnit.Pixel))
  using(var brush=new SolidBrush(color))
  using(var fmt=new StringFormat()){fmt.Alignment=StringAlignment.Center;fmt.LineAlignment=StringAlignment.Center;g.DrawString(s,f,brush,r,fmt);}
 }
 static int[] Scan(Bitmap img){
  var data=img.LockBits(new Rectangle(0,0,img.Width,img.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try{int stride=Math.Abs(data.Stride);byte[] b=new byte[stride*img.Height];Marshal.Copy(data.Scan0,b,0,b.Length);
   int minX=img.Width,minY=img.Height,maxX=-1,maxY=-1,edge=0,partial=0,visible=0;
   for(int y=0;y<img.Height;y++)for(int x=0;x<img.Width;x++){
    int a=b[y*stride+x*4+3];if(a==0)continue;
    visible++;minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
    if(a<255)partial++;if(a>32&&(x==0||y==0||x==img.Width-1||y==img.Height-1))edge++;
   }
   return new[]{minX,minY,maxX,maxY,edge,partial,visible};
  }finally{img.UnlockBits(data);}
 }
 public static string Build(string dir,string project,string[] keys,string[] names){
  var report=new StringBuilder("Id,Key,Width,Height,MinX,MinY,MaxX,MaxY,PartialAlpha,NativeEdgePixels\r\n");
  var cells=new Bitmap[14];var ink=Color.FromArgb(35,44,60);
  using(var atlas=Blank(2048,2048)){
   for(int i=0;i<14;i++){
    using(var source=new Bitmap(Path.Combine(dir,"Sources",keys[i]+".png"))){
     int[] native=Scan(source);if(native[4]!=0)throw new Exception("Native edge clipping: "+keys[i]+" pixels "+native[4]);
     cells[i]=Blank(512,512);double scale=480.0/Math.Max(source.Width,source.Height);int w=(int)Math.Round(source.Width*scale),h=(int)Math.Round(source.Height*scale);
     using(var g=Graphics.FromImage(cells[i])){g.Clear(Color.Transparent);Draw(g,source,new Rectangle((512-w)/2,(512-h)/2,w,h),new Rectangle(0,0,source.Width,source.Height));}
     int[] s=Scan(cells[i]);if(s[6]==0||s[0]<16||s[1]<16||s[2]>495||s[3]>495)throw new Exception("Invalid margin: "+keys[i]);
     cells[i].Save(Path.Combine(dir,"Characters","Synergy_"+keys[i]+".png"),ImageFormat.Png);
     report.AppendLine((6001+i)+","+keys[i]+",512,512,"+s[0]+","+s[1]+","+s[2]+","+s[3]+","+s[5]+","+native[4]);
     using(var g=Graphics.FromImage(atlas)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(cells[i],i%4*512,i/4*512);}
    }
   }
   atlas.Save(Path.Combine(dir,"Atlases","Synergy_Atlas_Cartoon_v1.png"),ImageFormat.Png);
  }
  using(var overview=Blank(1536,1784))using(var g=Graphics.FromImage(overview)){
   g.Clear(Color.FromArgb(239,236,228));
   Text(g,"시너지 14종 · 카툰 전신 / 마지막 두 칸: 일반 타워 비교",new Rectangle(0,0,1536,64),29,ink);
   for(int i=0;i<16;i++){
    int x=i%4*384,y=64+i/4*430;string label=i<14?names[i]:i==14?"일반 타워 · 레인저":"일반 타워 · 소드 마스터";
    Text(g,label,new Rectangle(x,y,384,42),23,ink);
    if(i<14)Draw(g,cells[i],new Rectangle(x,y+42,384,384),new Rectangle(0,0,512,512));
    else using(var ordinary=new Bitmap(Path.Combine(project,"Assets","4. Asset","2. Tower","TowerCharacter",i==14?"Bow_Atlas.png":"Sword_Atlas.png"))){Draw(g,ordinary,new Rectangle(x,y+42,384,384),new Rectangle(0,0,512,512));}
   }
   overview.Save(Path.Combine(dir,"Previews","Overview_14.png"),ImageFormat.Png);
  }
  using(var small=Blank(1008,360))using(var g=Graphics.FromImage(small)){
   g.Clear(Color.FromArgb(32,43,59));Text(g,"96px 축소 가독성 확인",new Rectangle(0,0,1008,48),23,Color.White);
   for(int i=0;i<14;i++){int x=i%7*144,y=48+i/7*156;Text(g,names[i],new Rectangle(x,y,144,34),15,Color.White);Draw(g,cells[i],new Rectangle(x+24,y+34,96,96),new Rectangle(0,0,512,512));}
   small.Save(Path.Combine(dir,"Previews","Readability_96.png"),ImageFormat.Png);
  }
  for(int i=0;i<cells.Length;i++)cells[i].Dispose();
  return report.ToString();
 }
}
'@
$root=Join-Path (Get-Location).Path 'ArtPreviews/Synergy_20261029/Cartoon_v1'
$specs=foreach($path in Get-ChildItem -LiteralPath (Join-Path $root 'Prompts') -Filter '*.json' -File){Get-Content -LiteralPath $path.FullName -Raw -Encoding UTF8 | ConvertFrom-Json}
$specs=@($specs | Sort-Object id)
if($specs.Count -ne 14){throw 'Need 14 source records'}
$utf8=New-Object Text.UTF8Encoding($true)
$manifest='['+[Environment]::NewLine+(($specs | ForEach-Object { $_ | ConvertTo-Json -Depth 10 }) -join (','+[Environment]::NewLine))+[Environment]::NewLine+']'
[IO.File]::WriteAllText((Join-Path $root 'Manifest_Prompts.json'),$manifest,$utf8)
$csv=[SynergyCartoonPack]::Build($root,(Get-Location).Path,[string[]]$specs.key,[string[]]$specs.name)
[IO.File]::WriteAllText((Join-Path $root 'Validation_14.csv'),$csv,$utf8)
Write-Output 'Built 14 transparent 512px sprites, 2048px atlas, overview and 96px review. Native clipping=0 and >=16px transparent margin for every cell.'
