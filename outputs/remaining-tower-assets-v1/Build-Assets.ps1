$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskJobs = Get-Content -Encoding UTF8 -LiteralPath (Join-Path $PSScriptRoot 'prompts.json') -Raw | ConvertFrom-Json
$taskAssetDir = Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Remaining_v1'
[void][IO.Directory]::CreateDirectory($taskAssetDir)
$taskFolderMeta=$taskAssetDir+'.meta'
if(!(Test-Path -LiteralPath $taskFolderMeta)){
    [IO.File]::WriteAllText($taskFolderMeta,("fileFormatVersion: 2`nguid: "+[Guid]::NewGuid().ToString('N')+"`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"),[Text.UTF8Encoding]::new($false))
}
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'png'))
[void][IO.Directory]::CreateDirectory((Join-Path $PSScriptRoot 'atlases'))
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
public static class RemainingTowerPack {
    public static Bitmap Tile(string path) {
        var tile = new Bitmap(256,256,PixelFormat.Format32bppArgb);
        using(var src=new Bitmap(path)) using(var g=Graphics.FromImage(tile)) using(var attr=new ImageAttributes()) {
            bool transparent=false,visible=false;
            for(int y=0;y<src.Height;y+=8) for(int x=0;x<src.Width;x+=8) {
                int a=src.GetPixel(x,y).A; transparent |= a==0; visible |= a>240;
            }
            if(!transparent || !visible) throw new Exception("Missing transparent/visible pixels: "+path);
            g.Clear(Color.Transparent); g.CompositingMode=CompositingMode.SourceCopy;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic; g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            attr.SetWrapMode(WrapMode.TileFlipXY);
            double scale=Math.Min(256.0/src.Width,256.0/src.Height);
            int w=(int)Math.Round(src.Width*scale),h=(int)Math.Round(src.Height*scale);
            g.DrawImage(src,new Rectangle((256-w)/2,(256-h)/2,w,h),0,0,src.Width,src.Height,GraphicsUnit.Pixel,attr);
        }
        return tile;
    }
    public static void Atlas(string[] sources,string[] individual,string output) {
        using(var atlas=new Bitmap(512,512,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(atlas)) {
            g.Clear(Color.Transparent); g.CompositingMode=CompositingMode.SourceCopy;
            for(int i=0;i<4;i++) using(var tile=Tile(sources[i])) {
                tile.Save(individual[i],ImageFormat.Png); g.DrawImageUnscaled(tile,(i%2)*256,(i/2)*256);
            }
            atlas.Save(output,ImageFormat.Png);
        }
    }
    public static void Preview(string[] files,string[] names,string[] groups,string framePath,string output) {
        int rows=files.Length/4, cell=224, row=262, top=78;
        using(var image=new Bitmap(cell*4,top+rows*row+24,PixelFormat.Format32bppArgb))
        using(var g=Graphics.FromImage(image)) using(var frame=new Bitmap(framePath))
        using(var font=new Font("Malgun Gothic",12,FontStyle.Bold))
        using(var title=new Font("Malgun Gothic",18,FontStyle.Bold))
        using(var format=new StringFormat()) {
            g.Clear(Color.FromArgb(255,29,33,44)); format.Alignment=StringAlignment.Center;
            g.DrawString("큐브 타워 · 남은 48종",title,Brushes.Ivory,new RectangleF(0,4,cell*4,32),format);
            string[] role={"레드 · 광역","블루 · 단일","화이트 · 버프","블랙 · 디버프"};
            Brush[] colors={Brushes.Salmon,Brushes.LightSkyBlue,Brushes.Ivory,Brushes.Plum};
            for(int c=0;c<4;c++) g.DrawString(role[c],font,colors[c],new RectangleF(c*cell,42,cell,30),format);
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            for(int i=0;i<files.Length;i++) using(var portrait=new Bitmap(files[i])) {
                int x=(i%4)*cell+12,y=top+(i/4)*row;
                g.DrawString(groups[i]+" · "+names[i],font,Brushes.Ivory,new RectangleF(x-12,y,cell,30),format);
                g.DrawImage(frame,new Rectangle(x,y+32,200,200),0,0,256,256,GraphicsUnit.Pixel);
                g.DrawImage(portrait,new Rectangle(x,y+32,200,200));
            }
            image.Save(output,ImageFormat.Png);
        }
    }
}
'@
$taskTemplate = [IO.File]::ReadAllText((Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Axe_FaceHair_Atlas_v1.png.meta'))
$taskOldNames = @('Tower_Axe_Viking_FaceHair','Tower_Axe_Berserker_FaceHair','Tower_Axe_Skald_FaceHair','Tower_Axe_Executioner_FaceHair')
$taskGroups = $taskJobs | Group-Object group
$taskManifest = @()
foreach($taskGroup in $taskGroups) {
    $taskSet = @($taskGroup.Group | Sort-Object classId)
    $taskSources = @(); $taskIndividuals = @()
    foreach($taskJob in $taskSet) {
        $taskFile = 'Tower_'+$taskJob.group+'_'+$taskJob.role+'_'+$taskJob.name+'.png'
        $taskSource = Join-Path $PSScriptRoot ('source/'+$taskFile)
        if(!(Test-Path -LiteralPath $taskSource)){ throw ('Missing requested tower: '+$taskFile) }
        $taskSources += $taskSource
        $taskIndividuals += Join-Path $PSScriptRoot ('png/'+$taskFile)
        $taskManifest += [ordered]@{emblemId=$taskJob.emblemId;emblem=$taskJob.groupKo;classId=$taskJob.classId;role=$taskJob.role;name=$taskJob.nameKo;source=('source/'+$taskFile);sprite=('Tower_'+$taskJob.group+'_'+$taskJob.name);atlas=('Tower_'+$taskJob.group+'_Characters_Atlas_v1.png');planningId=(1000+100*$taskJob.classId+$taskJob.emblemId)}
    }
    $taskAtlasFile='Tower_'+$taskGroup.Name+'_Characters_Atlas_v1.png'
    $taskAtlasPath=Join-Path $PSScriptRoot ('atlases/'+$taskAtlasFile)
    [RemainingTowerPack]::Atlas([string[]]$taskSources,[string[]]$taskIndividuals,$taskAtlasPath)
    Copy-Item -LiteralPath $taskAtlasPath -Destination (Join-Path $taskAssetDir $taskAtlasFile)
    $taskMetaPath=Join-Path $taskAssetDir ($taskAtlasFile+'.meta')
    if(!(Test-Path -LiteralPath $taskMetaPath)) {
        $taskMeta=[regex]::Replace($taskTemplate,'(?m)^guid: [a-f0-9]+','guid: '+[Guid]::NewGuid().ToString('N'))
        $taskSpriteMatches=[regex]::Matches($taskMeta,'(?m)^      spriteID: ([a-f0-9]+)')
        for($taskI=0;$taskI -lt 4;$taskI++) {
            $taskMeta=$taskMeta.Replace($taskOldNames[$taskI],('Tower_'+$taskSet[$taskI].group+'_'+$taskSet[$taskI].name))
            $taskMeta=$taskMeta.Replace($taskSpriteMatches[$taskI].Groups[1].Value,[Guid]::NewGuid().ToString('N'))
            $taskMeta=$taskMeta.Replace((42121+$taskI).ToString(),(48000+10*$taskSet[$taskI].emblemId+$taskI).ToString())
        }
        [IO.File]::WriteAllText($taskMetaPath,$taskMeta,[Text.UTF8Encoding]::new($false))
    }
}
$taskFrame=Join-Path $taskRoot 'Assets/4. Asset/2. Tower/CubeConcepts/Tower_Frames_Atlas_v1.png'
$taskFiles=@($taskJobs | ForEach-Object {Join-Path $PSScriptRoot ('png/Tower_'+$_.group+'_'+$_.role+'_'+$_.name+'.png')})
$taskNames=@($taskJobs | ForEach-Object {$_.nameKo})
$taskEmblems=@($taskJobs | ForEach-Object {$_.groupKo})
[RemainingTowerPack]::Preview([string[]]$taskFiles,[string[]]$taskNames,[string[]]$taskEmblems,$taskFrame,(Join-Path $PSScriptRoot 'preview-all.png'))
foreach($taskRange in @(@(0,15,'weapons'),@(16,31,'elements'),@(32,47,'derived-elements'))) {
    $taskIndices=[int]$taskRange[0]..[int]$taskRange[1]
    [RemainingTowerPack]::Preview([string[]]$taskFiles[$taskIndices],[string[]]$taskNames[$taskIndices],[string[]]$taskEmblems[$taskIndices],$taskFrame,(Join-Path $PSScriptRoot ('preview-'+$taskRange[2]+'.png')))
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'asset-manifest.json'),($taskManifest | ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
$taskGeneration=@()
foreach($taskJob in $taskJobs){
    $taskBase='Tower_'+$taskJob.group+'_'+$taskJob.role+'_'+$taskJob.name
    $taskRecord=Join-Path $PSScriptRoot ('records/'+$taskBase+'.json')
    if(Test-Path -LiteralPath $taskRecord){$taskEntry=Get-Content -Encoding UTF8 -Raw -LiteralPath $taskRecord | ConvertFrom-Json}else{$taskEntry=$taskJob}
    $taskCorrection=Join-Path $PSScriptRoot ('records/'+$taskBase+'-correction.json')
    $taskGeneration += [ordered]@{name=$taskJob.nameKo;group=$taskJob.groupKo;role=$taskJob.role;mode='built-in image_gen';prompt=$taskEntry.prompt;selectedFile=('source/'+$taskBase+'.png');correction=$(if(Test-Path -LiteralPath $taskCorrection){Get-Content -Encoding UTF8 -Raw -LiteralPath $taskCorrection | ConvertFrom-Json}else{$null})}
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'generation-manifest.json'),($taskGeneration | ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
$taskCorrections=@(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'records') -Filter '*-correction.json')
$taskAudit=@(('Requested portraits: '+$taskJobs.Count),('Selected source PNGs: '+$taskFiles.Count),('Unity atlases: '+$taskGroups.Count),'Sprites per atlas: 4','Atlas dimensions: 512x512','Sprite dimensions: 256x256','Pixels per unit: 192','Pivot: 0.5,0.5','Transparent and opaque pixels verified in every source during packing',('Emblem corrections: '+$taskCorrections.Count),'Unity execution: not performed','Gameplay / prefab bindings: not changed')
[IO.File]::WriteAllLines((Join-Path $PSScriptRoot 'validation.txt'),[string[]]$taskAudit,[Text.UTF8Encoding]::new($false))
'Saved 48 transparent portraits, 12 Unity atlases (256px cells / PPU192), and four previews.'
