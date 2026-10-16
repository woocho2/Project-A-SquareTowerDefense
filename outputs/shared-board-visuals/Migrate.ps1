$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'manifest.json')){throw 'One-time migration is already applied. Do not rerun over edited palettes.'}
$common=Join-Path $repo 'Assets/4. Asset/0. Common'
$enemy=Join-Path $repo 'Assets/4. Asset/4. Enemy'
$floorRoot=Join-Path $repo 'Assets/4. Asset/1. BackGround'
$utf8=[Text.UTF8Encoding]::new($false)
$inv=[Globalization.CultureInfo]::InvariantCulture
function Save([string]$path,[string]$value){[IO.File]::WriteAllText($path,$value,$utf8)}
function Backup([string]$path){
    if(!(Test-Path -LiteralPath $path)){return}
    $relative=[IO.Path]::GetFullPath($path).Substring($repo.Length+1)
    $target=Join-Path $PSScriptRoot ('before/'+$relative)
    if(!(Test-Path -LiteralPath $target)){
        New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
        Copy-Item -LiteralPath $path -Destination $target
    }
}
function GuidOf([string]$path){return [regex]::Match([IO.File]::ReadAllText($path+'.meta'),'(?m)^guid: (\w+)').Groups[1].Value}
function Meta([string]$path,[string]$kind='asset'){
    if(Test-Path -LiteralPath ($path+'.meta')){return (GuidOf $path)}
    $id=[guid]::NewGuid().ToString('N')
    $body="fileFormatVersion: 2`nguid: $id`n"
    if($kind -eq 'folder'){$body+="folderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"}
    elseif($kind -eq 'script'){$body+="MonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {fileID: 0}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"}
    else{$body+="NativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"}
    Save ($path+'.meta') $body
    return $id
}
function ColorText([Drawing.Color]$c){
    return ('{r: '+($c.R/255.0).ToString('0.########',$inv)+', g: '+($c.G/255.0).ToString('0.########',$inv)+', b: '+($c.B/255.0).ToString('0.########',$inv)+', a: 1}')
}
function MatColor([string]$text,[string]$name){
    $match=[regex]::Match($text,'(?m)^    - '+$name+': (\{[^}]+\})')
    if(!$match.Success){throw ('Missing material color '+$name)}
    return $match.Groups[1].Value
}
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;
public static class CommonBoardArt {
 public static Color Sample(string path,int ox,int oy,int size,bool frame){
  using(var b=new Bitmap(path)){
   if(!frame)return b.GetPixel(ox+size/2,oy+size/2);
   var r=new System.Collections.Generic.List<int>();var g=new System.Collections.Generic.List<int>();var blue=new System.Collections.Generic.List<int>();
   for(int y=4;y<size/3;y++)for(int x=4;x<size/3;x++){var c=b.GetPixel(ox+x,oy+y);if(c.A<240)continue;r.Add(c.R);g.Add(c.G);blue.Add(c.B);}
   if(r.Count==0)throw new Exception("Empty frame sample");r.Sort();g.Sort();blue.Sort();return Color.FromArgb(r[r.Count/2],g[g.Count/2],blue[blue.Count/2]);
  }
 }
 public static void Normalize(string source,string destination,bool frame){
  using(var b=new Bitmap(source))using(var o=new Bitmap(b.Width,b.Height,PixelFormat.Format32bppArgb)){
   int min=255,max=0,top=b.Height,bottom=-1;
   for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){var c=b.GetPixel(x,y);if(c.A==0)continue;min=Math.Min(min,c.R);max=Math.Max(max,c.R);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
   for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){
    var c=b.GetPixel(x,y);int v=frame?(int)Math.Round(150+(c.R-min)*105.0/Math.Max(1,max-min)):(int)Math.Round(255-55.0*(y-top)/Math.Max(1,bottom-top));
    bool core=c.A>0;
    for(int dy=-1;dy<=1&&core;dy++)for(int dx=-1;dx<=1;dx++){
     int xx=x+dx,yy=y+dy;if(xx<0||xx>=b.Width||yy<0||yy>=b.Height||b.GetPixel(xx,yy).A==0){core=false;break;}
    }
    int alpha=core?255:c.A;
    v=Math.Max(frame?150:200,Math.Min(255,v));o.SetPixel(x,y,alpha==0?Color.FromArgb(0,0,0,0):Color.FromArgb(alpha,v,v,v));
   }
   o.Save(destination,ImageFormat.Png);
  }
 }
 public static void Arrow(string source,string destination){
  // Existing right-facing arrow only; strip its frame and gold RGB, preserve its antialiased shape.
  using(var b=new Bitmap(source))using(var o=new Bitmap(256,256,PixelFormat.Format32bppArgb)){
   for(int y=80;y<176;y++)for(int x=80;x<176;x++){var c=b.GetPixel(512+x,y);if(c.A>0)o.SetPixel(x,y,Color.FromArgb(c.A,255,255,255));}
   o.Save(destination,ImageFormat.Png);
  }
 }
}
'@
New-Item -ItemType Directory -Path $common -Force | Out-Null
$null=Meta $common 'folder'
$scriptPath=Join-Path $repo 'Assets/3. Scripts/08.Common/SharedBoardVisuals.cs'
$scriptGuid=Meta $scriptPath 'script'
$null=Meta (Join-Path $repo 'Assets/3. Scripts/04. Manager/TileManager.Visuals.cs') 'script'
$null=Meta (Join-Path $repo 'Assets/Editor/SharedBoardVisualsEditor.cs') 'script'
$settingsPath=Join-Path $repo 'Assets/Resources/SharedBoardVisuals.asset'
$settingsGuid=Meta $settingsPath

$realms=@('Asgard','Alfheim','Vanaheim','Midgard','Jotunheim','Nidavellir','Niflheim','Muspelheim','Hel','Ragnarok')
$states=@('Heal','Defense','Action','Mobility','Locked','Hourglass')
$characters=@('Valkyrie','Brunhild','Loki')
$spriteMap=@{}
$floorGuid=GuidOf (Join-Path $floorRoot 'Tile_Floor_Atlas.png')
$oldTowerBg=Join-Path $repo 'Assets/4. Asset/2. Tower/Tower_Backgrounds_Atlas_v1.png'
$oldTowerDec=Join-Path $repo 'Assets/4. Asset/2. Tower/Tower_Decorations_Atlas_v1.png'
$stageColors=@();$tierColors=@();$enemyColors=@{};$materialMap=@{};$retire=@()

# Capture current authored colors before any references are consolidated.
for($i=0;$i -lt $realms.Count;$i++){
    $cx=@(0,1,2,0,1,2,0,1,2,3)[$i]*256;$cy=@(0,0,0,1,1,1,2,2,2,0)[$i]*256
    $overlay=Join-Path $floorRoot ('Tile_Path_Overlay_'+$realms[$i]+'_Atlas.png')
    $stageColors+=@{spawn=(ColorText ([CommonBoardArt]::Sample((Join-Path $floorRoot 'Tile_Floor_Atlas.png'),$cx+1024,$cy,256,$false)));path=(ColorText ([CommonBoardArt]::Sample((Join-Path $floorRoot 'Tile_Floor_Atlas.png'),$cx,$cy,256,$false)));frame=(ColorText ([CommonBoardArt]::Sample($overlay,0,0,256,$true)))}
    $spriteMap[(GuidOf $overlay)]='decoration';$retire+=$overlay
}
for($i=0;$i -lt 6;$i++){
    $x=($i%3)*256;$y=[math]::Floor($i/3)*256
    $tierColors+=@{bg=(ColorText ([CommonBoardArt]::Sample($oldTowerBg,$x,$y,256,$false)));frame=(ColorText ([CommonBoardArt]::Sample($oldTowerDec,$x,$y,256,$true)))}
}
foreach($character in $characters){
    $enemyColors[$character]=@()
    foreach($state in $states){
        $mat=Join-Path $enemy "Asgard/${character}_${state}.mat"
        $text=[IO.File]::ReadAllText($mat)
        $enemyColors[$character]+=@{background=(MatColor $text '_BackgroundColor');equipment=(MatColor $text '_EquipmentColor')}
        Backup $mat;Backup ($mat+'.meta')
    }
}
$spriteMap[$floorGuid]='background';$spriteMap[(GuidOf $oldTowerBg)]='background';$spriteMap[(GuidOf $oldTowerDec)]='decoration'
$retire+=@(Join-Path $floorRoot 'Tile_Floor_Atlas.png');$retire+=$oldTowerBg;$retire+=$oldTowerDec

$bg=Join-Path $common 'Board_Background.png';$dec=Join-Path $common 'Board_Decoration.png';$arrow=Join-Path $common 'Board_Arrow.png'
foreach($pair in @(@('Enemy_Background.png',$bg),@('Enemy_Decoration.png',$dec))){
    $source=Join-Path $enemy $pair[0];Backup $source;Backup ($source+'.meta')
    if(!(Test-Path -LiteralPath $pair[1])){
        Move-Item -LiteralPath $source -Destination $pair[1]
        Move-Item -LiteralPath ($source+'.meta') -Destination ($pair[1]+'.meta')
    }
}
$bgGuid=GuidOf $bg;$decGuid=GuidOf $dec
foreach($path in @($bg,$dec)){
    $original=Join-Path $PSScriptRoot ('before/Assets/4. Asset/4. Enemy/'+$(if($path -eq $bg){'Enemy_Background.png'}else{'Enemy_Decoration.png'}))
    [CommonBoardArt]::Normalize($original,$path,($path -eq $dec))
    $meta=[IO.File]::ReadAllText($path+'.meta')
    $meta=$meta.Replace('  spriteMode: 0','  spriteMode: 1').Replace('  textureType: 0','  textureType: 8').Replace('  spriteMeshType: 0','  spriteMeshType: 1').Replace('  alphaIsTransparency: 0','  alphaIsTransparency: 1')
    Save ($path+'.meta') $meta
}
[CommonBoardArt]::Arrow((Join-Path $floorRoot 'Tile_Path_Overlay_Asgard_Atlas.png'),$arrow)
if(!(Test-Path -LiteralPath ($arrow+'.meta'))){
    $arrowGuid=[guid]::NewGuid().ToString('N')
    $meta=[IO.File]::ReadAllText($bg+'.meta').Replace($bgGuid,$arrowGuid).Replace('  spritePixelsToUnits: 384','  spritePixelsToUnits: 192')
    Save ($arrow+'.meta') $meta
}
$arrowGuid=GuidOf $arrow

function MakeTile([string]$name,[string]$spriteGuid){
    $path=Join-Path $common ($name+'.asset');$id=Meta $path
    $text=@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 13312, guid: 0000000000000000e000000000000000, type: 0}
  m_Name: $name
  m_EditorClassIdentifier: UnityEngine.dll::UnityEngine.Tilemaps.Tile
  m_Sprite: {fileID: 21300000, guid: $spriteGuid, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_Transform:
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1
  m_InstancedGameObject: {fileID: 0}
  m_Flags: 0
  m_ColliderType: 1
"@
    Save $path ($text+"`n");return $id
}
$bgTile=MakeTile 'Board_Background_Tile' $bgGuid
$decTile=MakeTile 'Board_Decoration_Tile' $decGuid
$arrowTile=MakeTile 'Board_Arrow_Tile' $arrowGuid
$tileMap=@{}
foreach($folder in @('Path','TowerSpawn','PathOverlay')){
    foreach($tile in Get-ChildItem -LiteralPath (Join-Path $floorRoot ('Tiles/'+$folder)) -Filter '*.asset'){
        $tileMap[(GuidOf $tile.FullName)]=$(if($folder -eq 'PathOverlay'){$decTile}else{$bgTile})
    }
}

$lines=[Collections.Generic.List[string]]::new()
$lines.Add(@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: SharedBoardVisuals
  m_EditorClassIdentifier: Assembly-CSharp::SharedBoardVisuals
  m_background: {fileID: 21300000, guid: $bgGuid, type: 3}
  m_decoration: {fileID: 21300000, guid: $decGuid, type: 3}
  m_arrow: {fileID: 21300000, guid: $arrowGuid, type: 3}
  m_backgroundTile: {fileID: 11400000, guid: $bgTile, type: 2}
  m_decorationTile: {fileID: 11400000, guid: $decTile, type: 2}
  m_arrowTile: {fileID: 11400000, guid: $arrowTile, type: 2}
  m_stages:
"@)
for($i=0;$i -lt $realms.Count;$i++){
    $c=$stageColors[$i]
    $lines.Add("  - realm: $i`n    displayName: $($realms[$i])`n    towerSpawn:`n      background: $($c.spawn)`n      decoration: $($c.frame)`n    path:`n      background: $($c.path)`n      decoration: $($c.frame)`n    arrow: $($c.frame)")
}
$lines.Add('  m_tiers:')
$tierNames=@('Bronze','Silver','Gold','Mithril','Diamond','Synergy Ruby')
for($i=0;$i -lt 6;$i++){$c=$tierColors[$i];$lines.Add("  - tier: $($i+1)`n    displayName: $($tierNames[$i])`n    colors:`n      background: $($c.bg)`n      decoration: $($c.frame)")}
$lines.Add('  m_towers: []');$lines.Add('  m_enemyStates:')
for($i=0;$i -lt 6;$i++){$c=$enemyColors['Valkyrie'][$i];$lines.Add("  - state: $i`n    background: $($c.background)`n    decoration: $($c.equipment)`n    equipment: $($c.equipment)")}
$lines.Add('  m_enemies:')
foreach($character in $characters){
    $appearance=GuidOf (Join-Path $enemy "Asgard/${character}_Appearance.asset")
    $different=@()
    for($i=0;$i -lt 6;$i++){
        $c=$enemyColors[$character][$i];$base=$enemyColors['Valkyrie'][$i]
        $a=@([regex]::Matches(($c.background+$c.equipment),'[rgba]: ([\d.]+)') | ForEach-Object {[double]::Parse($_.Groups[1].Value,$inv)})
        $b=@([regex]::Matches(($base.background+$base.equipment),'[rgba]: ([\d.]+)') | ForEach-Object {[double]::Parse($_.Groups[1].Value,$inv)})
        $same=$a.Count -eq $b.Count
        for($j=0;$same -and $j -lt $a.Count;$j++){if([math]::Abs($a[$j]-$b[$j]) -gt 0.000001){$same=$false}}
        if(!$same){$different+="    - state: $i`n      background: $($c.background)`n      decoration: $($c.equipment)`n      equipment: $($c.equipment)"}
    }
    if($different.Count -eq 0){$lines.Add("  - appearance: {fileID: 11400000, guid: $appearance, type: 2}`n    states: []")}
    else{$lines.Add("  - appearance: {fileID: 11400000, guid: $appearance, type: 2}`n    states:`n"+($different -join "`n"))}
}
if(!(Test-Path -LiteralPath $settingsPath)){Save $settingsPath (($lines -join "`n")+"`n")}

# Keep six shared state materials. Character selection and all colors now come from property blocks.
foreach($state in $states){
    $source=Join-Path $enemy "Asgard/Valkyrie_${state}.mat";$guid=GuidOf $source
    foreach($character in @('Brunhild','Loki')){$old=Join-Path $enemy "Asgard/${character}_${state}.mat";$materialMap[(GuidOf $old)]=$guid;$retire+=$old}
    $destination=Join-Path $common "Enemy_State_${state}.mat"
    $value=[IO.File]::ReadAllText($source).Replace("m_Name: Valkyrie_$state","m_Name: Enemy_State_$state")
    if($value -notmatch '_DecorationColor:'){$value=$value.Replace('    - _EquipmentColor:',('    - _DecorationColor: '+$enemyColors['Valkyrie'][[array]::IndexOf($states,$state)].equipment+"`n    - _EquipmentColor:"))}
    Move-Item -LiteralPath $source -Destination $destination
    Move-Item -LiteralPath ($source+'.meta') -Destination ($destination+'.meta')
    Save $destination $value
}

# Update serialized scene/prefab/brush dependencies, including cached tile sprite tables.
$white='{r: 1, g: 1, b: 1, a: 1}'
foreach($file in Get-ChildItem -LiteralPath (Join-Path $repo 'Assets') -File -Recurse | Where-Object {$_.Extension -in @('.asset','.prefab','.unity','.mat') -and $_.FullName -notmatch '[\\/]_Recovery[\\/]'}){
    if($file.FullName -eq $settingsPath){continue}
    $old=[IO.File]::ReadAllText($file.FullName);$value=$old
    foreach($id in $spriteMap.Keys){
        $newGuid=$(if($spriteMap[$id] -eq 'background'){$bgGuid}else{$decGuid})
        $value=[regex]::Replace($value,'\{fileID: -?\d+, guid: '+$id+', type: 3\}',('{fileID: 21300000, guid: '+$newGuid+', type: 3}'))
    }
    foreach($id in $tileMap.Keys){$value=$value.Replace('guid: '+$id+', type: 2','guid: '+$tileMap[$id]+', type: 2')}
    foreach($id in $materialMap.Keys){$value=$value.Replace('guid: '+$id+', type: 2','guid: '+$materialMap[$id]+', type: 2')}
    if($file.Name -eq 'TowerBase.prefab'){
        $value=[regex]::Replace($value,'(?m)^  m_backgroundSprites:\r?\n(?:  - .*\r?\n)+','  m_sharedVisuals: {fileID: 11400000, guid: '+$settingsGuid+", type: 2}`n")
        $value=[regex]::Replace($value,'(?m)^  m_decorationSprites:\r?\n(?:  - .*\r?\n)+','')
        $value=[regex]::Replace($value,'(?m)^  m_backgroundSprite: .*\r?\n','')
    }
    if($file.Extension -eq '.prefab' -and $value -match 'm_appearance:' -and $value -notmatch 'm_sharedVisuals:'){
        $value=[regex]::Replace($value,'(?m)^(  m_appearance: [^\r\n]+)','$1'+"`n  m_sharedVisuals: {fileID: 11400000, guid: $settingsGuid, type: 2}")
    }
    if($old -ne $value){Backup $file.FullName;Save $file.FullName $value}
}

function Block([string]$text,[long]$id){return [regex]::Match($text,'(?ms)^--- !u!\d+ &'+$id+'\r?\n.*?(?=^--- !u!|\z)').Value}
function ReplaceBlock([string]$text,[long]$id,[string]$value){return [regex]::Replace($text,'(?ms)^--- !u!\d+ &'+$id+'\r?\n.*?(?=^--- !u!|\z)',[Text.RegularExpressions.MatchEvaluator]{param($m) $value})}
function PaintMap([string]$block,[string]$color){
    return [regex]::Replace($block,'(?ms)(  m_TileColorArray:\r?\n)(.*?)(?=  m_TileObjectToInstantiateArray:)',[Text.RegularExpressions.MatchEvaluator]{param($m) $m.Groups[1].Value+([regex]::Replace($m.Groups[2].Value,'m_Data: \{[^}]+\}',('m_Data: '+$color)))})
}
$scenePath=Join-Path $repo 'Assets/1. Scenes/3. Stage1.unity';Backup $scenePath
$scene=[IO.File]::ReadAllText($scenePath)
$manager=Block $scene 296768362
$manager=$manager.Replace('  m_towerSpawnTilemap: {fileID: 1535280681}',("  m_towerSpawnTilemap: {fileID: 1535280681}`n  m_sharedVisuals: {fileID: 11400000, guid: $settingsGuid, type: 2}`n  m_stageRealm: 0`n  m_towerDecorationTilemap: {fileID: 1768091552}`n  m_pathArrowTilemap: {fileID: 1900009003}"))
$scene=ReplaceBlock $scene 296768362 $manager
$maps=@(@(1535280681,$stageColors[0].spawn),@(2102427781,$stageColors[0].path),@(1768091552,$stageColors[0].frame),@(561276725,$stageColors[0].frame))
foreach($entry in $maps){$scene=ReplaceBlock $scene $entry[0] (PaintMap (Block $scene $entry[0]) $entry[1])}

# Serialize an arrow layer now, so it is connected and visible before pressing Play.
if($scene -notmatch 'm_Name: Path Arrows'){
    $ids=@{561276721=1900009001;561276722=1900009002;561276725=1900009003;561276724=1900009004}
    $clones=@();foreach($id in @(561276721,561276722,561276725,561276724)){
        $copy=Block $scene $id;foreach($key in $ids.Keys){$copy=$copy.Replace([string]$key,[string]$ids[$key])};$clones+=$copy
    }
    $clones[0]=$clones[0].Replace('m_Name: Path Direction','m_Name: Path Arrows')
    $pathMatches=[regex]::Matches($manager,'(?m)^  - \{x: (-?\d+), y: (-?\d+), z: (-?\d+)\}')
    $tileLines=[Collections.Generic.List[string]]::new();$tileLines.Add('  m_Tiles:');$seen=@{}
    for($i=0;$i -lt $pathMatches.Count-1;$i++){
        $x=[int]$pathMatches[$i].Groups[1].Value;$y=[int]$pathMatches[$i].Groups[2].Value;$z=[int]$pathMatches[$i].Groups[3].Value
        $dx=[int]$pathMatches[$i+1].Groups[1].Value-$x;$dy=[int]$pathMatches[$i+1].Groups[2].Value-$y
        if(($dx -eq 0 -and $dy -eq 0) -or $seen.ContainsKey("$x,$y,$z")){continue};$seen["$x,$y,$z"]=$true
        $matrix=if($dx -gt 0){0}elseif($dy -gt 0){1}elseif($dx -lt 0){2}else{3}
        $tileLines.Add("  - first: {x: $x, y: $y, z: $z}`n    second:`n      serializedVersion: 2`n      m_TileIndex: 0`n      m_TileSpriteIndex: 0`n      m_TileMatrixIndex: $matrix`n      m_TileColorIndex: 0`n      m_TileObjectToInstantiateIndex: 65535`n      dummyAlignment: 0`n      m_AllTileFlags: 0")
    }
    $tileBlock=($tileLines -join "`n")+"`n"
    $count=$seen.Count
    $copy=$clones[2]
    $copy=[regex]::Replace($copy,'(?ms)^  m_Tiles:.*?(?=^  m_AnimatedTiles:)',$tileBlock)
    $copy=[regex]::Replace($copy,'(?ms)^  m_TileAssetArray:.*?(?=^  m_TileSpriteArray:)',"  m_TileAssetArray:`n  - m_RefCount: $count`n    m_Data: {fileID: 11400000, guid: $arrowTile, type: 2}`n")
    $copy=[regex]::Replace($copy,'(?ms)^  m_TileSpriteArray:.*?(?=^  m_TileMatrixArray:)',"  m_TileSpriteArray:`n  - m_RefCount: $count`n    m_Data: {fileID: 21300000, guid: $arrowGuid, type: 3}`n")
    $matrixLines="  m_TileMatrixArray:`n"
    foreach($rotation in @(@(1,0,0,1),@(0,-1,1,0),@(-1,0,0,-1),@(0,1,-1,0))){
        $matrixLines+="  - m_RefCount: $count`n    m_Data:`n"
        for($row=0;$row -lt 4;$row++){for($col=0;$col -lt 4;$col++){
            $v=0;if($row -eq $col){$v=1};if($row -lt 2 -and $col -lt 2){$v=$rotation[$row*2+$col]}
            $matrixLines+="      e${row}${col}: $v`n"
        }}
    }
    $copy=[regex]::Replace($copy,'(?ms)^  m_TileMatrixArray:.*?(?=^  m_TileColorArray:)',$matrixLines)
    $copy=[regex]::Replace($copy,'(?ms)^  m_TileColorArray:.*?(?=^  m_TileObjectToInstantiateArray:)',"  m_TileColorArray:`n  - m_RefCount: $count`n    m_Data: $($stageColors[0].frame)`n")
    $clones[2]=$copy
    $clones[3]=[regex]::Replace($clones[3],'(?m)^  m_SortingOrder: (\d+)',[Text.RegularExpressions.MatchEvaluator]{param($m) '  m_SortingOrder: '+([int]$m.Groups[1].Value+1)})
    $grid=Block $scene 1458002666
    $grid=$grid.Replace('  m_Children:',"  m_Children:`n  - {fileID: 1900009002}")
    $scene=ReplaceBlock $scene 1458002666 $grid
    $scene+=($clones -join '')
}
Save $scenePath $scene

# Archive replaced pixels/material duplicates outside Assets; preserve every GUID and original byte in the backup.
$activeFiles=@(Get-ChildItem -LiteralPath (Join-Path $repo 'Assets') -File -Recurse | Where-Object {$_.Extension -in @('.asset','.prefab','.unity','.mat') -and $_.FullName -notmatch '[\\/]_Recovery[\\/]'})
foreach($path in $retire){
    $id=GuidOf $path
    $references=@($activeFiles | Where-Object {$_.FullName -ne $path -and [IO.File]::ReadAllText($_.FullName).Contains($id)})
    if($references.Count -gt 0){throw ('Retired asset still referenced: '+$path+' from '+($references.FullName -join ', '))}
}
foreach($path in $retire){
    Backup $path;Backup ($path+'.meta')
    $target=Join-Path $PSScriptRoot ('retired/'+$path.Substring($repo.Length+1))
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($target)) -Force | Out-Null
    Move-Item -LiteralPath $path -Destination $target
    Move-Item -LiteralPath ($path+'.meta') -Destination ($target+'.meta')
}
$manifest=@{backgroundGuid=$bgGuid;decorationGuid=$decGuid;arrowGuid=$arrowGuid;settingsGuid=$settingsGuid;backgroundTile=$bgTile;decorationTile=$decTile;arrowTile=$arrowTile;retired=@($retire);stageColors=$stageColors;tierColors=$tierColors}
Save (Join-Path $PSScriptRoot 'manifest.json') ($manifest | ConvertTo-Json -Depth 8)
Write-Output ('Applied shared visuals. Retired duplicate textures/materials: '+$retire.Count)
