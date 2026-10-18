#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이름의 A/B/C/Frame 역할을 공통 원본에 연결합니다. 비율 표기는 필요 없습니다.</summary>
public static class UIAtlasNameConnector
{
    public const string DefaultAtlasPath = "Assets/4. Asset/5. UI/Shared_UI_Atlas.png";
    public sealed class Row
    {
        public Image image;
        public Sprite sprite;
        public string layer;
        public string reason;
        public UITileFrameStyle style;
        public float thickness;
        public bool matchColor;
        public UIStageThemeController theme;
        public bool CanApply => image != null && sprite != null;
    }
    private static readonly Regex LayerPattern = new Regex(@"(?:^|[_\s])([ABC])(?=[_\s]|$)", RegexOptions.IgnoreCase);
    private static readonly Regex FramePattern = new Regex(@"(?:^|[_\s])Frame(?=[_\s]|$)", RegexOptions.IgnoreCase);

    public static List<Row> BuildPlan(IEnumerable<GameObject> selection, Texture2D atlas,
        bool includeChildren, UITileFrameStyle style, float thickness = 2f, bool matchColors = false)
    {
        string assetPath = atlas != null ? AssetDatabase.GetAssetPath(atlas) : string.Empty;
        var sprites = string.IsNullOrEmpty(assetPath) ? new Dictionary<string, Sprite>() :
            AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToDictionary(s => s.name);
        var images = new HashSet<Image>();
        foreach (var root in selection)
        {
            if (root == null || EditorUtility.IsPersistent(root)) continue;
            if (includeChildren) images.UnionWith(root.GetComponentsInChildren<Image>(true));
            else if (root.TryGetComponent<Image>(out var image)) images.Add(image);
        }
        var result = new List<Row>();
        foreach (var image in images.OrderBy(i => HierarchyPath(i.transform), StringComparer.Ordinal))
        {
            var row = new Row { image = image, style = style, thickness = Mathf.Max(0.25f, thickness), matchColor = matchColors,
                theme = image.GetComponentInParent<UIStageThemeController>(true), layer = FindLayer(image) };
            result.Add(row);
            if (image.type == Image.Type.Filled) { row.reason = "Filled 게이지 유지"; continue; }
            if (row.layer == null) { row.reason = "A/B/C/Frame 역할 표시 없음"; continue; }
            var resolved = style == UITileFrameStyle.Auto
                ? Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height) < 96f ? UITileFrameStyle.Compact : UITileFrameStyle.Decorated : style;
            string name = row.layer != "Frame" ? "Shared_A_Surface" :
                resolved == UITileFrameStyle.Compact ? "Shared_Frame_Compact" : "Shared_Frame_Decorated";
            if (!sprites.TryGetValue(name, out row.sprite)) row.reason = "공통 스킨 원본 없음: " + name;
        }
        return result;
    }

    public static int Apply(IEnumerable<Row> plan)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("공통 UI 스킨 연결");
        int count = 0;
        foreach (var row in plan)
        {
            if (!row.CanApply || EditorUtility.IsPersistent(row.image)) continue;
            var image = row.image;
            Color originalColor = image.color;
            Undo.RecordObject(image, "공통 UI 스킨 연결");
            var surface = image.GetComponent<UITileSurface>() ?? Undo.AddComponent<UITileSurface>(image.gameObject);
            Undo.RecordObject(surface, "공통 UI 스킨 설정");
            var role = (UITileColorRole)Enum.Parse(typeof(UITileColorRole), row.layer);
            surface.Configure(UITileSkin.Default, role, row.style, row.thickness, row.matchColor || row.theme != null, originalColor);
            if (row.theme != null)
            {
                Undo.RecordObject(row.theme, "공통 UI 색상 유지");
                if (row.matchColor) row.theme.ApplyImageThemeColor(image);
                else { image.color = originalColor; row.theme.RememberImageColor(image); surface.Refresh(); }
                EditorUtility.SetDirty(row.theme);
                PrefabUtility.RecordPrefabInstancePropertyModifications(row.theme);
            }
            else if (!row.matchColor) image.color = originalColor;
            EditorUtility.SetDirty(image); EditorUtility.SetDirty(surface);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            if (image.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(image.gameObject.scene);
            count++;
        }
        Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
        return count;
    }

    private static string FindLayer(Image image)
    {
        if (image.TryGetComponent<UITileSurface>(out var surface)) return surface.ThemeLayer;
        if (FramePattern.IsMatch(image.name) || image.name.Contains("프레임")) return "Frame";
        string[] roles = LayerPattern.Matches(image.name).Cast<Match>().Select(m => m.Groups[1].Value.ToUpperInvariant()).Distinct().ToArray();
        return roles.Length == 1 ? roles[0] : roles.Length > 1 ? null : UIStageThemeController.GetImageLayer(image);
    }
    public static string HierarchyPath(Transform item) => item.parent == null ? item.name : HierarchyPath(item.parent) + "/" + item.name;
}

public sealed class UIAtlasNameConnectorWindow : EditorWindow
{
    private Texture2D atlas;
    private bool includeChildren = true, matchColors, showSkipped;
    private UITileFrameStyle style;
    private float thickness = 2f;
    private Vector2 scroll;
    private List<UIAtlasNameConnector.Row> rows = new List<UIAtlasNameConnector.Row>();
    [MenuItem("Tools/UI/공통 스킨 연결")]
    [MenuItem("Tools/UI/이름의 비율로 스프라이트 연결")]
    public static void Open() => GetWindow<UIAtlasNameConnectorWindow>("공통 UI 스킨");
    private void OnEnable()
    {
        minSize = new Vector2(520, 340);
        atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(UIAtlasNameConnector.DefaultAtlasPath);
        Refresh();
    }
    private void OnSelectionChange() { Refresh(); Repaint(); }
    private void Refresh() => rows = UIAtlasNameConnector.BuildPlan(Selection.gameObjects, atlas, includeChildren, style, thickness, matchColors);
    private void OnGUI()
    {
        EditorGUILayout.HelpBox("A/B/C/Frame 역할만 있으면 어떤 비율에도 연결합니다.\n자동은 짧은 변 96 UI 단위 미만에서 간결한 프레임을 사용합니다. 기존 색상은 기본적으로 유지합니다.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("공통 아틀라스", atlas, typeof(Texture2D), false);
        includeChildren = EditorGUILayout.Toggle("하위 UI 포함", includeChildren);
        style = (UITileFrameStyle)EditorGUILayout.EnumPopup("프레임 모양", style);
        thickness = Mathf.Max(0.25f, EditorGUILayout.FloatField("테두리 두께 (UI 단위)", thickness));
        matchColors = EditorGUILayout.Toggle("스테이지 색상으로 초기화", matchColors);
        showSkipped = EditorGUILayout.Toggle("제외 항목 표시", showSkipped);
        if (EditorGUI.EndChangeCheck()) Refresh();
        EditorGUILayout.LabelField($"연결 가능 {rows.Count(r => r.CanApply)}개 / 제외 {rows.Count(r => !r.CanApply)}개");
        using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || !rows.Any(r => r.CanApply)))
            if (GUILayout.Button("선택한 UI에 적용 (Ctrl+Z 가능)")) { UIAtlasNameConnector.Apply(rows); Refresh(); }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var row in rows)
        {
            if (!row.CanApply && !showSkipped) continue;
            EditorGUILayout.LabelField(row.image.name, row.CanApply ? row.layer + " → " + row.sprite.name : row.reason);
        }
        EditorGUILayout.EndScrollView();
    }
}
#endif
