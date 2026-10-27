using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD
{
    public class BoardManager : Singleton<BoardManager>
    {
        [SerializeField] private Tilemap m_pathTilemap;
        [SerializeField] private Tilemap m_spawnTilemap;
        [SerializeField] private List<Vector3Int> m_pathCells = new List<Vector3Int>();
        [SerializeField] private SpecialTileCounts m_tileCounts = new SpecialTileCounts();
        [SerializeField] private int m_layoutSeed;

        public BoardGrid Grid { get; private set; }

        public SpecialTileLayout Tiles { get; private set; }

        public ZoneRegistry Zones { get; private set; }

        public PathOccupancy Occupancy { get; private set; }

        public TowerSlots Slots { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            if (m_pathTilemap.transform.position != m_spawnTilemap.transform.position ||
             m_pathTilemap.layoutGrid != m_spawnTilemap.layoutGrid)
            {
                Debug.LogError("[BoardManager] 길 타일맵과 스폰 타일맵의 칸 좌표가 다릅니다. 같은 Grid 밑, 같은 위치에 두세요.");
            }

            List<Vector3Int> spawnCells = CollectSpawnCells();
            Grid = new BoardGrid(m_pathCells, spawnCells);
            Occupancy = new PathOccupancy();
            Slots = new TowerSlots(Grid.SpawnCount);

            System.Random random = m_layoutSeed == 0 ? new System.Random() : new System.Random(m_layoutSeed);

            Tiles = new SpecialTileLayout();
            Tiles.Generate(Grid, m_tileCounts, random);

            Zones = new ZoneRegistry(Grid, Tiles);
        }

        private List<Vector3Int> CollectSpawnCells()
        {
            var cells = new List<Vector3Int>();
            BoundsInt bounds = m_spawnTilemap.cellBounds;

            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (m_spawnTilemap.HasTile(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }
            return cells;
        }

        public Vector3 GetWorldPosition(Vector3Int cell)
        {
            return m_pathTilemap.GetCellCenterWorld(cell);
        }

        public Vector3 GetSpawnWorldPosition(int slot) => GetWorldPosition(Grid.GetSpawnCell(slot));

        public bool TryGetPoint(Vector3 world, out BoardPoint point)
        {
            Vector3Int cell = m_pathTilemap.WorldToCell(world);
            int pathIndex = Grid.TryGetPathIndex(cell, out int foundPath) ? foundPath : -1;
            int spawnIndex = Grid.TryGetSpawnIndex(cell, out int foundSpawn) ? foundSpawn : -1;
            point = new BoardPoint(world, pathIndex, spawnIndex);
            return pathIndex >= 0 || spawnIndex >= 0;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (m_pathTilemap == null) return;

            var pathStyle = new GUIStyle();
            pathStyle.normal.textColor = Color.yellow;
            pathStyle.fontStyle = FontStyle.Bold;

            for (int i = 0; i < m_pathCells.Count; i++)
            {
                Vector3 pos = GetWorldPosition(m_pathCells[i]);
                PathTileType tile = Tiles != null ? Tiles.GetPathTile(i) : PathTileType.None;
                string label = tile == PathTileType.None ? i.ToString() : $"{i} {tile}";
                if (Zones != null) label += ZoneLabel(Zones.GetAtPath(i));
                UnityEditor.Handles.Label(pos, label, pathStyle);
            }

            if (Grid == null) return;

            var spawnStyle = new GUIStyle();
            spawnStyle.normal.textColor = Color.black;

            for (int i = 0; i < Grid.SpawnCount; i++)
            {
                Vector3 pos = GetWorldPosition(Grid.GetSpawnCell(i));
                SpawnTileType tile = Tiles != null ? Tiles.GetSpawnTile(i) : SpawnTileType.None;
                string label = tile == SpawnTileType.None ? i.ToString() : $"{i} {tile}";
                if (Zones != null) label += ZoneLabel(Zones.GetAtSpawn(i));
                UnityEditor.Handles.Label(pos, label, spawnStyle);
            }
        }
        
        private static string ZoneLabel(List<Zone> zones)
        {
            string text = "";
            foreach (Zone zone in zones)
            {
                string name = EmblemNames.GetEmblemName(zone.Emblem);
                text += zone.Stage == ZoneStage.Skill2 ? $"\n[{name} ★]" : $"\n[{name} {zone.Stack:0}/{zone.Owner.Stats.Stack}]";
            }
            return text;
        }
#endif
    }
}