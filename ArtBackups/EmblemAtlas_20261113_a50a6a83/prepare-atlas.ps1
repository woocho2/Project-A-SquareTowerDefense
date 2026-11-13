$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$backupPath = $PSScriptRoot
$sourcePath = 'C:\Users\user\.codex\generated_images\01a113e3-050d-77a2-9b09-b97ca0dcaa81\exec-664068f3-13fc-4cad-aa13-9779f2b79f69.png'
Copy-Item -LiteralPath $sourcePath -Destination (Join-Path $backupPath 'generated-transparent-source.png') -Force
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
public class EmblemPacking20261113 {
    public class Entry {
        public string Name;
        public int SourceIndex, AtlasIndex;
        public int SourceX, SourceY, SourceWidth, SourceHeight;
        public int TargetX, TargetY, TargetWidth, TargetHeight;
        public int MarginLeft, MarginTop, MarginRight, MarginBottom;
        public double Scale;
    }
    public static Entry[] Pack(string sourcePath, string outputPath, string previewPath, string contactPath) {
        string[] names = {"Sword","Axe","Bow","Shield","Spear","Hammer","Fire","Water","Earth","Wind","Ice","Electricity","Light","Darkness"};
        int[] atlasSlots = {0,4,1,2,3,5,6,13,10,9,7,8,11,12};
        var entries = new List<Entry>();
        using (var source = new Bitmap(sourcePath))
        using (var atlas = new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(atlas)) {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            for (int i=0; i<14; i++) {
                int sx0=(i%4)*source.Width/4, sx1=((i%4)+1)*source.Width/4;
                int sy0=(i/4)*source.Height/4, sy1=((i/4)+1)*source.Height/4;
                int minX=sx1,minY=sy1,maxX=-1,maxY=-1;
                for(int y=sy0;y<sy1;y++) for(int x=sx0;x<sx1;x++) {
                    if(source.GetPixel(x,y).A<64) continue;
                    minX=Math.Min(minX,x); minY=Math.Min(minY,y); maxX=Math.Max(maxX,x); maxY=Math.Max(maxY,y);
                }
                if(maxX<minX || maxY<minY) throw new Exception("Empty symbol: "+names[i]);
                minX=Math.Max(sx0,minX-2); minY=Math.Max(sy0,minY-2);
                maxX=Math.Min(sx1-1,maxX+2); maxY=Math.Min(sy1-1,maxY+2);
                int sw=maxX-minX+1, sh=maxY-minY+1;
                double scale=Math.Min(1.0,Math.Min(224.0/sw,224.0/sh));
                int tw=(int)Math.Round(sw*scale), th=(int)Math.Round(sh*scale);
                int slot=atlasSlots[i], tx=(slot%4)*256+(256-tw)/2, ty=(slot/4)*256+(256-th)/2;
                using(var glyph = new Bitmap(sw,sh,PixelFormat.Format32bppArgb)) {
                    for(int y=0;y<sh;y++) for(int x=0;x<sw;x++) {
                        Color pixel=source.GetPixel(minX+x,minY+y);
                        if(pixel.A>0) glyph.SetPixel(x,y,Color.FromArgb(pixel.A,255,255,255));
                    }
                    if(tw==sw && th==sh) graphics.DrawImageUnscaled(glyph,tx,ty);
                    else graphics.DrawImage(glyph,new Rectangle(tx,ty,tw,th),0,0,sw,sh,GraphicsUnit.Pixel);
                }
                entries.Add(new Entry {
                    Name=names[i],SourceIndex=i,AtlasIndex=slot,SourceX=minX,SourceY=minY,SourceWidth=sw,SourceHeight=sh,
                    TargetX=tx,TargetY=ty,TargetWidth=tw,TargetHeight=th,Scale=scale,
                    MarginLeft=(256-tw)/2,MarginTop=(256-th)/2,MarginRight=256-tw-(256-tw)/2,MarginBottom=256-th-(256-th)/2
                });
            }
            // Normalize the generated white mask after GDI's premultiplied-alpha conversion.
            // Remove virtually invisible alpha residue and restore opaque flat interiors;
            // keep the intermediate edge-alpha range for antialiasing.
            graphics.Flush();
            for(int y=0;y<atlas.Height;y++) for(int x=0;x<atlas.Width;x++) {
                int a=atlas.GetPixel(x,y).A;
                if(a<=4) atlas.SetPixel(x,y,Color.FromArgb(0,255,255,255));
                else atlas.SetPixel(x,y,Color.FromArgb(a>=249?255:a,255,255,255));
            }
            atlas.Save(outputPath,ImageFormat.Png);
            using(var preview=new Bitmap(1024,1024)) using(var pg=Graphics.FromImage(preview)) {
                pg.Clear(Color.FromArgb(30,32,45)); pg.DrawImageUnscaled(atlas,0,0);
                preview.Save(previewPath,ImageFormat.Png);
            }
            using(var contact=new Bitmap(1120,440)) using(var cg=Graphics.FromImage(contact))
            using(var font=new Font("Segoe UI",11)) {
                cg.Clear(Color.FromArgb(30,32,45));
                cg.InterpolationMode=InterpolationMode.HighQualityBicubic;
                for(int i=0;i<14;i++) {
                    int x=(i%7)*160, y=(i/7)*220, slot=atlasSlots[i];
                    Rectangle rect=new Rectangle((slot%4)*256,(slot/4)*256,256,256);
                    cg.DrawImage(atlas,new Rectangle(x+24,y+6,112,112),rect,GraphicsUnit.Pixel);
                    cg.DrawString(names[i],font,Brushes.White,x+30,y+124);
                    cg.DrawImage(atlas,new Rectangle(x+62,y+158,36,36),rect,GraphicsUnit.Pixel);
                }
                contact.Save(contactPath,ImageFormat.Png);
            }
        }
        return entries.ToArray();
    }
}
'@
$entries = [EmblemPacking20261113]::Pack($sourcePath,(Join-Path $backupPath 'prepared-atlas.png'),(Join-Path $backupPath 'atlas-preview.png'),(Join-Path $backupPath 'all-emblems-preview.png'))
$entries | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $backupPath 'packing.json') -Encoding UTF8
$entries | Select-Object Name,AtlasIndex,TargetWidth,TargetHeight,MarginLeft,MarginTop | ConvertTo-Json
