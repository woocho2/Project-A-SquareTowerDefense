#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>오브젝트 이름의 레이어/비율로 공통 UI 아틀라스를 연결합니다.</summary>
public static class UIAtlasNameConnector
{
    public const string DefaultAtlasPath = "Assets/4. Asset/5. UI/Shared_UI_Atlas.png";
    public enum SquareStyle { Sliced128B, Simple128 }

    public sealed class Row
    {
        public Image image;
        public Sprite sprite;
        public Image.Type type;
        public string ratio;
        public string sourceName;
        public string reason;
        public bool matchColor;
        public Color color;
        public UIStageThemeController theme;
        public bool CanApply => image != null && sprite != null;
        public bool NeedsChange => CanApply && (image.sprite != sprite || image.type != type ||
            matchColor && (image.color != color || theme != null && theme.HasImageColorOverride(image)));
    }

    private static readonly Regex RatioPattern = new Regex(@"(?<![\d:：])(\d+)\s*[:：]\s*(\d+)(?![\d:：])");
    private static readonly Regex LayerPattern = new Regex(@"(?:^|[_\s])([ABC])(?=[_\s]|$)", RegexOptions.IgnoreCase);
    private static readonly Regex FramePattern = new Regex(@"(?:^|[_\s])Frame(?=[_\s]|$)", RegexOptions.IgnoreCase);
    private static readonly Dictionary<string, string> Shapes = new Dictionary<string, string>
    {
        { "1:1", "Btn_128" }, { "1:2", "Capsule_128x256" }, { "1:3", "Gauge_128x384" },
        { "1:4", "TallBanner_128x512" }, { "2:3", "CharCard_256x384" }, { "2:1", "Capsule_256x128" },
        { "3:1", "Capsule_384x128" }, { "4:1", "Capsule_512x128" }, { "3:2", "SkillCard_384x256" },
        { "6:1", "MegaDock_768x128" }, { "1:6", "MegaRail_128x768" }
    };

    public static List<Row> BuildPlan(IEnumerable<GameObject> selection, Texture2D atlas,
        bool includeChildren, bool useParentRatio, SquareStyle squareStyle, bool matchColors = true)
    {
        string path = atlas != null ? AssetDatabase.GetAssetPath(atlas) : string.Empty;
        var sprites = string.IsNullOrEmpty(path) ? new Dictionary<string, Sprite>() :
            AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        var images = new HashSet<Image>();
        foreach (GameObject root in selection)
        {
            // Hierarchy / Prefab Mode에서 편집하며 Project의 프리팹 에셋을 직접 덮어쓰지 않습니다.
            if (root == null || EditorUtility.IsPersistent(root)) continue;
            if (includeChildren) images.UnionWith(root.GetComponentsInChildren<Image>(true));
            else if (root.TryGetComponent(out Image image)) images.Add(image);
        }

        var result = new List<Row>();
        foreach (Image image in images.OrderBy(item => HierarchyPath(item.transform), StringComparer.Ordinal))
        {
            var row = new Row { image = image };
            string layer = FindLayer(image);
            if (layer == null) { row.reason = "A/B/C/Frame 표시 없음"; result.Add(row); continue; }
            Transform source = image.transform;
            MatchCollection ratios = RatioPattern.Matches(source.name);
            while (ratios.Count == 0 && useParentRatio && source.parent != null)
            {
                source = source.parent;
                ratios = RatioPattern.Matches(source.name);
            }
            row.sourceName = source.name;
            if (ratios.Count == 0) { row.reason = "이름에 비율 없음"; result.Add(row); continue; }
            var keys = new HashSet<string>();
            foreach (Match match in ratios)
            {
                if (!int.TryParse(match.Groups[1].Value, out int width) ||
                    !int.TryParse(match.Groups[2].Value, out int height) || width <= 0 || height <= 0)
                { row.reason = "비율은 양의 정수여야 함"; break; }
                int divisor = Gcd(width, height);
                keys.Add($"{width / divisor}:{height / divisor}");
            }
            if (row.reason != null) { result.Add(row); continue; }
            if (keys.Count != 1) { row.reason = "서로 다른 비율이 여러 개 있음"; result.Add(row); continue; }
            row.ratio = keys.First();
            if (!Shapes.TryGetValue(row.ratio, out string suffix))
            { row.reason = $"아틀라스에 {row.ratio} 조각 없음"; result.Add(row); continue; }
            if (row.ratio == "1:1" && squareStyle == SquareStyle.Sliced128B) suffix = "Btn_128_B";
            string spriteName = $"Shared_{layer}_{suffix}";
            if (!sprites.TryGetValue(spriteName, out row.sprite))
            { row.reason = $"{spriteName} 조각 없음"; result.Add(row); continue; }
            row.type = image.type == Image.Type.Filled ? Image.Type.Filled :
                row.sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
            row.matchColor = matchColors;
            row.theme = image.GetComponentInParent<UIStageThemeController>(true);
            Color color = row.theme != null ? row.theme.GetLayerColor(layer) :
                UIStageThemeController.GetDefaultLayerColor(StageRealm.Asgard, layer);
            row.color = new Color(color.r, color.g, color.b, image.color.a);
            result.Add(row);
        }
        return result;
    }

    public static int Apply(IEnumerable<Row> plan)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        const string action = "이름 비율로 UI 스프라이트 연결";
        Undo.SetCurrentGroupName(action);
        int count = 0;
        foreach (Row row in plan)
        {
            if (!row.NeedsChange || EditorUtility.IsPersistent(row.image)) continue;
            Image image = row.image;
            Undo.RecordObject(image, action);
            image.sprite = row.sprite;
            image.type = row.type;

            UIStageThemeController theme = row.theme;
            if (theme != null && theme.ManagesImage(image))
            {
                Undo.RecordObject(theme, action);
                if (row.matchColor) theme.ApplyImageThemeColor(image);
                else theme.RememberImageColor(image);
                EditorUtility.SetDirty(theme);
                PrefabUtility.RecordPrefabInstancePropertyModifications(theme);
            }
            else if (row.matchColor) image.color = row.color;
            EditorUtility.SetDirty(image);
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            if (image.gameObject.scene.IsValid() && image.gameObject.scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(image.gameObject.scene);
            count++;
        }
        Undo.FlushUndoRecordObjects();
        Undo.CollapseUndoOperations(group);
        return count;
    }

    private static string FindLayer(Image image)
    {
        string name = image.name;
        if (FramePattern.IsMatch(name) || name.Contains("프레임")) return "Frame";
        Match ratio = RatioPattern.Match(name);
        if (ratio.Success) name = name.Substring(0, ratio.Index);
        string[] explicitLayers = LayerPattern.Matches(name).Cast<Match>()
            .Select(match => match.Groups[1].Value.ToUpperInvariant()).Distinct().ToArray();
        if (explicitLayers.Length == 1) return explicitLayers[0];
        if (explicitLayers.Length > 1) return null;
        return UIStageThemeController.GetSharedLayer(image.sprite);
    }

    private static int Gcd(int a, int b) { while (b != 0) { int next = a % b; a = b; b = next; } return a; }
    public static string HierarchyPath(Transform item) => item.parent == null ? item.name : HierarchyPath(item.parent) + "/" + item.name;
}

public class UIAtlasNameConnectorWindow : EditorWindow
{
    private Texture2D atlas;
    private bool includeChildren = true;
    private bool useParentRatio = true;
    private bool showSkipped;
    private bool matchColors = true;
    private UIAtlasNameConnector.SquareStyle squareStyle;
    private List<UIAtlasNameConnector.Row> rows = new List<UIAtlasNameConnector.Row>();
    private Vector2 scroll;
    private string lastResult;

    [MenuItem("Tools/UI/이름의 비율로 스프라이트 연결")]
    public static void Open() => GetWindow<UIAtlasNameConnectorWindow>("UI 스프라이트 연결");

    private void OnEnable()
    {
        minSize = new Vector2(560, 360);
        atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(UIAtlasNameConnector.DefaultAtlasPath);
        RefreshPlan();
    }
    private void OnSelectionChange() { RefreshPlan(); Repaint(); }
    private void OnHierarchyChange() { RefreshPlan(); Repaint(); }
    private void RefreshPlan() => rows = UIAtlasNameConnector.BuildPlan(Selection.gameObjects, atlas,
        includeChildren, useParentRatio, squareStyle, matchColors);

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Hierarchy 또는 Prefab Mode에서 UI를 선택하세요.\n예: Img_A_Name_BG 3:2 → A 384×256 / Img_Name_Frame 3:1 → Frame 384×128\nUI 크기와 알파는 유지합니다. 색상은 상위 Canvas의 현재 테마를 사용하며, 테마가 없으면 아스가르드를 사용합니다.", MessageType.Info);
        EditorGUI.BeginChangeCheck();
        atlas = (Texture2D)EditorGUILayout.ObjectField("공통 아틀라스", atlas, typeof(Texture2D), false);
        includeChildren = EditorGUILayout.Toggle("선택한 UI의 하위도 포함", includeChildren);
        useParentRatio = EditorGUILayout.Toggle("비율이 없으면 부모 이름 확인", useParentRatio);
        matchColors = EditorGUILayout.Toggle("A/B/C/Frame 테마색도 적용", matchColors);
        if (matchColors) EditorGUILayout.HelpBox("선택한 항목의 수동 색상을 테마색으로 바꿉니다. 이미 연결된 스프라이트도 색상만 다시 적용할 수 있습니다.", MessageType.None);
        int square = EditorGUILayout.Popup("1:1 연결 방식", (int)squareStyle,
            new[] { "Sliced — 128×128_B", "Simple — 128×128" });
        squareStyle = (UIAtlasNameConnector.SquareStyle)square;
        if (EditorGUI.EndChangeCheck()) { RefreshPlan(); lastResult = null; }
        int pending = rows.Count(row => row.NeedsChange);
        int skipped = rows.Count(row => !row.CanApply);
        EditorGUILayout.LabelField($"변경 {pending}개 · 이미 연결됨 {rows.Count - pending - skipped}개 · 건너뜀 {skipped}개");
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("미리보기 새로고침")) RefreshPlan();
        using (new EditorGUI.DisabledScope(pending == 0 || atlas == null || EditorApplication.isPlaying))
        {
            if (GUILayout.Button($"연결 적용 ({pending}개)"))
            {
                RefreshPlan();
                int applied = UIAtlasNameConnector.Apply(rows);
                lastResult = $"{applied}개 연결 완료. Ctrl+Z로 되돌릴 수 있습니다. 씬/프리팹은 Ctrl+S로 저장하세요.";
                Debug.Log($"[UI 아틀라스 연결] {applied}개 연결 완료.");
                RefreshPlan();
            }
        }
        EditorGUILayout.EndHorizontal();
        if (lastResult != null) EditorGUILayout.HelpBox(lastResult, MessageType.Info);
        showSkipped = EditorGUILayout.Toggle("건너뛴 항목 표시", showSkipped);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (UIAtlasNameConnector.Row row in rows)
        {
            if (!row.CanApply && !showSkipped) continue;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField(row.image, typeof(Image), true);
            if (row.CanApply)
            {
                EditorGUILayout.LabelField($"{row.ratio} → {row.sprite.name} ({row.type})");
                if (row.matchColor)
                    using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ColorField("적용할 색상", row.color);
                if (row.sourceName != row.image.name) EditorGUILayout.LabelField("비율 출처: " + row.sourceName, EditorStyles.miniLabel);
            }
            else EditorGUILayout.LabelField(row.reason, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        if (rows.Count == 0) EditorGUILayout.HelpBox("Hierarchy 또는 Prefab Mode에서 Image가 있는 UI를 선택하세요.", MessageType.Info);
    }
}
#endif
