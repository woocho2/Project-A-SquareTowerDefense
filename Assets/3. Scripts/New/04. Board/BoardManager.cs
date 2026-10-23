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

        public BoardGrid Grid { get; private set; }

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
                UnityEditor.Handles.Label(pos, i.ToString(), pathStyle);
            }

            if (Grid == null) return;

            var spawnStyle = new GUIStyle();
            spawnStyle.normal.textColor = Color.black;

            for (int i = 0; i < Grid.SpawnCount; i++)
            {
                Vector3 pos = GetWorldPosition(Grid.GetSpawnCell(i));
                UnityEditor.Handles.Label(pos, i.ToString(), spawnStyle);
            }
        }
#endif
    }
}