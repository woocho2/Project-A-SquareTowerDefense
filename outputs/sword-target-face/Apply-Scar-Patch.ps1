$ErrorActionPreference='Stop'
$taskRoot=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
public static class ScarPatch
{
    public static void Apply(string originalPath,string editedPath)
    {
        using(var original=new Bitmap(originalPath))
        using(var stream=new MemoryStream(File.ReadAllBytes(editedPath)))
        using(var generated=new Bitmap(stream))
        using(var result=new Bitmap(256,256,PixelFormat.Format32bppArgb))
        {
            int changed=0;var scarArea=new Rectangle(66,130,34,73);
            for(int y=0;y<256;y++)for(int x=0;x<256;x++)
            {
                var old=original.GetPixel(x,y);var edited=generated.GetPixel(x,y);
                float hue=edited.GetHue();
                bool scar=scarArea.Contains(x,y)&&edited.R>=120&&edited.R<220&&edited.G<edited.R*.80f&&edited.B>edited.G*.72f&&edited.B<edited.G*1.10f&&hue>=2&&hue<=24;
                // Keep existing dark eyebrow/eyelid strokes above the healed scar.
                if(old.R<120&&old.G<80)scar=false;
                var pixel=scar?Color.FromArgb(old.A,edited.R,edited.G,edited.B):old;
                result.SetPixel(x,y,pixel);
                if(pixel.ToArgb()!=old.ToArgb())changed++;
            }
            if(changed<10||changed>400)throw new Exception("Unexpected scar patch size: "+changed);
            result.Save(editedPath,ImageFormat.Png);
            Console.WriteLine("Added "+changed+" generated scar pixels; original eyebrows, eyes, mouth, all remaining RGB and tile alpha preserved.");
        }
    }
}
'@
[ScarPatch]::Apply((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v2.png'),(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Bronze_Sword_Target_Front_v3.png'))
