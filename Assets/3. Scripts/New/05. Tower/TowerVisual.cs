using UnityEngine;
using UnityEngine.Serialization;

namespace SquareTD
{
    public class TowerVisual : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        [SerializeField] private SpriteRenderer m_backgroundRenderer;
        [SerializeField] private SpriteRenderer m_characterRenderer;
        [SerializeField] private SpriteRenderer m_decorationRenderer;
        [SerializeField] private SpriteRenderer m_emblemRenderer;
        [Header("정면 캐릭터")]
        [SerializeField] private TowerCharacterCatalog m_regularCharacters;
        [SerializeField] private SynergyCharacterCatalog m_synergyCharacters;
        [FormerlySerializedAs("m_swordCanvasWorldSize")]
        [SerializeField, Min(0.01f)] private float m_canvasWorldSize = 1f;
        [SerializeField, Min(0.01f)] private float m_synergyCanvasWorldSize = 1f;
        [Header("문양 / 정보 패널 아이콘")]
        [Tooltip("검, 도끼, 활, 창, 방패, 해머, 불, 물, 대지, 바람, 얼음, 전기, 빛, 어둠 순서.")]
        [SerializeField] private Sprite[] m_emblemSprites;
        [SerializeField] private Sprite[] m_tierSprites;
        [Header("문양 색상 (1 광역 / 2 단일 / 3 버프 / 4 디버프)")]
        [SerializeField] private Color m_splashColor = new Color32(226, 93, 98, 255);
        [SerializeField] private Color m_targetColor = new Color32(83, 138, 205, 255);
        [SerializeField] private Color m_buffColor = new Color32(213, 173, 40, 255);
        [SerializeField] private Color m_debuffColor = new Color32(32, 32, 39, 255);
        private int m_appliedTowerID;
        private int m_appliedVisualID;
        public bool UsesCharacterVisual { get; private set; }
        public Sprite CharacterSprite => m_characterRenderer != null ? m_characterRenderer.sprite : null;
        public Sprite ProjectileEmblemSprite => GetSprite(m_emblemSprites, m_appliedVisualID % 100);
        public Color ProjectileEmblemColor => GetEmblemColor((m_appliedVisualID % 1000) / 100);
        public Sprite TierSprite => m_appliedTowerID / 1000 == 6
            ? (m_decorationRenderer != null ? m_decorationRenderer.sprite : null)
            : GetSprite(m_tierSprites, m_appliedTowerID / 1000);
        public Color TierColor => m_appliedTowerID / 1000 == 6 && m_decorationRenderer != null
            ? m_decorationRenderer.color : Color.white;
        public Sprite ColorSprite => IsRegular && m_backgroundRenderer != null ? m_backgroundRenderer.sprite : null;
        public Sprite EmblemSprite => IsRegular ? GetSprite(m_emblemSprites, m_appliedTowerID % 100) : null;
        public Color EmblemColor => GetEmblemColor((m_appliedTowerID % 1000) / 100);
        private bool IsRegular => m_appliedTowerID / 1000 >= 1 && m_appliedTowerID / 1000 <= 5;

        public void Apply(int towerID)
        {
            m_appliedTowerID = towerID;
            m_appliedVisualID = towerID;
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
            float canvasSize = synergy ? m_synergyCanvasWorldSize : m_canvasWorldSize;
            Sprite background = m_backgroundRenderer != null ? m_backgroundRenderer.sprite : null;
            Sprite decoration = m_decorationRenderer != null ? m_decorationRenderer.sprite : null;
            ApplyLayer(m_backgroundRenderer, background, -1, canvasSize);
            ApplyLayer(m_characterRenderer, portrait, 0, canvasSize);
            ApplyLayer(m_decorationRenderer, decoration, 1, canvasSize);
            if (m_emblemRenderer != null)
            {
                m_emblemRenderer.sprite = synergy ? null : GetSprite(m_emblemSprites, towerID % 100);
                m_emblemRenderer.color = GetEmblemColor(attack);
                m_emblemRenderer.enabled = m_emblemRenderer.sprite != null;
                m_emblemRenderer.SetPropertyBlock(null);
                if (m_characterRenderer != null)
                {
                    m_emblemRenderer.sortingLayerID = m_characterRenderer.sortingLayerID;
                    m_emblemRenderer.sortingOrder = m_characterRenderer.sortingOrder + 2;
                }
            }
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
                renderer.transform.localPosition = m_characterRenderer.transform.localPosition;
                renderer.transform.localRotation = m_characterRenderer.transform.localRotation;
                renderer.sortingLayerID = m_characterRenderer.sortingLayerID;
                renderer.sortingOrder = m_characterRenderer.sortingOrder + orderOffset;
            }
            if (sprite == null) return;
            Vector3 size = sprite.bounds.size;
            renderer.transform.localScale = new Vector3(
                canvasSize / Mathf.Max(0.0001f, size.x),
                canvasSize / Mathf.Max(0.0001f, size.y), renderer.transform.localScale.z);
        }

        private Color GetEmblemColor(int attackClass) => attackClass switch
        {
            1 => m_splashColor, 2 => m_targetColor, 3 => m_buffColor, 4 => m_debuffColor, _ => Color.white
        };

        private static Sprite GetSprite(Sprite[] sprites, int id)
        {
            int index = id - 1;
            return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
        }

        private void Awake() => CacheRenderers();
        private void OnValidate() => CacheRenderers();

        private void CacheRenderers()
        {
            if (m_backgroundRenderer == null) m_backgroundRenderer = FindRenderer("Background");
            if (m_characterRenderer == null) m_characterRenderer = FindRenderer("Character");
            if (m_decorationRenderer == null) m_decorationRenderer = FindRenderer("Decoration");
            if (m_emblemRenderer == null) m_emblemRenderer = FindRenderer("Emblem");
        }

        private SpriteRenderer FindRenderer(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }
    }
}
