$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8=[Text.UTF8Encoding]::new($false)
function Read([string]$path){[IO.File]::ReadAllText((Join-Path $repoRoot $path))}
function Save([string]$path,[string]$value){[IO.File]::WriteAllText((Join-Path $repoRoot $path),$value,$utf8)}
function Assert([bool]$value,[string]$message){if(!$value){throw $message}}
function Blocks([string]$yaml){[regex]::Matches($yaml,'(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')}
function OneBlock([string]$yaml,[string]$type){$b=@(Blocks $yaml | Where-Object {$_.Groups['type'].Value -eq $type});Assert ($b.Count -eq 1) "Expected one block of type $type";$b[0].Value}
function SetLine([string]$text,[string]$key,[string]$value,[int]$indent=2){
 $pattern='(?m)^'+(' '*$indent)+[regex]::Escape($key)+':[^\r\n]*'
 Assert ([regex]::Matches($text,$pattern).Count -eq 1) "Expected one field: $key"
 [regex]::Replace($text,$pattern,((' '*$indent)+$key+': '+$value))
}
function Section([string]$text,[string]$key,[int]$indent){
 $space=' '*$indent
 $m=[regex]::Match($text,'(?ms)^'+$space+[regex]::Escape($key)+':\r?\n.*?(?=^'+$space+'[A-Za-z_]|\z)')
 Assert $m.Success "Missing section: $key"
 $m.Value
}
function MinMax([string]$text,[string]$key,[string]$min,[string]$max,[int]$state=3){
 $old=Section $text $key 4
 $new=SetLine $old 'minMaxState' ([string]$state) 6
 $new=SetLine $new 'scalar' $max 6
 $new=SetLine $new 'minScalar' $min 6
 $text.Replace($old,$new)
}
function Rehome([string]$block,[string]$type,[string]$id,[string]$goId){
 $block=[regex]::Replace($block,'(?m)^--- !u!'+$type+' &-?\d+','--- !u!'+$type+' &'+$id)
 SetLine $block 'm_GameObject' ('{fileID: '+$goId+'}')
}
function RenderSettings([string]$block){
 $block=SetLine $block 'm_SortingLayerID' '-1018217311'
 $block=SetLine $block 'm_SortingLayer' '4'
 $block=SetLine $block 'm_SortingOrder' '-1'
 $block=SetLine $block 'm_CastShadows' '0'
 SetLine $block 'm_ReceiveShadows' '0'
}

Assert (!(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'baseline.json'))) 'Already prepared; do not run twice'
$prefabPath='Assets/2. Prefab/4. Effect/Projectile.prefab'
$catalogPath='Assets/2. Prefab/4. Effect/ProjectileCatalog.asset'
foreach($path in @($prefabPath,$catalogPath,$prefabPath+'.meta',$catalogPath+'.meta')){Assert (!(Test-Path -LiteralPath (Join-Path $repoRoot $path))) ('Already exists: '+$path)}
$clips=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/4. Asset/3. Projectile/Target') -Filter '*_Projectile_Animation_*.anim')
Assert ($clips.Count -eq 28) 'Expected 28 blue animation clips'
$ids=@(101..114)+@(201..214)
$sources=@{}
$rows=[Collections.Generic.List[object]]::new()
foreach($id in $ids){
 $path='Assets/Resources/Projectiles/'+$id+'.prefab'
 $s=Read $path; $sources[$id]=$s
 $sr=OneBlock $s '212'; $anim=OneBlock $s '95'
 $color=[regex]::Match($s,'(?m)^  m_prefabEffectColor: (?<value>[^\r\n]+)').Groups['value'].Value
 $sprite=[regex]::Match($sr,'(?m)^  m_Sprite: (?<value>[^\r\n]+)').Groups['value'].Value
 $controller=[regex]::Match($anim,'(?m)^  m_Controller: (?<value>[^\r\n]+)').Groups['value'].Value
 Assert ($color -and $sprite -and $controller) ('Missing source look: '+$id)
 $rows.Add([pscustomobject]@{Id=$id;Sprite=$sprite;EffectColor=$color;Animation=$controller})
}
$scriptGuid=[regex]::Match((Read 'Assets/3. Scripts/New/08. Projectile/ProjectileCatalog.cs.meta'),'(?m)^guid: ([a-f0-9]{32})').Groups[1].Value
Assert ($scriptGuid.Length -eq 32) 'Catalog script has not been imported'
$splash=Read 'Assets/4. Asset/3. Projectile/Splash/Shared_Orb_Projectile_Animation_Idle.anim'
$orbHash=[regex]::Match($splash,'(?m)^      path: (\d+)').Groups[1].Value
Assert ($orbHash -eq '1222197203') 'Unexpected OrbVisual binding hash'
$clipChanges=[Collections.Generic.List[object]]::new()
foreach($clip in $clips){
 $s=[IO.File]::ReadAllText($clip.FullName)
 $curves=@([regex]::Matches($s,'(?m)^    attribute: ([^\r\n]+)') | ForEach-Object {$_.Groups[1].Value})
 Assert ($curves.Count -eq 1 -and $curves[0] -eq 'm_Sprite') ('Unexpected curves: '+$clip.Name)
 foreach($field in @('Rotation','CompressedRotation','Euler','Position','Scale','Float','Editor','EulerEditor')){
  Assert ($s -match ('(?m)^  m_'+$field+'Curves: \[\]\r?$')) ('Unexpected non-sprite curves: '+$clip.Name)
 }
 $curvePath='(?m)(^    attribute: m_Sprite\r?\n)    path: (?:""|)[ \t]*(?=\r?$)'
 Assert ([regex]::Matches($s,$curvePath).Count -eq 1) ('Unexpected sprite path: '+$clip.Name)
 $new=[regex]::Replace($s,$curvePath,'${1}    path: OrbVisual')
 Assert ([regex]::Matches($new,'(?m)^      path: 0\r?$').Count -eq 1) ('Unexpected binding count: '+$clip.Name)
 $new=[regex]::Replace($new,'(?m)^      path: 0(?=\r?$)',('      path: '+$orbHash))
 $clipChanges.Add([pscustomobject]@{Path=$clip.FullName.Substring($repoRoot.Length+1);Before=$s;After=$new})
}

# Retain the complete Unity-serialized module schema from an existing prefab.
$template=Read 'Assets/2. Prefab/MapBuff.prefab'
$templateBlocks=Blocks $template
$trail=($templateBlocks | Where-Object {$_.Groups['type'].Value -eq '96'} | Select-Object -First 1).Value
$ps=($templateBlocks | Where-Object {$_.Groups['type'].Value -eq '198'} | Select-Object -First 1).Value
$psRenderer=($templateBlocks | Where-Object {$_.Groups['type'].Value -eq '199'} | Select-Object -First 1).Value
$trail=RenderSettings (Rehome $trail '96' '9600000' '100000')
$trail=SetLine $trail 'm_Time' '0.2'
$trail=SetLine $trail 'm_MinVertexDistance' '0.04'
$trail=SetLine $trail 'm_Autodestruct' '0'
$trail=SetLine $trail 'm_Emitting' '0'
$trail=SetLine $trail 'widthMultiplier' '1' 4
$curve=Section $trail 'widthCurve' 4
$keys=@([regex]::Matches($curve,'(?ms)^      - serializedVersion: 3\r?\n.*?(?=^      - |^      m_|\z)'))
Assert ($keys.Count -eq 2) 'Expected two trail width keys'
$curveNew=$curve
for($i=0;$i -lt 2;$i++){
 $key=$keys[$i].Value
 $newKey=SetLine $key 'value' (@('0.38','0')[$i]) 8
 $newKey=SetLine $newKey 'inSlope' '0' 8
 $newKey=SetLine $newKey 'outSlope' '0' 8
 $curveNew=$curveNew.Replace($key,$newKey)
}
$trail=$trail.Replace($curve,$curveNew)
$ps=Rehome $ps '198' '19800000' '100000'
$ps=SetLine $ps 'moveWithTransform' '0'
$ps=SetLine $ps 'looping' '1'
$ps=SetLine $ps 'playOnAwake' '0'
$initial=Section $ps 'InitialModule' 2
$initialNew=MinMax $initial 'startLifetime' '0.18' '0.28'
$initialNew=MinMax $initialNew 'startSpeed' '0.1' '0.35'
$initialNew=MinMax $initialNew 'startSize' '0.06' '0.12'
$initialNew=SetLine $initialNew 'maxNumParticles' '50' 4
$initialNew=SetLine $initialNew 'size3D' '0' 4
$startColor=Section $initialNew 'startColor' 4
$initialNew=$initialNew.Replace($startColor,(SetLine $startColor 'minMaxState' '0' 6))
$ps=$ps.Replace($initial,$initialNew)
$emission=Section $ps 'EmissionModule' 2
$emissionNew=MinMax $emission 'rateOverTime' '0' '0' 0
$emissionNew=MinMax $emissionNew 'rateOverDistance' '14' '14' 0
$ps=$ps.Replace($emission,$emissionNew)
$shape=Section $ps 'ShapeModule' 2
$radius=Section $shape 'radius' 4
$shapeNew=$shape.Replace($radius,(SetLine $radius 'value' '0.1' 6))
$shapeNew=SetLine $shapeNew 'type' '0' 4
$ps=$ps.Replace($shape,$shapeNew)
foreach($module in @('InitialModule','EmissionModule','ShapeModule','ColorModule','SizeModule')){
 $old=Section $ps $module 2
 $ps=$ps.Replace($old,(SetLine $old 'enabled' '1' 4))
}
$psRenderer=RenderSettings (Rehome $psRenderer '199' '19900000' '100000')
$anim=Rehome (OneBlock $sources[101] '95') '95' '9500000' '100000'
$anim=SetLine $anim 'm_Controller' '{fileID: 0}'
$anim=SetLine $anim 'm_CullingMode' '0'
$sr=Rehome (OneBlock $sources[101] '212') '212' '21200000' '100001'
Assert ($sr.Contains('guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}')) 'Unexpected sprite material'
$sr=SetLine $sr 'm_SortingLayerID' '-1018217311'
$sr=SetLine $sr 'm_SortingLayer' '4'
$sr=SetLine $sr 'm_SortingOrder' '0'
$root=@'
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000}
  - component: {fileID: 9500000}
  - component: {fileID: 9600000}
  - component: {fileID: 19800000}
  - component: {fileID: 19900000}
  m_Layer: 7
  m_Name: Projectile
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 0.5, y: 0.5, z: 0.5}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 400001}
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'@
$child=@'
--- !u!1 &100001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400001}
  - component: {fileID: 21200000}
  m_Layer: 7
  m_Name: OrbVisual
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 400000}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'@
$prefab=(@($root,$anim,$trail,$ps,$psRenderer,$child,$sr) | ForEach-Object {$_.TrimEnd("`r","`n")}) -join "`n"
$prefab=$prefab.Replace("`r`n","`n")+"`n"
$catalog=@"
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
  m_Name: ProjectileCatalog
  m_EditorClassIdentifier: Assembly-CSharp::SquareTD.ProjectileCatalog
  m_entries:
"@
foreach($row in $rows){$catalog += "`n  - Id: $($row.Id)`n    Sprite: $($row.Sprite)`n    EffectColor: $($row.EffectColor)`n    Animation: $($row.Animation)"}
$catalog=$catalog.Replace("`r`n","`n")+"`n"

$protected=@(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets') -Filter '*.cs' -Recurse)
$protected+=Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/Resources/Projectiles') -File
$protected+=Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/4. Asset/3. Projectile/Splash') -Filter '*.anim*'
$protected+=@($clips | ForEach-Object {Get-Item -LiteralPath ($_.FullName+'.meta')})
$baseline=@($protected | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($repoRoot.Length+1);Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'baseline.json'),(ConvertTo-Json -InputObject $baseline -Depth 5),$utf8)
foreach($change in $clipChanges){
 Assert ((Read $change.Path) -ceq $change.Before) ('Clip changed during work: '+$change.Path)
 $backup=Join-Path $PSScriptRoot ('before/'+$change.Path)
 [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
 [IO.File]::Copy((Join-Path $repoRoot $change.Path),$backup)
}
$prefabGuid=[System.Guid]::NewGuid().ToString('N');$catalogGuid=[System.Guid]::NewGuid().ToString('N')
Save ($prefabPath+'.meta') "fileFormatVersion: 2`nguid: $prefabGuid`nPrefabImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
Save $prefabPath $prefab
Save ($catalogPath+'.meta') "fileFormatVersion: 2`nguid: $catalogGuid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
Save $catalogPath $catalog
foreach($change in $clipChanges){Save $change.Path $change.After}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'catalog-source.json'),(ConvertTo-Json -InputObject @($rows) -Depth 5),$utf8)
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'manifest.json'),([pscustomobject]@{Prefab=$prefabPath;PrefabGuid=$prefabGuid;Catalog=$catalogPath;CatalogGuid=$catalogGuid;BlueClips=@($clipChanges | ForEach-Object {$_.Path});OtherBlueCurves=@();OrbVisualBindingHash=$orbHash}|ConvertTo-Json -Depth 6),$utf8)
"Created shared projectile and 28-entry catalog; retargeted 28 sprite-only clips."
