$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class SharedUIGradients
{
    sealed class Entry { public string Name; public Rectangle Rect; public int Group; }
    static List<Entry> Entries(string meta,int imageHeight)
    {
        var entries=new List<Entry>();
        string pattern=@"(?ms)^      name: ([^\r\n]+)\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)";
        foreach(Match m in Regex.Matches(meta,pattern))
        {
            string name=m.Groups[1].Value;var layer=Regex.Match(name,@"^Shared_([ABC])_");if(!layer.Success)continue;
            int x=int.Parse(m.Groups[2].Value),y=int.Parse(m.Groups[3].Value),w=int.Parse(m.Groups[4].Value),h=int.Parse(m.Groups[5].Value);
            entries.Add(new Entry{Name=name,Group=layer.Groups[1].Value[0]-'A',Rect=new Rectangle(x,imageHeight-y-h,w,h)});
        }
        if(entries.Count!=45)throw new Exception("Expected 45 A/B/C sprite entries");return entries;
    }
    static double[] Profile(Bitmap image,int group)
    {
        int cx=(int)Math.Round(image.Width*(group+.5)/3.0),first=-1,last=-1;
        for(int y=0;y<image.Height;y++)if(image.GetPixel(cx,y).A>=250){if(first<0)first=y;last=y;}
        if(first<0||last-first<32)throw new Exception("Missing gradient profile");
        first+=3;last-=3;int n=last-first+1;var values=new double[n];
        for(int i=0;i<n;i++)
        {
            double sum=0;int count=0;
            for(int dy=-8;dy<=8;dy++)for(int dx=-10;dx<=10;dx++)
            {
                int y=Math.Max(first,Math.Min(last,first+i+dy));var p=image.GetPixel(cx+dx,y);
                if(p.A<240)continue;sum=(sum+(p.R+p.G+p.B)/3.0);count++;
            }
            if(count==0)throw new Exception("Transparent gradient sample");values[i]=sum/count;
        }
        double span=values[0]-values[n-1];if(span<2)throw new Exception("Gradient contrast is missing");
        var progress=new double[n];double previous=0;
        for(int i=0;i<n;i++){progress[i]=Math.Max(previous,Math.Max(0,Math.Min(1,(values[0]-values[i])/span)));previous=progress[i];}
        progress[0]=0;progress[n-1]=1;return progress;
    }
    static double Sample(double[] profile,double t)
    {
        double p=Math.Max(0,Math.Min(1,t))*(profile.Length-1);int lo=(int)Math.Floor(p),hi=Math.Min(lo+1,profile.Length-1);
        return profile[lo]+(profile[hi]-profile[lo])*(p-lo);
    }
    public static void Apply(string beforePath,string metaPath,string profilePath,string outputPath,string previewPath,string reportPath)
    {
        using(var source=new Bitmap(beforePath))using(var reference=new Bitmap(profilePath))
        using(var result=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb))
        {
            var bounds=new Rectangle(0,0,source.Width,source.Height);
            var sourceData=source.LockBits(bounds,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            int stride=sourceData.Stride;var original=new byte[stride*source.Height];Marshal.Copy(sourceData.Scan0,original,0,original.Length);source.UnlockBits(sourceData);
            var bytes=(byte[])original.Clone();var entries=Entries(File.ReadAllText(metaPath),source.Height);
            int[] minimum={230,200,245};var profiles=new[]{Profile(reference,0),Profile(reference,1),Profile(reference,2)};
            var touched=new bool[source.Width*source.Height];var report=new List<string>();
            foreach(var entry in entries)
            {
                var rect=entry.Rect;int top=rect.Height,bottom=-1;
                for(int y=0;y<rect.Height;y++)for(int x=0;x<rect.Width;x++)
                {int p=(rect.Y+y)*stride+(rect.X+x)*4;if(original[p+3]<250)continue;top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                if(bottom<=top)throw new Exception("Empty sprite: "+entry.Name);
                int minSeen=255,maxSeen=0;
                for(int y=0;y<rect.Height;y++)
                {
                    double t=Math.Max(0,Math.Min(1,(y-top)/(double)(bottom-top)));
                    byte gray=(byte)Math.Round(255-(255-minimum[entry.Group])*Sample(profiles[entry.Group],t));
                    for(int x=0;x<rect.Width;x++)
                    {
                        int px=rect.X+x,py=rect.Y+y,p=py*stride+px*4;if(original[p+3]==0)continue;
                        bytes[p]=bytes[p+1]=bytes[p+2]=gray;touched[py*source.Width+px]=true;
                        minSeen=Math.Min(minSeen,gray);maxSeen=Math.Max(maxSeen,gray);
                    }
                }
                if(minSeen!=minimum[entry.Group]||maxSeen!=255)throw new Exception("Wrong endpoint range: "+entry.Name);
                report.Add(entry.Name+": RGB "+minSeen+".."+maxSeen+", top=255, bottom="+minimum[entry.Group]);
            }
            for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)
            {
                int p=y*stride+x*4;
                if(bytes[p+3]!=original[p+3])throw new Exception("Alpha changed");
                if(!touched[y*source.Width+x])for(int c=0;c<4;c++)if(bytes[p+c]!=original[p+c])throw new Exception("Frame or padding pixel changed");
            }
            var destData=result.LockBits(bounds,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            if(destData.Stride!=stride)throw new Exception("Unexpected output stride");Marshal.Copy(bytes,0,destData.Scan0,bytes.Length);result.UnlockBits(destData);
            result.Save(outputPath,ImageFormat.Png);File.WriteAllLines(reportPath,report);
            using(var preview=new Bitmap(864,644,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
            using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var format=new StringFormat())
            {
                g.Clear(Color.FromArgb(255,30,34,44));g.InterpolationMode=InterpolationMode.HighQualityBicubic;format.Alignment=StringAlignment.Center;
                for(int i=0;i<3;i++)
                {
                    var e=entries.Single(v=>v.Name=="Shared_"+(char)('A'+i)+"_Btn_512");
                    string title=((char)('A'+i))+" · 255 → "+minimum[i];g.DrawString(title,font,Brushes.Ivory,new RectangleF(i*288,5,288,30),format);
                    var dest=new Rectangle(i*288+20,44,248,248);
                    g.DrawImage(result,dest,e.Rect.X,e.Rect.Y,e.Rect.Width,e.Rect.Height,GraphicsUnit.Pixel);
                    var matrix=new ColorMatrix();matrix.Matrix00=95f/255;matrix.Matrix11=169f/255;matrix.Matrix22=221f/255;
                    using(var attributes=new ImageAttributes()){attributes.SetColorMatrix(matrix);g.DrawImage(result,new Rectangle(i*288+20,365,248,248),e.Rect.X,e.Rect.Y,e.Rect.Width,e.Rect.Height,GraphicsUnit.Pixel,attributes);}
                }
                g.DrawString("같은 하늘색을 곱했을 때의 밝기 차이",font,Brushes.LightSkyBlue,new RectangleF(0,323,864,32),format);
                preview.Save(previewPath,ImageFormat.Png);
            }
            Console.WriteLine("Updated 45 sprites: A 230..255, B 200..255, C 245..255; grayscale, original alpha/frame/padding pixels preserved. Asset pixel checks only; no Unity execution.");
        }
    }
}
'@
[SharedUIGradients]::Apply((Join-Path $PSScriptRoot 'before/Shared_UI_Atlas.png'),(Join-Path $PSScriptRoot 'before/Shared_UI_Atlas.png.meta'),(Join-Path $PSScriptRoot 'source-gradient.png'),(Join-Path $PSScriptRoot 'Shared_UI_Atlas.png'),(Join-Path $PSScriptRoot 'preview.png'),(Join-Path $PSScriptRoot 'pixel-ranges.txt'))
