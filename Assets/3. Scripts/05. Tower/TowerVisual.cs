using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace SquareTD
{
    // 타워의 그림과 색을 입힘
    public class TowerVisual : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer m_backgroundRenderer;
        [SerializeField] private SpriteRenderer m_characterRenderer;
        [SerializeField] private SpriteRenderer m_decorationRenderer;
        [Header("정면 캐릭터")]
        [SerializeField] private TowerCharacterCatalog m_regularCharacters;
        [SerializeField] private SynergyCharacterCatalog m_synergyCharacters;
        [FormerlySerializedAs("m_swordCanvasWorldSize")]
        [SerializeField, Min(0.01f)] private float m_canvasWorldSize = 1f;
        [SerializeField, Min(0.01f)] private float m_synergyCanvasWorldSize = 1f;
        [Header("프레임 안 캐릭터 (마스크)")]
        [Tooltip("프리팹에서 맞춘 크기·위치를 게임에서도 그대로 씀. 캐릭터 그림이 바뀌어도 프리팹의 그림과 같은 크기로 맞춤")]
        [SerializeField] private SpriteMask m_characterMask;
        [Header("티어 색상 (장식 테두리, 1 브론즈 ~ 5 다이아)")]
        [SerializeField] private Color[] m_tierColors =
        {
            new Color(0.78f, 0.46f, 0.24f), new Color(0.77f, 0.85f, 0.92f), new Color(1f, 0.78f, 0.27f),
            new Color(0.56f, 0.91f, 0.80f), new Color(0.32f, 0.80f, 1f)
        };
        [Header("테두리 반짝임·후광 (FrameShine, SelectionGlow 셰이더)")]
        [SerializeField, Min(1f)] private float m_haloScale = 1.5f;
        [SerializeField, Range(0.5f, 1f)] private float m_frameEdge = 0.95f;
        [SerializeField, Range(0f, 0.5f)] private float m_frameCorner = 0.25f;
        [SerializeField] private Vector2 m_haloAlpha = new Vector2(0.35f, 0.9f);
        [SerializeField] private Vector2 m_haloIntensity = new Vector2(0.8f, 2f);

        private static readonly int s_tierId = Shader.PropertyToID("_Tier");
        private static readonly int s_phaseId = Shader.PropertyToID("_Phase");
        private static readonly int s_halfSizeId = Shader.PropertyToID("_HalfSize");
        private static readonly int s_edgeId = Shader.PropertyToID("_Edge");
        private static readonly int s_cornerId = Shader.PropertyToID("_Corner");
        private static readonly int s_lineId = Shader.PropertyToID("_Line");
        private static readonly int s_speedId = Shader.PropertyToID("_Speed");
        private static readonly int s_intensityId = Shader.PropertyToID("_Intensity");
        private static Material s_frameMaterial;
        private static Material s_haloMaterial;

        private SpriteRenderer m_halo;
        private MaterialPropertyBlock m_shineBlock;
        private float m_shinePhase = -1f;
        private int m_appliedTowerID;
        public bool UsesCharacterVisual { get; private set; }
        public Sprite CharacterSprite => m_characterRenderer != null ? m_characterRenderer.sprite : null;
        public Sprite IllustrationSprite { get; private set; }
        public Sprite DecorationSprite => m_decorationRenderer != null ? m_decorationRenderer.sprite : null;
        public Color TierColor => m_decorationRenderer != null ? m_decorationRenderer.color : Color.white;
        public Sprite ColorSprite => IsRegular && m_backgroundRenderer != null ? m_backgroundRenderer.sprite : null;
        private bool IsRegular => m_appliedTowerID / 1000 >= 1 && m_appliedTowerID / 1000 <= 5;

        public void Apply(int towerID)
        {
            m_appliedTowerID = towerID;
            CacheRenderers();
            int tier = towerID / 1000;
            int attack = (towerID % 1000) / 100;
            bool synergy = tier == 6;
            Sprite portrait = null;
            if (synergy)
            {
                if (m_synergyCharacters == null)
                    m_synergyCharacters = Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
                if (m_synergyCharacters != null) portrait = m_synergyCharacters.GetFront(towerID);
            }
            else if (IsRegular && attack >= 1 && attack <= 4)
            {
                if (m_regularCharacters == null)
                    m_regularCharacters = Resources.Load<TowerCharacterCatalog>("TowerCharacterCatalog");
                if (m_regularCharacters != null) portrait = m_regularCharacters.GetCharacter(towerID % 100, attack);
            }
            IllustrationSprite = portrait;
            if (synergy && m_synergyCharacters != null)
                portrait = m_synergyCharacters.GetTowerCharacter(towerID);
            else if (IsRegular && m_regularCharacters != null)
                portrait = m_regularCharacters.GetTowerCharacter(towerID % 100, attack);
            float canvasSize = synergy ? m_synergyCanvasWorldSize : m_canvasWorldSize;
            Sprite background = m_backgroundRenderer != null ? m_backgroundRenderer.sprite : null;
            Sprite decoration = m_decorationRenderer != null ? m_decorationRenderer.sprite : null;
            ApplyLayer(m_backgroundRenderer, background, -1, canvasSize);
            ApplyLayer(m_characterRenderer, portrait, 0, canvasSize);
            ApplyLayer(m_decorationRenderer, decoration, 1, canvasSize);
            ApplyCharacterFrame(background, canvasSize);
            if (m_decorationRenderer != null && !synergy && tier >= 1 && m_tierColors != null && tier <= m_tierColors.Length)
                m_decorationRenderer.color = m_tierColors[tier - 1];
            ApplyFrameShine(synergy ? 6 : Mathf.Clamp(tier, 1, 5));
            UsesCharacterVisual = portrait != null;
            if (portrait == null || background == null || decoration == null)
                Debug.LogWarning($"[TowerVisual] TowerID {towerID}의 초상·배경·장식 연결을 확인해 주세요.", this);
        }

        public void Apply(TowerDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("[TowerVisual] 타워 정의가 없습니다.", this);
                return;
            }
            Apply(definition.Id);
        }

        private void ApplyLayer(SpriteRenderer renderer, Sprite sprite, int orderOffset, float canvasSize)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
            if (renderer == m_characterRenderer) renderer.color = Color.white;
            renderer.flipX = false;
            renderer.flipY = false;
            if (renderer == m_characterRenderer) renderer.SetPropertyBlock(null);
            if (m_characterRenderer != null && renderer != m_characterRenderer)
            {
                renderer.sortingLayerID = m_characterRenderer.sortingLayerID;
                renderer.sortingOrder = m_characterRenderer.sortingOrder + orderOffset;
            }
            if (sprite == null) return;

            Vector3 size = sprite.bounds.size;
            float ratio = canvasSize / Mathf.Max(0.0001f, m_canvasWorldSize);
            if (m_authored.TryGetValue(renderer, out Authored authored) && authored.SpriteSize.x > 0f && authored.SpriteSize.y > 0f)
            {
                renderer.transform.localScale = new Vector3(
                    authored.Scale.x * authored.SpriteSize.x / Mathf.Max(0.0001f, size.x) * ratio,
                    authored.Scale.y * authored.SpriteSize.y / Mathf.Max(0.0001f, size.y) * ratio,
                    authored.Scale.z);
                return;
            }
            renderer.transform.localScale = new Vector3(
                canvasSize / Mathf.Max(0.0001f, size.x),
                canvasSize / Mathf.Max(0.0001f, size.y), renderer.transform.localScale.z);
        }

        private void ApplyFrameShine(int tier)
        {
            if (!Application.isPlaying || m_decorationRenderer == null || m_decorationRenderer.sprite == null) return;
            if (s_frameMaterial == null) s_frameMaterial = MakeMaterial("FrameShine");
            if (s_haloMaterial == null) s_haloMaterial = MakeMaterial("SelectionGlow");
            if (m_shineBlock == null) m_shineBlock = new MaterialPropertyBlock();
            if (m_shinePhase < 0f) m_shinePhase = SimulationRunner.SuppressCosmetics ? 0f : Random.Range(0f, 10f);
            float strength = (tier - 1) / 5f;

            if (s_frameMaterial != null)
            {
                m_decorationRenderer.sharedMaterial = s_frameMaterial;
                m_decorationRenderer.GetPropertyBlock(m_shineBlock);
                m_shineBlock.SetFloat(s_tierId, tier);
                m_shineBlock.SetFloat(s_phaseId, m_shinePhase);
                m_decorationRenderer.SetPropertyBlock(m_shineBlock);
            }

            if (s_haloMaterial == null) return;
            if (m_halo == null)
            {
                var halo = new GameObject("Frame Halo");
                halo.transform.SetParent(m_decorationRenderer.transform.parent, false);
                m_halo = halo.AddComponent<SpriteRenderer>();
                m_halo.sharedMaterial = s_haloMaterial;
            }

            Sprite frame = m_decorationRenderer.sprite;
            m_halo.sprite = frame;
            m_halo.transform.localPosition = m_decorationRenderer.transform.localPosition;
            m_halo.transform.localScale = m_decorationRenderer.transform.localScale * m_haloScale;
            m_halo.sortingLayerID = m_decorationRenderer.sortingLayerID;
            m_halo.sortingOrder = (m_backgroundRenderer != null ? m_backgroundRenderer.sortingOrder : m_decorationRenderer.sortingOrder) - 1;
            Color glow = m_decorationRenderer.color;
            glow.a = Mathf.Lerp(m_haloAlpha.x, m_haloAlpha.y, strength);
            m_halo.color = glow;

            m_shineBlock.Clear();
            m_halo.GetPropertyBlock(m_shineBlock);
            m_shineBlock.SetFloat(s_halfSizeId, frame.bounds.extents.x);
            m_shineBlock.SetFloat(s_edgeId, m_frameEdge / m_haloScale);
            m_shineBlock.SetFloat(s_cornerId, m_frameCorner);
            m_shineBlock.SetFloat(s_lineId, 0.012f);
            m_shineBlock.SetFloat(s_speedId, Mathf.Lerp(0.8f, 1.8f, strength));
            m_shineBlock.SetFloat(s_intensityId, Mathf.Lerp(m_haloIntensity.x, m_haloIntensity.y, strength));
            m_halo.SetPropertyBlock(m_shineBlock);
            m_shineBlock.Clear();
        }

        private static Material MakeMaterial(string shaderName)
        {
            Shader shader = Resources.Load<Shader>(shaderName);
            return shader != null && shader.isSupported ? new Material(shader) { name = $"{shaderName} (Runtime)" } : null;
        }

        private void ApplyCharacterFrame(Sprite frameInside, float canvasSize)
        {
            if (m_characterRenderer == null || m_characterMask == null) return;
            m_characterRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (m_characterMask.sprite == null) m_characterMask.sprite = frameInside;
        }

        private struct Authored
        {
            public Vector3 Scale;
            public Vector3 SpriteSize;
        }

        private readonly System.Collections.Generic.Dictionary<SpriteRenderer, Authored> m_authored =
            new System.Collections.Generic.Dictionary<SpriteRenderer, Authored>();

        private void RememberAuthored(SpriteRenderer renderer)
        {
            if (renderer == null || m_authored.ContainsKey(renderer)) return;
            m_authored[renderer] = new Authored
            {
                Scale = renderer.transform.localScale,
                SpriteSize = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.zero,
            };
        }

        private void Awake()
        {
            CacheRenderers();
            RememberAuthored(m_backgroundRenderer);
            RememberAuthored(m_characterRenderer);
            RememberAuthored(m_decorationRenderer);
            if (m_characterMask != null && GetComponent<SortingGroup>() == null)
            {
                SortingGroup group = gameObject.AddComponent<SortingGroup>();
                if (m_characterRenderer != null) group.sortingLayerID = m_characterRenderer.sortingLayerID;
            }
        }

        private void OnValidate() => CacheRenderers();

        private void CacheRenderers()
        {
            if (m_backgroundRenderer == null) m_backgroundRenderer = FindRenderer("Background");
            if (m_characterRenderer == null) m_characterRenderer = FindRenderer("Character");
            if (m_decorationRenderer == null) m_decorationRenderer = FindRenderer("Decoration");
            if (m_characterMask == null) m_characterMask = GetComponentInChildren<SpriteMask>(true);
        }

        private SpriteRenderer FindRenderer(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }
    }
}
