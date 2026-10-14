param([switch]$RequireAll)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class FrontPortraitPack {
    // Remove disconnected fragments spilling from adjacent AI sheet cells.
    // Keep the original RGBA of the main subject; a two-person portrait keeps two components.
    static Bitmap Isolate(Bitmap source,Rectangle rect,int keep,bool lowerRow,bool trimForeignLeft) {
        int w=rect.Width,h=rect.Height;var colors=new Color[w*h];var labels=new int[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)colors[y*w+x]=source.GetPixel(rect.X+x,rect.Y+y);
        // Two native sheets have a neighboring portrait's clothing attached at the
        // far left of the next cell. Remove that narrow edge fragment before packing.
        if(trimForeignLeft)for(int y=0;y<h;y++)for(int x=0;x<(int)Math.Ceiling(w*.06);x++)colors[y*w+x]=Color.Transparent;
        // Flat full-width seam artifacts can attach to the next portrait's hair.
        // Remove only unusually wide scanlines at the generated lower-row cell boundary.
        if(lowerRow)for(int y=0;y<Math.Min(12,h);y++) {
            int count=0;for(int x=0;x<w;x++)if(colors[y*w+x].A>=8)count++;
            if(count>w*.60)for(int x=0;x<w;x++)colors[y*w+x]=Color.Transparent;
        }
        var counts=new List<int>();counts.Add(0);var queue=new Queue<int>();int label=0;
        for(int i=0;i<colors.Length;i++)if(colors[i].A>=8&&labels[i]==0) {
            label++;counts.Add(0);labels[i]=label;queue.Enqueue(i);
            while(queue.Count>0){int p=queue.Dequeue();counts[label]++;int x=p%w,y=p/w;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++) {
                    int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;int n=yy*w+xx;
                    if(labels[n]==0&&colors[n].A>=8){labels[n]=label;queue.Enqueue(n);}
                }
            }
        }
        if(label==0)throw new Exception("Empty portrait quadrant.");
        var order=new List<int>();for(int i=1;i<=label;i++)order.Add(i);order.Sort((a,b)=>counts[b].CompareTo(counts[a]));
        var selected=new HashSet<int>();for(int i=0;i<Math.Min(keep,order.Count);i++)if(i==0||counts[order[i]]>counts[order[0]]*.05)selected.Add(order[i]);
        var keepPixel=new bool[w*h];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(selected.Contains(labels[y*w+x]))
            for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int xx=x+dx,yy=y+dy;if(xx>=0&&xx<w&&yy>=0&&yy<h)keepPixel[yy*w+xx]=true;}
        var result=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(keepPixel[y*w+x]&&colors[y*w+x].A>0)result.SetPixel(x,y,colors[y*w+x]);
        return result;
    }
    static Rectangle Bounds(Bitmap b) {
        int l=b.Width,t=b.Height,r=-1,bot=-1;
        for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++)if(b.GetPixel(x,y).A>=8){l=Math.Min(l,x);t=Math.Min(t,y);r=Math.Max(r,x);bot=Math.Max(bot,y);}
        if(r<l)throw new Exception("No visible portrait.");return Rectangle.FromLTRB(l,t,r+1,bot+1);
    }
    public static void Pack(string input,string output,int count,int duoIndex) {
        using(var source=new Bitmap(input))using(var atlas=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
            for(int i=0;i<count;i++) {
                int cellW=source.Width/2,cellH=source.Height/2;
                // Some generated sheets contain an unrelated colored scanline at the bottom.
                // The bust already has a flat crop above it; exclude only the last 2% of its cell.
                int cropHeight=cellH-Math.Max(2,cellH/50);
                var rect=new Rectangle(i%2*cellW,i/2*cellH,cellW,cropHeight);
                bool trimForeignLeft=i==1&&(Path.GetFileNameWithoutExtension(input)=="06_Hammer"||Path.GetFileNameWithoutExtension(input)=="18_Synergy_D");
                using(var clean=Isolate(source,rect,i==duoIndex?2:1,i>=2,trimForeignLeft))using(var sprite=new Bitmap(256,256,PixelFormat.Format32bppArgb)) {
                    var bounds=Bounds(clean);double scale=Math.Min(216.0/bounds.Width,188.0/bounds.Height);
                    int dw=(int)Math.Round(bounds.Width*scale),dh=(int)Math.Round(bounds.Height*scale);
                    var target=new Rectangle((256-dw)/2,244-dh,dw,dh);
                    using(var g=Graphics.FromImage(sprite))using(var attrs=new ImageAttributes()) {
                        g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                        attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(clean,target,bounds.X,bounds.Y,bounds.Width,bounds.Height,GraphicsUnit.Pixel,attrs);
                    }
                    var visible=Bounds(sprite);if(visible.X<18||visible.Right>238||visible.Y<50||visible.Bottom>248)throw new Exception("Portrait escaped common canvas.");
                    int pixels=0;for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(sprite.GetPixel(x,y).A>=8)pixels++;
                    if(pixels<2500)throw new Exception("Portrait too empty.");
                    using(var g=Graphics.FromImage(atlas)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(sprite,i%2*256,i/2*256);}
                    Console.WriteLine(Path.GetFileName(output)+" cell "+i+": "+visible+"; visible pixels="+pixels);
                }
            }
            atlas.Save(output,ImageFormat.Png);
        }
    }
    public static void Synergy(string[] sources,string output) {
        using(var atlas=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(atlas)) {
            g.CompositingMode=CompositingMode.SourceCopy;
            for(int group=0;group<4;group++)using(var source=new Bitmap(sources[group]))
                for(int cell=0;cell<(group==3?2:4);cell++) {
                    int i=group*4+cell;
                    using(var crop=source.Clone(new Rectangle(cell%2*256,cell/2*256,256,256),PixelFormat.Format32bppArgb))g.DrawImageUnscaled(crop,i%4*256,i/4*256);
                }
            atlas.Save(output,ImageFormat.Png);
        }
    }
    public static void Preview(string[] atlases,string[] groupNames,string[][] labels,string output,int columns) {
        int count=0;foreach(var names in labels)count+=names.Length;
        int rows=columns==4?atlases.Length:(count+columns-1)/columns;
        int tileW=256,tileH=284;int width=columns*tileW+120;
        using(var image=new Bitmap(width,rows*tileH+60,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(image))
        using(var font=new Font("Malgun Gothic",12,FontStyle.Bold))using(var fmt=new StringFormat()) {
            fmt.Alignment=StringAlignment.Center;g.Clear(Color.FromArgb(255,237,238,242));g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
            int k=0;
            for(int group=0;group<atlases.Length;group++)using(var atlas=new Bitmap(atlases[group])) {
                if(columns==4)g.DrawString(groupNames[group],font,Brushes.DarkSlateGray,new RectangleF(0,30+group*tileH+100,120,50),fmt);
                for(int i=0;i<labels[group].Length;i++) {
                    int px=120+(k%columns)*tileW,py=30+(k/columns)*tileH;
                    g.DrawImage(atlas,new Rectangle(px,py,256,256),i%2*256,i/2*256,256,256,GraphicsUnit.Pixel);
                    g.DrawString(labels[group][i],font,Brushes.DarkSlateGray,new RectangleF(px,py+251,256,30),fmt);k++;
                }
            }
            image.Save(output,ImageFormat.Png);
        }
    }
}
'@
$roster=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'roster.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$packedFolder=Join-Path $PSScriptRoot 'packed'
New-Item -ItemType Directory -Force -Path $packedFolder | Out-Null
$normalAtlases=[Collections.Generic.List[string]]::new()
$normalGroups=[Collections.Generic.List[string]]::new()
$normalLabels=[Collections.Generic.List[string[]]]::new()
$synergyAtlases=[Collections.Generic.List[string]]::new()
$synergyGroups=[Collections.Generic.List[string]]::new()
$synergyLabels=[Collections.Generic.List[string[]]]::new()
$missing=[Collections.Generic.List[string]]::new()
foreach($group in $roster){
    $inputPath=Join-Path $PSScriptRoot ('source/'+$group.key+'.png')
    if(-not(Test-Path -LiteralPath $inputPath)){$missing.Add($group.key); continue}
    $outputPath=Join-Path $packedFolder ($group.key+'.png')
    $duoIndex=if($group.key -eq '18_Synergy_D'){0}else{-1}
    [FrontPortraitPack]::Pack($inputPath,$outputPath,$group.portraits.Count,$duoIndex)
    $labelArray=[string[]]@($group.portraits | ForEach-Object {$_[1]})
    if($group.kind -eq 'normal'){$normalAtlases.Add($outputPath);$normalGroups.Add($group.label);$normalLabels.Add($labelArray)}
    else{$synergyAtlases.Add($outputPath);$synergyGroups.Add($group.label);$synergyLabels.Add($labelArray)}
}
if($normalAtlases.Count -gt 0){[FrontPortraitPack]::Preview($normalAtlases.ToArray(),$normalGroups.ToArray(),$normalLabels.ToArray(),(Join-Path $PSScriptRoot 'normal-preview.png'),4)}
if($synergyAtlases.Count -gt 0){[FrontPortraitPack]::Preview($synergyAtlases.ToArray(),$synergyGroups.ToArray(),$synergyLabels.ToArray(),(Join-Path $PSScriptRoot 'synergy-preview.png'),4)}
if($synergyAtlases.Count -eq 4){[FrontPortraitPack]::Synergy($synergyAtlases.ToArray(),(Join-Path $packedFolder 'Synergy_All14.png'))}
if($RequireAll -and $missing.Count -gt 0){throw ('Missing production groups: '+($missing -join ', '))}
Write-Output ('Packed '+$normalAtlases.Count+' regular groups and '+$synergyAtlases.Count+' synergy groups. Missing: '+($missing -join ', '))
