#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(UITilePanel)), CanEditMultipleObjects]
public sealed class UITilePanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("RectTransform 크기만 바꾸면 됩니다. 두께는 UI 단위이며 Auto는 작은 UI에 간결한 프레임을 사용합니다.", MessageType.Info);
        foreach (string name in new[] { "frameStyle", "borderThickness", "showBackground", "showFrame" })
        {
            var property = serializedObject.FindProperty(name);
            string label = name == "frameStyle" ? "프레임 모양" : name == "borderThickness" ? "테두리 두께" :
                name == "showBackground" ? "배경 표시" : "프레임 표시";
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("skin"), new GUIContent("공통 스킨 (비우면 기본)"));
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/UI/타일 공통 패널", false, 2050)]
    private static void CreatePanel(MenuCommand command)
    {
        var root = new GameObject("UITilePanel", typeof(RectTransform), typeof(UITilePanel));
        Undo.RegisterCreatedObjectUndo(root, "타일 공통 패널 생성");
        GameObject parent = command.context as GameObject;
        if (parent == null) parent = Selection.activeGameObject;
        if (parent == null || parent.GetComponentInParent<Canvas>() == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObject, "Canvas 생성");
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvas = canvasObject.GetComponent<Canvas>();
            }
            parent = canvas.gameObject;
        }
        GameObjectUtility.SetParentAndAlign(root, parent);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 180);
        var bg = CreateImage(root.transform, "Img_A_BG", true);
        if (UITileSkin.Default != null) bg.sprite = UITileSkin.Default.background;
        var edgeImage = CreateImage(root.transform, "Img_Frame", false);
        var edge = Undo.AddComponent<UITileSurface>(edgeImage.gameObject);
        root.GetComponent<UITilePanel>().SetSurfaces(bg, edge);
        Selection.activeGameObject = root;
    }

    private static Image CreateImage(Transform parent, string name, bool raycast)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        child.layer = parent.gameObject.layer;
        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = child.GetComponent<Image>();
        image.raycastTarget = raycast;
        image.type = Image.Type.Sliced;
        return image;
    }
}
#endif
