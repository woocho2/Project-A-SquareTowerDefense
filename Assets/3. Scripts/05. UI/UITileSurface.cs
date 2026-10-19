using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>비율별 PNG 대신 공통 원본을 사용하고 선 두께를 UI 단위로 맞춥니다.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class UITileSurface : MonoBehaviour
{
    [SerializeField] private UITileSkin skin;
    [SerializeField] private UITileColorRole colorRole;
    [SerializeField] private UITileFrameStyle frameStyle;
    [Tooltip("화면 픽셀이 아닌 RectTransform의 UI 단위입니다. 작은 UI에서는 자동으로 줄입니다.")]
    [Min(0.25f), SerializeField] private float borderThickness = 2f;
    private Image target;
    public string ThemeLayer => colorRole.ToString();
    public UITileFrameStyle FrameStyle => frameStyle;
    public float BorderThickness => borderThickness;
    public UITileSkin Skin => skin != null ? skin : UITileSkin.Default;
    private Image Target => target != null ? target : target = GetComponent<Image>();

    public UITileFrameStyle ResolvedStyle => frameStyle != UITileFrameStyle.Auto ? frameStyle :
        Mathf.Min(Target.rectTransform.rect.width, Target.rectTransform.rect.height) < 96f
            ? UITileFrameStyle.Compact : UITileFrameStyle.Decorated;

    public void Configure(UITileSkin value, UITileColorRole role, UITileFrameStyle style,
        float thickness)
    {
        skin = value; colorRole = role; frameStyle = style;
        borderThickness = Mathf.Max(0.25f, thickness);
        Refresh();
    }

    public void Refresh() => RefreshGeometry();

    public void RefreshGeometry()
    {
        var currentSkin = Skin;
        if (currentSkin == null) return;
        var style = ResolvedStyle;
        var frame = currentSkin.GetFrame(style);
        var sprite = colorRole == UITileColorRole.Frame ? frame : currentSkin.background;
        if (sprite == null || frame == null || currentSkin.background == null) return;
        Image image = Target;
        if (image.sprite != sprite) image.sprite = sprite;
        // Filled 게이지의 fillAmount와 방향은 유지합니다.
        if (image.type == Image.Type.Filled) return;
        if (image.type != Image.Type.Sliced) image.type = Image.Type.Sliced;
        var border = Vector4.Max(frame.border, currentSkin.background.border);
        float largestBorder = Mathf.Max(Mathf.Max(border.x, border.y), Mathf.Max(border.z, border.w));
        float shortSide = Mathf.Max(1f, Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height));
        float density = Mathf.Max(currentSkin.GetStrokePixels(style) / Mathf.Max(0.25f, borderThickness),
            largestBorder * 2f / (shortSide * 0.9f));
        float multiplier = density / Mathf.Max(0.001f, image.pixelsPerUnit);
        if (!Mathf.Approximately(image.pixelsPerUnitMultiplier, multiplier)) image.pixelsPerUnitMultiplier = multiplier;
    }

    private void OnEnable()
    {
        // CanvasScaler의 기준 PPU가 바뀌어도 지정한 UI 선 두께를 유지합니다.
        Canvas.preWillRenderCanvases += RefreshGeometry;
        Refresh();
    }
    private void OnDisable() => Canvas.preWillRenderCanvases -= RefreshGeometry;
    private void OnTransformParentChanged() => Refresh();
    private void OnCanvasHierarchyChanged() => Refresh();
    private void OnRectTransformDimensionsChange() => RefreshGeometry();
#if UNITY_EDITOR
    private bool refreshQueued;
    private void OnValidate()
    {
        if (refreshQueued) return;
        refreshQueued = true;
        EditorApplication.delayCall += () => { if (this == null) return; refreshQueued = false; Refresh(); };
    }
#endif
}
