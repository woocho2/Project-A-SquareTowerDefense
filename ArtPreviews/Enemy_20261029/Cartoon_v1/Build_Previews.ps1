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
public static class EnemyCartoonPack {
 static Bitmap Blank(int w,int h){return new Bitmap(w,h,PixelFormat.Format32bppArgb);}
 static void Draw(Graphics g,Image src,Rectangle dst,Rectangle rect){
  g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
  using(var attr=new ImageAttributes()){attr.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(src,dst,rect.X,rect.Y,rect.Width,rect.Height,GraphicsUnit.Pixel,attr);}
 }
 static void Text(Graphics g,string s,Rectangle r,int size,Color color){
  using(var f=new Font("Malgun Gothic",size,FontStyle.Bold,GraphicsUnit.Pixel))
  using(var b=new SolidBrush(color))
  using(var fmt=new StringFormat()){fmt.Alignment=StringAlignment.Center;fmt.LineAlignment=StringAlignment.Center;g.DrawString(s,f,b,r,fmt);}
 }
 static int[] Scan(Bitmap img){
  var data=img.LockBits(new Rectangle(0,0,img.Width,img.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try{int stride=Math.Abs(data.Stride);byte[] b=new byte[stride*img.Height];Marshal.Copy(data.Scan0,b,0,b.Length);
   int minX=img.Width,minY=img.Height,maxX=-1,maxY=-1,edge=0,partial=0,visible=0;
   for(int y=0;y<img.Height;y++)for(int x=0;x<img.Width;x++){
    int a=b[y*stride+x*4+3];if(a==0)continue;visible++;
    minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
    if(a<255)partial++;if(a>32&&(x==0||y==0||x==img.Width-1||y==img.Height-1))edge++;
   }return new[]{minX,minY,maxX,maxY,edge,partial,visible};
  }finally{img.UnlockBits(data);}
 }
 public static string Build(string dir,string[] groups,string[] stages,string[] keys,string[] names,string[] roles){
  var report=new StringBuilder("Stage,Group,Key,Width,Height,MinX,MinY,MaxX,MaxY,PartialAlpha,NativeEdgePixels\r\n");
  var cells=new Bitmap[27];var ink=Color.FromArgb(36,43,56);
  for(int i=0;i<27;i++){
   using(var source=new Bitmap(Path.Combine(dir,"Sources",groups[i]+"_"+keys[i]+".png"))){
    int[] native=Scan(source);if(native[4]!=0)throw new Exception("Native clip "+keys[i]);
    cells[i]=Blank(512,512);double scale=480.0/Math.Max(source.Width,source.Height);
    int w=(int)Math.Round(source.Width*scale),h=(int)Math.Round(source.Height*scale);
    using(var g=Graphics.FromImage(cells[i])){g.Clear(Color.Transparent);Draw(g,source,new Rectangle((512-w)/2,(512-h)/2,w,h),new Rectangle(0,0,source.Width,source.Height));}
    int[] s=Scan(cells[i]);if(s[6]==0||s[0]<16||s[1]<16||s[2]>495||s[3]>495)throw new Exception("Invalid gutter "+keys[i]);
    cells[i].Save(Path.Combine(dir,"Characters","Enemy_"+groups[i]+"_"+keys[i]+".png"),ImageFormat.Png);
    report.AppendLine((i/3+1)+","+groups[i]+","+keys[i]+",512,512,"+s[0]+","+s[1]+","+s[2]+","+s[3]+","+s[5]+","+native[4]);
   }
  }
  for(int stage=0;stage<9;stage++){
   int start=stage*3;
   using(var atlas=Blank(1024,1024))using(var g=Graphics.FromImage(atlas)){
    g.CompositingMode=CompositingMode.SourceCopy;for(int r=0;r<3;r++)g.DrawImageUnscaled(cells[start+r],r%2*512,r/2*512);
    atlas.Save(Path.Combine(dir,"Atlases",groups[start]+"_Atlas.png"),ImageFormat.Png);
   }
   using(var preview=Blank(1536,792))using(var g=Graphics.FromImage(preview)){
    g.Clear(Color.FromArgb(239,236,228));Text(g,(stage+1)+". "+stages[start]+" · 일반 / 중간 보스 / 최종 보스",new Rectangle(0,0,1536,64),30,ink);
    for(int r=0;r<3;r++){
     int i=start+r;Text(g,names[i]+" · "+roles[i],new Rectangle(r*512,64,512,40),25,ink);
     Draw(g,cells[i],new Rectangle(r*512,104,512,512),new Rectangle(0,0,512,512));
     Draw(g,cells[i],new Rectangle(r*512+208,644,96,96),new Rectangle(0,0,512,512));
    }
    Text(g,"512px 원본 / 아래: 96px 축소 비교",new Rectangle(0,746,1536,42),21,ink);
    preview.Save(Path.Combine(dir,"Previews",groups[start]+"_3_Concept_Preview.png"),ImageFormat.Png);
   }
  }
  using(var overview=Blank(2304,1090))using(var g=Graphics.FromImage(overview)){
   g.Clear(Color.FromArgb(239,236,228));Text(g,"적 27종 · 9개 스테이지 카툰 전신 시안",new Rectangle(0,0,2304,64),34,ink);
   for(int i=0;i<27;i++){
    int x=i%9*256,y=64+i/9*342;
    if(i%3==0)Text(g,(i/3+1)+". "+stages[i],new Rectangle(x,y,768,36),23,ink);
    Text(g,names[i],new Rectangle(x,y+36,256,42),18,ink);
    Draw(g,cells[i],new Rectangle(x,y+78,256,256),new Rectangle(0,0,512,512));
   }
   overview.Save(Path.Combine(dir,"Previews","Overview_27.png"),ImageFormat.Png);
  }
  using(var small=Blank(1296,544))using(var g=Graphics.FromImage(small)){
   g.Clear(Color.FromArgb(32,43,59));Text(g,"96px 축소 가독성 · 적 27종",new Rectangle(0,0,1296,64),25,Color.White);
   for(int i=0;i<27;i++){int x=i%9*144,y=64+i/9*160;Text(g,names[i],new Rectangle(x,y,144,42),14,Color.White);Draw(g,cells[i],new Rectangle(x+24,y+42,96,96),new Rectangle(0,0,512,512));}
   small.Save(Path.Combine(dir,"Previews","Readability_96.png"),ImageFormat.Png);
  }
  foreach(var c in cells)c.Dispose();return report.ToString();
 }
}
'@
$root=Join-Path (Get-Location).Path 'ArtPreviews/Enemy_20261029/Cartoon_v1'
$specs=foreach($path in Get-ChildItem -LiteralPath (Join-Path $root 'Prompts') -Filter '*.json' -File){Get-Content -LiteralPath $path.FullName -Raw -Encoding UTF8 | ConvertFrom-Json}
$specs=@($specs | Sort-Object index)
if($specs.Count -ne 27){throw 'Need 27 prompt records'}
$utf8=New-Object Text.UTF8Encoding($true)
$parts=@($specs | ForEach-Object { $_ | ConvertTo-Json -Depth 10 })
$manifest='['+[Environment]::NewLine+($parts -join (','+[Environment]::NewLine))+[Environment]::NewLine+']'
[IO.File]::WriteAllText((Join-Path $root 'Manifest_Prompts.json'),$manifest,$utf8)
$csv=[EnemyCartoonPack]::Build($root,[string[]]$specs.group,[string[]]$specs.stageName,[string[]]$specs.key,[string[]]$specs.name,[string[]]$specs.roleName)
[IO.File]::WriteAllText((Join-Path $root 'Validation_27.csv'),$csv,$utf8)
Write-Output 'Built 27 transparent 512px sprites, 9 stage atlases (1024px), 9 stage previews, overview and 96px review.'
