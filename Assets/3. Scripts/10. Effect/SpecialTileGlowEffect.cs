using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 판 위 특수 타일을 칸마다 빛으로 보여 줌
    [DisallowMultipleComponent]
    public sealed class SpecialTileGlowEffect : MonoBehaviour
    {
        private static readonly int s_colorId = Shader.PropertyToID("_Color");
        private static readonly int s_shapeId = Shader.PropertyToID("_Shape");
        private static readonly int s_intensityId = Shader.PropertyToID("_Intensity");
        private static readonly int s_alphaId = Shader.PropertyToID("_Alpha");
        private static readonly int s_phaseId = Shader.PropertyToID("_Phase");
        private static readonly int s_seedId = Shader.PropertyToID("_Seed");

        private const int GridShape = 10;

        [SerializeField] private float m_intensity = 1.6f;
        [SerializeField, Range(0f, 1f)] private float m_alpha = 0.5f;
        [SerializeField, Range(0.5f, 1f)] private float m_sizeRatio = 0.96f;
        [Header("격자")]
        [SerializeField] private bool m_showGrid = true;
        [SerializeField] private Color m_gridColor = new Color(0.55f, 0.55f, 0.55f, 0.35f);

        private readonly List<GameObject> m_tiles = new List<GameObject>();
        private Material m_material;
        private Mesh m_mesh;
        private float m_phase;

        private void Start()
        {
            BoardManager board = BoardManager.Instance;
            if (board == null || board.Grid == null || board.Tiles == null) return;

            Shader shader = Resources.Load<Shader>("SpecialTile");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[SpecialTileGlowEffect] SpecialTile 셰이더를 쓸 수 없음", this);
                return;
            }
            m_material = new Material(shader) { name = "Special Tile (Runtime)" };
            m_material.SetFloat(s_intensityId, m_intensity);
            m_material.SetFloat(s_alphaId, m_alpha);
            m_mesh = MakeQuad();

            Renderer boardRenderer = GetComponent<Renderer>();
            int sortingLayer = boardRenderer != null ? boardRenderer.sortingLayerID : 0;
            int gridOrder = boardRenderer != null ? boardRenderer.sortingOrder + 1 : 1;
            int tileOrder = gridOrder + 1;
            float cell = EffectSpace.CellWorldSize();
            float size = cell * m_sizeRatio;

            if (m_showGrid)
            {
                for (int i = 0; i < board.Grid.PathCount; i++)
                    Place($"Grid Path {i}", board.GetWorldPosition(board.Grid.GetPathCell(i)), cell, GridShape, m_gridColor, sortingLayer, gridOrder);
                for (int i = 0; i < board.Grid.SpawnCount; i++)
                    Place($"Grid Spawn {i}", board.GetWorldPosition(board.Grid.GetSpawnCell(i)), cell, GridShape, m_gridColor, sortingLayer, gridOrder);
            }

            for (int i = 0; i < board.Grid.PathCount; i++)
            {
                PathTileType type = board.Tiles.GetPathTile(i);
                if (type == PathTileType.None) continue;
                (int shape, Color color) = PathLook(type);
                Place($"Path {i} {type}", board.GetWorldPosition(board.Grid.GetPathCell(i)), size, shape, color, sortingLayer, tileOrder);
            }

            for (int i = 0; i < board.Grid.SpawnCount; i++)
            {
                SpawnTileType type = board.Tiles.GetSpawnTile(i);
                if (type == SpawnTileType.None) continue;
                (int shape, Color color) = SpawnLook(type);
                Place($"Spawn {i} {type}", board.GetWorldPosition(board.Grid.GetSpawnCell(i)), size, shape, color, sortingLayer, tileOrder);
            }

            DOTween.To(() => m_phase, value => { m_phase = value; m_material.SetFloat(s_phaseId, value); }, 1f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            if (m_material != null) Destroy(m_material);
            if (m_mesh != null) Destroy(m_mesh);
        }

        private static (int shape, Color color) PathLook(PathTileType type) => type switch
        {
            PathTileType.Defense => (0, new Color(0.35f, 0.65f, 1f)),
            PathTileType.Speed => (1, new Color(1f, 0.85f, 0.25f)),
            PathTileType.ActionCount => (2, new Color(1f, 0.55f, 0.2f)),
            PathTileType.Heal => (3, new Color(0.35f, 1f, 0.5f)),
            PathTileType.Purify => (4, new Color(0.9f, 0.95f, 1f)),
            PathTileType.Bifrost => (5, new Color(0.8f, 0.6f, 1f)),
            _ => (0, Color.white),
        };

        private static (int shape, Color color) SpawnLook(SpawnTileType type) => type switch
        {
            SpawnTileType.Power => (6, new Color(1f, 0.3f, 0.3f)),
            SpawnTileType.ActionCount => (7, new Color(0.3f, 0.95f, 1f)),
            SpawnTileType.AttackCount => (8, new Color(0.8f, 0.45f, 1f)),
            SpawnTileType.Mastery => (9, new Color(1f, 0.82f, 0.35f)),
            _ => (6, Color.white),
        };

        private void Place(string name, Vector3 position, float size, int shape, Color color, int sortingLayer, int sortingOrder)
        {
            var tile = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            tile.transform.SetParent(transform, false);
            tile.transform.position = position;
            tile.transform.localScale = new Vector3(size, size, 1f);
            tile.GetComponent<MeshFilter>().sharedMesh = m_mesh;

            var renderer = tile.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = m_material;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var block = new MaterialPropertyBlock();
            block.SetColor(s_colorId, color);
            block.SetFloat(s_shapeId, shape);
            block.SetFloat(s_seedId, m_tiles.Count * 0.73f);
            renderer.SetPropertyBlock(block);
            m_tiles.Add(tile);
        }

        private static Mesh MakeQuad()
        {
            var mesh = new Mesh { name = "Special Tile Quad" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            return mesh;
        }
    }
}
