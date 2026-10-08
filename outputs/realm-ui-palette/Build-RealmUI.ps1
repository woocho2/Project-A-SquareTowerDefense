$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$utf8 = New-Object System.Text.UTF8Encoding($false)
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class UISpriteRect {
    public string Name;
    public long Id;
    public Rectangle Rect;
    public UISpriteRect(string name, long id, Rectangle rect) { Name=name; Id=id; Rect=rect; }
}
public static class RealmUIArt {
    public static List<UISpriteRect> Sprites(string meta, int imageHeight) {
        var result = new List<UISpriteRect>();
        string pattern = @"(?ms)^      name: ([^\r\n]+)\r?\n      rect:\r?\n        serializedVersion: 2\r?\n        x: (\d+)\r?\n        y: (\d+)\r?\n        width: (\d+)\r?\n        height: (\d+).*?^      internalID: (-?\d+)";
        foreach (Match m in Regex.Matches(meta,pattern)) {
            int x=int.Parse(m.Groups[2].Value), y=int.Parse(m.Groups[3].Value);
            int w=int.Parse(m.Groups[4].Value), h=int.Parse(m.Groups[5].Value);
            result.Add(new UISpriteRect(m.Groups[1].Value,long.Parse(m.Groups[6].Value),new Rectangle(x,imageHeight-y-h,w,h)));
        }
        if (result.Count == 0) throw new Exception("No sprite rectangles found.");
        return result;
    }
    public static double Luma(Color c) { return .299*c.R+.587*c.G+.114*c.B; }
    static int Byte(double value) { return (int)Math.Max(0,Math.Min(255,Math.Round(value))); }
    static Color Shade(Color c,double scale,int alpha) {
        if (scale > 1) return Color.FromArgb(alpha,Byte(c.R+(255-c.R)*(scale-1)),Byte(c.G+(255-c.G)*(scale-1)),Byte(c.B+(255-c.B)*(scale-1)));
        return Color.FromArgb(alpha,Byte(c.R*scale),Byte(c.G*scale),Byte(c.B*scale));
    }
    public static Bitmap Recolor(Bitmap input,Color target,double strength) {
        Bitmap result=input.Clone(new Rectangle(0,0,input.Width,input.Height),PixelFormat.Format32bppArgb);
        var data=result.LockBits(new Rectangle(0,0,result.Width,result.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
        byte[] pixels=new byte[data.Stride*data.Height]; Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
        long[] histogram=new long[256]; long count=0;
        for(int y=0;y<result.Height;y++) for(int x=0;x<result.Width;x++) {
            int p=y*data.Stride+x*4; if(pixels[p+3]<160) continue;
            histogram[Byte(.114*pixels[p]+.587*pixels[p+1]+.299*pixels[p+2])]++; count++;
        }
        if(count==0) throw new Exception("Empty artwork.");
        long accumulated=0; int median=1;
        for(int i=0;i<256;i++) { accumulated+=histogram[i]; if(accumulated>=count/2) { median=Math.Max(1,i); break; } }
        for(int y=0;y<result.Height;y++) for(int x=0;x<result.Width;x++) {
            int p=y*data.Stride+x*4; if(pixels[p+3]==0) continue;
            double l=.114*pixels[p]+.587*pixels[p+1]+.299*pixels[p+2];
            double scale=Math.Max(.35,Math.Min(1.5,1+strength*(l/median-1)));
            Color c=Shade(target,scale,pixels[p+3]); pixels[p]=c.B; pixels[p+1]=c.G; pixels[p+2]=c.R;
        }
        Marshal.Copy(pixels,0,data.Scan0,pixels.Length); result.UnlockBits(data); return result;
    }
    public static void RecolorBoard(string source,string dest,string[] realms,string[] lights,string[] darks,string[] accents) {
        using(var input=new Bitmap(source)) using(var result=input.Clone(new Rectangle(0,0,input.Width,input.Height),PixelFormat.Format32bppArgb)) {
            var sprites=Sprites(File.ReadAllText(source+".meta"),input.Height);
            Rectangle reference=sprites.Find(s=>s.Name=="Board_Asgard_TL").Rect;
            for(int i=0;i<realms.Length;i++) {
                Rectangle origin=sprites.Find(s=>s.Name=="Board_"+realms[i]+"_TL").Rect;
                Color light=ColorTranslator.FromHtml(lights[i]), dark=ColorTranslator.FromHtml(darks[i]), accent=ColorTranslator.FromHtml(accents[i]);
                for(int y=0;y<576;y++) for(int x=0;x<576;x++) {
                    Color old=input.GetPixel(origin.X+x,origin.Y+y); if(old.A==0) continue;
                    Color c=input.GetPixel(reference.X+x,reference.Y+y);
                    Color target; double scale;
                    if(c.R>c.B+35 && c.G>c.B+25) { target=accent; scale=Math.Max(.45,Math.Min(1.4,Luma(c)/175)); }
                    else if(Luma(c)<160) { target=dark; scale=Math.Max(.45,Math.Min(1.2,Luma(c)/50)); }
                    else { target=light; scale=1+.2*(Luma(c)/245-1); }
                    result.SetPixel(origin.X+x,origin.Y+y,Shade(target,scale,old.A));
                }
            }
            result.Save(dest,ImageFormat.Png);
        }
    }
    public static void SaveCrop(Bitmap source,Rectangle rect,Color target,double strength,string path) {
        using(var crop=source.Clone(rect,PixelFormat.Format32bppArgb)) using(var result=Recolor(crop,target,strength)) result.Save(path,ImageFormat.Png);
    }
    public static void CheckAlpha(string source,string dest) {
        using(var a=new Bitmap(source)) using(var b=new Bitmap(dest)) {
            if(a.Size!=b.Size) throw new Exception("Image size changed: "+dest);
            for(int y=0;y<a.Height;y++) for(int x=0;x<a.Width;x++) if(a.GetPixel(x,y).A!=b.GetPixel(x,y).A) throw new Exception("Alpha changed: "+dest);
        }
    }
    public static string ColorAt(string path,Rectangle rect) {
        using(var b=new Bitmap(path)) return ColorTranslator.ToHtml(b.GetPixel(rect.X+rect.Width/2,rect.Y+rect.Height/2));
    }
    public static void Preview(string repo,string dest,string[] realms,string[] labels) {
        string ui=Path.Combine(repo,"Assets/4. Asset/5. UI/Themes");
        using(var canvas=new Bitmap(1600,1250,PixelFormat.Format32bppArgb)) using(var g=Graphics.FromImage(canvas))
        using(var title=new Font("Malgun Gothic",17,FontStyle.Bold)) using(var small=new Font("Malgun Gothic",11)) {
            g.Clear(ColorTranslator.FromHtml("#19212C"));
            g.DrawString("타일 · 보드 · UI / 동일한 월드 색상",title,Brushes.White,25,15);
            using(var board=new Bitmap(Path.Combine(repo,"Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png"))) {
                var boardSprites=Sprites(File.ReadAllText(Path.Combine(repo,"Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png.meta")),board.Height);
                for(int i=0;i<realms.Length;i++) {
                    int left=25+(i%5)*315, top=65+(i/5)*585;
                    g.DrawString(labels[i],title,Brushes.White,left,top);
                    var origin=boardSprites.Find(s=>s.Name=="Board_"+realms[i]+"_TL").Rect;
                    g.DrawImage(board,new Rectangle(left,top+35,285,285),new Rectangle(origin.X,origin.Y,576,576),GraphicsUnit.Pixel);
                    string[] layers={"A","B","C"};
                    for(int layer=0;layer<3;layer++) {
                        int y=top+315+layer*67;
                        g.DrawString(layer==0 ? "밝은 바탕" : layer==1 ? "강조 색상" : "짙은 바탕",small,Brushes.LightGray,left,y);
                        using(var bg=new Bitmap(Path.Combine(ui,realms[i],"Simple",realms[i]+"_"+layers[layer]+"_3x1.png")))
                        using(var frame=new Bitmap(Path.Combine(ui,realms[i],"Simple",realms[i]+"_Frame_3x1.png"))) {
                            g.DrawImage(bg,new Rectangle(left,y+24,270,45)); g.DrawImage(frame,new Rectangle(left,y+24,270,45));
                        }
                    }
                }
            }
            canvas.Save(dest,ImageFormat.Png);
        }
    }
}
'@

function Backup-Asset([string]$relative) {
    $source = Join-Path $repo $relative
    $backup = Join-Path $PSScriptRoot ('source/'+$relative)
    if (!(Test-Path -LiteralPath $backup)) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
        Copy-Item -LiteralPath $source -Destination $backup
        if (Test-Path -LiteralPath ($source+'.meta')) { Copy-Item -LiteralPath ($source+'.meta') -Destination ($backup+'.meta') }
    }
    return $backup
}
function Get-Guid([string]$path) {
    return [regex]::Match([System.IO.File]::ReadAllText($path),'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
}
function New-StableGuid([string]$path) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($path.Replace('\','/'))))).Replace('-','').Substring(0,32).ToLowerInvariant() }
    finally { $sha.Dispose() }
}
function Add-FolderMeta([string]$relative) {
    $path = Join-Path $repo $relative
    New-Item -ItemType Directory -Force -Path $path | Out-Null
    if (!(Test-Path -LiteralPath ($path+'.meta'))) {
        $guid = New-StableGuid $relative
        [IO.File]::WriteAllText(($path+'.meta'),"fileFormatVersion: 2`nguid: $guid`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n",$utf8)
    }
}

$realms = @('Asgard','Alfheim','Vanaheim','Midgard','Jotunheim','Nidavellir','Niflheim','Muspelheim','Hel','Ragnarok')
$labels = @('아스가르드','알프헤임','바나헤임','미드가르드','요툰헤임','니다벨리르','니플헤임','무스펠헤임','헬','라그나로크')
$accents = @('#ECBB41','#A56E37','#A4BE8F','#8FB6C8','#9DA6B0','#AB71D9','#AEC2D4','#D9A996','#43BEAE','#B6A681')
$floorRelative = 'Assets/4. Asset/1. BackGround/Tile_Floor_Atlas.png'
$floorPath = Join-Path $repo $floorRelative
$floorImage = New-Object System.Drawing.Bitmap($floorPath)
try { $floorSprites = [RealmUIArt]::Sprites([IO.File]::ReadAllText($floorPath+'.meta'),$floorImage.Height) } finally { $floorImage.Dispose() }
$lights = @(); $darks = @(); $palette = @()
for($i=0;$i -lt $realms.Count;$i++) {
    $lightRect = $floorSprites.Find([Predicate[UISpriteRect]]{param($s) $s.Name -eq ('Tile_TowerSpawn_'+$realms[$i])}).Rect
    $darkRect = $floorSprites.Find([Predicate[UISpriteRect]]{param($s) $s.Name -eq ('Tile_Path_'+$realms[$i])}).Rect
    $lights += [RealmUIArt]::ColorAt($floorPath,$lightRect)
    $darks += [RealmUIArt]::ColorAt($floorPath,$darkRect)
    $palette += [PSCustomObject]@{ realm=$realms[$i]; label=$labels[$i]; light=$lights[$i]; dark=$darks[$i]; accent=$accents[$i] }
}
$palette | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'palette.json') -Encoding UTF8

$boardRelative = 'Assets/4. Asset/1. BackGround/Tile_BoardFrame_All_Atlas.png'
$boardSource = Backup-Asset $boardRelative
$boardDest = Join-Path $repo $boardRelative
[RealmUIArt]::RecolorBoard($boardSource,$boardDest,$realms,$lights,$darks,$accents)
[RealmUIArt]::CheckAlpha($boardSource,$boardDest)
Write-Output 'Board: ten realm palettes applied; sprite geometry, alpha and metadata preserved.'

$templateMeta = [IO.File]::ReadAllText((Join-Path $repo 'Assets/4. Asset/3. Projectile/Cartoon/Bullet/Bullet_Sword.png.meta'))
$shapes = @(
    @{key='1x1'; suffix='Btn_128'; width=128; height=128},
    @{key='1x2'; suffix='Capsule_128x256'; width=128; height=256},
    @{key='1x3'; suffix='Gauge_128x384'; width=128; height=384},
    @{key='1x4'; suffix='TallBanner_128x512'; width=128; height=512},
    @{key='2x3'; suffix='CharCard_256x384'; width=256; height=384},
    @{key='2x1'; suffix='Capsule_256x128'; width=256; height=128},
    @{key='3x1'; suffix='Capsule_384x128'; width=384; height=128},
    @{key='4x1'; suffix='Capsule_512x128'; width=512; height=128},
    @{key='3x2'; suffix='SkillCard_384x256'; width=384; height=256},
    @{key='1x1_Sliced'; suffix='Btn_128_B'; width=128; height=128}
)
$layers = @('A','B','C','Frame')
$assets = @(); $atlasLookup = @{}
for($realmIndex=0;$realmIndex -lt 10;$realmIndex++) {
    $realm = $realms[$realmIndex]
    Add-FolderMeta "Assets/4. Asset/5. UI/Themes/$realm/Simple"
    Add-FolderMeta "Assets/4. Asset/5. UI/Themes/$realm/Sliced"
    foreach($layer in $layers) {
        $templateRelative = "Assets/4. Asset/5. UI/Themes/Asgard/Asgard_Atlas_$layer.png"
        $sourcePath = Backup-Asset $templateRelative
        $image = New-Object System.Drawing.Bitmap($sourcePath)
        try {
            $sprites = [RealmUIArt]::Sprites([IO.File]::ReadAllText($sourcePath+'.meta'),$image.Height)
            $colorHex = switch($layer) { 'A' { $lights[$realmIndex] } 'B' { $accents[$realmIndex] } 'C' { $darks[$realmIndex] } 'Frame' { $accents[$realmIndex] } }
            $color = [Drawing.ColorTranslator]::FromHtml($colorHex)
            $strength = if($layer -eq 'Frame') { 0.8 } else { 0.12 }
            foreach($shape in $shapes) {
                $sprite = $sprites.Find([Predicate[UISpriteRect]]{param($s) $s.Name -eq "Asgard_${layer}_$($shape.suffix)"})
                if($null -eq $sprite -or $sprite.Rect.Width -ne $shape.width -or $sprite.Rect.Height -ne $shape.height) { throw "Unexpected source geometry: $layer $($shape.key)" }
                $kind = if($shape.key -eq '1x1_Sliced') { 'Sliced' } else { 'Simple' }
                $relative = "Assets/4. Asset/5. UI/Themes/$realm/$kind/${realm}_${layer}_$($shape.key).png"
                $dest = Join-Path $repo $relative
                [RealmUIArt]::SaveCrop($image,$sprite.Rect,$color,$strength,$dest)
                $guid = if(Test-Path -LiteralPath ($dest+'.meta')) { Get-Guid ($dest+'.meta') } else { New-StableGuid $relative }
                $meta = [regex]::Replace($templateMeta,'(?m)^guid: [a-f0-9]{32}',"guid: $guid")
                $meta = $meta.Replace('spritePixelsToUnits: 1254','spritePixelsToUnits: 128').Replace('maxTextureSize: 2048','maxTextureSize: 512')
                if($kind -eq 'Sliced') { $meta = $meta.Replace('spriteBorder: {x: 0, y: 0, z: 0, w: 0}','spriteBorder: {x: 48, y: 48, z: 48, w: 48}') }
                $meta = [regex]::Replace($meta,'(?m)^  spriteID: [a-f0-9]{32}',('  spriteID: '+(New-StableGuid ($relative+':sprite'))))
                [IO.File]::WriteAllText(($dest+'.meta'),$meta,$utf8)
                $assets += [PSCustomObject]@{ realm=$realm; key=($layer+'_'+$shape.key); path=$relative; guid=$guid; width=$shape.width; height=$shape.height; imageType=$kind; color=$colorHex }
            }
        } finally { $image.Dispose() }
        # Preserve GUIDs and every sub-sprite rectangle in legacy atlases, including existing menu references.
        $legacyRelative = "Assets/4. Asset/5. UI/Themes/$realm/${realm}_Atlas_$layer.png"
        $legacySource = Backup-Asset $legacyRelative
        $legacyImage = New-Object System.Drawing.Bitmap($legacySource)
        try {
            $legacySprites = [RealmUIArt]::Sprites([IO.File]::ReadAllText($legacySource+'.meta'),$legacyImage.Height)
            $atlasLookup[(Get-Guid ($legacySource+'.meta'))] = @{ realm=$realm; layer=$layer; sprites=$legacySprites }
            $recolored = [RealmUIArt]::Recolor($legacyImage,$color,$strength)
            try { $recolored.Save((Join-Path $repo $legacyRelative),[Drawing.Imaging.ImageFormat]::Png) } finally { $recolored.Dispose() }
        } finally { $legacyImage.Dispose() }
        if($realm -eq 'Asgard') {
            $oldName = switch($layer) { 'A' {'Ivory'} 'B' {'Gold'} 'C' {'Night'} 'Frame' {'Frame'} }
            $oldRelative = "Assets/4. Asset/5. UI/Asgard/Asgard_Atlas_$oldName.png"
            $oldSource = Backup-Asset $oldRelative
            $oldImage = New-Object System.Drawing.Bitmap($oldSource)
            try {
                $oldSprites = [RealmUIArt]::Sprites([IO.File]::ReadAllText($oldSource+'.meta'),$oldImage.Height)
                $atlasLookup[(Get-Guid ($oldSource+'.meta'))] = @{ realm=$realm; layer=$layer; sprites=$oldSprites }
                $recolored = [RealmUIArt]::Recolor($oldImage,$color,$strength)
                try { $recolored.Save((Join-Path $repo $oldRelative),[Drawing.Imaging.ImageFormat]::Png) } finally { $recolored.Dispose() }
            } finally { $oldImage.Dispose() }
        }
    }
    Write-Output "${realm}: nine Simple ratios and one Sliced square, four layers each."
}
$assets | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'assets.json') -Encoding UTF8

$canvasRelative = 'Assets/2. Prefab/Canvas.prefab'
$canvasSource = Backup-Asset $canvasRelative
$canvas = [IO.File]::ReadAllText($canvasSource)
$converted = 0
$canvas = [regex]::Replace($canvas,'(?ms)^--- !u!114 &\d+\r?\n.*?(?=^---|\z)',[System.Text.RegularExpressions.MatchEvaluator]{ param($match)
    $block = $match.Value
    if(!$block.Contains('guid: fe87c0e1cc204ed48ad3b37840f39efc')) { return $block }
    $spriteMatch = [regex]::Match($block,'m_Sprite: \{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: 3\}')
    if(!$spriteMatch.Success -or !$atlasLookup.ContainsKey($spriteMatch.Groups[2].Value)) { return $block }
    $sourceAtlas = $atlasLookup[$spriteMatch.Groups[2].Value]
    $id = [long]$spriteMatch.Groups[1].Value
    $sprite = $sourceAtlas.sprites.Find([Predicate[UISpriteRect]]{param($s) $s.Id -eq $id})
    if($null -eq $sprite) { throw "Unknown source sprite ID: $id" }
    $oldType = [int][regex]::Match($block,'m_Type: (\d+)').Groups[1].Value
    $w=$sprite.Rect.Width; $h=$sprite.Rect.Height
    if($w -eq $h) { $shapeKey = if($oldType -eq 1 -or $w -gt 128 -or $sprite.Name.EndsWith('Btn_128_B')) { '1x1_Sliced' } else { '1x1' } }
    else { $shapeKey = [string]([int]($w/128))+'x'+[string]([int]($h/128)); if($shapeKey -eq '6x1') { $shapeKey='4x1' }; if($shapeKey -eq '1x6') { $shapeKey='1x4' } }
    $key=$sourceAtlas.layer+'_'+$shapeKey
    $asset = $assets | Where-Object { $_.realm -eq $sourceAtlas.realm -and $_.key -eq $key } | Select-Object -First 1
    if($null -eq $asset) { throw "Missing ratio asset: $key" }
    $newRef = "m_Sprite: {fileID: 21300000, guid: $($asset.guid), type: 3}"
    $block=$block.Replace($spriteMatch.Value,$newRef)
    if($oldType -ne 3) { $newType=if($shapeKey.EndsWith('_Sliced')) { 1 } else { 0 }; $block=[regex]::Replace($block,'(?m)^  m_Type: \d+',"  m_Type: $newType") }
    $script:converted++
    return $block
})

$canvasComponent = [regex]::Match($canvas,'(?ms)^--- !u!223 &(\d+)\r?\nCanvas:.*?^  m_GameObject: \{fileID: (\d+)\}')
if(!$canvasComponent.Success) { throw 'Canvas component not found.' }
$rootId = $canvasComponent.Groups[2].Value
$themeId = '910100010001099'
if($canvas.Contains('&'+$themeId)) { throw 'Theme component ID collision.' }
$rootPattern = '(?ms)^--- !u!1 &'+$rootId+'\r?\nGameObject:.*?(?=^---|\z)'
$eol = if($canvas.Contains("`r`n")) { "`r`n" } else { "`n" }
$canvas = [regex]::Replace($canvas,$rootPattern,[System.Text.RegularExpressions.MatchEvaluator]{ param($match) return $match.Value.Replace('  m_Layer:',("  - component: {fileID: $themeId}"+$eol+'  m_Layer:')) })
$theme = @(
    "--- !u!114 &$themeId", 'MonoBehaviour:', '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}',
    '  m_PrefabInstance: {fileID: 0}', '  m_PrefabAsset: {fileID: 0}', "  m_GameObject: {fileID: $rootId}",
    '  m_Enabled: 1', '  m_EditorHideFlags: 0', '  m_Script: {fileID: 11500000, guid: 42abc7f6d3ac4c40a467fb007ee45f49, type: 3}',
    '  m_Name:', '  m_EditorClassIdentifier: Assembly-CSharp::UIStageThemeController', '  currentRealm: 0',
    "  targetCanvas: {fileID: $($canvasComponent.Groups[1].Value)}", '  bgKeyword: _bg', '  frameKeyword: _frame',
    '  useThemeSprites: 1', '  realmSprites:'
)
for($i=0;$i -lt $realms.Count;$i++) {
    $theme += "  - realm: $i"
    $theme += "    displayName: $($labels[$i]) ($($realms[$i]))"
    foreach($field in @('panelMainFrame','panelMainBG','btnHighFrame','btnHighBG','btnMiddleFrame','btnMiddleBG','btnLowFrame','btnLowBG')) { $theme += "    ${field}: {fileID: 0}" }
    $theme += '    ratioSprites:'
    foreach($asset in ($assets | Where-Object { $_.realm -eq $realms[$i] })) { $theme += "    - key: $($asset.key)"; $theme += "      sprite: {fileID: 21300000, guid: $($asset.guid), type: 3}" }
}
$theme += @('  preserveOriginalAlpha: 1','  useCustomColors: 0','  customThemes: []','  towerInfoBackground: {fileID: 0}','  towerInfoBackgroundBrightness: 0.35','  autoTextContrast: 0','  textContrastThreshold: 0.55')
$canvas += ($theme -join $eol)+$eol
[IO.File]::WriteAllText((Join-Path $repo $canvasRelative),$canvas,$utf8)
Write-Output "Canvas: $converted images rewired; all ten realm sprite sets serialized for player builds."
[RealmUIArt]::Preview($repo,(Join-Path $PSScriptRoot 'preview.png'),$realms,$labels)
Write-Output 'Preview saved to outputs/realm-ui-palette/preview.png'
