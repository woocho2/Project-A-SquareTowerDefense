using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class SharedVisual : EditorWindow
{
    // 한 테마에 필요한 색상을 한 묶음으로 보관합니다.
    [System.Serializable]
    private class ThemeColors
    {
        public string name;
        [FormerlySerializedAs("towerSpawn")]
        public Color MainColor_a = Color.white;
        [FormerlySerializedAs("path")]
        public Color MainColor_b = Color.white;
        [FormerlySerializedAs("direction")]
        public Color SubColor = Color.white;
        [FormerlySerializedAs("frame")]
        public Color FrameColor = Color.white;
    }

    // 씬 오브젝트는 참조 대신 재시작 후에도 유지되는 고유 ID로 저장합니다.
    [System.Serializable]
    private class ThemeSettings
    {
        public int selectedTheme;
        public List<ThemeColors> themes = new List<ThemeColors>();
        public string gridGlobalId;
        public string canvasGlobalId;
    }

    private static readonly string[] ThemeNames =
    {
        "아스가르드", "알프헤임", "바나헤임", "미드가르드", "요툰헤임",
        "니다벨리르", "니플헤임", "무스펠헤임", "헬", "라그나로크"
    };

    private static string SettingsPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "../ProjectSettings/SharedVisualThemes.json"));

    [SerializeField] private List<ThemeColors> m_themes = new List<ThemeColors>();
    [SerializeField] private int m_selectedTheme;
    [SerializeField] private string m_gridGlobalId;
    [SerializeField] private string m_canvasGlobalId;
    [SerializeField] private Grid m_grid;
    [SerializeField] private List<string> m_towerSpawnNames = new List<string> { "TowerSpawn" };
    [SerializeField] private List<string> m_pathNames = new List<string> { "Path" };
    [SerializeField] private List<string> m_frameNames = new List<string> { "Frame" };
    [SerializeField] private List<string> m_directionNames = new List<string> { "Direction" };

    [SerializeField] private Tilemap m_towerSpawnTile;
    [SerializeField] private Tilemap m_pathTile;
    [SerializeField] private Tilemap m_frameTile;
    [SerializeField] private Tilemap m_directionTile;

    [SerializeField] private Canvas m_canvas;

    [SerializeField] List<Image> m_uiThemeBG_a = new List<Image>();
    [SerializeField] List<Image> m_uiThemeBG_b = new List<Image>();
    [SerializeField] List<Image> m_uiThemeBG_c = new List<Image>();
    [SerializeField] List<Image> m_uiThemeFrame = new List<Image>();

    // TMP와 기존 UI Text 모두 Graphic을 상속하므로 같은 목록에 담습니다.
    [SerializeField] private List<Graphic> m_uiThemeText_a = new List<Graphic>();
    [SerializeField] private List<Graphic> m_uiThemeText_b = new List<Graphic>();
    [SerializeField] private List<Graphic> m_uiThemeText_c = new List<Graphic>();

    private Vector2 m_scrollPositiion;

    [MenuItem("Tools/Visuals/테마 색상")]
    private static void open()
    {
        GetWindow<SharedVisual>("테마 색상");
    }

    private void OnEnable()
    {
        LoadThemeSettings();
        EditorApplication.delayCall += RestoreSavedTargets;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneSaved += OnSceneSaved;
    }

    private void OnDisable()
    {
        EditorApplication.delayCall -= RestoreSavedTargets;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneSaved -= OnSceneSaved;
        if (RememberAvailableRootIds()) SaveThemeSettings();
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        RestoreSavedTargets();
    }

    private void OnSceneSaved(Scene scene)
    {
        // 새 씬을 처음 저장하거나 다른 이름으로 저장한 경우에도 ID를 갱신합니다.
        if (RememberAvailableRootIds()) SaveThemeSettings();
    }

    private static string GetPersistentId(Component target)
    {
        if (target == null) return string.Empty;
        if (target.gameObject.scene.IsValid() && string.IsNullOrEmpty(target.gameObject.scene.path))
            return string.Empty;
        return GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
    }

    private static T FindSavedObject<T>(string savedId) where T : Component
    {
        if (!GlobalObjectId.TryParse(savedId, out GlobalObjectId id)) return null;
        return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as T;
    }

    private bool RememberAvailableRootIds()
    {
        bool changed = false;
        string gridId = GetPersistentId(m_grid);
        if (!string.IsNullOrEmpty(gridId) && gridId != m_gridGlobalId)
        {
            m_gridGlobalId = gridId;
            changed = true;
        }
        string canvasId = GetPersistentId(m_canvas);
        if (!string.IsNullOrEmpty(canvasId) && canvasId != m_canvasGlobalId)
        {
            m_canvasGlobalId = canvasId;
            changed = true;
        }
        return changed;
    }

    private void RestoreSavedTargets()
    {
        if (this == null) return;
        if (!string.IsNullOrEmpty(m_gridGlobalId))
        {
            m_grid = FindSavedObject<Grid>(m_gridGlobalId);
            ClearTilemapTargets();
        }
        if (!string.IsNullOrEmpty(m_canvasGlobalId))
        {
            m_canvas = FindSavedObject<Canvas>(m_canvasGlobalId);
            ClearCanvasTargets();
        }
        if (m_grid != null) CollectTilemaps(false);
        if (m_canvas != null) CollectCanvasTargets(false);
        if (RememberAvailableRootIds()) SaveThemeSettings();
        Repaint();
    }

    private void OnGUI()
    {
        m_scrollPositiion = EditorGUILayout.BeginScrollView(
            m_scrollPositiion,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true));

        EditorGUILayout.LabelField("테마 색상 설정", EditorStyles.boldLabel);

        DrawThemeSection();
        DrawTilemapSection();

        EditorGUILayout.LabelField("UI 대상", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        m_canvas = (Canvas)EditorGUILayout.ObjectField(
            "대상 Canvas",
            m_canvas,
            typeof(Canvas),
            true);
        if (EditorGUI.EndChangeCheck())
        {
            m_canvasGlobalId = GetPersistentId(m_canvas);
            ClearCanvasTargets();
            if (m_canvas != null) CollectCanvasTargets(false);
            SaveThemeSettings();
        }
        DrawTargetPersistenceHint(m_canvas, m_canvasGlobalId);

        using (new EditorGUI.DisabledScope(m_canvas == null))
        {
            if (GUILayout.Button("이름으로 UI 담기"))
            {
                CollectCanvasTargets();
            }
        }

        EditorGUILayout.Space();

        SerializedObject settings = new SerializedObject(this);
        settings.Update();

        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeBG_a"), new GUIContent("A 배경"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeBG_b"), new GUIContent("B 배경"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeBG_c"), new GUIContent("C 배경"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeFrame"), new GUIContent("프레임"), true);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("텍스트 대상", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeText_a"), new GUIContent("A 텍스트 (검은색)"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeText_b"), new GUIContent("B 텍스트 (흰색)"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_uiThemeText_c"), new GUIContent("C 텍스트 (검은색)"), true);
        settings.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();
    }

    private void DrawThemeSection()
    {
        EnsureThemes();
        EditorGUI.BeginChangeCheck();
        m_selectedTheme = EditorGUILayout.Popup("테마 선택", m_selectedTheme, ThemeNames);
        ThemeColors colors = m_themes[m_selectedTheme];

        EditorGUILayout.LabelField("타일맵·UI 공용 색상", EditorStyles.boldLabel);
        colors.MainColor_a = EditorGUILayout.ColorField(
            new GUIContent("MainColor_a", "스폰 배경과 UI A 배경"), colors.MainColor_a);
        colors.MainColor_b = EditorGUILayout.ColorField(
            new GUIContent("MainColor_b", "패스 배경과 UI B 배경"), colors.MainColor_b);
        colors.SubColor = EditorGUILayout.ColorField(
            new GUIContent("SubColor", "화살표와 UI C 배경"), colors.SubColor);
        colors.FrameColor = EditorGUILayout.ColorField(
            new GUIContent("FrameColor", "타일 장식과 UI 프레임"), colors.FrameColor);

        // 색을 고르면 설정만 저장하고, 실제 오브젝트는 적용 버튼에서 변경합니다.
        if (EditorGUI.EndChangeCheck()) SaveThemeSettings();

        EditorGUILayout.HelpBox(
            "테마별 색상은 자동 저장됩니다. 아래에서 대상을 수집한 뒤 '테마 적용'을 누르세요. 적용 결과는 Ctrl+Z로 되돌릴 수 있습니다.",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(
            EditorApplication.isPlayingOrWillChangePlaymode || !HasThemeTargets()))
        {
            if (GUILayout.Button("테마 적용")) ApplyTheme(colors);
        }

        EditorGUILayout.Space(12);
    }

    private void EnsureThemes()
    {
        if (m_themes == null) m_themes = new List<ThemeColors>();
        // 목록 순서를 고정해 테마를 바꿔도 다른 테마의 색상이 섞이지 않게 합니다.
        foreach (string name in ThemeNames)
        {
            if (!m_themes.Exists(theme => theme != null && theme.name == name))
                m_themes.Add(new ThemeColors { name = name });
        }
        var ordered = new List<ThemeColors>();
        foreach (string name in ThemeNames)
            ordered.Add(m_themes.Find(theme => theme != null && theme.name == name));
        m_themes = ordered;
        m_selectedTheme = Mathf.Clamp(m_selectedTheme, 0, m_themes.Count - 1);
    }

    private void LoadThemeSettings()
    {
        if (File.Exists(SettingsPath))
        {
            try
            {
                string json = File.ReadAllText(SettingsPath);
                // 기존 8색 설정이 있으면 타일맵 쪽 값을 새 공용 색상으로 이어받습니다.
                json = json.Replace("\"towerSpawn\":", "\"MainColor_a\":")
                    .Replace("\"path\":", "\"MainColor_b\":")
                    .Replace("\"direction\":", "\"SubColor\":")
                    .Replace("\"frame\":", "\"FrameColor\":");
                ThemeSettings saved = JsonUtility.FromJson<ThemeSettings>(json);
                if (saved != null && saved.themes != null)
                {
                    m_themes = saved.themes;
                    m_selectedTheme = saved.selectedTheme;
                    m_gridGlobalId = saved.gridGlobalId;
                    m_canvasGlobalId = saved.canvasGlobalId;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"테마 색상 설정을 읽지 못했습니다: {exception.Message}");
            }
        }
        EnsureThemes();
    }

    private void SaveThemeSettings()
    {
        var settings = new ThemeSettings
        {
            selectedTheme = m_selectedTheme,
            themes = m_themes,
            gridGlobalId = m_gridGlobalId,
            canvasGlobalId = m_canvasGlobalId
        };
        try
        {
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, true));
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"테마 색상 설정을 저장하지 못했습니다: {exception.Message}");
        }
    }

    private bool HasThemeTargets()
    {
        return IsSceneTarget(m_towerSpawnTile) || IsSceneTarget(m_pathTile) ||
            IsSceneTarget(m_frameTile) || IsSceneTarget(m_directionTile) ||
            m_uiThemeBG_a.Exists(image => IsSceneTarget(image)) ||
            m_uiThemeBG_b.Exists(image => IsSceneTarget(image)) ||
            m_uiThemeBG_c.Exists(image => IsSceneTarget(image)) ||
            m_uiThemeFrame.Exists(image => IsSceneTarget(image)) ||
            m_uiThemeText_a.Exists(text => IsSceneTarget(text) && IsTextGraphic(text)) ||
            m_uiThemeText_b.Exists(text => IsSceneTarget(text) && IsTextGraphic(text)) ||
            m_uiThemeText_c.Exists(text => IsSceneTarget(text) && IsTextGraphic(text));
    }

    private static bool IsSceneTarget(Component target)
    {
        return target != null && target.gameObject.scene.IsValid() && target.gameObject.scene.isLoaded;
    }

    private void ApplyTheme(ThemeColors colors)
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("테마 색상 적용");

        int tilemapCount = 0;
        if (ApplyTilemapColor(m_towerSpawnTile, colors.MainColor_a)) tilemapCount++;
        if (ApplyTilemapColor(m_pathTile, colors.MainColor_b)) tilemapCount++;
        if (ApplyTilemapColor(m_frameTile, colors.FrameColor)) tilemapCount++;
        if (ApplyTilemapColor(m_directionTile, colors.SubColor)) tilemapCount++;

        int imageCount = ApplyImageColors(m_uiThemeBG_a, colors.MainColor_a);
        imageCount += ApplyImageColors(m_uiThemeBG_b, colors.MainColor_b);
        imageCount += ApplyImageColors(m_uiThemeBG_c, colors.SubColor);
        imageCount += ApplyImageColors(m_uiThemeFrame, colors.FrameColor);

        int textCount = ApplyTextColors(m_uiThemeText_a, Color.black);
        textCount += ApplyTextColors(m_uiThemeText_b, Color.white);
        textCount += ApplyTextColors(m_uiThemeText_c, Color.black);

        Undo.FlushUndoRecordObjects();
        Undo.CollapseUndoOperations(undoGroup);
        SaveThemeSettings();
        SceneView.RepaintAll();
        Repaint();
        Debug.Log($"{colors.name} 테마 적용: 타일맵 {tilemapCount}개 / UI 이미지 {imageCount}개 / 텍스트 {textCount}개. 결과를 유지하려면 씬 또는 프리팹을 저장하세요.");
    }

    private bool ApplyTilemapColor(Tilemap tilemap, Color color)
    {
        if (!IsSceneTarget(tilemap)) return false;
        Undo.RegisterCompleteObjectUndo(tilemap, "타일맵 테마 색상 적용");

        // Tilemap.color와 칸별 색상은 곱해집니다. 옛 테마색은 칸에서 지우고 맵에 새 색을 지정합니다.
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell) || tilemap.GetColor(cell) == Color.white) continue;
            TileFlags flags = tilemap.GetTileFlags(cell);
            tilemap.SetTileFlags(cell, flags & ~TileFlags.LockColor);
            tilemap.SetColor(cell, Color.white);
            tilemap.SetTileFlags(cell, flags);
        }
        tilemap.color = color;
        MarkTargetChanged(tilemap);
        return true;
    }

    private int ApplyImageColors(List<Image> images, Color color)
    {
        int count = 0;
        foreach (Image image in images)
        {
            if (!IsSceneTarget(image)) continue;
            Undo.RecordObject(image, "UI 테마 색상 적용");
            image.color = color;
            MarkTargetChanged(image);
            count++;
        }
        return count;
    }

    private static bool IsTextGraphic(Graphic graphic)
    {
        return graphic is TMP_Text || graphic is Text;
    }

    private int ApplyTextColors(List<Graphic> texts, Color color)
    {
        int count = 0;
        foreach (Graphic text in texts)
        {
            if (!IsSceneTarget(text) || !IsTextGraphic(text)) continue;
            Undo.RecordObject(text, "텍스트 테마 색상 적용");
            text.color = color;
            MarkTargetChanged(text);
            count++;
        }
        return count;
    }

    private void MarkTargetChanged(Component target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
    }

    private void DrawTilemapSection()
    {
        EditorGUILayout.LabelField("타일맵 대상", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        m_grid = (Grid)EditorGUILayout.ObjectField(
            "대상 Grid", m_grid, typeof(Grid), true);
        if (EditorGUI.EndChangeCheck())
        {
            m_gridGlobalId = GetPersistentId(m_grid);
            ClearTilemapTargets();
            if (m_grid != null) CollectTilemaps(false);
            SaveThemeSettings();
        }
        DrawTargetPersistenceHint(m_grid, m_gridGlobalId);

        // 목록에 적은 이름과 정확히 일치하는 타일맵을 찾습니다.
        SerializedObject settings = new SerializedObject(this);
        settings.Update();
        EditorGUILayout.PropertyField(settings.FindProperty("m_towerSpawnNames"), new GUIContent("스폰 이름 목록"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_pathNames"), new GUIContent("패스 이름 목록"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_frameNames"), new GUIContent("장식 이름 목록"), true);
        EditorGUILayout.PropertyField(settings.FindProperty("m_directionNames"), new GUIContent("화살표 이름 목록"), true);
        settings.ApplyModifiedProperties();

        using (new EditorGUI.DisabledScope(m_grid == null))
        {
            if (GUILayout.Button("이름으로 타일맵 담기"))
            {
                CollectTilemaps();
            }
        }

        // 버튼에서 담은 참조를 다시 읽어 화면에 표시합니다.
        settings.Update();
        EditorGUILayout.PropertyField(settings.FindProperty("m_towerSpawnTile"), new GUIContent("스폰 타일맵"));
        EditorGUILayout.PropertyField(settings.FindProperty("m_pathTile"), new GUIContent("패스 타일맵"));
        EditorGUILayout.PropertyField(settings.FindProperty("m_frameTile"), new GUIContent("장식 타일맵"));
        EditorGUILayout.PropertyField(settings.FindProperty("m_directionTile"), new GUIContent("화살표 타일맵"));
        settings.ApplyModifiedProperties();
        EditorGUILayout.Space(12);
    }

    private void DrawTargetPersistenceHint(Component target, string savedId)
    {
        if (target == null && !string.IsNullOrEmpty(savedId))
            EditorGUILayout.HelpBox("저장된 대상이 있는 씬을 열면 자동으로 연결됩니다.", MessageType.Info);
        else if (target != null && target.gameObject.scene.IsValid() && string.IsNullOrEmpty(target.gameObject.scene.path))
            EditorGUILayout.HelpBox("대상 연결을 기억하려면 먼저 씬을 저장하세요.", MessageType.Info);
    }

    private void ClearTilemapTargets()
    {
        m_towerSpawnTile = null;
        m_pathTile = null;
        m_frameTile = null;
        m_directionTile = null;
    }

    private void CollectTilemaps(bool logResult = true)
    {
        if (m_grid == null) return;

        // Grid 하위의 비활성 타일맵도 포함합니다.
        Tilemap[] tilemaps = m_grid.GetComponentsInChildren<Tilemap>(true);
        m_towerSpawnTile = FindNamedTilemap(tilemaps, m_towerSpawnNames);
        m_pathTile = FindNamedTilemap(tilemaps, m_pathNames);
        m_frameTile = FindNamedTilemap(tilemaps, m_frameNames);
        m_directionTile = FindNamedTilemap(tilemaps, m_directionNames);

        int count = 0;
        if (m_towerSpawnTile != null) count++;
        if (m_pathTile != null) count++;
        if (m_frameTile != null) count++;
        if (m_directionTile != null) count++;
        if (logResult)
            Debug.Log($"타일맵 수집 완료: 검사 {tilemaps.Length}개 / 연결 {count}개 (총 4개 역할)");
    }

    private Tilemap FindNamedTilemap(Tilemap[] tilemaps, List<string> names)
    {
        if (names == null) return null;

        foreach (Tilemap tilemap in tilemaps)
        {
            if (names.Contains(tilemap.name)) return tilemap;
        }

        // 일치하는 이름이 없으면 이전 참조를 비웁니다.
        return null;
    }

    private void ClearCanvasTargets()
    {
        m_uiThemeBG_a.Clear();
        m_uiThemeBG_b.Clear();
        m_uiThemeBG_c.Clear();
        m_uiThemeFrame.Clear();
        m_uiThemeText_a.Clear();
        m_uiThemeText_b.Clear();
        m_uiThemeText_c.Clear();
    }

    private void CollectCanvasTargets(bool logResult = true)
    {
        if (m_canvas == null) return;

        ClearCanvasTargets();

        Image[] images = m_canvas.GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            string name = image.name.Split(' ')[0];

            if (IsBackGround(name, "A"))
            {
                m_uiThemeBG_a.Add(image);
            }
            else if (IsBackGround(name, "B"))
            {
                m_uiThemeBG_b.Add(image);
            }
            else if (IsBackGround(name, "C"))
            {
                m_uiThemeBG_c.Add(image);
            }
            else if (IsFrame(name))
            {
                m_uiThemeFrame.Add(image);
            }
        }

        Graphic[] graphics = m_canvas.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (!IsTextGraphic(graphic)) continue;
            string name = graphic.name.Split(' ')[0];
            if (IsTextName(name, "A")) m_uiThemeText_a.Add(graphic);
            else if (IsTextName(name, "B")) m_uiThemeText_b.Add(graphic);
            else if (IsTextName(name, "C")) m_uiThemeText_c.Add(graphic);
        }

        if (logResult)
            Debug.Log($"UI 수집 완료: 이미지 A {m_uiThemeBG_a.Count}개 / B {m_uiThemeBG_b.Count}개 / C {m_uiThemeBG_c.Count}개 / 프레임 {m_uiThemeFrame.Count}개, 텍스트 A {m_uiThemeText_a.Count}개 / B {m_uiThemeText_b.Count}개 / C {m_uiThemeText_c.Count}개");
    }

    private bool IsTextName(string name, string role)
    {
        return name.StartsWith($"Txt_{role}_");
    }

    private bool IsBackGround(string name, string role)
    {
        return name.StartsWith($"Img_{role}_") && name.EndsWith("_BG");
    }

    private bool IsFrame(string name)
    {
        return name.StartsWith($"Img_") && name.EndsWith("_Frame");
    }
}
