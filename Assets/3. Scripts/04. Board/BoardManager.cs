using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD
{
    // 판 전체를 들고 있는 매니저 (격자, 특수 타일, 존, 칸 점유, 타워 자리, 장판)
    public class BoardManager : Singleton<BoardManager>
    {
        [SerializeField] private Tilemap m_pathTilemap;
        [SerializeField] private Tilemap m_spawnTilemap;
        [SerializeField] private Tilemap m_benchTilemap;
        [SerializeField] private Tilemap m_synergyTilemap;
        [SerializeField] private List<Vector3Int> m_pathCells = new List<Vector3Int>();
        [SerializeField] private SpecialTileCounts m_tileCounts = new SpecialTileCounts();
        [SerializeField] private int m_layoutSeed;

        public Tilemap PathTilemap => m_pathTilemap;
        public Tilemap SpawnTilemap => m_spawnTilemap;

        public BoardGrid Grid { get; private set; }

        public SpecialTileLayout Tiles { get; private set; }

        public ZoneRegistry Zones { get; private set; }

        public PathOccupancy Occupancy { get; private set; }

        public TowerSlots Slots { get; private set; }

        public TowerSlots Bench { get; private set; }

        public TowerSlots SynergySlots { get; private set; }

        private List<Vector3Int> m_benchCells = new List<Vector3Int>();
        private List<Vector3Int> m_synergyCells = new List<Vector3Int>();

        public GroundEffectRegistry Grounds { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this) return;

            if (m_pathTilemap.transform.position != m_spawnTilemap.transform.position ||
             m_pathTilemap.layoutGrid != m_spawnTilemap.layoutGrid)
            {
                Debug.LogError("[BoardManager] 길 타일맵과 스폰 타일맵의 칸 좌표가 다릅니다. 같은 Grid 밑, 같은 위치에 두세요.");
            }

            List<Vector3Int> spawnCells = CollectCells(m_spawnTilemap);
            Grid = new BoardGrid(m_pathCells, spawnCells);
            Occupancy = new PathOccupancy();
            Slots = new TowerSlots(Grid.SpawnCount);

            if (m_benchTilemap == null) m_benchTilemap = FindSiblingTilemap("Wating Seat", "Waiting Seat", "Bench");
            if (m_synergyTilemap == null) m_synergyTilemap = FindSiblingTilemap("Synergy");
            m_benchCells = CollectCells(m_benchTilemap);
            m_synergyCells = CollectCells(m_synergyTilemap);
            Bench = new TowerSlots(m_benchCells.Count);
            SynergySlots = new TowerSlots(m_synergyCells.Count);
            Grounds = new GroundEffectRegistry();

            System.Random random = m_layoutSeed == 0
                ? SimulationRunner.SuppressCosmetics ? new System.Random(SimulationRunner.CurrentSeed) : new System.Random()
                : new System.Random(m_layoutSeed);

            Tiles = new SpecialTileLayout();
            Tiles.Generate(Grid, m_tileCounts, random);

            Zones = new ZoneRegistry(Grid, Tiles);
        }

        private Tilemap FindSiblingTilemap(params string[] names)
        {
            Transform parent = m_pathTilemap != null ? m_pathTilemap.transform.parent : null;
            if (parent == null) return null;
            foreach (Tilemap tilemap in parent.GetComponentsInChildren<Tilemap>(true))
                foreach (string name in names)
                    if (tilemap.name == name) return tilemap;
            return null;
        }

        private static List<Vector3Int> CollectCells(Tilemap tilemap)
        {
            var cells = new List<Vector3Int>();
            if (tilemap == null) return cells;
            tilemap.CompressBounds();
            BoundsInt bounds = tilemap.cellBounds;

            for (int y = bounds.yMax - 1; y >= bounds.yMin; y--)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (tilemap.HasTile(cell))
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

        public TowerSlots GetSlots(TowerPlace place)
        {
            switch (place)
            {
                case TowerPlace.Bench: return Bench;
                case TowerPlace.Synergy: return SynergySlots;
                default: return Slots;
            }
        }

        public Vector3 GetPlaceWorldPosition(TowerPlace place, int index)
        {
            switch (place)
            {
                case TowerPlace.Bench: return m_benchTilemap.GetCellCenterWorld(m_benchCells[index]);
                case TowerPlace.Synergy: return m_synergyTilemap.GetCellCenterWorld(m_synergyCells[index]);
                default: return GetSpawnWorldPosition(index);
            }
        }

        public List<TowerController> GetTowers(TowerPlace place)
        {
            var towers = new List<TowerController>();
            TowerSlots slots = GetSlots(place);
            if (slots == null) return towers;
            for (int i = 0; i < slots.Count; i++)
                if (slots.Get(i) != null) towers.Add(slots.Get(i));
            return towers;
        }

        public List<TowerController> GetBoardAndBenchTowers()
        {
            List<TowerController> towers = GetTowers(TowerPlace.Board);
            towers.AddRange(GetTowers(TowerPlace.Bench));
            return towers;
        }

        public bool TryGetPoint(Vector3 world, out BoardPoint point)
        {
            Vector3Int cell = m_pathTilemap.WorldToCell(world);
            int pathIndex = Grid.TryGetPathIndex(cell, out int foundPath) ? foundPath : -1;
            int spawnIndex = Grid.TryGetSpawnIndex(cell, out int foundSpawn) ? foundSpawn : -1;
            int benchIndex = m_benchTilemap != null ? m_benchCells.IndexOf(m_benchTilemap.WorldToCell(world)) : -1;
            int synergyIndex = m_synergyTilemap != null ? m_synergyCells.IndexOf(m_synergyTilemap.WorldToCell(world)) : -1;
            point = new BoardPoint(world, pathIndex, spawnIndex, benchIndex, synergyIndex);
            return pathIndex >= 0 || spawnIndex >= 0 || benchIndex >= 0 || synergyIndex >= 0;
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
                string label = i.ToString();
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
                text += $"\n[{name} {zone.Owner.State.SkillCount:0.#}/{zone.Owner.Stats.SkillCount:0.#}]";
            }
            return text;
        }
#endif
    }
}
