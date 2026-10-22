$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8 = [Text.UTF8Encoding]::new($false)
function Read([string]$relative) { [IO.File]::ReadAllText((Join-Path $repoRoot $relative)) }
function Guid([string]$relative) {
    $match = [regex]::Match((Read $relative), '(?m)^guid: ([a-f0-9]{32})')
    if (!$match.Success) { throw "No GUID: $relative" }
    $match.Groups[1].Value
}
function Save([string]$relative, [string]$text) { [IO.File]::WriteAllText((Join-Path $repoRoot $relative), $text, $utf8) }
function Blocks([string]$yaml) { [regex]::Matches($yaml, '(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)') }

if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'baseline.json')) { throw 'Already prepared; do not run twice' }
$protected = @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets') -Filter '*.cs' -Recurse)
$protected += Get-Item -LiteralPath (Join-Path $repoRoot 'Assets/2. Prefab/2. Tower/TowerBase.prefab')
$protected += Get-ChildItem -LiteralPath (Join-Path $repoRoot 'Assets/1. Scenes') -Filter '*.unity'
$baseline = @($protected | ForEach-Object { [pscustomobject]@{Path=$_.FullName.Substring($repoRoot.Length+1); Hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })

$catalogs = @(
    @{Name='TowerCharacterCatalog'; OldGuid='24c24873dcee41018d2bd84bdbf9e2b5'},
    @{Name='SynergyCharacterCatalog'; OldGuid='97fee378c3e247228e62e41bdd57fe2e'}
)
foreach ($c in $catalogs) {
    $c.Path = 'Assets/Resources/' + $c.Name + '.asset'
    $c.OldPath = 'Assets/Resources/' + $c.Name + '_Old.asset'
    if (Test-Path -LiteralPath (Join-Path $repoRoot $c.OldPath)) { throw "Already exists: $($c.OldPath)" }
    if ((Guid ($c.Path+'.meta')) -ne $c.OldGuid) { throw "Original GUID mismatch: $($c.Name)" }
    $c.ScriptGuid = Guid ('Assets/3. Scripts/New/05. Tower/'+$c.Name+'.cs.meta')
    $c.NewGuid = [System.Guid]::NewGuid().ToString('N')
    $c.Source = Read $c.Path
    $c.Meta = Read ($c.Path+'.meta')
    if ($c.Source -notmatch '(?m)^  m_entries:') { throw "Missing entries: $($c.Name)" }
    $c.NewText = [regex]::Replace($c.Source, '(?m)^  m_Script: .+$', ('  m_Script: {fileID: 11500000, guid: '+$c.ScriptGuid+', type: 3}'))
    $c.NewText = [regex]::Replace($c.NewText, '(?m)^  m_EditorClassIdentifier: [^\r\n]*', ('  m_EditorClassIdentifier: Assembly-CSharp::SquareTD.'+$c.Name))
}
$towerPath = 'Assets/2. Prefab/2. Tower/Tower.prefab'
if ((Guid ($towerPath+'.meta')) -ne '0c1709b3e0e9eb84c9ca8e6c98d237fe') { throw 'Tower prefab GUID mismatch' }
$tower = Read $towerPath
$towerBlocks = Blocks $tower
$oldVisualGuid = Guid 'Assets/3. Scripts/01. Tower/TowerVisual.cs.meta'
$newVisualGuid = Guid 'Assets/3. Scripts/New/05. Tower/TowerVisual.cs.meta'
if ($tower.Contains($newVisualGuid)) { throw 'New visual already attached' }
$sourceBlocks = Blocks (Read 'Assets/2. Prefab/2. Tower/TowerBase.prefab')
$sourceVisual = @($sourceBlocks | Where-Object { $_.Value.Contains('guid: '+$oldVisualGuid+',') })
if ($sourceVisual.Count -ne 1) { throw 'Expected one old visual component' }
$root = @($towerBlocks | Where-Object { $_.Groups['type'].Value -eq '1' -and $_.Value -match '(?m)^  m_Name: Tower\r?$' })
if ($root.Count -ne 1) { throw 'Expected one Tower root' }
$rootId = $root[0].Groups['id'].Value
$visualId = '3374333988479624537'
if (@($towerBlocks | Where-Object {$_.Groups['id'].Value -eq $visualId}).Count -gt 0) { throw 'Component ID collision' }
$visual = $sourceVisual[0].Value
$visual = [regex]::Replace($visual, '(?m)^--- !u!114 &-?\d+', ('--- !u!114 &'+$visualId))
$visual = [regex]::Replace($visual, '(?m)^  m_GameObject: [^\r\n]*', ('  m_GameObject: {fileID: '+$rootId+'}'))
$visual = $visual.Replace($oldVisualGuid, $newVisualGuid)
$visual = [regex]::Replace($visual, '(?m)^  m_EditorClassIdentifier: [^\r\n]*', '  m_EditorClassIdentifier: Assembly-CSharp::SquareTD.TowerVisual')
$rendererMap = @{}
foreach ($pair in @(@('Background','m_backgroundRenderer'), @('Character','m_characterRenderer'), @('Decoration','m_decorationRenderer'), @('Emblem','m_emblemRenderer'))) {
    $name = $pair[0]; $field = $pair[1]
    $go = @($towerBlocks | Where-Object { $_.Groups['type'].Value -eq '1' -and $_.Value -match ('(?m)^  m_Name: '+$name+'\r?$') })
    if ($go.Count -ne 1) { throw "Missing/ambiguous child: $name" }
    $goId = $go[0].Groups['id'].Value
    $renderer = @($towerBlocks | Where-Object { $_.Groups['type'].Value -eq '212' -and $_.Value.Contains('m_GameObject: {fileID: '+$goId+'}') })
    if ($renderer.Count -ne 1) { throw "Missing renderer: $name" }
    $rendererId = $renderer[0].Groups['id'].Value
    $rendererMap[$field] = $rendererId
    $visual = [regex]::Replace($visual, ('(?m)^  '+$field+': [^\r\n]*'), ('  '+$field+': {fileID: '+$rendererId+'}'))
}
foreach ($c in $catalogs) { $visual = $visual.Replace($c.OldGuid, $c.NewGuid) }
$rootText = $root[0].Value
$newRoot = $rootText.Replace('  m_Layer:', ('  - component: {fileID: '+$visualId+"}`r`n  m_Layer:"))
$newTower = $tower.Replace($rootText, $newRoot).TrimEnd("`r", "`n") + "`r`n" + $visual

# Snapshot exactly the files being changed and verify protected files after the task.
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'baseline.json'), (ConvertTo-Json -InputObject $baseline -Depth 4), $utf8)
$backupPaths = @($towerPath) + @($catalogs | ForEach-Object {$_.Path; $_.Path+'.meta'})
foreach ($path in $backupPaths) {
    $dest = Join-Path $PSScriptRoot ('before/'+$path)
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest)) | Out-Null
    [IO.File]::Copy((Join-Path $repoRoot $path), $dest)
}
# Keep original asset GUIDs on the renamed files. The replacement assets get new GUIDs.
foreach ($c in $catalogs) {
    if ((Read $c.Path) -cne $c.Source -or (Read ($c.Path+'.meta')) -cne $c.Meta) { throw 'Catalog changed during preparation' }
    Move-Item -LiteralPath (Join-Path $repoRoot $c.Path) -Destination (Join-Path $repoRoot $c.OldPath)
    Move-Item -LiteralPath (Join-Path $repoRoot ($c.Path+'.meta')) -Destination (Join-Path $repoRoot ($c.OldPath+'.meta'))
    Save $c.OldPath ([regex]::Replace($c.Source, '(?m)^  m_Name: [^\r\n]*', ('  m_Name: '+$c.Name+'_Old')))
    Save $c.Path $c.NewText
    Save ($c.Path+'.meta') ($c.Meta.Replace($c.OldGuid, $c.NewGuid))
}
if ((Read $towerPath) -cne $tower) { throw 'Tower prefab changed during preparation' }
Save $towerPath $newTower
$report = [pscustomobject]@{
    Catalogs = @($catalogs | ForEach-Object {[pscustomobject]@{Name=$_.Name; OldPath=$_.OldPath; OldGuid=$_.OldGuid; NewGuid=$_.NewGuid; ScriptGuid=$_.ScriptGuid}})
    Prefab=$towerPath; VisualScriptGuid=$newVisualGuid; VisualComponentId=$visualId; RendererMap=$rendererMap
}
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'wiring.json'), ($report | ConvertTo-Json -Depth 8), $utf8)
$report | ConvertTo-Json -Depth 8
