using UnityEngine;

// 맵 버프의 시각 표시만 담당한다. 실제 버프 계산은 TileManager에서 처리한다.
[DisallowMultipleComponent]
public sealed class PathTileBubbleEffect : MonoBehaviour
{
    [Header("기포 이미지")]
    [SerializeField] private Sprite m_bubbleSprite;

    [Header("표시 설정")]
    [SerializeField, Range(1, 2)] private int m_bubbleCount = 2;
    [SerializeField, Min(0.5f)] private float m_lifetime = 2.8f;
    [SerializeField, Range(0f, 1f)] private float m_opacity = 0.75f;

    private SpriteRenderer[] m_bubbles;
    private float[] m_ages;
    private float[] m_startX;
    private float[] m_sizes;
    private Vector2 m_tileSize = Vector2.one;
    private Color m_color = Color.white;
    private float m_spriteSize = 1f;
    private System.Random m_random;
    private int m_sortingLayerID;
    private int m_sortingOrder;

    private void Awake()
    {
        if (m_bubbleSprite == null)
        {
            Debug.LogWarning("[PathTileBubbleEffect] 기포 이미지가 없습니다.", this);
            enabled = false;
            return;
        }

        // 최초 생성 때만 오브젝트를 만들고, 이후에는 재사용한다.
        int count = Mathf.Clamp(m_bubbleCount, 1, 2);
        // 연출용 난수는 맵 버프 배치와 타워 소환의 난수에 영향을 주지 않는다.
        m_random = new System.Random(GetInstanceID());
        m_bubbles = new SpriteRenderer[count];
        m_ages = new float[count];
        m_startX = new float[count];
        m_sizes = new float[count];
        m_spriteSize = Mathf.Max(m_bubbleSprite.bounds.size.x, m_bubbleSprite.bounds.size.y, 0.001f);
        float startAge = (float)m_random.NextDouble();

        for (int i = 0; i < count; i++)
        {
            GameObject bubble = new GameObject($"Bubble_{i + 1}");
            bubble.transform.SetParent(transform, false);
            SpriteRenderer renderer = bubble.AddComponent<SpriteRenderer>();
            renderer.sprite = m_bubbleSprite;
            renderer.sortingLayerID = m_sortingLayerID;
            renderer.sortingOrder = m_sortingOrder;
            m_bubbles[i] = renderer;
            ResetBubble(i);
            // 타일마다 시작 시점은 다르게, 같은 타일의 기포끼리는 일정 간격으로 올라온다.
            m_ages[i] = (startAge + i / (float)count) % 1f;
            DrawBubble(i);
        }
    }

    // TileManager가 타일 크기, 버프 색상, 렌더 순서를 전달한다.
    public void Configure(Color color, Vector2 tileSize, int sortingLayerID, int sortingOrder)
    {
        m_color = Color.Lerp(color, Color.white, 0.25f);
        m_sortingLayerID = sortingLayerID;
        m_sortingOrder = sortingOrder;
        m_tileSize = new Vector2(Mathf.Max(0.01f, tileSize.x), Mathf.Max(0.01f, tileSize.y));
        if (m_bubbles == null) return;

        for (int i = 0; i < m_bubbles.Length; i++)
        {
            m_bubbles[i].sortingLayerID = sortingLayerID;
            m_bubbles[i].sortingOrder = sortingOrder;
            DrawBubble(i);
        }
    }

    private void Update()
    {
        if (m_bubbles == null) return;
        float step = Time.deltaTime / Mathf.Max(0.5f, m_lifetime);
        if (step <= 0f) return;
        for (int i = 0; i < m_bubbles.Length; i++)
        {
            m_ages[i] += step;
            if (m_ages[i] >= 1f)
            {
                m_ages[i] %= 1f;
                ResetBubble(i);
            }
            DrawBubble(i);
        }
    }

    private void ResetBubble(int index)
    {
        m_startX[index] = Mathf.Lerp(-0.045f, 0.045f, (float)m_random.NextDouble());
        m_sizes[index] = Mathf.Lerp(0.14f, 0.182f, (float)m_random.NextDouble());
    }

    private void DrawBubble(int index)
    {
        float age = m_ages[index];
        // 중앙에서만 위로 올라가므로 외곽의 화살표와 장식을 가리지 않는다.
        float sway = Mathf.Sin(age * Mathf.PI * 2f + index) * 0.009f;
        Transform bubble = m_bubbles[index].transform;
        bubble.localPosition = new Vector3(
            (m_startX[index] + sway) * m_tileSize.x,
            Mathf.Lerp(-0.07f, 0.07f, age) * m_tileSize.y, 0f);
        float size = m_sizes[index] * Mathf.Min(m_tileSize.x, m_tileSize.y);
        bubble.localScale = Vector3.one * (size / m_spriteSize) * Mathf.Lerp(0.85f, 1.1f, age);

        // 처음과 끝만 서서히 흐려지며, 과한 발광은 사용하지 않는다.
        float fade = Mathf.Clamp01(age / 0.15f) * Mathf.Clamp01((1f - age) / 0.3f);
        Color tint = m_color;
        tint.a = m_opacity * fade;
        m_bubbles[index].color = tint;
    }
}
