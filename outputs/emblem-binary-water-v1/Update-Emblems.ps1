$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$atlasRel='Assets/4. Asset/2. Tower/Tower_Emblem_White_Atlas.png'
$waterRel='Assets/4. Asset/2. Tower/Emblem_Water_White.png'
$atlasPath=Join-Path $root $atlasRel
$backup=Join-Path $PSScriptRoot 'before'
$paths=@($atlasRel,($atlasRel+'.meta'),$waterRel,($waterRel+'.meta'),'Assets/2. Prefab/2. Tower/TowerBase.prefab','Assets/2. Prefab/DebuffZone.prefab')
foreach($path in $paths){
    $dest=Join-Path $backup $path
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest)) | Out-Null
    if(!(Test-Path -LiteralPath $dest)){[IO.File]::Copy((Join-Path $root $path),$dest)}
}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public static class BinaryTowerEmblems {
    public static void Build(string folder,string atlasPath) {
        using(var source=new Bitmap(atlasPath))
        using(var native=new Bitmap(Path.Combine(folder,"water-edited.png")))
        using(var atlas=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb)) {
            if(source.Width!=1024||source.Height!=1024)throw new Exception("Unexpected atlas size.");
            // Every existing emblem keeps its coverage at the 50% alpha cutoff.
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)atlas.SetPixel(x,y,source.GetPixel(x,y).A>=128?Color.White:Color.FromArgb(0,0,0,0));
            // Find only the main generated droplet, discarding detached speckles.
            int w=native.Width,h=native.Height;var labels=new int[w*h];var sizes=new List<int>();sizes.Add(0);
            var queue=new Queue<int>();int label=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++) {
                int p=y*w+x;Color c=native.GetPixel(x,y);
                if(labels[p]!=0||c.A<128||c.R<128||c.G<128||c.B<128)continue;
                label++;sizes.Add(0);labels[p]=label;queue.Enqueue(p);
                while(queue.Count>0){int n=queue.Dequeue(),xx=n%w,yy=n/w;sizes[label]++;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
                        int a=xx+dx,b=yy+dy;if(a<0||b<0||a>=w||b>=h)continue;int q=b*w+a;
                        if(labels[q]!=0)continue;Color color=native.GetPixel(a,b);
                        if(color.A>=128&&color.R>=128&&color.G>=128&&color.B>=128){labels[q]=label;queue.Enqueue(q);}
                    }
                }
            }
            int largest=0;for(int i=1;i<sizes.Count;i++)if(sizes[i]>sizes[largest])largest=i;
            if(largest==0||sizes[largest]<1000)throw new Exception("Droplet not found.");
            int left=w,right=0,top=h,bottom=0;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(labels[y*w+x]==largest){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            var bounds=Rectangle.FromLTRB(left,top,right+1,bottom+1);
            using(var isolated=new Bitmap(w,h,PixelFormat.Format32bppArgb))
            using(var droplet=new Bitmap(256,256,PixelFormat.Format32bppArgb)) {
                for(int y=top;y<=bottom;y++)for(int x=left;x<=right;x++)if(labels[y*w+x]==largest)isolated.SetPixel(x,y,Color.White);
                int targetHeight=180,targetWidth=(int)Math.Round(bounds.Width*180.0/bounds.Height);
                using(var g=Graphics.FromImage(droplet)){
                    g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                    g.DrawImage(isolated,new Rectangle((256-targetWidth)/2,38,targetWidth,targetHeight),bounds.X,bounds.Y,bounds.Width,bounds.Height,GraphicsUnit.Pixel);
                }
                for(int y=0;y<256;y++)for(int x=0;x<256;x++){
                    Color c=droplet.GetPixel(x,y).A>=128?Color.White:Color.FromArgb(0,0,0,0);
                    droplet.SetPixel(x,y,c);atlas.SetPixel(256+x,768+y,c);
                }
                droplet.Save(Path.Combine(folder,"water-packed.png"),ImageFormat.Png);
            }
            long opaque=0,transparent=0;
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++){
                Color c=atlas.GetPixel(x,y);
                if(c.A==255&&c.R==255&&c.G==255&&c.B==255)opaque++;
                else if(c.A==0&&c.R==0&&c.G==0&&c.B==0)transparent++;
                else throw new Exception("Non-binary RGBA pixel.");
            }
            atlas.Save(Path.Combine(folder,"Tower_Emblem_White_Atlas.png"),ImageFormat.Png);
            using(var preview=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(var g=Graphics.FromImage(preview)){
                g.Clear(Color.FromArgb(45,52,66));g.DrawImage(atlas,new Rectangle(0,0,1024,1024),0,0,1024,1024,GraphicsUnit.Pixel);
                preview.Save(Path.Combine(folder,"preview.png"),ImageFormat.Png);
            }
            File.WriteAllText(Path.Combine(folder,"pixel-verification.txt"),"PASS: 1048576 atlas pixels contain exactly RGBA(255,255,255,255) or RGBA(0,0,0,0).\nOpaque="+opaque+"; transparent="+transparent+".\nWater: single droplet in 256x256 cell at PNG (256,768); height 180, centered.\n");
        }
    }
}
'@
[BinaryTowerEmblems]::Build($PSScriptRoot,(Join-Path $backup $atlasRel))
$utf8=New-Object Text.UTF8Encoding($false)
$meta=[IO.File]::ReadAllText((Join-Path $backup ($atlasRel+'.meta'))).Replace("`r`n","`n")
$newName='Emblem_08_Water'
$meta=$meta.Replace("  externalObjects: {}",("  - first:`n      213: 1014`n    second: "+$newName+"`n  externalObjects: {}"))
$first=[regex]::Match($meta,'(?ms)^    - serializedVersion: 2\n      name: Emblem_01_Sword\n.*?(?=^    - serializedVersion: 2)').Value
if(!$first){throw 'Slice template missing.'}
$newSlice=$first.Replace('Emblem_01_Sword',$newName).Replace('        x: 0','        x: 256').Replace('        y: 768','        y: 0').Replace('      internalID: 1001','      internalID: 1014')
$newSlice=[regex]::Replace($newSlice,'(?m)^      spriteID: .*',('      spriteID: '+[guid]::NewGuid().ToString('N')))
$newSlice=[regex]::Replace($newSlice,'(?m)[ \t]+$','')
$meta=$meta.Replace("    outline: []`n    customData:",($newSlice+"    outline: []`n    customData:"))
if($meta -notmatch 'internalID: 1014'){throw 'New slice insertion failed.'}
$meta=$meta.Replace('      Emblem_13_Dark: 1013',("      Emblem_13_Dark: 1013`n      "+$newName+': 1014'))
# Point sampling and no compression/mipmaps preserve the requested binary alpha.
$meta=$meta.Replace('    filterMode: 1','    filterMode: 0')
if([regex]::Matches($meta,'(?m)^      name: ').Count -ne 14){throw 'Expected 14 slices.'}
[IO.File]::WriteAllText(($atlasPath+'.meta'),$meta.Replace("`n","`r`n"),$utf8)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Tower_Emblem_White_Atlas.png') -Destination $atlasPath -Force
$old='{fileID: 21300000, guid: ac4c220652d946b393da83a480951449, type: 3}'
$new='{fileID: 1014, guid: 7e3f1605aca04ab5b6e525a542b6b7b3, type: 3}'
foreach($path in @('Assets/2. Prefab/2. Tower/TowerBase.prefab','Assets/2. Prefab/DebuffZone.prefab')){
    $absolute=Join-Path $root $path;$content=[IO.File]::ReadAllText($absolute)
    if(!$content.Contains($old)){throw ('Water reference not found: '+$path)}
    [IO.File]::WriteAllText($absolute,$content.Replace($old,$new),$utf8)
}
$leftovers=@(rg -l -F 'ac4c220652d946b393da83a480951449' (Join-Path $root 'Assets') -g '!*.meta')
if($leftovers.Count -gt 0){throw ('Old water still referenced: '+($leftovers -join ', '))}
$deletePath=[IO.Path]::GetFullPath((Join-Path $root $waterRel))
$allowed=[IO.Path]::GetFullPath((Join-Path $root 'Assets/4. Asset/2. Tower'))+'\'
if(!$deletePath.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid water deletion target.'}
Remove-Item -LiteralPath $deletePath
Remove-Item -LiteralPath ($deletePath+'.meta')
Get-Content -LiteralPath (Join-Path $PSScriptRoot 'pixel-verification.txt')
'PASS: 14 atlas slices; existing 13 names/IDs/rectangles preserved; water references migrated; old separate water PNG and meta removed.'
