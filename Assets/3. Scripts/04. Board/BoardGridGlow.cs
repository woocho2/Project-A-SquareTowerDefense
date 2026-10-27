using UnityEngine;
using UnityEngine.Tilemaps;

namespace SquareTD
{
    // Visual-only copy: game rules and occupied cells continue to use the original tilemaps.
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
    public sealed class BoardGridGlow : MonoBehaviour
    {
        [SerializeField] private Material m_glowMaterial;
        [Header("키보드 조명 패턴")]
        [Tooltip("11개 패턴을 무작위로 섞어서 이 간격으로 전환합니다. 같은 패턴은 연속으로 나오지 않습니다.")]
        [SerializeField, Min(1f)] private float m_patternDuration = 6f;
        [Tooltip("이전 패턴에서 다음 패턴으로 부드럽게 전환하는 시간입니다.")]
        [SerializeField, Min(0f)] private float m_transitionDuration = 1f;
        private const int PatternCount = 11;
        private static readonly int s_cellSize = Shader.PropertyToID("_CellSize");
        private static readonly int s_gridOrigin = Shader.PropertyToID("_GridOrigin");
        private static readonly int s_boardRect = Shader.PropertyToID("_BoardRect");
        private static readonly int s_patternA = Shader.PropertyToID("_PatternA");
        private static readonly int s_patternB = Shader.PropertyToID("_PatternB");
        private static readonly int s_patternBlend = Shader.PropertyToID("_PatternBlend");
        private static readonly int s_animationTime = Shader.PropertyToID("_AnimationTime");
        private readonly System.Random m_random = new System.Random();
        private readonly int[] m_patternOrder = new int[PatternCount];
        private int m_patternCursor;
        private int m_currentPattern;
        private int m_previousPattern;
        private double m_animationStart;
        private double m_nextPatternAt;
        private double m_transitionStartedAt;
        private Tilemap m_source;
        private GameObject m_visual;
        private Tilemap m_overlay;
        private TilemapRenderer m_renderer;
        private MaterialPropertyBlock m_properties;
        private bool m_dirty;

        private void OnEnable()
        {
            m_source = GetComponent<Tilemap>();
            Tilemap.tilemapTileChanged += OnTilesChanged;
            m_dirty = true;
            m_currentPattern = -1;
            m_patternCursor = PatternCount;
            m_currentPattern = NextPattern();
            m_previousPattern = m_currentPattern;
            m_animationStart = Time.realtimeSinceStartupAsDouble;
            m_transitionStartedAt = m_animationStart;
            m_nextPatternAt = m_animationStart + Mathf.Max(1f, m_patternDuration);
        }

        private void OnValidate() => m_dirty = true;

        private void OnTilesChanged(Tilemap map, Tilemap.SyncTile[] changes)
        {
            if (map == m_source) m_dirty = true;
        }

        private void LateUpdate()
        {
            if (m_dirty) RebuildVisual();
            if (m_renderer == null || !m_renderer.enabled) return;

            double now = Time.realtimeSinceStartupAsDouble;
            if (now >= m_nextPatternAt)
            {
                m_previousPattern = m_currentPattern;
                m_currentPattern = NextPattern();
                m_transitionStartedAt = now;
                m_nextPatternAt = now + Mathf.Max(1f, m_patternDuration);
            }
            float transition = Mathf.Clamp(m_transitionDuration, 0f, Mathf.Max(1f, m_patternDuration));
            float blend = transition <= 0f ? 1f : Mathf.Clamp01((float)(now - m_transitionStartedAt) / transition);
            m_properties.SetFloat(s_patternA, m_previousPattern);
            m_properties.SetFloat(s_patternB, m_currentPattern);
            m_properties.SetFloat(s_patternBlend, Mathf.SmoothStep(0f, 1f, blend));
            m_properties.SetFloat(s_animationTime, (float)(now - m_animationStart));
            m_renderer.SetPropertyBlock(m_properties);
        }

        private int NextPattern()
        {
            if (m_patternCursor >= PatternCount)
            {
                for (int i = 0; i < PatternCount; i++) m_patternOrder[i] = i;
                for (int i = PatternCount - 1; i > 0; i--)
                {
                    int other = m_random.Next(i + 1);
                    int pattern = m_patternOrder[i];
                    m_patternOrder[i] = m_patternOrder[other];
                    m_patternOrder[other] = pattern;
                }
                // Prevent the last pattern of one bag repeating at the start of the next.
                if (m_patternOrder[0] == m_currentPattern)
                {
                    int other = m_random.Next(1, PatternCount);
                    int pattern = m_patternOrder[0];
                    m_patternOrder[0] = m_patternOrder[other];
                    m_patternOrder[other] = pattern;
                }
                m_patternCursor = 0;
            }
            return m_patternOrder[m_patternCursor++];
        }

        private void RebuildVisual()
        {
            m_dirty = false;
            if (m_glowMaterial == null)
            {
                if (m_renderer != null) m_renderer.enabled = false;
                return;
            }
            if (m_visual == null)
            {
                m_visual = new GameObject("Board Neon Grid (Preview)", typeof(Tilemap), typeof(TilemapRenderer));
                m_visual.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                m_visual.layer = gameObject.layer;
                m_visual.transform.SetParent(transform, false);
                m_overlay = m_visual.GetComponent<Tilemap>();
                m_renderer = m_visual.GetComponent<TilemapRenderer>();
                m_properties = new MaterialPropertyBlock();
            }

            m_overlay.ClearAllTiles();
            BoundsInt bounds = m_source.cellBounds;
            if (bounds.size.x > 0 && bounds.size.y > 0)
                m_overlay.SetTilesBlock(bounds, m_source.GetTilesBlock(bounds));
            m_overlay.tileAnchor = m_source.tileAnchor;
            TilemapRenderer sourceRenderer = GetComponent<TilemapRenderer>();
            m_renderer.enabled = true;
            m_renderer.sharedMaterial = m_glowMaterial;
            m_renderer.sortingLayerID = sourceRenderer.sortingLayerID;
            m_renderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            Vector3 origin = m_source.CellToWorld(Vector3Int.zero);
            Vector3 stepX = m_source.CellToWorld(Vector3Int.right) - origin;
            Vector3 stepY = m_source.CellToWorld(Vector3Int.up) - origin;
            m_properties.SetVector(s_cellSize, new Vector4(stepX.magnitude, stepY.magnitude, 0f, 0f));
            m_properties.SetVector(s_gridOrigin, new Vector4(origin.x, origin.y, 0f, 0f));
            Vector3 halfCell = new Vector3(stepX.magnitude, stepY.magnitude, 0f) * 0.5f;
            Bounds occupied = new Bounds(origin, Vector3.zero);
            bool hasCell = false;
            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!m_source.HasTile(cell)) continue;
                Vector3 center = m_source.GetCellCenterWorld(cell);
                if (!hasCell)
                {
                    occupied = new Bounds(center, halfCell * 2f);
                    hasCell = true;
                }
                else
                {
                    occupied.Encapsulate(center - halfCell);
                    occupied.Encapsulate(center + halfCell);
                }
            }
            m_properties.SetVector(s_boardRect, new Vector4(occupied.min.x, occupied.min.y,
                Mathf.Max(0.001f, occupied.size.x), Mathf.Max(0.001f, occupied.size.y)));
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilesChanged;
            if (m_visual != null)
            {
                if (Application.isPlaying) Destroy(m_visual);
                else DestroyImmediate(m_visual);
            }
            m_visual = null;
            m_overlay = null;
            m_renderer = null;
        }
    }
}
