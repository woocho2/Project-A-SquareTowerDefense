$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
public static class SwordFaceOffsetPreview
{
    public static void Build(string characterPath,string framePath,string tilePath,string output)
    {
        using(var characters=new Bitmap(characterPath))using(var frames=new Bitmap(framePath))using(var tile=new Bitmap(tilePath))
        using(var preview=new Bitmap(1024,616,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview))
        using(var font=new Font("Malgun Gothic",16,FontStyle.Bold))using(var format=new StringFormat())
        {
            g.Clear(Color.FromArgb(255,29,33,44));format.Alignment=StringAlignment.Center;
            g.DrawString("기존",font,Brushes.LightGray,new RectangleF(0,3,1024,30),format);
            g.DrawString("8px 위로 이동",font,Brushes.LightSkyBlue,new RectangleF(0,306,1024,30),format);
            string[] names={"히어로","검객","팔라딘","데스나이트"};
            for(int row=0;row<2;row++)for(int i=0;i<4;i++)
            {
                int top=38+row*302;
                g.DrawImageUnscaled(tile,i*256,top);
                g.DrawImage(frames,new Rectangle(i*256,top,256,256),0,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(characters,new Rectangle(i*256,top-row*8,256,256),(i%2)*256,(i/2)*256,256,256,GraphicsUnit.Pixel);
                if(row==1)g.DrawString(names[i],font,Brushes.Ivory,new RectangleF(i*256,592,256,25),format);
            }
            preview.Save(output,ImageFormat.Png);
        }
    }
}
'@
[SwordFaceOffsetPreview]::Build((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Sword_Characters_Atlas_v2.png'),(Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png'),(Join-Path $taskRoot 'outputs/bronze-sword-splash-front/spawn-tile-reference.png'),(Join-Path $PSScriptRoot 'preview.png'))
