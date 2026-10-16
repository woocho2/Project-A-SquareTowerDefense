$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$checkRoot = Join-Path $PSScriptRoot 'logic'
New-Item -ItemType Directory -Path $checkRoot -Force | Out-Null
$source = [IO.File]::ReadAllText((Join-Path $repoRoot 'Assets/3. Scripts/04. Manager/TileManager.cs'))
# Execute the real occupancy and label refresh methods with lightweight display stubs.
# This checks game logic outside Unity; it does not validate Unity rendering.
$regionStart = $source.IndexOf('    public bool SetEnemyAtPathIndex(')
$regionEnd = $source.IndexOf('    #endregion', $regionStart)
$methods = $source.Substring($regionStart, $regionEnd - $regionStart)
$header = @'
using System;
using System.Collections.Generic;
public partial class TileManager
{
    private readonly Dictionary<int, Vector3Int> m_pathCellsByIndex = new();
    private readonly Dictionary<Vector3Int, List<int>> m_pathIndicesByCell = new();
    private readonly Dictionary<int, List<EnemyHealthController>> m_enemiesByPathIndex = new();
    private readonly Dictionary<EnemyHealthController, int> m_pathIndexByEnemy = new();
    private readonly Dictionary<EnemyHealthController, long> m_enemyArrivalOrder = new();
    private readonly Dictionary<Vector3Int, TextMeshPro> m_enemyCountLabels = new();
    private long m_nextEnemyArrivalOrder;
    private TMP_FontAsset m_enemyCountFont;
    private float m_enemyCountFontSize = 2.5f;
    private Color m_enemyCountColor = Color.white;
    private Vector2 m_enemyCountOffset = new(0.27f, -0.3f);
    private Tilemap tilemap = new();
    private GameObject gameObject = new();
    private Transform transform = new();
    public event Action EnemyCountChanged;
    public void AddPath(int index, Vector3Int cell) {
        m_pathCellsByIndex[index] = cell;
        if (!m_pathIndicesByCell.TryGetValue(cell, out var visits)) m_pathIndicesByCell[cell] = visits = new();
        visits.Add(index);
    }
    public TextMeshPro Label(Vector3Int cell) => m_enemyCountLabels[cell];
'@
[IO.File]::WriteAllText((Join-Path $checkRoot 'ActualOccupancy.cs'), $header + "`n" + $methods + "`n}", [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LogicFixture.cs') -Destination (Join-Path $checkRoot 'LogicFixture.cs') -Force
[IO.File]::WriteAllText((Join-Path $checkRoot 'LogicChecks.csproj'), '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup></Project>')
& dotnet run --project (Join-Path $checkRoot 'LogicChecks.csproj') --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Occupancy checks failed' }
