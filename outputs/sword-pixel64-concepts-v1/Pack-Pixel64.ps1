$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class SwordPixel64Pack
{
    static readonly Color[] skin={Color.FromArgb(255,255,209,170),Color.FromArgb(255,239,165,121),Color.FromArgb(255,255,226,195),Color.FromArgb(255,50,35,46)};
    static readonly Color[] dead={Color.FromArgb(255,239,235,250),Color.FromArgb(255,193,186,215),Color.FromArgb(255,255,253,255),Color.FromArgb(255,50,35,46)};
    static readonly Color[] scarColors={Color.FromArgb(255,181,76,85),Color.FromArgb(255,222,130,123)};
    static readonly Color[] violetColors={Color.FromArgb(255,109,36,132),Color.FromArgb(255,194,104,226)};
    static bool Skin(Color c,bool ghost)
    {
        if(c.A<220)return false;
        if(ghost)return c.R>180&&c.G>175&&c.B>185&&Math.Abs(c.R-c.G)<55&&Math.Abs(c.G-c.B)<65;
        return c.R>175&&c.G>100&&c.B>75&&c.R-c.G>8&&c.R-c.G<95&&c.G-c.B>=0&&c.G-c.B<85;
    }
    static Rectangle Bounds(Bitmap b)
    {
        int l=b.Width,t=b.Height,r=-1,bot=-1;
        for(int y=2;y<b.Height-2;y++)for(int x=2;x<b.Width-2;x++)if(b.GetPixel(x,y).A>=240){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    static Rectangle FaceBounds(Bitmap b,bool ghost)
    {
        int w=b.Width,h=b.Height;var labels=new bool[w,h];var seen=new bool[w,h];
        for(int y=(int)(h*.45);y<h;y++)for(int x=(int)(w*.12);x<w*.88;x++)labels[x,y]=Skin(b.GetPixel(x,y),ghost);
        int best=0;Rectangle result=Rectangle.Empty;var queue=new Queue<Point>();
        for(int y=(int)(h*.45);y<h;y++)for(int x=(int)(w*.12);x<w*.88;x++){
            if(!labels[x,y]||seen[x,y])continue;queue.Enqueue(new Point(x,y));seen[x,y]=true;int n=0,l=x,r=x,t=y,bot=y;
            while(queue.Count>0){var p=queue.Dequeue();n++;l=Math.Min(l,p.X);r=Math.Max(r,p.X);t=Math.Min(t,p.Y);bot=Math.Max(bot,p.Y);
                int[] xx={p.X-1,p.X+1,p.X,p.X},yy={p.Y,p.Y,p.Y-1,p.Y+1};
                for(int i=0;i<4;i++){int nx=xx[i],ny=yy[i];if(nx<0||ny<0||nx>=w||ny>=h||seen[nx,ny]||!labels[nx,ny])continue;seen[nx,ny]=true;queue.Enqueue(new Point(nx,ny));}
            }
            if(n>best){best=n;result=Rectangle.FromLTRB(l,t,r+1,bot+1);}
        }
        if(best<20000)throw new Exception("Could not identify face base");return result;
    }
    static double EyeRow(Bitmap b,Rectangle face)
    {
        var seen=new bool[b.Width,b.Height];int bestLeft=0,bestRight=0;double left=0,right=0;
        int y0=face.Y+(int)(face.Height*.2),y1=face.Y+(int)(face.Height*.7);
        var queue=new Queue<Point>();
        Func<int,int,bool> dark=(x,y)=>{var c=b.GetPixel(x,y);return c.A>=220&&c.R<105&&c.G<85&&c.B<120;};
        for(int y=y0;y<y1;y++)for(int x=face.X+(int)(face.Width*.12);x<face.Right-face.Width*.12;x++){
            if(seen[x,y]||!dark(x,y))continue;queue.Enqueue(new Point(x,y));seen[x,y]=true;int n=0,l=x,r=x,t=y,bot=y;
            while(queue.Count>0){var p=queue.Dequeue();n++;l=Math.Min(l,p.X);r=Math.Max(r,p.X);t=Math.Min(t,p.Y);bot=Math.Max(bot,p.Y);
                int[] xx={p.X-1,p.X+1,p.X,p.X},yy={p.Y,p.Y,p.Y-1,p.Y+1};
                for(int i=0;i<4;i++){int nx=xx[i],ny=yy[i];if(nx<=face.X||nx>=face.Right||ny<y0||ny>=y1||seen[nx,ny]||!dark(nx,ny))continue;seen[nx,ny]=true;queue.Enqueue(new Point(nx,ny));}
            }
            double cx=((l+r)*.5-face.X)/face.Width,cy=((t+bot)*.5-face.Y)/face.Height;
            if(r-l>face.Width*.28||cy<.34||cy>.67)continue;
            if(cx>.16&&cx<.43&&n>bestLeft){bestLeft=n;left=(t+bot)*.5;}
            if(cx>.57&&cx<.84&&n>bestRight){bestRight=n;right=(t+bot)*.5;}
        }
        if(bestLeft>0&&bestRight>0)return (left+right)*.5;
        if(bestLeft>0)return left;if(bestRight>0)return right;
        return face.Y+face.Height*.47;
    }
    static double Map(double dest,double[] d,double[] s)
    {
        for(int i=0;i<d.Length-1;i++)if(dest<=d[i+1])return s[i]+(dest-d[i])/(d[i+1]-d[i])*(s[i+1]-s[i]);return s[s.Length-1];
    }
    static int Distance(Color a,Color b){int r=a.R-b.R,g=a.G-b.G,blue=a.B-b.B;return r*r+g*g+blue*blue;}
    static Color Nearest(Color c,Color[] palette)
    {int i=0,best=int.MaxValue;for(int j=0;j<palette.Length;j++){int d=Distance(c,palette[j]);if(d<best){best=d;i=j;}}return palette[i];}
    static Bitmap CommonFace(Bitmap source)
    {
        var bounds=Bounds(source);var result=new Bitmap(64,64,PixelFormat.Format32bppArgb);
        for(int y=0;y<26;y++)for(int x=0;x<42;x++){
            int sx=bounds.X+Math.Min(bounds.Width-1,(int)((x+.5)*bounds.Width/42)),sy=bounds.Y+Math.Min(bounds.Height-1,(int)((y+.5)*bounds.Height/26));
            var c=source.GetPixel(sx,sy);if(c.A<240)continue;result.SetPixel(11+x,28+y,Nearest(c,skin));
        }
        return result;
    }
    static void Quantize(Bitmap b,int count)
    {
        var colors=new Dictionary<int,int>();for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);if(c.A==0)continue;int v=c.ToArgb();colors[v]=colors.ContainsKey(v)?colors[v]+1:1;}
        if(colors.Count<=count)return;
        var points=colors.Select(p=>new {C=Color.FromArgb(p.Key),Weight=p.Value}).ToArray();var palette=new Color[count];
        palette[0]=points.OrderByDescending(p=>p.Weight).First().C;
        for(int i=1;i<count;i++){double best=-1;Color chosen=palette[0];foreach(var p in points){int d=int.MaxValue;for(int j=0;j<i;j++)d=Math.Min(d,Distance(p.C,palette[j]));double score=d*Math.Sqrt(p.Weight);if(score>best){best=score;chosen=p.C;}}palette[i]=chosen;}
        for(int pass=0;pass<10;pass++){
            var sums=new double[count,4];foreach(var p in points){int index=0,d=int.MaxValue;for(int j=0;j<count;j++){int v=Distance(p.C,palette[j]);if(v<d){d=v;index=j;}}sums[index,0]+=p.C.R*p.Weight;sums[index,1]+=p.C.G*p.Weight;sums[index,2]+=p.C.B*p.Weight;sums[index,3]+=p.Weight;}
            for(int j=0;j<count;j++)if(sums[j,3]>0)palette[j]=Color.FromArgb(255,(int)Math.Round(sums[j,0]/sums[j,3]),(int)Math.Round(sums[j,1]/sums[j,3]),(int)Math.Round(sums[j,2]/sums[j,3]));
        }
        for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);if(c.A>0)b.SetPixel(x,y,Nearest(c,palette));}
    }
    static void Normalize(Bitmap source,Bitmap common,int index,out Bitmap faceLayer,out Bitmap styleLayer,out Bitmap sprite)
    {
        Rectangle head=Bounds(source),face=FaceBounds(source,index==3);double eyes=EyeRow(source,face);
        double bottom=Math.Min(62,53+Math.Max(1,head.Bottom-face.Bottom)*26.0/face.Height);
        double[] dx={2,11,52,61},sx={head.X,face.X,face.Right-1,head.Right-1};
        double[] dy={2,28,40,53,bottom},sy={head.Y,face.Y,eyes,face.Bottom-1,Math.Max(head.Bottom-1,face.Bottom)};
        var normalized=new Bitmap(64,64,PixelFormat.Format32bppArgb);
        for(int y=2;y<=Math.Floor(bottom);y++)for(int x=2;x<=61;x++){
            int px=Math.Max(0,Math.Min(source.Width-1,(int)Math.Round(Map(x,dx,sx)))),py=Math.Max(0,Math.Min(source.Height-1,(int)Math.Round(Map(y,dy,sy))));
            var c=source.GetPixel(px,py);if(c.A>=220)normalized.SetPixel(x,y,Color.FromArgb(255,c.R,c.G,c.B));
        }
        faceLayer=new Bitmap(64,64,PixelFormat.Format32bppArgb);styleLayer=new Bitmap(64,64,PixelFormat.Format32bppArgb);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            var baseC=common.GetPixel(x,y);if(baseC.A>0){var c=baseC;if(index==3){int shade=Array.FindIndex(skin,s=>s.ToArgb()==baseC.ToArgb());c=dead[Math.Max(0,shade)];}faceLayer.SetPixel(x,y,c);}
            var n=normalized.GetPixel(x,y);if(n.A==0)continue;
            bool dark=n.R<110&&n.G<90&&n.B<120;
            bool scar=index==1&&x>=16&&x<=25&&y>=32&&y<=47&&n.R>110&&n.R>n.G+40&&n.G<125&&n.B<145;
            bool violet=index==3&&x>=16&&x<=48&&y>=36&&y<=45&&n.B>n.G+35&&n.R>60;
            bool expression=baseC.A>0&&((dark&&x>=16&&x<=48&&y>=37&&y<=50)||scar||violet);
            if(expression){
                if(index==3&&dark&&y<38&&Math.Abs(x-32)<4)continue;
                faceLayer.SetPixel(x,y,dark?skin[3]:Nearest(n,scar?scarColors:violetColors));continue;
            }
            // Skin and its old cheek/chin outline are replaced by ONE common template.
            bool cheekRegion=x>=11&&x<=52&&y>=28;
            if(cheekRegion&&Skin(n,index==3))continue;
            if(cheekRegion&&dark&&y>=36)continue;
            styleLayer.SetPixel(x,y,n);
        }
        Quantize(styleLayer,14);
        sprite=new Bitmap(64,64,PixelFormat.Format32bppArgb);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){
            var f=faceLayer.GetPixel(x,y);var h=styleLayer.GetPixel(x,y);sprite.SetPixel(x,y,h.A>0?h:f);
        }
        normalized.Dispose();Console.WriteLine(index+" face bounds "+face+", eye row "+eyes.ToString("F1")+" → common x11..52 y28..53, eye y40");
    }
    static void DrawPixels(Graphics g,Bitmap b,int x,int y,int scale)
    {using(var brush=new SolidBrush(Color.White))for(int yy=0;yy<b.Height;yy++)for(int xx=0;xx<b.Width;xx++){var c=b.GetPixel(xx,yy);if(c.A==0)continue;brush.Color=c;g.FillRectangle(brush,x+xx*scale,y+yy*scale,scale,scale);}}
    public static void Build(string dir,string framePath,string tilePath)
    {
        string[] names={"Hero","Swordsman","Paladin","DeathKnight"},labels={"히어로 · 광역","검객 · 단일","팔라딘 · 버프","데스나이트 · 디버프"};
        var sprites=new Bitmap[4];var faces=new Bitmap[4];var styles=new Bitmap[4];
        using(var sourceFace=new Bitmap(Path.Combine(dir,"source/CommonFace.png")))using(var common=CommonFace(sourceFace))
        using(var atlas=new Bitmap(128,128,PixelFormat.Format32bppArgb))
        using(var faceAtlas=new Bitmap(128,128,PixelFormat.Format32bppArgb))using(var styleAtlas=new Bitmap(128,128,PixelFormat.Format32bppArgb))
        {
            common.Save(Path.Combine(dir,"CommonFace64.png"),ImageFormat.Png);
            for(int i=0;i<4;i++)using(var source=new Bitmap(Path.Combine(dir,"source/"+names[i]+".png"))){
                Normalize(source,common,i,out faces[i],out styles[i],out sprites[i]);
                for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                    if(faces[i].GetPixel(x,y).A!=common.GetPixel(x,y).A)throw new Exception("Face silhouette changed");
                    var c=sprites[i].GetPixel(x,y);if(c.A!=0&&c.A!=255)throw new Exception("Soft alpha introduced");
                    atlas.SetPixel(i%2*64+x,i/2*64+y,c);faceAtlas.SetPixel(i%2*64+x,i/2*64+y,faces[i].GetPixel(x,y));styleAtlas.SetPixel(i%2*64+x,i/2*64+y,styles[i].GetPixel(x,y));
                }
                sprites[i].Save(Path.Combine(dir,names[i]+"64.png"),ImageFormat.Png);
                var colors=new HashSet<int>();for(int y=0;y<64;y++)for(int x=0;x<64;x++){var c=sprites[i].GetPixel(x,y);if(c.A>0)colors.Add(c.ToArgb());}
                if(colors.Count>20)throw new Exception("Palette exceeds 20 colors");Console.WriteLine(names[i]+": "+colors.Count+" colors, binary alpha, 64x64.");
            }
            atlas.Save(Path.Combine(dir,"Tower_Sword_Pixel64_Concept_Atlas_v1.png"),ImageFormat.Png);
            faceAtlas.Save(Path.Combine(dir,"Faces64_Atlas.png"),ImageFormat.Png);styleAtlas.Save(Path.Combine(dir,"HeadStyles64_Atlas.png"),ImageFormat.Png);
            using(var preview=new Bitmap(1152,580,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var small=new Font("Malgun Gothic",12))using(var fmt=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,43,47,58));fmt.Alignment=StringAlignment.Center;
                for(int i=0;i<4;i++){
                    int x=i*288;g.DrawString(labels[i],font,Brushes.Ivory,new RectangleF(x,6,288,32),fmt);
                    DrawPixels(g,sprites[i],x+16,45,4);DrawPixels(g,sprites[i],x+112,348,1);
                    g.DrawString("64×64 원본",small,Brushes.LightSkyBlue,new RectangleF(x,423,288,26),fmt);
                    DrawPixels(g,faces[i],x+80,455,2);
                }
                g.DrawString("공통 얼굴형 · 같은 얼굴 크기/눈 높이 · 표정과 머리 장식으로 구분",font,Brushes.LightSkyBlue,new RectangleF(0,307,1152,30),fmt);
                preview.Save(Path.Combine(dir,"preview.png"),ImageFormat.Png);
            }
            using(var frame=new Bitmap(framePath))using(var tile=new Bitmap(tilePath))
            using(var preview=new Bitmap(768,120,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var small=new Font("Malgun Gothic",12))using(var fmt=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,43,47,58));fmt.Alignment=StringAlignment.Center;
                for(int i=0;i<4;i++){
                    int x=i*192+64;g.DrawImage(tile,new Rectangle(x,3,64,64),0,0,tile.Width,tile.Height,GraphicsUnit.Pixel);
                    g.DrawImage(frame,new Rectangle(x,3,64,64),0,0,256,256,GraphicsUnit.Pixel);DrawPixels(g,sprites[i],x,3,1);
                    g.DrawString(labels[i],small,Brushes.Ivory,new RectangleF(i*192,78,192,28),fmt);
                }
                preview.Save(Path.Combine(dir,"tile-preview.png"),ImageFormat.Png);
            }
        }
        foreach(var b in sprites)b.Dispose();foreach(var b in faces)b.Dispose();foreach(var b in styles)b.Dispose();
        Console.WriteLine("Four identical face masks verified; 64x64 portraits and a 128x128 atlas saved. No runtime hookup or Unity execution.");
    }
}
'@
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
[SwordPixel64Pack]::Build($PSScriptRoot,(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png'),(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'))
