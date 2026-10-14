$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class IcePortraitRevision {
    public static void Pack(string input,string output) {
        using(var source=new Bitmap(input))using(var pair=new Bitmap(512,256,PixelFormat.Format32bppArgb)) {
            int w=source.Width/2,h=(int)Math.Floor(source.Height*.97);
            for(int cell=0;cell<2;cell++) {
                var colors=new Color[w*h];var labels=new int[w*h];var counts=new List<int>();counts.Add(0);
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)colors[y*w+x]=source.GetPixel(cell*w+x,y);
                int label=0;var queue=new Queue<int>();
                for(int i=0;i<colors.Length;i++)if(colors[i].A>=8&&labels[i]==0) {
                    label++;counts.Add(0);labels[i]=label;queue.Enqueue(i);
                    while(queue.Count>0) {
                        int p=queue.Dequeue(),x=p%w,y=p/w;counts[label]++;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                            int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;
                            int next=yy*w+xx;
                            if(labels[next]==0&&colors[next].A>=8){labels[next]=label;queue.Enqueue(next);}
                        }
                    }
                }
                int largest=0;for(int i=1;i<counts.Count;i++)if(counts[i]>counts[largest])largest=i;
                if(largest==0)throw new Exception("Missing creature portrait.");
                var keep=new bool[w*h];
                int left=w,top=h,right=-1,bottom=-1;
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(labels[y*w+x]==largest) {
                    left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);
                    for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++) {
                        int xx=x+dx,yy=y+dy;if(xx>=0&&xx<w&&yy>=0&&yy<h)keep[yy*w+xx]=true;
                    }
                }
                using(var clean=new Bitmap(w,h,PixelFormat.Format32bppArgb)) {
                    for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(keep[y*w+x])clean.SetPixel(x,y,colors[y*w+x]);
                    var bounds=Rectangle.FromLTRB(left,top,right+1,bottom+1);
                    double scale=Math.Min(216.0/bounds.Width,188.0/bounds.Height);
                    int dw=(int)Math.Round(bounds.Width*scale),dh=(int)Math.Round(bounds.Height*scale);
                    using(var g=Graphics.FromImage(pair))using(var attrs=new ImageAttributes()) {
                        g.CompositingMode=CompositingMode.SourceCopy;
                        g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                        attrs.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(clean,new Rectangle(cell*256+(256-dw)/2,244-dh,dw,dh),bounds.X,bounds.Y,bounds.Width,bounds.Height,GraphicsUnit.Pixel,attrs);
                    }
                    Console.WriteLine("Creature "+cell+": packed "+dw+"x"+dh+", baseline y244.");
                }
            }
            pair.Save(output,ImageFormat.Png);
        }
    }
    public static void Replace(string original,string pairPath,string output) {
        using(var source=new Bitmap(original))using(var atlas=source.Clone(new Rectangle(0,0,512,512),PixelFormat.Format32bppArgb))using(var pair=new Bitmap(pairPath)) {
            for(int y=0;y<256;y++)for(int x=0;x<512;x++)atlas.SetPixel(x,y+256,pair.GetPixel(x,y));
            for(int y=0;y<256;y++)for(int x=0;x<512;x++)if(atlas.GetPixel(x,y).ToArgb()!=source.GetPixel(x,y).ToArgb())throw new Exception("Upper portraits changed.");
            for(int cell=0;cell<2;cell++) {
                int visible=0,transparent=0,bottom=-1;
                for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
                    int a=atlas.GetPixel(cell*256+x,y+256).A;
                    if(a==0)transparent++;if(a>=8){visible++;bottom=Math.Max(bottom,y);}
                    if((x<16||x>=240||y<48||y>=248)&&a!=0)throw new Exception("Nontransparent portrait gutter.");
                }
                if(visible<2500||transparent<15000||bottom<241||bottom>244)throw new Exception("Portrait packing validation failed.");
            }
            atlas.Save(output,ImageFormat.Png);
        }
    }
    public static void UpdatePreview(string input,string pairPath,string output) {
        using(var source=new Bitmap(input))using(var preview=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb))using(var pair=new Bitmap(pairPath))using(var g=Graphics.FromImage(preview)) {
            // Ice is group 11, and its two changed creatures are cells 3 and 4.
            using(var background=new SolidBrush(Color.FromArgb(255,237,238,242)))g.FillRectangle(background,120+2*256,30+10*284,512,250);
            g.DrawImageUnscaled(pair,120+2*256,30+10*284);
            preview.Save(output,ImageFormat.Png);
        }
    }
}
'@
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$target=Join-Path $projectRoot 'Assets/4. Asset/2. Tower/Portraits_SD_Front_v2/Tower_Ice_Front_Atlas_v2.png'
$preview=Join-Path $projectRoot 'outputs/tower-front-portraits-v2/normal-preview.png'
$backup=Join-Path $PSScriptRoot 'atlas-before.png'
if(-not(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup}
$metaHash=(Get-FileHash -LiteralPath ($target+'.meta') -Algorithm SHA256).Hash
$packed=Join-Path $PSScriptRoot 'creatures-packed.png'
$result=Join-Path $PSScriptRoot 'atlas-after.png'
[IcePortraitRevision]::Pack((Join-Path $PSScriptRoot 'native-approved.png'),$packed)
# Preserve the current upper row, including any independent later changes.
[IcePortraitRevision]::Replace($target,$packed,$result)
[IcePortraitRevision]::UpdatePreview($preview,$packed,(Join-Path $PSScriptRoot 'overview-updated.png'))
Copy-Item -LiteralPath $result -Destination $target -Force
Copy-Item -LiteralPath $result -Destination (Join-Path $projectRoot 'outputs/tower-front-portraits-v2/packed/11_Ice.png') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'overview-updated.png') -Destination $preview -Force
if((Get-FileHash -LiteralPath ($target+'.meta') -Algorithm SHA256).Hash -ne $metaHash){throw 'Sprite metadata changed.'}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'verification.txt'),"Approved closed-mouth Glacier and small-eyed Yeti saved. Upper row RGBA pixels identical. Dimensions 512x512, transparent gutters and baseline validated. Sprite metadata SHA256 unchanged: $metaHash. Unity was not launched.",[Text.UTF8Encoding]::new($false))
Write-Output 'Updated Ice atlas and preview; upper two portraits and sprite metadata retained.'
