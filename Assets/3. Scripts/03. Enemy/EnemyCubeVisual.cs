using System.Collections.Generic;
using UnityEngine;

/// <summary>두 면을 정사영해 모서리를 넘어가는 큐브를 그립니다. 루트 충돌체와 HP UI는 변형하지 않습니다.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyCubeVisual : MonoBehaviour
{
    [Header("큐브의 면 (오른쪽 그림이 없으면 옆모습을 좌우 반전)")]
    [SerializeField] private Sprite m_front;
    [SerializeField] private Sprite m_side;
    [SerializeField] private Sprite m_right;
    [SerializeField] private Sprite m_back;
    [SerializeField] private Sprite m_top;
    [SerializeField] private Sprite m_bottom;

    [Header("장비 부분 색상 (SpriteRenderer의 장비 머티리얼 사용)")]
    [SerializeField] private bool m_useEquipmentPalette;

    [Header("주사위 굴림")]
    [SerializeField, Range(0.5f, 1.2f)] private float m_faceScale = 0.86f;
    [SerializeField, Range(0f, 0.2f)] private float m_landingPause = 0.1f;
    [SerializeField, Range(0f, 0.5f)] private float m_faceShade = 0.1f;
    [Tooltip("224px 머리 면의 모서리 반경입니다. 원본 그림을 유지하고 렌더링할 때만 모서리를 둥글게 자릅니다.")]
    [SerializeField, Range(0f,32f)] private float m_cornerRadius = 8f;

    [Header("캐릭터 영역 / 투명 여백 제외 (픽셀)")]
    [SerializeField] private Rect m_frontRect = new Rect(20, 41, 216, 178);
    [SerializeField] private Rect m_sideRect = new Rect(32, 30, 200, 200);
    [SerializeField] private Rect m_rightRect = new Rect(16, 16, 224, 224);
    [SerializeField] private Rect m_backRect = new Rect(21, 39, 215, 180);
    [SerializeField] private Rect m_topRect = new Rect(19, 42, 218, 174);
    [SerializeField] private Rect m_bottomRect = new Rect(20, 31, 216, 191);

    private SpriteRenderer m_renderer;
    private SpriteRenderer m_arrivingRenderer;
    private Vector3 m_baseScale;
    private Vector3 m_basePosition;
    private Quaternion m_baseRotation;
    private Color m_baseTint;
    private Sprite m_frontFace, m_sideFace, m_rightFace, m_backFace, m_topFace, m_bottomFace;
    private static readonly Dictionary<(Sprite source, Rect rect), Sprite> s_faceSprites = new Dictionary<(Sprite, Rect), Sprite>();
    private MaterialPropertyBlock m_faceMaterialProperties;
    private static readonly int s_faceIndex = Shader.PropertyToID("_FaceIndex");
    private static readonly int s_spriteRect = Shader.PropertyToID("_SpriteRect");
    private static readonly int s_headRect = Shader.PropertyToID("_HeadRect");
    private static readonly int s_cornerRadius = Shader.PropertyToID("_CornerRadius");

    public bool HasAllFaces => m_front != null && m_side != null && m_back != null && m_top != null && m_bottom != null;
    public float LandingPause => m_landingPause;

    private void Awake()
    {
        m_renderer = GetComponent<SpriteRenderer>();
        m_baseScale = transform.localScale;
        m_basePosition = transform.localPosition;
        m_baseRotation = transform.localRotation;
        m_baseTint = m_useEquipmentPalette ? Color.white : m_renderer.color;
        m_faceMaterialProperties = new MaterialPropertyBlock();
        m_frontFace = GetContentSprite(m_front, m_frontRect);
        m_sideFace = GetContentSprite(m_side, m_sideRect);
        m_rightFace = GetContentSprite(m_right, m_rightRect);
        m_backFace = GetContentSprite(m_back, m_backRect);
        m_topFace = GetContentSprite(m_top, m_topRect);
        m_bottomFace = GetContentSprite(m_bottom, m_bottomRect);
        // 다른 면은 형제 오브젝트에 두어 이전 면의 압축과 회전을 상속하지 않습니다.
        m_arrivingRenderer = CreateRenderer("CubeArrivingFace", transform.parent, m_renderer.sortingOrder);
        m_arrivingRenderer.enabled = false;
    }

    private SpriteRenderer CreateRenderer(string objectName, Transform parent, int order)
    {
        GameObject child = new GameObject(objectName);
        child.layer = gameObject.layer;
        child.transform.SetParent(parent, false);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = m_renderer.sharedMaterial;
        renderer.sortingLayerID = m_renderer.sortingLayerID;
        renderer.sortingOrder = order;
        return renderer;
    }

    public void ShowOrientation(EnemyCubeOrientation orientation)
    {
        DrawPlane(m_renderer, orientation, Vector3.right, 1f, 0f, 1f);
        if (m_arrivingRenderer != null) m_arrivingRenderer.enabled = false;
    }

    public void AnimateRoll(EnemyCubeOrientation before, EnemyCubeOrientation after,
        EnemyCubeRollDirection direction, float progress)
    {
        EnemyCubeRollGeometry.Frame frame = EnemyCubeRollGeometry.Sample(progress);
        Vector3 axis;
        switch (direction)
        {
            case EnemyCubeRollDirection.Left: axis = Vector3.left; break;
            case EnemyCubeRollDirection.Up: axis = Vector3.up; break;
            case EnemyCubeRollDirection.Down: axis = Vector3.down; break;
            default: axis = Vector3.right; break;
        }
        // 이전 면은 진행 쪽, 새 면은 뒤쪽에 놓습니다. 두 면의 공유 모서리는 항상 일치합니다.
        DrawPlane(m_renderer, before, axis, frame.DepartingWidth,
            frame.DepartingOffset, 1f - m_faceShade * frame.ArrivingWidth);
        DrawPlane(m_arrivingRenderer, after, axis, frame.ArrivingWidth,
            frame.ArrivingOffset, 1f - m_faceShade * frame.DepartingWidth);
    }

    private void DrawPlane(SpriteRenderer renderer, EnemyCubeOrientation orientation,
        Vector3 axis, float width, float offset, float shade)
    {
        if (renderer == null) return;
        Sprite sprite = GetFace(orientation.VisibleFace);
        if (sprite != null && renderer.sprite != sprite) renderer.sprite = sprite;
        renderer.enabled = width > 0.0001f;
        renderer.flipX = orientation.VisibleFace == EnemyCubeFace.Right && m_rightFace == null;
        renderer.flipY = false;
        renderer.color = new Color(m_baseTint.r * shade, m_baseTint.g * shade, m_baseTint.b * shade, m_baseTint.a);
        ApplyFaceMaterialProperties(renderer, orientation.VisibleFace);

        bool horizontal = Mathf.Abs(axis.x) > 0f;
        bool compressLocalX = horizontal != (orientation.CounterClockwiseQuarterTurns % 2 != 0);
        Vector3 referenceSize = m_front != null ? m_front.bounds.size : Vector3.one;
        Vector3 faceSize = Vector3.Scale(referenceSize, m_baseScale) * m_faceScale;
        Vector3 spriteSize = sprite != null ? sprite.bounds.size : Vector3.one;
        Vector3 fullScale = new Vector3(faceSize.x / Mathf.Max(0.0001f, spriteSize.x),
            faceSize.y / Mathf.Max(0.0001f, spriteSize.y), m_baseScale.z);
        float side = horizontal ? faceSize.x : faceSize.y;
        Transform plane = renderer.transform;
        plane.localPosition = m_basePosition + axis * (side * offset);
        plane.localRotation = m_baseRotation * Quaternion.Euler(0f, 0f, orientation.CounterClockwiseQuarterTurns * 90f);
        plane.localScale = Vector3.Scale(fullScale,
            compressLocalX ? new Vector3(width, 1f, 1f) : new Vector3(1f, width, 1f));

    }

    private Sprite GetFace(EnemyCubeFace face)
    {
        switch (face)
        {
            case EnemyCubeFace.Back: return m_backFace;
            case EnemyCubeFace.Left:
                return m_sideFace;
            case EnemyCubeFace.Right: return m_rightFace != null ? m_rightFace : m_sideFace;
            case EnemyCubeFace.Top: return m_topFace;
            case EnemyCubeFace.Bottom: return m_bottomFace;
            default: return m_frontFace;
        }
    }

    private void ApplyFaceMaterialProperties(SpriteRenderer renderer, EnemyCubeFace face)
    {
        Sprite source;
        Rect content;
        int index;
        switch (face)
        {
            case EnemyCubeFace.Left:
                source = m_side; content = m_sideRect; index = 1; break;
            case EnemyCubeFace.Right: source = m_right != null ? m_right : m_side; content = m_right != null ? m_rightRect : m_sideRect; index = m_right != null ? 5 : 1; break;
            case EnemyCubeFace.Back: source = m_back; content = m_backRect; index = 2; break;
            case EnemyCubeFace.Top: source = m_top; content = m_topRect; index = 3; break;
            case EnemyCubeFace.Bottom: source = m_bottom; content = m_bottomRect; index = 4; break;
            default: source = m_front; content = m_frontRect; index = 0; break;
        }
        if (source == null) return;
        Rect rect = source.rect;
        renderer.GetPropertyBlock(m_faceMaterialProperties);
        m_faceMaterialProperties.SetFloat(s_faceIndex, index);
        m_faceMaterialProperties.SetVector(s_spriteRect, new Vector4(rect.x, rect.y, rect.width, rect.height));
        m_faceMaterialProperties.SetVector(s_headRect,new Vector4(content.x,rect.height-content.y-content.height,content.width,content.height));
        m_faceMaterialProperties.SetFloat(s_cornerRadius,m_cornerRadius);
        renderer.SetPropertyBlock(m_faceMaterialProperties);
    }

    private static Sprite GetContentSprite(Sprite source, Rect content)
    {
        if (source == null) return null;
        if (s_faceSprites.TryGetValue((source, content), out Sprite cached) && cached != null) return cached;
        Rect original = source.rect;
        float x = Mathf.Clamp(content.x, 0f, original.width - 1f);
        float y = Mathf.Clamp(content.y, 0f, original.height - 1f);
        Rect rect = new Rect(original.x + x, original.y + y,
            Mathf.Clamp(content.width, 1f, original.width - x), Mathf.Clamp(content.height, 1f, original.height - y));
        // PNG는 그대로 두고, 투명 여백을 뺀 그림만 정사각형 면에 맞춰 렌더링합니다.
        Sprite cropped = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit,
            0, SpriteMeshType.FullRect);
        cropped.name = source.name + "_CubeContent";
        s_faceSprites[(source, content)] = cropped;
        return cropped;
    }

    private void OnDestroy()
    {
        if (m_arrivingRenderer != null) Destroy(m_arrivingRenderer.gameObject);
    }
}
