$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class SwordKnightLayers
{
    static long Cross(Point a,Point b,Point c){return (long)(b.X-a.X)*(c.Y-a.Y)-(long)(b.Y-a.Y)*(c.X-a.X);}
    static Point[] Hull(List<Point> points)
    {
        points.Sort((a,b)=>a.X==b.X?a.Y.CompareTo(b.Y):a.X.CompareTo(b.X));
        var hull=new List<Point>();
        foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        int lower=hull.Count;
        for(int i=points.Count-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        hull.RemoveAt(hull.Count-1);return hull.ToArray();
    }
    public static void Prepare(string sourcePath,string outputDir)
    {
        using(var source=new Bitmap(sourcePath))
        using(var face=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var helmet=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        using(var faceMask=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        {
            if(source.Width!=256||source.Height!=256)throw new Exception("Expected fitted 256-square source");
            bool[] skin=new bool[256*256],visited=new bool[256*256];
            for(int y=90;y<256;y++)for(int x=0;x<256;x++)
            {
                var p=source.GetPixel(x,y);
                skin[y*256+x]=p.A>200&&p.R>200&&p.G>135&&p.B>100&&p.G>p.R*.66&&p.B>p.G*.78;
            }
            List<int> largest=new List<int>();
            for(int start=0;start<skin.Length;start++)
            {
                if(!skin[start]||visited[start])continue;
                var component=new List<int>();var queue=new Queue<int>();queue.Enqueue(start);visited[start]=true;
                while(queue.Count>0)
                {
                    int p=queue.Dequeue();component.Add(p);int x=p%256,y=p/256;
                    foreach(int neighbor in new[]{x>0?p-1:-1,x<255?p+1:-1,y>0?p-256:-1,y<255?p+256:-1})
                    {
                        if(neighbor<0||visited[neighbor]||!skin[neighbor])continue;
                        visited[neighbor]=true;queue.Enqueue(neighbor);
                    }
                }
                if(component.Count>largest.Count)largest=component;
            }
            if(largest.Count<4000)throw new Exception("Cannot identify visible face region");
            int[] first=new int[256],last=new int[256];for(int y=0;y<256;y++){first[y]=256;last[y]=-1;}
            foreach(int p in largest){int x=p%256,y=p/256;first[y]=Math.Min(first[y],x);last[y]=Math.Max(last[y],x);}
            var points=new List<Point>();foreach(int p in largest)points.Add(new Point(p%256,p/256));
            using(var g=Graphics.FromImage(faceMask)){g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.None;g.FillPolygon(Brushes.White,Hull(points));}
            int faceCount=0;
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                var pixel=source.GetPixel(x,y);
                // A convex visible-face window also keeps skin edge shadows with the face.
                bool onFace=faceMask.GetPixel(x,y).A>0;
                face.SetPixel(x,y,onFace?pixel:Color.Transparent);
                helmet.SetPixel(x,y,onFace?Color.Transparent:pixel);
                faceMask.SetPixel(x,y,onFace?Color.White:Color.Transparent);
                if(onFace)faceCount++;
                var restored=onFace?face.GetPixel(x,y):helmet.GetPixel(x,y);
                if(restored.ToArgb()!=pixel.ToArgb())throw new Exception("Original pixel changed during layer extraction");
            }
            face.Save(System.IO.Path.Combine(outputDir,"Face_Common.png"),ImageFormat.Png);
            helmet.Save(System.IO.Path.Combine(outputDir,"Helmet_Bronze.png"),ImageFormat.Png);
            faceMask.Save(System.IO.Path.Combine(outputDir,"face-mask.png"),ImageFormat.Png);
            Console.WriteLine("PASS: common face + bronze helmet reconstruct all original pixels exactly; face window="+faceCount+" pixels.");
        }
    }
}
'@
[SwordKnightLayers]::Prepare((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Splash_Front_v3.png'),$PSScriptRoot)
