using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace SquareTD
{
    // 판 위의 버프존·디버프존을 보여 줌 (2026-10-28 사용자: 존이 안 보임)
    //   놓인 존마다 칸 둘레에 고리: 버프 노랑, 디버프 보라 (역할 색). 스택만큼 시계 방향으로 차고, 다 차면(스킬 2) 숨 쉬듯 빛남
    //   칸 왼쪽 아래에 그 존을 만든 타워의 문양 엠블럼. 한 칸에 존이 여럿이면 고리는 안쪽으로 하나씩, 엠블럼은 옆으로
    //   모양은 Resources/SpecialTile.shader의 11번. SpecialTileGlowEffect와 같이 보드 타일맵(BoardTile)에 붙임
    //   규칙은 모름: ZoneRegistry를 읽기만 함. 존이 생기고·놓이고·사라질 때(ZoneChanged) 다시 깔고, 스택은 매 프레임 읽음
    //   (스택이 오를 때마다 알리는 이벤트는 없음 — 단계가 바뀔 때만 ZoneChanged)
    [DisallowMultipleComponent]
    public sealed class ZoneGlowEffect : MonoBehaviour
    {
        private const int ZoneShape = 11;
        private const float RingInsetStep = 0.13f;
        private static readonly int s_colorId = Shader.PropertyToID("_Color");
        private static readonly int s_shapeId = Shader.PropertyToID("_Shape");
        private static readonly int s_intensityId = Shader.PropertyToID("_Intensity");
        private static readonly int s_alphaId = Shader.PropertyToID("_Alpha");
        private static readonly int s_phaseId = Shader.PropertyToID("_Phase");
        private static readonly int s_seedId = Shader.PropertyToID("_Seed");
        private static readonly int s_progressId = Shader.PropertyToID("_Progress");
        private static readonly int s_insetId = Shader.PropertyToID("_Inset");

        [SerializeField] private float m_intensity = 1.6f;
        [SerializeField, Range(0f, 1f)] private float m_alpha = 0.85f;
        [SerializeField] private Color m_buffColor = new Color(1f, 0.82f, 0.3f);
        [SerializeField] private Color m_debuffColor = new Color(0.72f, 0.4f, 1f);
        [SerializeField, Range(0.1f, 0.5f)] private float m_iconSize = 0.28f;      // 칸에 대한 비율

        private sealed class View
        {
            public Zone Zone;
            public GameObject Ring;
            public MeshRenderer Renderer;
            public MaterialPropertyBlock Block;
            public GameObject Icon;
        }

        private readonly List<View> m_views = new List<View>();
        private BoardManager m_board;
        private Material m_material;
        private Mesh m_mesh;
        private float m_phase;
        private float m_cell;
        private int m_sortingLayer;
        private int m_ringOrder;
        private bool m_dirty;

        private void Start()
        {
            m_board = BoardManager.Instance;
            if (m_board == null || m_board.Zones == null) return;

            Shader shader = Resources.Load<Shader>("SpecialTile");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[ZoneGlowEffect] SpecialTile 셰이더를 쓸 수 없음", this);
                return;
            }
            m_material = new Material(shader) { name = "Zone Ring (Runtime)" };
            m_material.SetFloat(s_intensityId, m_intensity);
            m_material.SetFloat(s_alphaId, m_alpha);
            m_mesh = MakeQuad();

            Renderer boardRenderer = GetComponent<Renderer>();
            m_sortingLayer = boardRenderer != null ? boardRenderer.sortingLayerID : 0;
            m_ringOrder = (boardRenderer != null ? boardRenderer.sortingOrder : 0) + 3;     // 격자(+1)·특수 타일(+2) 위
            m_cell = EffectSpace.CellWorldSize();

            m_board.Zones.ZoneChanged += OnZoneChanged;
            m_dirty = true;

            DOTween.To(() => m_phase, value => { m_phase = value; m_material.SetFloat(s_phaseId, value); }, 1f, 1f)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            if (m_board != null && m_board.Zones != null) m_board.Zones.ZoneChanged -= OnZoneChanged;
            if (m_material != null) Destroy(m_material);
            if (m_mesh != null) Destroy(m_mesh);
        }

        private void OnZoneChanged(Zone zone) => m_dirty = true;

        private void LateUpdate()
        {
            if (m_material == null) return;
            if (m_dirty) Rebuild();

            foreach (View view in m_views)
            {
                view.Block.SetFloat(s_progressId, Progress(view.Zone));
                view.Renderer.SetPropertyBlock(view.Block);
            }
        }

        // 스킬 2면 1, 스킬 1이면 지금 스택 ÷ 최대 스택 (존 주인의 Stack)
        private static float Progress(Zone zone)
        {
            if (zone.Stage == ZoneStage.Skill2) return 1f;
            int max = zone.Owner != null ? zone.Owner.Stats.Stack : 0;
            return max > 0 ? Mathf.Clamp01(zone.Stack / max) * 0.999f : 0f;      // 0.999: 다 찬 모습은 스킬 2일 때만
        }

        // 존이 생기거나 사라지거나 놓일 때만 부름 (드묾) — 통째로 다시 깔아도 가벼움
        private void Rebuild()
        {
            m_dirty = false;
            foreach (View view in m_views)
            {
                Destroy(view.Ring);
                if (view.Icon != null) Destroy(view.Icon);
            }
            m_views.Clear();

            // 같은 칸의 존끼리 묶어 몇 번째인지 셈 (버프존은 스폰 칸, 디버프존은 길 칸)
            var countAtCell = new Dictionary<(bool, int), int>();
            foreach (Zone zone in m_board.Zones.All)
            {
                if (!zone.IsPlaced || zone.Owner == null) continue;
                var key = (zone.IsBuff, zone.CellIndex);
                countAtCell.TryGetValue(key, out int index);
                countAtCell[key] = index + 1;
                m_views.Add(MakeView(zone, index));
            }
        }

        private View MakeView(Zone zone, int indexInCell)
        {
            Vector3Int cell = zone.IsBuff ? m_board.Grid.GetSpawnCell(zone.CellIndex) : m_board.Grid.GetPathCell(zone.CellIndex);
            Vector3 center = m_board.GetWorldPosition(cell);
            Color color = zone.IsBuff ? m_buffColor : m_debuffColor;

            var ring = new GameObject($"Zone {EmblemNames.GetEmblemName(zone.Emblem)} {(zone.IsBuff ? "Buff" : "Debuff")} {zone.CellIndex}",
                typeof(MeshFilter), typeof(MeshRenderer));
            ring.transform.SetParent(transform, false);
            ring.transform.position = center;
            ring.transform.localScale = new Vector3(m_cell, m_cell, 1f);
            ring.GetComponent<MeshFilter>().sharedMesh = m_mesh;

            var renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = m_material;
            renderer.sortingLayerID = m_sortingLayer;
            renderer.sortingOrder = m_ringOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var block = new MaterialPropertyBlock();
            block.SetColor(s_colorId, color);
            block.SetFloat(s_shapeId, ZoneShape);
            block.SetFloat(s_insetId, indexInCell * RingInsetStep);
            block.SetFloat(s_seedId, zone.CellIndex * 0.61f);
            renderer.SetPropertyBlock(block);

            return new View { Zone = zone, Ring = ring, Renderer = renderer, Block = block, Icon = MakeIcon(zone, center, indexInCell, color) };
        }

        // 칸 왼쪽 아래에 주인 타워의 문양 엠블럼 (타워 그림에 가리지 않게 투사체 층에 그림)
        private GameObject MakeIcon(Zone zone, Vector3 center, int indexInCell, Color color)
        {
            TowerVisual visual = zone.Owner.GetComponent<TowerVisual>();
            Sprite sprite = visual != null ? visual.EmblemSprite : null;
            if (sprite == null) return null;

            var icon = new GameObject("Zone Emblem", typeof(SpriteRenderer));
            icon.transform.SetParent(transform, false);
            float size = m_cell * m_iconSize;
            Vector3 offset = new Vector3(-0.5f + m_iconSize * 0.5f + 0.06f + indexInCell * (m_iconSize + 0.02f), -0.5f + m_iconSize * 0.5f + 0.06f, 0f) * m_cell;
            icon.transform.position = center + offset;
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            icon.transform.localScale = Vector3.one * (spriteSize > 0f ? size / spriteSize : 1f);

            var renderer = icon.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.Lerp(color, Color.white, 0.35f);
            renderer.sortingLayerName = "Projectile";
            renderer.sortingOrder = -10;
            return icon;
        }

        private static Mesh MakeQuad()
        {
            var mesh = new Mesh { name = "Zone Ring Quad" };
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
