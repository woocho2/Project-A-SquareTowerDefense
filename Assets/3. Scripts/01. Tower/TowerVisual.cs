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
    [Tooltip("Index 0 = type/color 1, index 1 = type/color 2, ...")]
    [SerializeField] private Sprite[] m_colorSprites;
    [Tooltip("Index 0 = Sword(1), index 1 = Bow(2), ...")]
    [SerializeField] private Sprite[] m_emblemSprites;

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
    /// towerID ?뺤떇: [Tier][Type][Variant]
    /// ?? 1203 = Tier 1, Color(Type) 2, Emblem(Variant) 3
    /// </summary>
    public void Apply(int towerID)
    {
        int tier = towerID / 1000;
        int type = (towerID % 1000) / 100;
        int variant = towerID % 100;

        SetSprite(m_tierRenderer, m_tierSprites, tier, "Tier", towerID);
        SetSprite(m_colorRenderer, m_colorSprites, type, "Color", towerID);
        SetSprite(m_emblemRenderer, m_emblemSprites, variant, "Emblem", towerID);
    }

    public void Apply(TowerData towerData)
    {
        if (towerData == null)
        {
            Debug.LogWarning("[TowerVisual] TowerData媛 ?놁뒿?덈떎.", this);
            return;
        }

        Apply(towerData.towerID);
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
            Debug.LogWarning($"[TowerVisual] {visualName} SpriteRenderer媛 ?곌껐?섏? ?딆븯?듬땲??", this);
            return;
        }

        int index = visualID - 1;
        if (index < 0 || sprites == null || index >= sprites.Length || sprites[index] == null)
        {
            Debug.LogWarning($"[TowerVisual] TowerID {towerID}??{visualName} ID {visualID}???대떦?섎뒗 ?ㅽ봽?쇱씠?멸? ?놁뒿?덈떎.", this);
            renderer.sprite = null;
            return;
        }

        renderer.sprite = sprites[index];
    }
}