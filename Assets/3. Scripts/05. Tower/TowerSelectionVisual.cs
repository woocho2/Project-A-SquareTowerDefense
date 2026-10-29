using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace SquareTD
{
    // 정보창에 띄운 타워의 선택 표시(HightLight, 셰이더 빛 테두리)와 사거리(Range, 사거리만큼 키운 칸)를 켜고 끔
    [DisallowMultipleComponent]
    public sealed class TowerSelectionVisual : MonoBehaviour
    {
        private static readonly int s_halfSizeId = Shader.PropertyToID("_HalfSize");
        private static readonly int s_edgeId = Shader.PropertyToID("_Edge");
        private static Material s_glowMaterial;

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

        private void OnDisable() => m_sequence?.Kill();

        private void LateUpdate()
        {
            bool selected = UIManager.Instance != null && UIManager.Instance.IsTowerSelected(m_tower);
            if (selected != m_shown)
            {
                if (selected) Show();
                else Hide();
            }
            if (m_shown) FitRange(false);
        }

        private void Show()
        {
            m_shown = true;
            m_sequence?.Kill();
            m_sequence = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (m_highlight != null)
            {
                m_highlight.gameObject.SetActive(true);
                ApplyGlowShape();
                m_highlight.transform.localScale = Vector3.one * (m_highlightScale * m_popScale);
                m_sequence.Join(m_highlight.transform.DOScale(Vector3.one * m_highlightScale, m_popDuration).SetEase(Ease.OutBack));
            }

            if (m_range != null)
            {
                m_range.gameObject.SetActive(true);
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
            if (!force && range == m_shownRange) return;
            m_shownRange = range;

            float side = (range * 2 + 1) * EffectSpace.CellWorldSize();
            float spriteSize = m_range.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
            float scale = side / Mathf.Max(0.0001f, spriteSize);
            m_range.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void SetRangeAlpha(float alpha)
        {
            Color color = m_range.color;
            color.a = alpha;
            m_range.color = color;
        }
    }
}
