using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 같은 위성 프리팹을 맵 버프와 디버프 존에서 공용으로 쓰기 위한 공전 형태입니다.
/// </summary>
public enum TileSatelliteEffectType
{
    Buff,   // 대각선 두 축으로 공전: X 형태
    Debuff  // 수평/수직 두 축으로 공전: + 형태
}

/// <summary>
/// 솔 야누스 스타일 3D 2-Layer 위성 공전 이펙트 컨트롤러
/// - 전면 호 (Front Arc): 전면 궤도선(Front Track)의 Sorting Layer & Order에 동기화되어 타워 앞을 활주 (최대 1.35x)
/// - 외곽 전환점 (Outer Turn): 외곽으로 갈수록 점진적으로 축소 (0.85x)되어 뚝 끊김 없이 부드럽게 후면으로 전환
/// - 후면 호 (Back Arc): 후면 궤도선(Back Track)의 Sorting Layer & Order에 동기화되어 타워 뒤를 공전 (0.65x, 타워 본체에 자연 차폐)
/// </summary>
public class TileSatelliteOrbiter : MonoBehaviour
{
    [System.Serializable]
    public struct DebuffSymbolMapping
    {
        public DebuffTarget target;
        public Sprite symbolSprite;
    }

    [Header("Effect Type")]
    [Tooltip("Buff는 X자(대각선), Debuff는 +자(수평/수직) 궤도로 공전합니다.")]
    [SerializeField] private TileSatelliteEffectType m_effectType = TileSatelliteEffectType.Buff;

    [Header("Debuff Symbol Settings")]
    [Tooltip("Effect Type이 Debuff일 때 위성에 표시할 현재 디버프 종류")]
    [SerializeField] private DebuffTarget m_currentDebuffTarget = DebuffTarget.None;
    [Tooltip("지정된 디버프 심볼이 없을 때 사용할 기본 심볼")]
    [SerializeField] private Sprite m_defaultSymbolSprite;
    [SerializeField] private List<DebuffSymbolMapping> m_symbolMappings = new List<DebuffSymbolMapping>();

    [Header("Color & Tint Settings")]
    [Tooltip("적용할 버프/디버프 색상 (기본값: 흰색, HDR 지원)")]
    [ColorUsage(true, true)]
    [SerializeField] private Color m_buffColor = Color.white;
    [Tooltip("색상이 적용될 SpriteRenderer 목록 (비어있으면 하위에서 자동 수집)")]
    [SerializeField] private List<SpriteRenderer> m_renderers = new List<SpriteRenderer>();

    [Header("Track Renderers (Sorting Layer Synchronization)")]
    [Tooltip("타워 앞면에 렌더링될 전면 궤도선 (SpriteRenderer)")]
    [SerializeField] private SpriteRenderer m_frontTrackRenderer;
    [Tooltip("타워 뒷면에 렌더링될 후면 궤도선 (SpriteRenderer)")]
    [SerializeField] private SpriteRenderer m_backTrackRenderer;
    [Tooltip("궤도선 SpriteRenderer의 Sorting Layer 및 Order를 위성에 자동 동기화할지 여부")]
    [SerializeField] private bool m_syncWithTrackRenderers = true;

    [Header("Orbit Dimensions & Speed")]
    [Tooltip("공전 대각선 반경")]
    [SerializeField] private float m_radiusMajor = 0.60f;
    [Tooltip("3D 깊이감 타원 단축 반경")]
    [SerializeField] private float m_radiusMinor = 0.30f;
    [Tooltip("공전 회전 속도")]
    [SerializeField] private float m_orbitSpeed = 2.4f;

    [Header("Satellites (Transforms)")]
    [Tooltip("+45도 궤도 위성들")]
    [SerializeField] private Transform[] m_orbit1Satellites;
    [Tooltip("-45도 궤도 위성들")]
    [SerializeField] private Transform[] m_orbit2Satellites;

    [Header("3D Layering & Order Offsets")]
    [Tooltip("전면 궤도선 기준 위성 Sorting Order 오프셋 (기본 +1: 전면 궤도선 바로 위에 렌더링)")]
    [SerializeField] private int m_frontOrderOffset = 1;
    [Tooltip("후면 궤도선 기준 위성 Sorting Order 오프셋 (기본 +1: 후면 궤도선 바로 위에 렌더링, 타워 뒤)")]
    [SerializeField] private int m_backOrderOffset = 1;

    [Header("Fallback Sorting (트랙 렌더러 미할당 시 사용)")]
    [Tooltip("전면 Sorting Layer 이름 (예: Tower 또는 Default)")]
    [SerializeField] private string m_fallbackFrontSortingLayer = "Tower";
    [Tooltip("전면 Sorting Order")]
    [SerializeField] private int m_fallbackFrontSortingOrder = 16;
    [Tooltip("후면 Sorting Layer 이름 (예: MapTile 또는 Default)")]
    [SerializeField] private string m_fallbackBackSortingLayer = "MapTile";
    [Tooltip("후면 Sorting Order")]
    [SerializeField] private int m_fallbackBackSortingOrder = 1;

    [Header("3D Smooth Scale (스케일 원근 설정)")]
    [Tooltip("타워 전면 정점을 지날 때의 최대 스케일")]
    [SerializeField] private float m_frontScale = 1.25f;
    [Tooltip("궤도 외곽 전환점(depth=0)을 지날 때의 스케일 (외곽으로 갈수록 자연스럽게 축소)")]
    [SerializeField] private float m_outerScale = 0.95f;
    [Tooltip("타워 후면 정점을 지날 때의 최소 스케일")]
    [SerializeField] private float m_backScale = 0.75f;

    [Header("3D Bloom & Brightness (앞으로 올 때 하얀 빛 발광)")]
    [Tooltip("앞으로 올 때 Bloom/밝기를 동적으로 조절할지 여부")]
    [SerializeField] private bool m_enableBloomEffect = true;
    [Tooltip("앞으로 올수록 순백색(White-Hot)으로 타오르는 비율 (0: 원래 색상 유지 ~ 1: 완전한 하얀 빛 발광, 0.75 권장)")]
    [Range(0f, 1f)]
    [SerializeField] private float m_frontWhitenAmount = 0.01f;
    [Tooltip("타워 전면(Front) 정점을 지날 때의 Bloom/밝기 배수 (2.0 ~ 3.5 권장: 강력한 HDR 하얀 발광)")]
    [SerializeField] private float m_frontBloomIntensity = 1f;
    [Tooltip("궤도 외곽 전환점을 지날 때의 Bloom/밝기 배수 (1.0: 기본 밝기)")]
    [SerializeField] private float m_outerBloomIntensity = 0.9f;
    [Tooltip("타워 후면(Back) 정점을 지날 때의 Bloom/밝기 배수 (0.5 ~ 0.8: 어두워짐)")]
    [SerializeField] private float m_backBloomIntensity = 0.75f;

    private float m_currentAngle = 0f;
    private SpriteRenderer[] m_orbit1Renderers;
    private SpriteRenderer[] m_orbit2Renderers;
    private Dictionary<DebuffTarget, Sprite> m_symbolMapCache;

    private void Awake()
    {
        BuildSymbolMapCache();
        AutoDetectReferences();
        CacheRenderers();
        ApplyDebuffSymbol(m_currentDebuffTarget);
        ApplyColor(m_buffColor);
    }

    private void BuildSymbolMapCache()
    {
        m_symbolMapCache = new Dictionary<DebuffTarget, Sprite>();
        foreach (DebuffSymbolMapping mapping in m_symbolMappings)
        {
            if (mapping.symbolSprite != null && !m_symbolMapCache.ContainsKey(mapping.target))
            {
                m_symbolMapCache.Add(mapping.target, mapping.symbolSprite);
            }
        }
    }

    private void AutoDetectReferences()
    {
        if (m_renderers == null || m_renderers.Count == 0)
        {
            GetComponentsInChildren(true, m_renderers);
        }

        // 트랙 렌더러가 비어있으면 하위 이름 기반 자동 탐색
        if (m_frontTrackRenderer == null || m_backTrackRenderer == null)
        {
            SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in allRenderers)
            {
                if (r == null) continue;
                string lower = r.gameObject.name.ToLower();
                if (m_frontTrackRenderer == null && (lower.Contains("front") || lower.Contains("앞")))
                {
                    m_frontTrackRenderer = r;
                }
                else if (m_backTrackRenderer == null && (lower.Contains("back") || lower.Contains("뒤")))
                {
                    m_backTrackRenderer = r;
                }
            }
        }
    }

    private void CacheRenderers()
    {
        if (m_orbit1Satellites != null)
        {
            m_orbit1Renderers = new SpriteRenderer[m_orbit1Satellites.Length];
            for (int i = 0; i < m_orbit1Satellites.Length; i++)
            {
                if (m_orbit1Satellites[i] != null)
                {
                    m_orbit1Renderers[i] = m_orbit1Satellites[i].GetComponentInChildren<SpriteRenderer>(true);
                }
            }
        }

        if (m_orbit2Satellites != null)
        {
            m_orbit2Renderers = new SpriteRenderer[m_orbit2Satellites.Length];
            for (int i = 0; i < m_orbit2Satellites.Length; i++)
            {
                if (m_orbit2Satellites[i] != null)
                {
                    m_orbit2Renderers[i] = m_orbit2Satellites[i].GetComponentInChildren<SpriteRenderer>(true);
                }
            }
        }
    }

    private void Update()
    {
        // 맵 버프(X)는 기존 시계방향을 유지하고,
        // 디버프 존(+)의 가로/세로 궤도는 함께 반시계방향으로 회전한다.
        float rotationDirection = m_effectType == TileSatelliteEffectType.Debuff ? -1f : 1f;
        m_currentAngle = Mathf.Repeat(
            m_currentAngle + (m_orbitSpeed * rotationDirection * Time.deltaTime),
            Mathf.PI * 2f);

        // 버프 타일은 X자, 디버프 존은 +자 배치를 사용한다.
        // 위성 Transform/Renderer 구성은 그대로 재사용하고 공전 축만 바꾼다.
        float firstOrbitAngle = m_effectType == TileSatelliteEffectType.Buff ? 45f : 0f;
        float secondOrbitAngle = m_effectType == TileSatelliteEffectType.Buff ? -45f : 90f;

        UpdateOrbitSatellites(m_orbit1Satellites, m_orbit1Renderers, firstOrbitAngle, 0f);
        UpdateOrbitSatellites(m_orbit2Satellites, m_orbit2Renderers, secondOrbitAngle, Mathf.PI * 0.5f);
    }

    private void UpdateOrbitSatellites(Transform[] satellites, SpriteRenderer[] renderers, float rotAngleDeg, float phaseOffset)
    {
        if (satellites == null || satellites.Length == 0) return;

        float rad = rotAngleDeg * Mathf.Deg2Rad;
        float cosRot = Mathf.Cos(rad);
        float sinRot = Mathf.Sin(rad);

        int count = satellites.Length;
        for (int i = 0; i < count; i++)
        {
            if (satellites[i] == null) continue;

            float theta = m_currentAngle + phaseOffset + ((Mathf.PI * 2f / count) * i);
            float u = m_radiusMajor * Mathf.Cos(theta);
            float v = m_radiusMinor * Mathf.Sin(theta);

            // Unity 2D 좌표 변환 (Y축 하향 보정)
            float x = u * cosRot - v * sinRot;
            float y = -(u * sinRot + v * cosRot);

            satellites[i].localPosition = new Vector3(x, y, 0f);

            // 3D Depth (+1: 전면 정점, 0: 외곽 전환점, -1: 후면 정점)
            float depth = Mathf.Sin(theta);

            // 1. 부드러운 3단 스케일 보간 (외곽으로 갈수록 자연스럽게 축소되어 뚝 끊김 방지)
            float scale;
            if (depth >= 0f)
            {
                // depth 0 -> 1은 전면 호입니다. 전면으로 올수록 크게 보입니다.
                scale = Mathf.Lerp(m_outerScale, m_frontScale, depth);
            }
            else
            {
                // depth 0 -> -1은 후면 호입니다. 뒤로 갈수록 작게 보입니다.
                scale = Mathf.Lerp(m_outerScale, m_backScale, -depth);
            }
            satellites[i].localScale = new Vector3(scale, scale, 1f);

            // 2. Sorting Layer 및 Sorting Order 동기화
            SpriteRenderer sr = (renderers != null && i < renderers.Length && renderers[i] != null)
                ? renderers[i]
                : satellites[i].GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                SyncSorting(sr, depth);

                // 3. Bloom & White-Hot 발광 동적 조절 (앞으로 올 때 눈부신 하얀 빛 발광)
                if (m_enableBloomEffect)
                {
                    float bloomMult;
                    float whitenRatio;

                    if (depth >= 0f)
                    {
                        // 전면은 밝아지고 백색 발광이 강해집니다.
                        bloomMult = Mathf.Lerp(m_outerBloomIntensity, m_frontBloomIntensity, depth);
                        whitenRatio = depth;
                    }
                    else
                    {
                        // 후면은 작고 어둡게 보여 타워/타일 뒤로 넘어간 느낌을 만듭니다.
                        bloomMult = Mathf.Lerp(m_outerBloomIntensity, m_backBloomIntensity, -depth);
                        whitenRatio = 0f;
                    }

                    // 전면으로 올수록 순백색(White-Hot)으로 블렌딩하여 하얀 코어 형성
                    Color baseC = Color.Lerp(m_buffColor, Color.white, whitenRatio * m_frontWhitenAmount);
                    baseC.a = m_buffColor.a;

                    // HDR 배수를 곱해 RGB 전체가 Bloom 임계값을 넘어 눈부신 백색광 방출
                    sr.color = new Color(baseC.r * bloomMult, baseC.g * bloomMult, baseC.b * bloomMult, baseC.a);
                }
            }
        }
    }

    private void SyncSorting(SpriteRenderer sr, float depth)
    {
        if (depth >= 0f)
        {
            // [전면 호] 전면 궤도선과 동기화
            if (m_syncWithTrackRenderers && m_frontTrackRenderer != null)
            {
                sr.sortingLayerID = m_frontTrackRenderer.sortingLayerID;
                sr.sortingOrder = m_frontTrackRenderer.sortingOrder + m_frontOrderOffset;
            }
            else
            {
                if (!string.IsNullOrEmpty(m_fallbackFrontSortingLayer))
                {
                    sr.sortingLayerName = m_fallbackFrontSortingLayer;
                }
                sr.sortingOrder = m_fallbackFrontSortingOrder;
            }
        }
        else
        {
            // [후면 호] 후면 궤도선과 동기화
            if (m_syncWithTrackRenderers && m_backTrackRenderer != null)
            {
                sr.sortingLayerID = m_backTrackRenderer.sortingLayerID;
                sr.sortingOrder = m_backTrackRenderer.sortingOrder + m_backOrderOffset;
            }
            else
            {
                if (!string.IsNullOrEmpty(m_fallbackBackSortingLayer))
                {
                    sr.sortingLayerName = m_fallbackBackSortingLayer;
                }
                sr.sortingOrder = m_fallbackBackSortingOrder;
            }
        }
    }

    /// <summary>
    /// 런타임에 버프/디버프 색상을 변경합니다.
    /// </summary>
    public void SetColor(Color color)
    {
        m_buffColor = color;
        ApplyColor(color);
    }

    /// <summary>
    /// 런타임에 공전 형태를 바꿉니다.
    /// 일반적으로는 프리팹 Inspector에서 설정하면 되며,
    /// 특수한 연출에서만 이 메서드를 사용합니다.
    /// </summary>
    public void SetEffectType(TileSatelliteEffectType effectType)
    {
        m_effectType = effectType;

        if (m_effectType == TileSatelliteEffectType.Debuff)
        {
            ApplyDebuffSymbol(m_currentDebuffTarget);
        }
    }

    /// <summary>
    /// 디버프 존에서 무기군에 맞는 위성 심볼을 표시할 때 사용합니다.
    /// 맵 버프 이펙트에는 심볼 매핑이 비어 있어도 안전하게 무시됩니다.
    /// </summary>
    public void SetDebuffType(DebuffTarget target)
    {
        m_currentDebuffTarget = target;
        ApplyDebuffSymbol(target);
    }

    private void ApplyDebuffSymbol(DebuffTarget target)
    {
        Sprite symbol = GetDebuffSymbolSprite(target);
        if (symbol == null) return;

        SetSatelliteSprites(m_orbit1Renderers, symbol);
        SetSatelliteSprites(m_orbit2Renderers, symbol);
    }

    private Sprite GetDebuffSymbolSprite(DebuffTarget target)
    {
        if (m_symbolMapCache == null)
        {
            BuildSymbolMapCache();
        }

        if (m_symbolMapCache.TryGetValue(target, out Sprite symbol) && symbol != null)
        {
            return symbol;
        }

        return m_defaultSymbolSprite;
    }

    private static void SetSatelliteSprites(SpriteRenderer[] renderers, Sprite sprite)
    {
        if (renderers == null) return;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sprite = sprite;
            }
        }
    }

    private void ApplyColor(Color color)
    {
        for (int i = 0; i < m_renderers.Count; i++)
        {
            if (m_renderers[i] != null)
            {
                m_renderers[i].color = color;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildSymbolMapCache();
        AutoDetectReferences();
        CacheRenderers();
        ApplyDebuffSymbol(m_currentDebuffTarget);
        ApplyColor(m_buffColor);
    }
#endif
}
