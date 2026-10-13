using UnityEngine;

public class TowerVisual : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer m_emblemRenderer;
    [SerializeField] private SpriteRenderer m_colorRenderer;
    [SerializeField] private SpriteRenderer m_tierRenderer;
    [SerializeField] private SpriteRenderer m_frameRimRenderer;
    [SerializeField] private SpriteRenderer m_headgearRenderer;

    [Header("Sprite Tables (index 0 = ID 1)")]
    [Tooltip("Index 0 = tier 1, index 1 = tier 2, ...")]
    [SerializeField] private Sprite[] m_tierSprites;
    [Tooltip("문양 뒤에 표시하는 순백색 배경입니다. 공격방식의 색은 문양에만 적용합니다.")]
    [SerializeField] private Sprite m_backgroundSprite;
    [Tooltip("문양 ID 순서입니다. 01 검, 02 도끼, 03 활, 04 창, 05 방패, 06 해머, 07 불, 08 얼음, 09 전기, 10 바람, 11 대지, 12 빛, 13 어둠.")]
    [SerializeField] private Sprite[] m_emblemSprites;

    [Header("검 타워 레이어 (광역 / 단일 / 버프 / 디버프)")]
    [Tooltip("얼굴·머리카락 레이어입니다. 프레임 아래에 표시합니다.")]
    [SerializeField] private Sprite[] m_swordCharacterSprites;
    [SerializeField] private Sprite[] m_swordHeadgearSprites;
    [Header("도끼 타워 레이어 (바이킹 / 버서커 / 스칼드 / 집행관)")]
    [SerializeField] private Sprite[] m_axeCharacterSprites;
    [SerializeField] private Sprite[] m_axeHeadgearSprites;
    [Header("검·도끼 공통 프레임")]
    [Tooltip("브론즈, 실버, 골드, 미스릴, 다이아 순서의 별도 캐릭터 테두리입니다.")]
    [SerializeField] private Sprite[] m_characterTierSprites;
    [SerializeField] private Sprite[] m_characterRimSprites;
    [Tooltip("검·도끼의 모든 레이어에 적용되는 공통 크기입니다. 기본값은 256px / 192 PPU 기준의 95%입니다.")]
    [SerializeField, Min(0.01f)] private float m_swordCanvasWorldSize = (256f / 192f) * 0.95f;

    [Header("정적 시너지 캐릭터")]
    [SerializeField] private SynergyCharacterCatalog m_synergyCharacters;
    [SerializeField] private Material m_synergyCharacterMaterial;
    [SerializeField, Range(0f,32f)] private float m_synergyCornerRadius = 8f;
    [Tooltip("256px / 192 PPU 스폰 타일과 같은 캔버스 크기입니다. 그림의 투명 여백으로 장식과 겹치지 않게 합니다.")]
    [SerializeField, Min(0.01f)] private float m_synergyCanvasWorldSize = 256f / 192f;
    private Vector3 m_defaultEmblemScale;
    private bool m_hasDefaultEmblemScale;
    private Material m_defaultEmblemMaterial;
    private MaterialPropertyBlock m_characterProperties;
    private Vector3 m_defaultTierScale;
    private Vector3 m_defaultTierPosition;
    private Quaternion m_defaultTierRotation;
    private Material m_defaultTierMaterial;
    private int m_defaultTierSortingLayer;
    private int m_defaultTierSortingOrder;
    private bool m_hasDefaultTierTransform;

    public bool UsesCharacterVisual { get; private set; }
    private int m_appliedVisualID;

    // 타워의 얼굴과 별개로, 전용 그림이 없는 투사체는 기존 문양과 공격방식 색을 사용합니다.
    public Sprite ProjectileEmblemSprite => GetSprite(m_emblemSprites, m_appliedVisualID % 100);
    public Color ProjectileEmblemColor => GetEmblemColor((m_appliedVisualID % 1000) / 100);

    [Header("문양 색상 (ID의 백의 자리: 1 광역, 2 단일, 3 버프, 4 디버프)")]
    [SerializeField] private Color m_splashColor = new Color32(226, 93, 98, 255);
    [SerializeField] private Color m_targetColor = new Color32(83, 138, 205, 255);
    [SerializeField] private Color m_buffColor = new Color32(213, 173, 40, 255);
    [SerializeField] private Color m_debuffColor = new Color32(32, 32, 39, 255);

    public Color EmblemColor
    {
        get
        {
            CacheRenderers();
            return m_emblemRenderer != null ? m_emblemRenderer.color : Color.white;
        }
    }

    public Sprite TierSprite
    {
        get
        {
            CacheRenderers();
            return m_tierRenderer != null && m_tierRenderer.enabled ? m_tierRenderer.sprite : null;
        }
    }

    public Sprite ColorSprite
    {
        get
        {
            CacheRenderers();
            return m_colorRenderer != null && m_colorRenderer.enabled ? m_colorRenderer.sprite : null;
        }
    }

    public Sprite EmblemSprite
    {
        get
        {
            CacheRenderers();
            return m_emblemRenderer != null ? m_emblemRenderer.sprite : null;
        }
    }

    public Sprite FrameRimSprite => m_frameRimRenderer != null && m_frameRimRenderer.enabled
        ? m_frameRimRenderer.sprite : null;
    public Sprite HeadgearSprite => m_headgearRenderer != null && m_headgearRenderer.enabled
        ? m_headgearRenderer.sprite : null;

    /// <summary>
    /// towerID 형식: [천의 자리: 티어][백의 자리: 색깔][뒤 두 자리: 문양]
    /// 예: 1202 = 1티어 테두리 + 파란색(단일 타워) + 도끼 문양(02).
    /// </summary>
    public void Apply(int towerID)
    {
        m_appliedVisualID = towerID;
        if (TryApplySynergyCharacter(towerID)) return;
        if (TryApplyLayeredCharacter(towerID)) return;
        int tier = towerID / 1000;
        int color = (towerID % 1000) / 100;
        int emblem = towerID % 100;

        ApplyVisual(tier, color, emblem, towerID);
    }

    public void Apply(TowerData towerData)
    {
        if (towerData == null)
        {
            Debug.LogWarning("[TowerVisual] 타워 데이터가 없습니다.", this);
            return;
        }

        m_appliedVisualID = towerData.VisualTowerID;
        // 실제 소환도 정수 ID를 적용할 때와 같은 방식으로 문양과 색깔을 결정합니다.
        if (towerData.Tier == 6 && TryApplySynergyCharacter(towerData.towerID)) return;
        // 소환·강화도 같은 ID 경로에서 검·도끼 캐릭터 또는 기존 문양 외형을 선택합니다.
        Apply(towerData.VisualTowerID);
    }

    private void ApplyVisual(int tier, int color, int emblem, int towerID)
    {
        CacheRenderers();
        HideCharacterOverlays();
        UsesCharacterVisual = false;
        RestoreTierTransform();
        if (m_emblemRenderer != null)
        {
            m_emblemRenderer.enabled = true;
            m_emblemRenderer.transform.localScale = m_defaultEmblemScale;
            m_emblemRenderer.sharedMaterial = m_defaultEmblemMaterial;
            m_emblemRenderer.SetPropertyBlock(null);
        }
        if (m_tierRenderer != null) m_tierRenderer.enabled = true;
        if (m_colorRenderer != null) m_colorRenderer.enabled = true;
        SetSprite(m_tierRenderer, m_tierSprites, tier, "Tier", towerID);
        SetSprite(m_emblemRenderer, m_emblemSprites, emblem, "Emblem", towerID);

        // 원본 PNG는 흰색 하나만 사용하고, 문양에만 공격방식의 색을 곱합니다.
        if (m_emblemRenderer != null)
            m_emblemRenderer.color = GetEmblemColor(color);

        if (m_colorRenderer != null)
        {
            m_colorRenderer.sprite = m_backgroundSprite;
            m_colorRenderer.color = Color.white;
        }
    }

    private Color GetEmblemColor(int colorID)
    {
        return colorID switch
        {
            1 => m_splashColor,
            2 => m_targetColor,
            3 => m_buffColor,
            4 => m_debuffColor,
            _ => Color.white
        };
    }

    private bool TryApplyLayeredCharacter(int towerID)
    {
        int tier = towerID / 1000;
        int attack = (towerID % 1000) / 100;
        int emblem = towerID % 100;
        if ((emblem != TowerEmblem.SWORD && emblem != TowerEmblem.AXE)
            || tier < 1 || tier > 5 || attack < 1 || attack > 4)
            return false;

        Sprite character = GetSprite(emblem == TowerEmblem.SWORD ? m_swordCharacterSprites : m_axeCharacterSprites, attack);
        Sprite headgear = GetSprite(emblem == TowerEmblem.SWORD ? m_swordHeadgearSprites : m_axeHeadgearSprites, attack);
        Sprite frame = GetSprite(m_characterTierSprites, tier);
        Sprite rim = GetSprite(m_characterRimSprites, tier);
        CacheRenderers();
        if (character == null || headgear == null || frame == null || rim == null
            || m_emblemRenderer == null || m_tierRenderer == null
            || m_frameRimRenderer == null || m_headgearRenderer == null)
            return false;

        m_emblemRenderer.sprite = character;
        m_emblemRenderer.color = Color.white;
        m_emblemRenderer.enabled = true;
        m_emblemRenderer.sharedMaterial = m_defaultEmblemMaterial;
        m_emblemRenderer.SetPropertyBlock(null);
        m_emblemRenderer.flipX = false;
        m_emblemRenderer.flipY = false;
        SetCanvasScale(m_emblemRenderer, character, m_swordCanvasWorldSize, m_defaultEmblemScale.z);

        // 흰 배경 → 얼굴·머리카락 → 투명 중앙 테두리 → 투구·장식.
        ApplyCharacterOverlay(m_tierRenderer, frame, -1, m_defaultTierScale.z);
        ApplyCharacterOverlay(m_frameRimRenderer, rim, 1, m_defaultEmblemScale.z);
        ApplyCharacterOverlay(m_headgearRenderer, headgear, 2, m_defaultEmblemScale.z);
        if (m_colorRenderer != null) m_colorRenderer.enabled = false;
        UsesCharacterVisual = true;
        return true;
    }

    private void ApplyCharacterOverlay(SpriteRenderer renderer, Sprite sprite, int orderOffset, float zScale)
    {
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.enabled = true;
        renderer.sharedMaterial = m_defaultEmblemMaterial;
        renderer.SetPropertyBlock(null);
        renderer.flipX = false;
        renderer.flipY = false;
        renderer.transform.localPosition = m_emblemRenderer.transform.localPosition;
        renderer.transform.localRotation = m_emblemRenderer.transform.localRotation;
        renderer.sortingLayerID = m_emblemRenderer.sortingLayerID;
        renderer.sortingOrder = m_emblemRenderer.sortingOrder + orderOffset;
        SetCanvasScale(renderer, sprite, m_swordCanvasWorldSize, zScale);
    }

    private void HideCharacterOverlays()
    {
        if (m_frameRimRenderer != null)
        {
            m_frameRimRenderer.enabled = false;
            m_frameRimRenderer.sprite = null;
        }
        if (m_headgearRenderer != null)
        {
            m_headgearRenderer.enabled = false;
            m_headgearRenderer.sprite = null;
        }
    }

    private static Sprite GetSprite(Sprite[] sprites, int visualID)
    {
        int index = visualID - 1;
        return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
    }

    private static void SetCanvasScale(SpriteRenderer renderer, Sprite sprite, float canvasSize, float zScale)
    {
        Vector3 size = sprite.bounds.size;
        renderer.transform.localScale = new Vector3(
            canvasSize / Mathf.Max(0.0001f, size.x),
            canvasSize / Mathf.Max(0.0001f, size.y), zScale);
    }

    private void RestoreTierTransform()
    {
        if (m_tierRenderer == null || !m_hasDefaultTierTransform) return;
        m_tierRenderer.transform.localScale = m_defaultTierScale;
        m_tierRenderer.transform.localPosition = m_defaultTierPosition;
        m_tierRenderer.transform.localRotation = m_defaultTierRotation;
        m_tierRenderer.sharedMaterial = m_defaultTierMaterial;
        m_tierRenderer.sortingLayerID = m_defaultTierSortingLayer;
        m_tierRenderer.sortingOrder = m_defaultTierSortingOrder;
    }

    private bool TryApplySynergyCharacter(int towerID)
    {
        if (towerID < 6001 || towerID > 6013) return false;
        CacheRenderers();
        if (m_synergyCharacters == null)
            m_synergyCharacters = Resources.Load<SynergyCharacterCatalog>("SynergyCharacterCatalog");
        Sprite front = m_synergyCharacters != null ? m_synergyCharacters.GetFront(towerID) : null;
        if (front == null || m_emblemRenderer == null) return false;
        HideCharacterOverlays();
        m_emblemRenderer.sprite = front;
        m_emblemRenderer.color = Color.white;
        m_emblemRenderer.enabled = true;
        if(m_synergyCharacterMaterial==null)m_synergyCharacterMaterial=Resources.Load<Material>("CharacterRounded");
        if(m_synergyCharacterMaterial!=null)m_emblemRenderer.sharedMaterial=m_synergyCharacterMaterial;
        if(m_characterProperties==null)m_characterProperties=new MaterialPropertyBlock();
        Rect rect=front.rect;
        m_emblemRenderer.GetPropertyBlock(m_characterProperties);
        m_characterProperties.SetVector("_SpriteRect",new Vector4(rect.x,rect.y,rect.width,rect.height));
        m_characterProperties.SetVector("_HeadRect",new Vector4(16,16,224,224));
        m_characterProperties.SetFloat("_CornerRadius",m_synergyCornerRadius);
        m_emblemRenderer.SetPropertyBlock(m_characterProperties);
        m_emblemRenderer.flipX = false;
        m_emblemRenderer.flipY = false;
        Vector3 size = front.bounds.size;
        m_emblemRenderer.transform.localScale = new Vector3(
            m_synergyCanvasWorldSize / Mathf.Max(0.0001f,size.x),
            m_synergyCanvasWorldSize / Mathf.Max(0.0001f,size.y),m_defaultEmblemScale.z);
        if (m_tierRenderer != null) m_tierRenderer.enabled = false;
        if (m_colorRenderer != null) m_colorRenderer.enabled = false;
        UsesCharacterVisual = true;
        return true;
    }

    private void Awake()
    {
        CacheRenderers();
    }

    private void OnValidate()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        if (m_emblemRenderer == null)
            m_emblemRenderer = FindRenderer("Emblem");

        if (m_colorRenderer == null)
            m_colorRenderer = FindRenderer("Color");

        if (m_tierRenderer == null)
            m_tierRenderer = FindRenderer("Tier");
        if (m_frameRimRenderer == null)
            m_frameRimRenderer = FindRenderer("FrameRim");
        if (m_headgearRenderer == null)
            m_headgearRenderer = FindRenderer("Headgear");
        if (!m_hasDefaultEmblemScale && m_emblemRenderer != null)
        {
            m_defaultEmblemScale = m_emblemRenderer.transform.localScale;
            m_defaultEmblemMaterial = m_emblemRenderer.sharedMaterial;
            m_hasDefaultEmblemScale = true;
        }
        if (!m_hasDefaultTierTransform && m_tierRenderer != null)
        {
            m_defaultTierScale = m_tierRenderer.transform.localScale;
            m_defaultTierPosition = m_tierRenderer.transform.localPosition;
            m_defaultTierRotation = m_tierRenderer.transform.localRotation;
            m_defaultTierMaterial = m_tierRenderer.sharedMaterial;
            m_defaultTierSortingLayer = m_tierRenderer.sortingLayerID;
            m_defaultTierSortingOrder = m_tierRenderer.sortingOrder;
            m_hasDefaultTierTransform = true;
        }
    }

    private SpriteRenderer FindRenderer(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<SpriteRenderer>() : null;
    }

    private void SetSprite(
        SpriteRenderer renderer,
        Sprite[] sprites,
        int visualID,
        string visualName,
        int towerID)
    {
        if (renderer == null)
        {
            Debug.LogWarning($"[TowerVisual] {visualName} SpriteRenderer가 연결되지 않았습니다.", this);
            return;
        }

        int index = visualID - 1;
        if (index < 0 || sprites == null || index >= sprites.Length || sprites[index] == null)
        {
            Debug.LogWarning($"[TowerVisual] TowerID {towerID}의 {visualName} ID {visualID}에 해당하는 스프라이트가 없습니다.", this);
            renderer.sprite = null;
            return;
        }

        renderer.sprite = sprites[index];
    }
}
