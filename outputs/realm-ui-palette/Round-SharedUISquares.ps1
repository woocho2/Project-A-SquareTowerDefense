$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskAtlas=Join-Path $taskRepo 'Assets/4. Asset/5. UI/Shared_UI_Atlas.png'
$taskUtf8=New-Object Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
public static class RoundedUISquares {
    public static string Apply(string path,string meta,string preview){
        using(var stream=new System.IO.MemoryStream(System.IO.File.ReadAllBytes(path)))using(var source=new Bitmap(stream))using(var result=source.Clone(new Rectangle(0,0,source.Width,source.Height),PixelFormat.Format32bppArgb)){
            var bounds=new Rectangle(0,0,result.Width,result.Height);
            var data=result.LockBits(bounds,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            byte[] pixels=new byte[data.Stride*data.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
            byte[] original=(byte[])pixels.Clone();bool[] edited=new bool[source.Width*source.Height];
            string pattern=@"(?ms)^      name: (Shared_(A|B|C|Frame)_Btn_\d+(?:_B)?)\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+)";
            var matches=Regex.Matches(meta,pattern);if(matches.Count!=20)throw new Exception("Expected twenty square sprites");
            var rectangles=new Rectangle[20];var names=new string[20];int item=0;
            foreach(Match match in matches){
                int x=int.Parse(match.Groups[3].Value),w=int.Parse(match.Groups[5].Value),h=int.Parse(match.Groups[6].Value);
                int y=source.Height-int.Parse(match.Groups[4].Value)-h;
                if(w!=h)throw new Exception("Selected a non-square sprite");
                int left=w,top=h,right=-1,bottom=-1;
                for(int iy=0;iy<h;iy++)for(int ix=0;ix<w;ix++){
                    int p=(y+iy)*data.Stride+(x+ix)*4;
                    if(original[p+3]>=8){left=Math.Min(left,ix);right=Math.Max(right,ix);top=Math.Min(top,iy);bottom=Math.Max(bottom,iy);}
                }
                if(right<left||bottom<top)throw new Exception("Empty square sprite");
                double width=right-left+1,height=bottom-top+1,radius=Math.Min(width,height)*.16;
                bool frame=match.Groups[2].Value=="Frame";
                for(int iy=0;iy<h;iy++)for(int ix=0;ix<w;ix++){
                    int p=(y+iy)*data.Stride+(x+ix)*4;edited[(y+iy)*source.Width+x+ix]=true;
                    double dx=Math.Abs(ix+.5-left-width*.5)-(width*.5-radius);
                    double dy=Math.Abs(iy+.5-top-height*.5)-(height*.5-radius);
                    double depth=radius-Math.Sqrt(Math.Pow(Math.Max(dx,0),2)+Math.Pow(Math.Max(dy,0),2))-Math.Min(Math.Max(dx,dy),0);
                    double coverage=Math.Max(0,Math.Min(1,depth+.5));
                    if(coverage==0){pixels[p]=pixels[p+1]=pixels[p+2]=pixels[p+3]=0;continue;}
                    int profileY=frame?top+(int)Math.Max(0,Math.Floor(depth)):iy;
                    profileY=Math.Max(top,Math.Min(bottom,profileY));
                    int sample=(y+profileY)*data.Stride+(x+(left+right)/2)*4;
                    pixels[p]=original[sample];pixels[p+1]=original[sample+1];pixels[p+2]=original[sample+2];
                    pixels[p+3]=(byte)Math.Round(original[sample+3]*coverage);
                }
                rectangles[item]=new Rectangle(x,y,w,h);names[item]=match.Groups[1].Value;item++;
            }
            long changed=0;
            for(int pixel=0;pixel<edited.Length;pixel++)for(int c=0;c<4;c++){
                if(pixels[pixel*4+c]==original[pixel*4+c])continue;
                if(!edited[pixel])throw new Exception("Changed a rectangular sprite or atlas padding");changed++;
            }
            Marshal.Copy(pixels,0,data.Scan0,pixels.Length);result.UnlockBits(data);
            using(var canvas=new Bitmap(1180,930,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(canvas))
            using(var font=new Font("Segoe UI",13,FontStyle.Bold)){
                g.Clear(ColorTranslator.FromHtml("#202830"));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                for(int n=0;n<20;n++){
                    int px=20+n%5*232,py=20+n/5*228;
                    g.DrawImage(result,new Rectangle(px,py,200,200),rectangles[n],GraphicsUnit.Pixel);
                    g.DrawString(names[n].Replace("Shared_",""),font,Brushes.White,px,py+202);
                }
                canvas.Save(preview,ImageFormat.Png);
            }
            result.Save(path,ImageFormat.Png);
            return "PASS rounded twenty square sprites; all forty rectangular sprites and atlas padding preserved pixel-for-pixel. Changed channels: "+changed;
        }
    }
}
'@
$taskMeta=[IO.File]::ReadAllText($taskAtlas+'.meta')
$taskReport=[RoundedUISquares]::Apply($taskAtlas,$taskMeta,(Join-Path $PSScriptRoot 'rounded-squares-preview.png'))
$taskBorders=0
$taskAfter=[regex]::Replace($taskMeta,'(?ms)^    - serializedVersion: 2\r?\n      name: [^\r\n]+.*?(?=^    - serializedVersion: 2|^    outline:)',[Text.RegularExpressions.MatchEvaluator]{param($m)
    $block=$m.Value
    $border=if($block -match '(?m)^      name: Shared_(A|B|C|Frame)_Btn_128_B\r?$'){$script:taskBorders++;'{x: 48, y: 48, z: 48, w: 48}'}else{'{x: 0, y: 0, z: 0, w: 0}'}
    return [regex]::Replace($block,'(?m)^      border: \{[^\r\n]+\}',('      border: '+$border))
})
if($taskBorders -ne 4){throw 'Expected one sliced 128_B sprite for each of A/B/C/Frame'}
$taskBeforeInvariant=[regex]::Replace($taskMeta,'(?m)^      border: [^\r\n]+','')
$taskAfterInvariant=[regex]::Replace($taskAfter,'(?m)^      border: [^\r\n]+','')
if($taskBeforeInvariant -ne $taskAfterInvariant){throw 'Unexpected sprite metadata change'}
[IO.File]::WriteAllText(($taskAtlas+'.meta'),$taskAfter,$taskUtf8)
$taskReport+="`nPASS A/B/C/Frame 128_B sprites have 48px slicing borders. All other 56 sprites have zero border. GUID, sprite IDs, rects and pivots preserved.`n"
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'rounded-squares-validation.txt'),$taskReport,$taskUtf8)
Write-Output $taskReport
