$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
public static class AppliedEmblemPreview {
 public static void Run(string root,string project){
  string[] names={"Sword","Bow","Shield","Spear","Axe","Hammer","Fire","Ice","Electricity","Wind","Earth","Light","Dark"};
  Color[] colors={ColorTranslator.FromHtml("#E25D62"),ColorTranslator.FromHtml("#538ACD"),ColorTranslator.FromHtml("#D5AD28"),Color.FromArgb(32,32,39)};
  using(var atlas=new Bitmap(project+"/Assets/4. Asset/2. Tower/3. Emblem/Tower_Emblem_White_Atlas.png"))
  using(var bg=new Bitmap(project+"/Assets/4. Asset/2. Tower/2. Color/Tower_White_Background.png"))
  using(var tier=new Bitmap(project+"/Assets/4. Asset/2. Tower/1. Tier/Diamond.png"))
  using(var spawn=new Bitmap(project+"/Assets/4. Asset/1. BackGround/Tile_TowerSpawn_Atlas.png"))
  using(var page=new Bitmap(1120,1280,PixelFormat.Format32bppArgb))
  using(var g=Graphics.FromImage(page))
  using(var font=new Font("Arial",13)){
   g.Clear(Color.FromArgb(42,54,68));
   g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   for(int i=0;i<13;i++){
    int px=(i%4)*280+12,py=(i/4)*320+28;
    using(var icon=atlas.Clone(new Rectangle((i%4)*256,(i/4)*256,256,256),PixelFormat.Format32bppArgb)){
     Color tint=colors[i%4];
     for(int y=0;y<256;y++)for(int x=0;x<256;x++)icon.SetPixel(x,y,Color.FromArgb(icon.GetPixel(x,y).A,tint.R,tint.G,tint.B));
     // Current prefab render order: spawn -> white face -> tintable emblem -> unchanged tier.
     g.DrawImage(spawn,new Rectangle(px,py,256,256),new Rectangle(0,0,256,256),GraphicsUnit.Pixel);
     g.DrawImageUnscaled(bg,px,py);
     g.DrawImage(icon,new Rectangle(px+32,py+32,192,192));
     g.DrawImage(tier,new RectangleF(px+128-128*1.06f,py+128-128*1.045f+2.46f,256*1.06f,256*1.045f));
     g.DrawString((i+1).ToString("D2")+" "+names[i],font,Brushes.White,px,py-24);
    }
   }
   page.Save(root+"/Applied_Emblems_Preview.png",ImageFormat.Png);
  }
 }
}
'@
$projectRoot=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[AppliedEmblemPreview]::Run($PSScriptRoot,$projectRoot)
