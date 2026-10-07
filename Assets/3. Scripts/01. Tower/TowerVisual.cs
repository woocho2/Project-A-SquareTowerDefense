using UnityEngine;

public class TowerVisual : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] private SpriteRenderer m_emblemRenderer;
    [SerializeField] private SpriteRenderer m_colorRenderer;
    [SerializeField] private SpriteRenderer m_tierRenderer;

    [Header("Sprite Tables (index 0 = ID 1)")]
    [Tooltip("Index 0 = tier 1, index 1 = tier 2, ...")]
    [SerializeField] private Sprite[] m_tierSprites;
    [Tooltip("문양 뒤에 표시하는 순백색 배경입니다. 공격방식의 색은 문양에만 적용합니다.")]
    [SerializeField] private Sprite m_backgroundSprite;
    [Tooltip("문양 ID 순서입니다. 01 검, 02 도끼, 03 활, 04 창, 05 방패, 06 해머, 07 불, 08 얼음, 09 전기, 10 바람, 11 대지, 12 빛, 13 어둠.")]
    [SerializeField] private Sprite[] m_emblemSprites;

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
            return m_tierRenderer != null ? m_tierRenderer.sprite : null;
        }
    }

    public Sprite ColorSprite
    {
        get
        {
            CacheRenderers();
            return m_colorRenderer != null ? m_colorRenderer.sprite : null;
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

    /// <summary>
    /// towerID 형식: [천의 자리: 티어][백의 자리: 색깔][뒤 두 자리: 문양]
    /// 예: 1202 = 1티어 테두리 + 파란색(단일 타워) + 도끼 문양(02).
    /// </summary>
    public void Apply(int towerID)
    {
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

        // 실제 소환도 정수 ID를 적용할 때와 같은 방식으로 문양과 색깔을 결정합니다.
        // 시너지 타워는 기존 외형 ID(VisualTowerID)를 사용합니다.
        Apply(towerData.VisualTowerID);
    }

    private void ApplyVisual(int tier, int color, int emblem, int towerID)
    {
        CacheRenderers();
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
