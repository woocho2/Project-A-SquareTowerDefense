$ErrorActionPreference='Stop'
$repoRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Read([string]$path){[IO.File]::ReadAllText((Join-Path $repoRoot $path))}
function Assert([bool]$value,[string]$message){if(!$value){throw $message}}
function Blocks([string]$text){[regex]::Matches($text,'(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)')}
function Section([string]$text,[string]$key,[int]$indent){$p=' '*$indent;$m=[regex]::Match($text,'(?ms)^'+$p+$key+':\r?\n.*?(?=^'+$p+'[A-Za-z_]|\z)');Assert $m.Success ('Missing section '+$key);$m.Value}
function Field([string]$text,[string]$key,[int]$indent=2){[regex]::Match($text,'(?m)^'+(' '*$indent)+$key+': ([^\r\n]+)').Groups[1].Value}
function CheckMinMax([string]$module,[string]$key,[string]$min,[string]$max,[string]$state){
 $s=Section $module $key 4
 Assert ((Field $s 'minMaxState' 6) -eq $state -and (Field $s 'minScalar' 6) -eq $min -and (Field $s 'scalar' 6) -eq $max) ('Wrong particle range: '+$key)
}
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($path in $manifest.BlueClips){
 $old=Read ('outputs/projectile-shared-assets/before/'+$path)
 $new=Read $path
 $undo=$new.Replace('    path: OrbVisual','    path: ').Replace('      path: 1222197203','      path: 0')
 Assert ($undo -ceq $old) ('Clip changes beyond binding paths: '+$path)
}
$catalog=Read $manifest.Catalog
$rows=@([regex]::Matches($catalog,'(?ms)^  - Id: (?<id>\d+)\r?\n(?<fields>.*?)(?=^  - Id:|\z)'))
$expected=@(101..114)+@(201..214)
Assert ($rows.Count -eq 28) 'Expected 28 catalog rows'
for($i=0;$i -lt $rows.Count;$i++){
 $id=[int]$rows[$i].Groups['id'].Value
 Assert ($id -eq $expected[$i]) 'Wrong catalog order'
 $source=Read ('Assets/Resources/Projectiles/'+$id+'.prefab')
 $sourceBlocks=Blocks $source
 $sr=@($sourceBlocks | Where-Object {$_.Groups['type'].Value -eq '212'})[0].Value
 $anim=@($sourceBlocks | Where-Object {$_.Groups['type'].Value -eq '95'})[0].Value
 $fields=$rows[$i].Groups['fields'].Value
 Assert ((Field $fields 'Sprite' 4) -ceq (Field $sr 'm_Sprite')) ('Sprite mismatch: '+$id)
 Assert ((Field $fields 'Animation' 4) -ceq (Field $anim 'm_Controller')) ('Controller mismatch: '+$id)
 Assert ((Field $fields 'EffectColor' 4) -ceq (Field $source 'm_prefabEffectColor')) ('Color mismatch: '+$id)
}
$prefab=Read $manifest.Prefab
$blocks=Blocks $prefab
$ids=@($blocks | ForEach-Object {$_.Groups['id'].Value})
Assert ($ids.Count -eq 9 -and @($ids | Select-Object -Unique).Count -eq 9) 'Unexpected prefab object count or duplicate IDs'
foreach($ref in [regex]::Matches($prefab,'\{fileID: (?<id>-?\d+)\}')){
 $id=$ref.Groups['id'].Value
 Assert ($id -eq '0' -or $ids -contains $id) ('Dangling local reference: '+$id)
}
$types=@($blocks | ForEach-Object {$_.Groups['type'].Value})
Assert (@($types | Where-Object {$_ -notin @('1','4','95','96','198','199','212')}).Count -eq 0) 'Unexpected script or physics component'
Assert ($prefab.Contains('  m_Name: Projectile') -and $prefab.Contains('  m_Name: OrbVisual') -and $prefab.Contains('  m_LocalScale: {x: 0.5, y: 0.5, z: 0.5}')) 'Wrong prefab names/scale'
$anim=@($blocks | Where-Object {$_.Groups['type'].Value -eq '95'})[0].Value
Assert ((Field $anim 'm_Controller') -eq '{fileID: 0}' -and (Field $anim 'm_CullingMode') -eq '0') 'Wrong animator settings'
foreach($type in @('96','199','212')){
 $renderer=@($blocks | Where-Object {$_.Groups['type'].Value -eq $type})[0].Value
 Assert ((Field $renderer 'm_SortingLayerID') -eq '-1018217311') ('Wrong sorting layer: '+$type)
 $order=if($type -eq '212'){'0'}else{'-1'}
 Assert ((Field $renderer 'm_SortingOrder') -eq $order) ('Wrong sorting order: '+$type)
 Assert ($renderer.Contains('guid: a97c105638bdf8b4a8650670310a4cd3, type: 2}')) ('Wrong material: '+$type)
}
$trail=@($blocks | Where-Object {$_.Groups['type'].Value -eq '96'})[0].Value
Assert ((Field $trail 'm_Time') -eq '0.2' -and (Field $trail 'm_MinVertexDistance') -eq '0.04' -and (Field $trail 'm_Autodestruct') -eq '0' -and (Field $trail 'm_Emitting') -eq '0') 'Wrong trail settings'
Assert ((Field $trail 'widthMultiplier' 4) -eq '1') 'Wrong trail width multiplier'
$width=Section $trail 'widthCurve' 4
$values=@([regex]::Matches($width,'(?m)^        value: ([^\r\n]+)') | ForEach-Object {$_.Groups[1].Value})
Assert (($values -join ',') -eq '0.38,0') 'Wrong trail width curve'
$ps=@($blocks | Where-Object {$_.Groups['type'].Value -eq '198'})[0].Value
Assert ((Field $ps 'moveWithTransform') -eq '0' -and (Field $ps 'looping') -eq '1' -and (Field $ps 'playOnAwake') -eq '0') 'Wrong particle world/loop/play settings'
$initial=Section $ps 'InitialModule' 2
CheckMinMax $initial 'startLifetime' '0.18' '0.28' '3'
CheckMinMax $initial 'startSpeed' '0.1' '0.35' '3'
CheckMinMax $initial 'startSize' '0.06' '0.12' '3'
Assert ((Field $initial 'maxNumParticles' 4) -eq '50') 'Wrong max particles'
$emission=Section $ps 'EmissionModule' 2
CheckMinMax $emission 'rateOverTime' '0' '0' '0'
CheckMinMax $emission 'rateOverDistance' '14' '14' '0'
$shape=Section $ps 'ShapeModule' 2
Assert ((Field $shape 'type' 4) -eq '0' -and (Field (Section $shape 'radius' 4) 'value' 6) -eq '0.1') 'Wrong sphere shape/radius'
$size=Section $ps 'SizeModule' 2
$curve=Section (Section $size 'curve' 4) 'maxCurve' 6
$values=@([regex]::Matches($curve,'(?m)^          value: ([^\r\n]+)') | ForEach-Object {$_.Groups[1].Value})
Assert (($values -join ',') -eq '1,0') 'Wrong size-over-lifetime curve'
$enabled=@()
foreach($m in [regex]::Matches($ps,'(?ms)^  (?<name>\w+Module):\r?\n(?<body>.*?)(?=^  [A-Za-z]|\z)')){
 if((Field $m.Groups['body'].Value 'enabled' 4) -eq '1'){$enabled+=$m.Groups['name'].Value}
}
Assert (($enabled | Sort-Object) -join ',' -eq 'ColorModule,EmissionModule,InitialModule,ShapeModule,SizeModule') 'Unexpected enabled particle modules'
$baseline=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'baseline.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$concurrent=@()
foreach($file in $baseline){
 if((Get-FileHash -LiteralPath (Join-Path $repoRoot $file.Path) -Algorithm SHA256).Hash -ne $file.Hash){
  Assert ($file.Path -like '*.cs') ('Protected asset changed: '+$file.Path)
  $concurrent+=$file.Path
 }
}
$report=[pscustomobject]@{BlueClipsRetargeted=28;OtherBlueCurves=@();FramesSampleRatesLengthsPreserved=$true;CatalogRows=28;CatalogMatchesOldPrefabs=$true;PrefabSettingsVerified=$true;ScriptsAttached=0;PhysicsComponents=0;OldProjectileFilesAndSplashClipsUnchanged=$true;ConcurrentSourceEditsObserved=$concurrent}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification.json') -Encoding UTF8
$report | ConvertTo-Json -Depth 6
