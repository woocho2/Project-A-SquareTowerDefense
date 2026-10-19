using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>배경과 프레임을 한곳에서 설정하는 재사용 UI 패널입니다.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
public sealed class UITilePanel : MonoBehaviour
{
    [SerializeField] private UITileSkin skin;
    [SerializeField] private Image background;
    [SerializeField] private UITileSurface frame;
    [SerializeField] private UITileFrameStyle frameStyle;
    [Min(0.25f), SerializeField] private float borderThickness = 2f;
    [SerializeField] private bool showBackground = true;
    [SerializeField] private bool showFrame = true;

    public void SetSurfaces(Image bg, UITileSurface edge) { background = bg; frame = edge; Refresh(); }
    public void SetStyle(UITileFrameStyle style, float thickness) { frameStyle = style; borderThickness = thickness; Refresh(); }

    [ContextMenu("패널 설정 적용")]
    public void Refresh()
    {
        if (background != null)
        {
            if (background.gameObject.activeSelf != showBackground) background.gameObject.SetActive(showBackground);
        }
        if (frame != null)
        {
            if (frame.gameObject.activeSelf != showFrame) frame.gameObject.SetActive(showFrame);
            frame.Configure(skin, UITileColorRole.Frame, frameStyle, borderThickness);
        }
    }
    private void OnEnable() => Refresh();
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
