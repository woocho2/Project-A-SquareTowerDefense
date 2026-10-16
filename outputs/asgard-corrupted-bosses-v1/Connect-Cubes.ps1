$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$utf8 = New-Object Text.UTF8Encoding($false)
function Read-Asset([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)).Replace("`r`n", "`n") }
function Save-Asset([string]$path, [string]$content) {
    $backup = Join-Path $PSScriptRoot ('cube-connection-before/' + $path)
    if (!(Test-Path -LiteralPath $backup)) {
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup)) | Out-Null
        [IO.File]::Copy((Join-Path $root $path), $backup)
    }
    [IO.File]::WriteAllText((Join-Path $root $path), $content.Replace("`n", "`r`n"), $utf8)
}
function Documents([string]$content) {
    @([regex]::Matches($content, '(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\z)'))
}
$normal = Read-Asset 'Assets/2. Prefab/3. Enemy/Enemy.prefab'
$normalDocs = Documents $normal
$visualIds = @('8851935862707142629', '9206132105379693727', '1242548267809813189', '2178990430585501220', '1910288065190084560')
$idMap = @{
    '8851935862707142629' = '900812001'; '9206132105379693727' = '900812002'
    '1242548267809813189' = '900812004'; '2178990430585501220' = '900812005'
    '1910288065190084560' = '900812003'; '1448361976983806495' = '7072637282772444813'
}
$poolMatch = [regex]::Match($normal, '(?m)^  m_faceAssetPool:\n(?<list>(?:  - [^\n]*\n){6})')
$normalMaterials = @([regex]::Matches($poolMatch.Groups['list'].Value, 'guid: ([a-f0-9]{32})') | ForEach-Object { $_.Groups[1].Value })
if ($normalMaterials.Count -ne 6) { throw 'Normal cube material pool must contain six entries.' }
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$targets = @(
    @{ Boss='Brunhild'; Path='Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Brunhild.prefab' },
    @{ Boss='Loki'; Path='Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Loki.prefab' },
    @{ Boss='Brunhild'; Path='Assets/2. Prefab/3. Enemy/4. Enemy_middleBoss.prefab' },
    @{ Boss='Loki'; Path='Assets/2. Prefab/3. Enemy/5. Enemy_boss.prefab' }
)
foreach ($target in $targets) {
    $materials = @($manifest | Where-Object boss -eq $target.Boss | Sort-Object cubeAssetPoolIndex)
    if ($materials.Count -ne 6) { throw 'Boss must have six materials.' }
    $visual = ($normalDocs | Where-Object { $_.Groups['id'].Value -in $visualIds } | ForEach-Object { $_.Value }) -join ''
    foreach ($key in $idMap.Keys) { $visual = $visual.Replace($key, $idMap[$key]) }
    $visual = $visual.Replace("  m_Name: `n", "  m_Name:`n")
    for ($i=0; $i -lt 6; $i++) { $visual = $visual.Replace($normalMaterials[$i], $materials[$i].materialGuid) }
    $source = Read-Asset $target.Path
    $docs = Documents $source
    $remove = @('900812001','900812002','900812003','900812004','900812005','8437019901037981175')
    $body = ($docs | Where-Object { $_.Groups['id'].Value -notin $remove } | ForEach-Object {
        $doc = $_.Value
        switch ($_.Groups['id'].Value) {
            '7072637282772444813' {
                # Same three-roll-per-tile geometry and nine-slot size as the normal cube.
                $doc = [regex]::Replace($doc, '  m_LocalScale: .*', '  m_LocalScale: {x: 0.33333334, y: 0.33333334, z: 0.33333334}')
            }
            '-509579309956034587' {
                $doc = $doc.Replace('  m_BodyType: 0', '  m_BodyType: 1').Replace('  m_Constraints: 0', '  m_Constraints: 4')
            }
            { $_ -in @('833550379723969138', '939248890575520806') } {
                $doc = $doc.Replace('Assembly-CSharp::EnemyMovementController', 'Assembly-CSharp::EnemyMoveController')
                $doc = [regex]::Replace($doc, '  m_moveSpeed: .*', '  m_moveSpeed: 1')
                $doc = [regex]::Replace($doc, '  m_cubeVisual: .*\n(?:  m_cubeMeshVisual: .*\n)?', "  m_cubeVisual: {fileID: 0}`n  m_cubeMeshVisual: {fileID: 900812003}`n")
            }
        }
        if ($_.Groups['type'].Value -eq '1001') {
            $doc = [regex]::Replace($doc, '(propertyPath: m_LocalPosition.z\n      value: )[^\n]*', '${1}-1.5')
        }
        $doc
    }) -join ''
    $result = "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n" + $body + $visual
    Save-Asset $target.Path $result
    Write-Output ('Connected: ' + $target.Path)
}
# Route both the reusable pool and its active scene overrides to the character prefabs.
$routes = @(
    @{ Old='{fileID: 8667325651164272175, guid: 6eb6174d1ae64f5449b881462ba06aec, type: 3}'; New='{fileID: 8667325651164272175, guid: 87c1241159284a9088223da54f5600f3, type: 3}' },
    @{ Old='{fileID: 1541420121208455735, guid: 68a2d2e6e734e2f4f889e39e452619a0, type: 3}'; New='{fileID: 1541420121208455735, guid: ec24315888414e95885402e897e27826, type: 3}' }
)
foreach ($path in @('Assets/2. Prefab/=====Pool=====.prefab', 'Assets/1. Scenes/3. Stage1.unity')) {
    $text = Read-Asset $path
    foreach ($route in $routes) { $text = $text.Replace($route.Old, $route.New) }
    Save-Asset $path $text
    Write-Output ('Routed: ' + $path)
}
