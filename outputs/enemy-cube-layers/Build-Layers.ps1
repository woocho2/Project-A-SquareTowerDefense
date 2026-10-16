$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$palettes=Get-Content -LiteralPath (Join-Path $root 'outputs/valkyrie-six-state-faces-v1/generation-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$states=@('SelfHeal','Defense','Action','Mobility','Locked','Hourglass')
$backgrounds=@($states | ForEach-Object {$key=$_;($palettes | Where-Object key -eq $key).bg})
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
public static class EnemyLayerExport {
    static readonly string[] States={"SelfHeal","Defense","Action","Mobility","Locked","Hourglass"};
    static readonly double[] Hues={351,46,141,198,0,268};
    static Color Palette(double h,bool gray) {
        if(gray)return Color.White;
        double x=1-Math.Abs((h/60)%2-1);double r=0,g=0,b=0;
        if(h<60){r=1;g=x;}else if(h<120){r=x;g=1;}else if(h<180){g=1;b=x;}else if(h<240){g=x;b=1;}else if(h<300){r=x;b=1;}else{r=1;b=x;}
        return Color.FromArgb(255,(int)Math.Round(r*255),(int)Math.Round(g*255),(int)Math.Round(b*255));
    }
    static Color Tint(Color source,Color target) {
        double v=Math.Max(source.R,Math.Max(source.G,source.B)),minimum=Math.Min(source.R,Math.Min(source.G,source.B));
        double s=v==0?0:(v-minimum)/v,tv=Math.Max(target.R,Math.Max(target.G,target.B));
        return Color.FromArgb(source.A,(int)Math.Round(v*((1-s)+s*target.R/tv)),(int)Math.Round(v*((1-s)+s*target.G/tv)),(int)Math.Round(v*((1-s)+s*target.B/tv)));
    }
    static void Draw(Graphics g,Bitmap b,Rectangle d,Rectangle s){g.DrawImage(b,d,s.X,s.Y,s.Width,s.Height,GraphicsUnit.Pixel);}
    public static void Build(string root,string output,string[] backgrounds) {
        string assets=Path.Combine(root,"Assets/4. Asset/4. Enemy");Directory.CreateDirectory(assets);
        string valk=Path.Combine(root,"outputs/valkyrie-six-state-faces-v1");
        var report=new StringBuilder();
        using(var reference=new Bitmap(Path.Combine(valk,"faces/SelfHeal.png")))
        using(var bg=new Bitmap(512,512,PixelFormat.Format32bppArgb))
        using(var frame=new Bitmap(Path.Combine(valk,"layers/Decoration_SelfHeal.png")))
        using(var badges=new Bitmap(384,256,PixelFormat.Format32bppArgb))
        using(var preview=new Bitmap(1200,720,PixelFormat.Format32bppArgb))
        using(var pg=Graphics.FromImage(preview))
        using(var font=new Font("Segoe UI",12,FontStyle.Bold)) {
            pg.Clear(Color.FromArgb(229,232,237));
            if(reference.GetPixel(0,0).A!=0||reference.GetPixel(256,256).A!=255)throw new Exception("Rounded outer mask expected, not the old flattened 3D texture.");
            for(int y=0;y<512;y++)for(int x=0;x<512;x++)bg.SetPixel(x,y,Color.FromArgb(reference.GetPixel(x,y).A,255,255,255));
            bg.Save(Path.Combine(assets,"Enemy_Background.png"),ImageFormat.Png);
            // Neutral base: keep every source alpha and shading value, remove its red hue.
            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                Color c=frame.GetPixel(x,y);int value=Math.Max(c.R,Math.Max(c.G,c.B));
                frame.SetPixel(x,y,Color.FromArgb(c.A,value,value,value));
            }
            frame.Save(Path.Combine(assets,"Enemy_Decoration.png"),ImageFormat.Png);
            for(int state=0;state<6;state++) {
                using(var source=new Bitmap(Path.Combine(valk,"faces/"+States[state]+".png"))) {
                    for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
                        double dx=x+.5-64,dy=y+.5-64,d=Math.Sqrt(dx*dx+dy*dy);
                        if(d>=38)continue;Color c=source.GetPixel(192+x,y);
                        int a=(int)Math.Round(c.A*Math.Min(1,38-d));
                        badges.SetPixel(state%3*128+x,state/3*128+y,Color.FromArgb(a,c.R,c.G,c.B));
                    }
                }
            }
            badges.Save(Path.Combine(assets,"Enemy_State_Badges_Atlas.png"),ImageFormat.Png);
            int characterIndex=0;
            foreach(string character in new[]{"Valkyrie","Brunhild","Loki"}) {
                string portraitPath=character=="Valkyrie"?Path.Combine(valk,"layers/Portrait_SelfHeal.png"):Path.Combine(root,"outputs/asgard-corrupted-bosses-v1/aligned-portraits/"+character+"-canonical.png");
                using(var portrait=new Bitmap(portraitPath))
                using(var mask=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
                    if(portrait.Width!=512||portrait.Height!=512)throw new Exception("Canonical portrait must be 512x512.");
                    if(character=="Valkyrie") {
                        for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                            Color c=portrait.GetPixel(x,y);double v=Math.Max(c.R,Math.Max(c.G,c.B))/255.0;
                            double s=v==0?0:(v-Math.Min(c.R,Math.Min(c.G,c.B))/255.0)/v;
                            bool face=x>=112&&x<=400&&y>=238&&y<=384;
                            bool marked=c.A>0&&!face&&(c.GetHue()>=335||c.GetHue()<=18)&&s>=.32&&v>=.1;
                            mask.SetPixel(x,y,marked?Color.White:Color.Black);
                        }
                    } else {
                        using(var originalMask=new Bitmap(Path.Combine(root,"outputs/asgard-corrupted-bosses-v1/aligned-portraits/"+character+"-equipment-mask.png")))
                            for(int y=0;y<512;y++)for(int x=0;x<512;x++)mask.SetPixel(x,y,originalMask.GetPixel(x,y).R>=128?Color.White:Color.Black);
                    }
                    portrait.Save(Path.Combine(assets,"Enemy_"+character+"_Character.png"),ImageFormat.Png);
                    mask.Save(Path.Combine(assets,"Enemy_"+character+"_EquipmentMask.png"),ImageFormat.Png);
                    pg.DrawString(character+" / SEPARATE LAYERS",font,Brushes.DarkSlateGray,10,characterIndex*240+5);
                    for(int state=0;state<6;state++) {
                        Color tint=Palette(Hues[state],state==4),background=ColorTranslator.FromHtml(backgrounds[state]);
                        using(var characterLayer=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                        using(var border=new Bitmap(512,512,PixelFormat.Format32bppArgb))
                        using(var tile=new Bitmap(512,512,PixelFormat.Format32bppArgb)) {
                            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                                Color original=portrait.GetPixel(x,y),c=original;
                                if(mask.GetPixel(x,y).R>128)c=Tint(c,tint);
                                characterLayer.SetPixel(x,y,c);
                                if(mask.GetPixel(x,y).R<128&&c.ToArgb()!=original.ToArgb())throw new Exception("Face/hair pixel changed.");
                                if(c.A!=original.A)throw new Exception("Portrait placement/alpha changed.");
                                Color f=frame.GetPixel(x,y);
                                border.SetPixel(x,y,Color.FromArgb(f.A,(int)Math.Round(f.R*tint.R/255.0),(int)Math.Round(f.G*tint.G/255.0),(int)Math.Round(f.B*tint.B/255.0)));
                                tile.SetPixel(x,y,Color.FromArgb(bg.GetPixel(x,y).A,background.R,background.G,background.B));
                            }
                            using(var g=Graphics.FromImage(tile)) {
                                Draw(g,characterLayer,new Rectangle(0,0,512,512),new Rectangle(0,0,512,512));
                                Draw(g,border,new Rectangle(0,0,512,512),new Rectangle(0,0,512,512));
                                Draw(g,badges,new Rectangle(192,0,128,128),new Rectangle(state%3*128,state/3*128,128,128));
                            }
                            for(int y=0;y<512;y++)for(int x=0;x<512;x++) {
                                Color c=tile.GetPixel(x,y);tile.SetPixel(x,y,Color.FromArgb(bg.GetPixel(x,y).A,c.R,c.G,c.B));
                                if(tile.GetPixel(x,y).A!=reference.GetPixel(x,y).A)throw new Exception("Outer shape changed.");
                            }
                            tile.Save(Path.Combine(output,character+"_"+States[state]+"_preview.png"),ImageFormat.Png);
                            Draw(pg,tile,new Rectangle(state*200+8,characterIndex*240+30,184,184),new Rectangle(0,0,512,512));
                            pg.DrawString(States[state],font,Brushes.DarkSlateGray,state*200+8,characterIndex*240+214);
                            report.AppendLine("PASS: "+character+"/"+States[state]+": unchanged portrait placement, unmasked face pixels, and rounded outer alpha.");
                        }
                    }
                }
                characterIndex++;
            }
            preview.Save(Path.Combine(output,"preview.png"),ImageFormat.Png);
        }
        report.AppendLine("Only original separated RGBA layers were exported; no character art was regenerated.");
        File.WriteAllText(Path.Combine(output,"layer-verification.txt"),report.ToString(),new UTF8Encoding(false));
    }
}
'@
[EnemyLayerExport]::Build($root,$PSScriptRoot,[string[]]$backgrounds)
'Exported 9 separate layer textures and 18 CPU composition previews.'
