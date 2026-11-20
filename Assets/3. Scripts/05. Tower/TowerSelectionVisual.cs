using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace SquareTD
{
    // 선택 타워의 사거리 격자와 존을 배치해야 하는 타워의 붉은 경고 표시
    [DisallowMultipleComponent]
    public sealed class TowerSelectionVisual : MonoBehaviour
    {
        private static readonly int s_halfSizeId = Shader.PropertyToID("_HalfSize");
        private static readonly int s_edgeId = Shader.PropertyToID("_Edge");
        private static Material s_glowMaterial;
        private static readonly int s_cellsId = Shader.PropertyToID("_Cells");

        [SerializeField] private SpriteRenderer m_highlight;
        [SerializeField] private SpriteRenderer m_range;
        [SerializeField] private SpriteRenderer m_tile;
        [SerializeField] private float m_highlightScale = 1.7f;
        [SerializeField] private float m_edgeMargin = 1.03f;
        [SerializeField] private float m_popScale = 1.25f;
        [SerializeField] private float m_popDuration = 0.18f;
        [SerializeField] private float m_rangeFade = 0.15f;
        [SerializeField] private string m_groundLayer = "MapTile";
        [SerializeField] private int m_rangeOrder = 50;
        [SerializeField] private int m_highlightOrder = 51;

        private TowerController m_tower;
        private MaterialPropertyBlock m_block;
        private Sequence m_sequence;
        private bool m_shown;
        private int m_shownRange = -1;
        private float m_rangeAlpha;
        private SpriteRenderer m_placementWarning;
        private float m_nextPlacementCheck;

        private void Awake()
        {
            m_tower = GetComponent<TowerController>();
            m_block = new MaterialPropertyBlock();

            if (m_highlight != null)
            {
                if (s_glowMaterial == null)
                {
                    Shader shader = Resources.Load<Shader>("SelectionGlow");
                    if (shader != null && shader.isSupported) s_glowMaterial = new Material(shader) { name = "Selection Glow (Runtime)" };
                }
                if (s_glowMaterial != null) m_highlight.sharedMaterial = s_glowMaterial;
                SortOnGround(m_highlight, m_highlightOrder);
                m_highlight.gameObject.SetActive(false);
            }

            if (m_range != null)
            {
                Material material = BoardOverlayStyle.Material(false);
                if (material != null)
                {
                    m_range.sprite = BoardOverlayStyle.Quad;
                    m_range.sharedMaterial = material;
                    m_range.transform.localPosition = Vector3.zero;
                    m_range.transform.localRotation = Quaternion.identity;
                    m_range.color = new Color(.18f, .65f, .82f, .9f);
                }
                m_rangeAlpha = m_range.color.a;
                SortOnGround(m_range, m_rangeOrder);
                m_range.gameObject.SetActive(false);
            }
        }

        private void SortOnGround(SpriteRenderer renderer, int order)
        {
            SortingGroup group = renderer.GetComponent<SortingGroup>();
            if (group == null) group = renderer.gameObject.AddComponent<SortingGroup>();
            group.sortAtRoot = true;
            group.sortingLayerName = m_groundLayer;
            group.sortingOrder = order;
        }

        private void OnDisable()
        {
            Hide();
            if (m_placementWarning != null) m_placementWarning.gameObject.SetActive(false);
            m_nextPlacementCheck = 0;
        }

        private void LateUpdate()
        {
            bool suppressed = SimulationRunner.SuppressCosmetics;
            bool selected = !suppressed && UIManager.Instance != null && UIManager.Instance.IsTowerSelected(m_tower);
            if (selected != m_shown)
            {
                if (selected) Show();
                else Hide();
            }
            if (m_shown) FitRange(false);
            if (Time.unscaledTime >= m_nextPlacementCheck)
            {
                m_nextPlacementCheck = Time.unscaledTime + .2f;
                RefreshPlacementWarning(suppressed);
            }
        }

        private void Show()
        {
            m_shown = true;
            m_sequence?.Kill();
            m_sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (m_highlight != null)
            {
                m_highlight.gameObject.SetActive(true);
                ApplyGlowShape();
                m_highlight.transform.localScale = Vector3.one * (m_highlightScale * m_popScale);
                m_sequence.Join(m_highlight.transform.DOScale(Vector3.one * m_highlightScale, m_popDuration).SetEase(Ease.OutBack));
            }

            if (m_range != null)
            {
                FitRange(true);
                SetRangeAlpha(0f);
                m_sequence.Join(DOTween.To(() => 0f, SetRangeAlpha, m_rangeAlpha, m_rangeFade));
            }
        }

        private void Hide()
        {
            m_shown = false;
            m_sequence?.Kill();
            if (m_highlight != null) m_highlight.gameObject.SetActive(false);
            if (m_range != null) m_range.gameObject.SetActive(false);
        }

        private void ApplyGlowShape()
        {
            if (m_highlight.sprite == null) return;

            float half = m_highlight.sprite.bounds.extents.x;
            float worldHalf = half * m_highlightScale * Mathf.Abs(transform.lossyScale.x);
            float tileHalf = m_tile != null ? m_tile.bounds.extents.x : worldHalf / m_highlightScale;

            m_highlight.GetPropertyBlock(m_block);
            m_block.SetFloat(s_halfSizeId, half);
            m_block.SetFloat(s_edgeId, Mathf.Clamp(tileHalf * m_edgeMargin / Mathf.Max(0.0001f, worldHalf), 0.1f, 1f));
            m_highlight.SetPropertyBlock(m_block);
        }

        private void FitRange(bool force)
        {
            if (m_range == null || m_range.sprite == null || m_tower == null || m_tower.Stats == null) return;

            int range = m_tower.Stats.Range;
            bool visible = m_tower.IsOnBoard && m_tower.IsAttackTower;
            m_range.gameObject.SetActive(visible);
            if (!visible) return;
            if (!force && range == m_shownRange) return;
            m_shownRange = range;

            float side = (range * 2 + 1) * EffectSpace.CellWorldSize();
            Vector3 parentScale = m_range.transform.parent.lossyScale;
            Vector3 size = m_range.sprite.bounds.size;
            m_range.transform.localScale = new Vector3(side / Mathf.Max(.0001f, size.x * Mathf.Abs(parentScale.x)), side / Mathf.Max(.0001f, size.y * Mathf.Abs(parentScale.y)), 1);
            m_range.GetPropertyBlock(m_block); m_block.SetFloat(s_cellsId, range * 2 + 1); m_range.SetPropertyBlock(m_block);
        }

        private void RefreshPlacementWarning(bool suppressed)
        {
            bool pending = !suppressed && BoardManager.Instance != null && BoardManager.Instance.Zones.NeedsPlacement(m_tower);
            if (pending && m_placementWarning == null)
            {
                Material material = BoardOverlayStyle.Material(true);
                if (material == null) return;
                var marker = new GameObject("ZonePlacementRequired", typeof(SpriteRenderer)); marker.transform.SetParent(transform, false);
                marker.layer = gameObject.layer;
                m_placementWarning = marker.GetComponent<SpriteRenderer>(); m_placementWarning.sprite = BoardOverlayStyle.Quad;
                m_placementWarning.sharedMaterial = material; m_placementWarning.color = new Color(1, .19f, .15f, 1);
                m_placementWarning.sortingLayerName = "Tower";
                m_placementWarning.sortingOrder = 10;
            }
            if (m_placementWarning == null) return;
            m_placementWarning.gameObject.SetActive(pending);
            if (!pending) return;
            float side = EffectSpace.CellWorldSize() * 1.43f;
            Vector3 parentScale = transform.lossyScale;
            m_placementWarning.transform.localScale = new Vector3(side / Mathf.Max(.0001f, Mathf.Abs(parentScale.x)), side / Mathf.Max(.0001f, Mathf.Abs(parentScale.y)), 1);
        }

        private void SetRangeAlpha(float alpha)
        {
            Color color = m_range.color;
            color.a = alpha;
            m_range.color = color;
        }
    }
}
