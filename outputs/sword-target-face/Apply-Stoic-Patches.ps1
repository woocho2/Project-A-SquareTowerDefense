$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
public static class StoicPatch
{
    public static void Apply(string originalPath,string editedPath)
    {
        using(var original=new Bitmap(originalPath))
        using(var stream=new MemoryStream(File.ReadAllBytes(editedPath)))
        using(var generated=new Bitmap(stream))
        using(var result=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        {
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)result.SetPixel(x,y,original.GetPixel(x,y));
            // Composite only the imagegen eye/mouth edits; retain all other approved pixels.
            var areas=new[]{new Rectangle(59,160,48,27),new Rectangle(103,188,51,29)};
            int changed=0;
            foreach(var area in areas)
                for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)
                {
                    int edge=Math.Min(Math.Min(x-area.Left,area.Right-1-x),Math.Min(y-area.Top,area.Bottom-1-y));
                    double mix=Math.Min(1,edge/3.0);
                    var a=original.GetPixel(x,y);var b=generated.GetPixel(x,y);
                    result.SetPixel(x,y,Color.FromArgb(a.A,(int)Math.Round(a.R+(b.R-a.R)*mix),(int)Math.Round(a.G+(b.G-a.G)*mix),(int)Math.Round(a.B+(b.B-a.B)*mix)));
                }
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                var before=original.GetPixel(x,y);var after=result.GetPixel(x,y);
                bool inside=areas[0].Contains(x,y)||areas[1].Contains(x,y);
                if(!inside&&before.ToArgb()!=after.ToArgb())throw new Exception("Unrequested pixels changed");
                if(before.A!=after.A)throw new Exception("Tile silhouette changed");
                if(before.ToArgb()!=after.ToArgb())changed++;
            }
            result.Save(editedPath,ImageFormat.Png);
            Console.WriteLine("Saved straight-eye/mouth expression; "+changed+" pixels changed only inside two expression patches; all other pixels and alpha unchanged.");
        }
    }
}
'@
[StoicPatch]::Apply((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v1.png'),(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v2.png'))
