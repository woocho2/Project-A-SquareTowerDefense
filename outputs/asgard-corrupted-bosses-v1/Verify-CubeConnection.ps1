$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Read-Text([string]$path) { [IO.File]::ReadAllText((Join-Path $root $path)).Replace("`r`n", "`n") }
function Assert([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
function Get-Docs([string]$text) {
    $map = @{}
    foreach ($m in [regex]::Matches($text, '(?ms)^--- !u!\d+ &(-?\d+)(?: stripped)?\n.*?(?=^--- !u!|\z)')) {
        $id = $m.Groups[1].Value
        Assert (!$map.ContainsKey($id)) ('Duplicate fileID ' + $id)
        $map[$id] = $m.Value
    }
    return $map
}
function Get-Materials([string]$text, [string]$field) {
    $m = [regex]::Match($text, "(?m)^  ${field}:\n(?<list>(?:  - [^\n]*\n){6})")
    @([regex]::Matches($m.Groups['list'].Value, 'guid: ([a-f0-9]{32})') | ForEach-Object { $_.Groups[1].Value })
}
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'asset-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$paths = @(
    @{ Boss='Brunhild'; Path='Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Brunhild.prefab'; Type=3 },
    @{ Boss='Loki'; Path='Assets/2. Prefab/3. Enemy/BossCharacters/EnemyBoss_Loki.prefab'; Type=4 },
    @{ Boss='Brunhild'; Path='Assets/2. Prefab/3. Enemy/4. Enemy_middleBoss.prefab'; Type=3 },
    @{ Boss='Loki'; Path='Assets/2. Prefab/3. Enemy/5. Enemy_boss.prefab'; Type=4 }
)
$log = New-Object Collections.Generic.List[string]
foreach ($target in $paths) {
    $text = Read-Text $target.Path
    $before = Read-Text ('outputs/asgard-corrupted-bosses-v1/cube-connection-before/' + $target.Path)
    $docs = Get-Docs $text
    $oldDocs = Get-Docs $before
    foreach ($ref in [regex]::Matches($text, '\{fileID: (-?\d+)\}')) {
        $id = $ref.Groups[1].Value
        Assert ($id -eq '0' -or $docs.ContainsKey($id)) ($target.Path + ': missing local reference ' + $id)
    }
    Assert (!$text.Contains('EnemyCubeVisual') -and !$text.Contains('SpriteRenderer:')) 'Legacy visual still present.'
    Assert ($text.Contains('m_cubeMeshVisual: {fileID: 900812003}')) 'Movement is not linked to mesh cube.'
    Assert ($docs['900812003'].Contains('5c4addbeb5994f00ae570f852388b10c')) 'Wrong cube script.'
    Assert ($docs['900812004'].Contains('6c7fb238fa2c19a44ab6bd0d7ba51f3d')) 'Wrong six-face mesh.'
    Assert ($docs['7072637282772444813'].Contains('m_LocalScale: {x: 0.33333334, y: 0.33333334, z: 0.33333334}')) 'Cube root size/Z is wrong.'
    Assert ($text.Contains('m_moveSpeed: 1') -and $text.Contains('m_tileRollDuration: 0.4')) 'Rolling speed differs from normal enemy.'
    $expected = @($manifest | Where-Object boss -eq $target.Boss | Sort-Object cubeAssetPoolIndex)
    $pool = @(Get-Materials $docs['900812003'] 'm_faceAssetPool')
    $renderer = @(Get-Materials $docs['900812005'] 'm_Materials')
    Assert ($pool.Count -eq 6 -and $renderer.Count -eq 6) 'Six materials required in both arrays.'
    for ($i=0; $i -lt 6; $i++) {
        Assert ($pool[$i] -eq $expected[$i].materialGuid -and $renderer[$i] -eq $expected[$i].materialGuid) ('Wrong state material at index ' + $i)
        $mat = Read-Text $expected[$i].material
        Assert ($mat.Contains('guid: ' + $expected[$i].guid)) 'Wrong atlas texture.'
        Assert ((Read-Text ($expected[$i].material + '.meta')).Contains('guid: ' + $expected[$i].materialGuid)) 'Material GUID mismatch.'
        Assert ($mat.Contains('72aefebd36af4c238fd6b9ba1c4343b6')) 'Wrong cube shader.'
    }
    foreach ($id in $oldDocs.Keys) {
        $old = $oldDocs[$id]
        if ($old.Contains('EnemyHealthController') -or $old.Contains('EnemyUIController') -or $old.Contains('EnemyDebuffController') -or $old.Contains('BoxCollider2D:') -or $old.Contains(' stripped')) {
            Assert ($docs[$id] -ceq $old) ('Gameplay/UI/stripped document changed: ' + $id)
        }
    }
    Assert ($text.Contains('m_enemyType: ' + $target.Type)) 'Boss identity changed.'
    $log.Add('PASS: ' + $target.Path + ' (six states, mesh movement, local references, preserved gameplay/HP references)')
}
$routes = @(
    @('{fileID: 8667325651164272175, guid: 6eb6174d1ae64f5449b881462ba06aec, type: 3}', '{fileID: 8667325651164272175, guid: 87c1241159284a9088223da54f5600f3, type: 3}'),
    @('{fileID: 1541420121208455735, guid: 68a2d2e6e734e2f4f889e39e452619a0, type: 3}', '{fileID: 1541420121208455735, guid: ec24315888414e95885402e897e27826, type: 3}')
)
foreach ($path in @('Assets/2. Prefab/=====Pool=====.prefab', 'Assets/1. Scenes/3. Stage1.unity')) {
    $expected = Read-Text ('outputs/asgard-corrupted-bosses-v1/cube-connection-before/' + $path)
    foreach ($route in $routes) { $expected = $expected.Replace($route[0], $route[1]) }
    Assert ((Read-Text $path) -ceq $expected) ('Unexpected pool/scene change since connection snapshot: ' + $path)
    $log.Add('PASS: ' + $path + ' (only two boss prefab references changed)')
}
$mesh = Read-Text 'Assets/4. Asset/4. Enemy/Asgard/CubeValkyrie/StateCube3D_v1/Enemy_StateCube.asset'
Assert (([regex]::Matches($mesh, '    indexCount: 6')).Count -eq 6) 'Mesh must have six quad submeshes.'
$log.Add('PASS: common six-submesh mesh and atlas UV-transform shader reused; no runtime code changes required.')
$log.Add('Unity was not launched. Import, rendering, and gameplay validation are left to the user.')
$log | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'cube-connection-verification.txt') -Encoding UTF8
$log
