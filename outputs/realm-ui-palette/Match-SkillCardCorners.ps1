$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskAtlas=Join-Path $taskRepo 'Assets/4. Asset/5. UI/Shared_UI_Atlas.png'
$taskMeta=[IO.File]::ReadAllText($taskAtlas+'.meta')
$taskMetaHash=(Get-FileHash -LiteralPath ($taskAtlas+'.meta')).Hash
$taskUtf8=New-Object Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
public static class SkillCardCorners {
    static Rectangle Rect(string meta,string name,int atlasHeight){
        var m=Regex.Match(meta,@"(?ms)^      name: "+name+@"\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)");
        if(!m.Success)throw new Exception("Missing sprite: "+name);
        int height=int.Parse(m.Groups[4].Value);
        return new Rectangle(int.Parse(m.Groups[1].Value),atlasHeight-int.Parse(m.Groups[2].Value)-height,int.Parse(m.Groups[3].Value),height);
    }
    static byte[] Pixels(Bitmap bitmap){
        var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        var bytes=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);bitmap.UnlockBits(data);return bytes;
    }
    static void DrawTint(Graphics g,Bitmap atlas,Rectangle source,Rectangle display,Color color){
        using(var attributes=new ImageAttributes()){
            var matrix=new ColorMatrix();matrix.Matrix00=color.R/255f;matrix.Matrix11=color.G/255f;matrix.Matrix22=color.B/255f;
            attributes.SetColorMatrix(matrix);g.DrawImage(atlas,display,source.X,source.Y,source.Width,source.Height,GraphicsUnit.Pixel,attributes);
        }
    }
    static Bitmap Render(Bitmap atlas,Rectangle source,int width){
        var image=new Bitmap(width,200,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(image)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.DrawImage(atlas,new Rectangle(0,0,width,200),source,GraphicsUnit.Pixel);}
        return image;
    }
    public static string Apply(string path,string meta,string preview){
        using(var stream=new MemoryStream(File.ReadAllBytes(path)))using(var source=new Bitmap(stream))
        using(var output=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb)){
            bool[] changedRegion=new bool[source.Width*source.Height];
            string[] layers={"A","B","C","Frame"};
            using(var g=Graphics.FromImage(output)){
                g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                foreach(string layer in layers){
                    var dock=Rect(meta,"Shared_"+layer+"_MegaDock_768x128",source.Height);
                    var card=Rect(meta,"Shared_"+layer+"_SkillCard_384x256",source.Height);
                    if(dock.Size!=new Size(768,128)||card.Size!=new Size(384,256))throw new Exception("Unexpected source dimensions");
                    using(var canonical=new Bitmap(192,128,PixelFormat.Format32bppArgb)){
                        // Preserve both end caps and shorten only the straight middle.
                        for(int y=0;y<128;y++)for(int x=0;x<192;x++){
                            int sampleX=x<64?x:x>=128?dock.Width-(192-x):64+(x-64)*(dock.Width-128)/64;
                            canonical.SetPixel(x,y,source.GetPixel(dock.X+sampleX,dock.Y+y));
                        }
                        // 3:2 has twice the source height, so corners/lines/padding also double.
                        g.DrawImage(canonical,card,new Rectangle(0,0,192,128),GraphicsUnit.Pixel);
                    }
                    for(int y=card.Top;y<card.Bottom;y++)for(int x=card.Left;x<card.Right;x++)changedRegion[y*source.Width+x]=true;
                }
            }
            byte[] before=Pixels(source),after=Pixels(output);int modified=0;
            for(int p=0;p<changedRegion.Length;p++)for(int c=0;c<4;c++)if(before[p*4+c]!=after[p*4+c]){
                if(!changedRegion[p])throw new Exception("Changed another sprite or atlas padding");modified++;
            }
            int maxCornerBoundaryDifference=0;
            foreach(string layer in layers){
                using(var card=Render(output,Rect(meta,"Shared_"+layer+"_SkillCard_384x256",source.Height),300))
                using(var dock=Render(output,Rect(meta,"Shared_"+layer+"_MegaDock_768x128",source.Height),1200)){
                    for(int y=0;y<80;y++){
                        int cardX=-1,dockX=-1;
                        for(int x=0;x<100;x++){if(cardX<0&&card.GetPixel(x,y).A>=128)cardX=x;if(dockX<0&&dock.GetPixel(x,y).A>=128)dockX=x;}
                        if(cardX>=0&&dockX>=0)maxCornerBoundaryDifference=Math.Max(maxCornerBoundaryDifference,Math.Abs(cardX-dockX));
                    }
                }
            }
            if(maxCornerBoundaryDifference>2)throw new Exception("Corners still differ at equal UI height");
            using(var canvas=new Bitmap(1640,610,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
            using(var font=new Font("Segoe UI",20,FontStyle.Bold))using(var label=new Font("Segoe UI",43,FontStyle.Bold)){
                g.Clear(ColorTranslator.FromHtml("#202830"));g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                for(int row=0;row<2;row++){
                    var atlas=row==0?source:output;int y=60+row*290;
                    g.DrawString(row==0?"Before - same UI height (50)":"After - same UI height (50)",font,Brushes.White,20,y-40);
                    DrawTint(g,atlas,Rect(meta,"Shared_B_SkillCard_384x256",source.Height),new Rectangle(20,y,300,200),ColorTranslator.FromHtml("#FFDF7D"));
                    DrawTint(g,atlas,Rect(meta,"Shared_Frame_SkillCard_384x256",source.Height),new Rectangle(20,y,300,200),ColorTranslator.FromHtml("#ECBB41"));
                    DrawTint(g,atlas,Rect(meta,"Shared_B_MegaDock_768x128",source.Height),new Rectangle(360,y,1200,200),ColorTranslator.FromHtml("#FFFCEC"));
                    DrawTint(g,atlas,Rect(meta,"Shared_Frame_MegaDock_768x128",source.Height),new Rectangle(360,y,1200,200),ColorTranslator.FromHtml("#ECBB41"));
                    g.DrawString("Lv:1",label,Brushes.DimGray,43,y+60);g.DrawString("Name",label,Brushes.DimGray,865,y+60);
                }
                canvas.Save(preview,ImageFormat.Png);
            }
            output.Save(path,ImageFormat.Png);
            return "PASS all four 3:2 sprites share the 6:1 corner/line/padding scale at equal UI height. Maximum boundary difference at 4x preview: "+maxCornerBoundaryDifference+" px (<=0.5 UI px).\nPASS remaining 56 sprites and padding unchanged pixel-for-pixel. Changed channels: "+modified;
        }
    }
}
'@
$taskReport=[SkillCardCorners]::Apply($taskAtlas,$taskMeta,(Join-Path $PSScriptRoot 'level-name-corners-preview.png'))
if((Get-FileHash -LiteralPath ($taskAtlas+'.meta')).Hash -ne $taskMetaHash){throw 'Atlas sprite metadata changed'}
$taskReport+="`nPASS atlas dimensions, sprite count, GUID, IDs, rects and slicing settings unchanged. UI layout/assignments not modified.`n"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'level-name-corners-validation.txt'),$taskReport,$taskUtf8)
Write-Output $taskReport
