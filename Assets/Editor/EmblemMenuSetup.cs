using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class EmblemMenuSetup
    {
        public const string Folder = "ArtPreviews/EmblemMenu_20261118";
        public const string MenuScene = "Assets/1. Scenes/2. Menu.unity";
        private static readonly Color Ink = new Color32(17, 23, 43, 255), Card = new Color32(32, 43, 63, 255), Gold = new Color32(221, 190, 112, 255), Muted = new Color32(160, 174, 197, 255);
        private static TMP_FontAsset s_font;
        private static Sprite s_surface;
        private static double s_next;
        static EmblemMenuSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string request = Folder + "/request.txt";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (command == "apply") { Apply(); File.WriteAllText(Folder + "/apply-result.txt", "Success"); }
                else if (command == "verify") EmblemMenuVerification.Run();
                else if (command == "capture") EmblemMenuVerification.CapturePreview();
                else if (command == "counts") ApplyCounts();
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/setup-error.txt", exception.ToString()); Debug.LogException(exception); }
        }

        [MenuItem("Tools/SquareTD/엠블럼 선택 화면 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 종료 후 적용해 주세요.");
            Scene previous = SceneManager.GetActiveScene();
            Scene menu = SceneManager.GetSceneByPath(MenuScene);
            bool opened = !menu.IsValid() || !menu.isLoaded;
            if (!opened && menu.isDirty) throw new InvalidOperationException("메뉴 씬의 저장하지 않은 변경이 있습니다.");
            if (opened) menu = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Additive);
            try
            {
                EmblemSelectPanel panel = menu.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EmblemSelectPanel>(true)).Single();
                int[] capacities = ReadCapacities();
                SceneManager.SetActiveScene(menu);
                Configure(panel, capacities);
                EditorSceneManager.MarkSceneDirty(menu); EditorSceneManager.SaveScene(menu);
            }
            finally { if (previous.IsValid()) SceneManager.SetActiveScene(previous); if (opened) EditorSceneManager.CloseScene(menu, true); }
            AssetDatabase.SaveAssets();
        }

        public static void ApplyCounts()
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene menu = SceneManager.GetSceneByPath(MenuScene); bool opened = !menu.IsValid() || !menu.isLoaded;
            if (!opened && menu.isDirty) throw new InvalidOperationException("메뉴 씬을 먼저 저장해 주세요.");
            if (opened) menu = EditorSceneManager.OpenScene(MenuScene, OpenSceneMode.Additive);
            try
            {
                EmblemSelectPanel panel = menu.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EmblemSelectPanel>(true)).Single();
                var so = new SerializedObject(panel);
                SerializedProperty buttons = so.FindProperty("m_slotButtons");
                var excess = new System.Collections.Generic.List<GameObject>();
                for (int i = 0; i < buttons.arraySize; i++)
                {
                    var button = (Button)buttons.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (button == null) continue;
                    if (i >= EmblemSelectPanel.HardCount) excess.Add(button.gameObject);
                    else button.gameObject.SetActive(i < EmblemSelectPanel.NormalCount);
                }
                foreach (string key in new[] { "img_slots", "m_slotButtons", "m_slotNames" }) so.FindProperty(key).arraySize = EmblemSelectPanel.HardCount;
                ((TextMeshProUGUI)so.FindProperty("txt_count").objectReferenceValue).text = $"0 / {EmblemSelectPanel.NormalCount} 선택";
                ((TextMeshProUGUI)so.FindProperty("m_hint").objectReferenceValue).text = $"문양 {EmblemSelectPanel.NormalCount}개를 더 선택해 주세요";
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (GameObject extra in excess) Object.DestroyImmediate(extra);
                EditorSceneManager.MarkSceneDirty(menu); EditorSceneManager.SaveScene(menu);
                File.WriteAllText(Folder + "/counts-result.txt", $"Normal={EmblemSelectPanel.NormalCount}, Hard={EmblemSelectPanel.HardCount}, Slots={so.FindProperty("img_slots").arraySize}");
            }
            finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if (opened) EditorSceneManager.CloseScene(menu, true); }
        }

        private static int[] ReadCapacities()
        {
            var capacities = new int[10]; var report = new System.Collections.Generic.List<string>();
            foreach (string path in Directory.GetFiles("Assets/1. Scenes", "*.unity"))
            {
                if (!Path.GetFileName(path).Contains("Stage")) continue;
                string name = Path.GetFileNameWithoutExtension(path); int stageNumber;
                if (!int.TryParse(name.Substring(name.LastIndexOf("Stage", StringComparison.Ordinal) + 5), out stageNumber) || stageNumber < 1 || stageNumber > 10) continue;
                Scene scene = SceneManager.GetSceneByPath(path.Replace('\\', '/')); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    BoardManager board = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BoardManager>(true)).FirstOrDefault();
                    Tilemap map = board != null ? new SerializedObject(board).FindProperty("m_synergyTilemap").objectReferenceValue as Tilemap : null;
                    if (map == null && board != null)
                    {
                        Tilemap pathMap = new SerializedObject(board).FindProperty("m_pathTilemap").objectReferenceValue as Tilemap;
                        if (pathMap != null && pathMap.transform.parent != null) map = pathMap.transform.parent.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(item => item.name == "Synergy");
                    }
                    int count = 0;
                    if (map != null) foreach (Vector3Int cell in map.cellBounds.allPositionsWithin) if (cell.z == 0 && map.HasTile(cell)) count++;
                    capacities[stageNumber - 1] = count; report.Add($"Stage{stageNumber}: {count} 시너지 칸");
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            File.WriteAllLines(Folder + "/stage-capacities.txt", report);
            return capacities;
        }

        private static void Configure(EmblemSelectPanel panel, int[] capacities)
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/NotoSansKR-Black SDF.asset");
            s_surface = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var so = new SerializedObject(panel); SerializedProperty originals = so.FindProperty("img_emblems");
            Sprite[] sprites = new Sprite[14];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = ((Image)originals.GetArrayElementAtIndex(i).objectReferenceValue).sprite;
            foreach (Transform child in panel.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            RectTransform root = (RectTransform)panel.transform; Stretch(root);
            Image shade = panel.GetComponent<Image>(); shade.color = new Color(0, .015f, .04f, .88f); shade.raycastTarget = true;
            Canvas canvas = panel.GetComponent<Canvas>(); if (canvas == null) canvas = panel.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 100;
            if (panel.GetComponent<GraphicRaycaster>() == null) panel.gameObject.AddComponent<GraphicRaycaster>();
            Image frame = Box(root, "Window", Gold, 0, 0, 1540, 900); RectTransform window = frame.rectTransform;
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f); window.anchoredPosition = Vector2.zero;
            Box(window, "Surface", Ink, 2, 2, 1536, 896);
            Text(window, "Heading", "출전 문양 선택", 32, Color.white, 46, 28, 900, 72);
            TextMeshProUGUI stage = Text(window, "Stage", "아스가르드 · 노말", 19, Muted, 48, 93, 930, 42);
            TextMeshProUGUI count = Text(window, "Count", $"0 / {EmblemSelectPanel.NormalCount} 선택", 25, Color.white, 1080, 43, 414, 60); count.alignment = TextAlignmentOptions.MidlineRight;
            Box(window, "HeaderRule", new Color(Gold.r, Gold.g, Gold.b, .35f), 46, 148, 1448, 1);
            Text(window, "GridHeading", "사용할 문양", 20, Gold, 46, 170, 870, 44);
            Text(window, "GridHelp", "문양을 눌러 선택하거나 해제하세요", 15, Muted, 365, 174, 550, 36).alignment = TextAlignmentOptions.MidlineRight;
            var icons = new Image[14]; var choices = new Button[14]; var borders = new Image[14]; var checks = new GameObject[14];
            for (int i = 0; i < 14; i++)
            {
                Image border = Box(window, "Emblem_" + (i + 1).ToString("00"), new Color32(72, 85, 108, 255), 46 + i % 7 * 126, 226 + i / 7 * 132, 114, 118);
                Box(border.transform, "Card", Card, 2, 2, 110, 114);
                Image icon = Box(border.transform, "Icon", Color.white, 32, 18, 50, 50); icon.sprite = sprites[i]; icon.type = Image.Type.Simple; icon.preserveAspect = true;
                Text(border.transform, "Name", EmblemNames.GetEmblemName((TowerEmblem)(i + 1)), 18, Color.white, 4, 74, 106, 38).alignment = TextAlignmentOptions.Top;
                Button button = MakeButton(border);
                Image badge = Box(border.transform, "Selected", Gold, 88, 5, 21, 21);
                Image check = Box(badge.transform, "Check", Ink, 3, 3, 15, 15); check.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"); check.type = Image.Type.Simple;
                badge.gameObject.SetActive(false);
                icons[i] = icon; choices[i] = button; borders[i] = border; checks[i] = badge.gameObject;
            }
            Text(window, "SelectionHeading", "선택한 문양", 20, Gold, 46, 519, 550, 44);
            Text(window, "SelectionHelp", "아래 칸을 눌러도 해제할 수 있습니다", 14, Muted, 420, 523, 500, 36).alignment = TextAlignmentOptions.MidlineRight;
            var slots = new Image[EmblemSelectPanel.HardCount]; var slotButtons = new Button[EmblemSelectPanel.HardCount]; var slotNames = new TextMeshProUGUI[EmblemSelectPanel.HardCount];
            for (int i = 0; i < EmblemSelectPanel.HardCount; i++)
            {
                Image tile = Box(window, "Selected_" + i, Card, 46 + i * 110, 577, 98, 102);
                slots[i] = Box(tile.transform, "Icon", Gold, 27, 9, 44, 44); slots[i].type = Image.Type.Simple; slots[i].preserveAspect = true;
                slotNames[i] = Text(tile.transform, "Name", "비어 있음", 15, Muted, 2, 62, 94, 35); slotNames[i].alignment = TextAlignmentOptions.Top;
                slotButtons[i] = MakeButton(tile);
                tile.gameObject.SetActive(i < EmblemSelectPanel.NormalCount);
            }
            TextMeshProUGUI hint = Text(window, "SelectionHint", $"문양 {EmblemSelectPanel.NormalCount}개를 더 선택해 주세요", 19, Muted, 46, 724, 880, 54);
            Box(window, "SynergySurface", new Color32(23, 32, 51, 255), 1004, 170, 490, 573);
            TextMeshProUGUI synergyCount = Text(window, "SynergyHeading", "예상 시너지 타워  0기", 20, Gold, 1024, 185, 450, 44);
            Text(window, "SynergyHelp", "선택 문양 일치 수", 14, Muted, 1024, 232, 450, 32).alignment = TextAlignmentOptions.MidlineRight;
            RectTransform viewport = Rect(window, "SynergyViewport", 1020, 276, 458, 451); viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            RectTransform content = Rect(viewport, "Content", 0, 0, 458, 451);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 8; layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            var rows = so.FindProperty("m_synergyRows"); rows.arraySize = 14;
            for (int i = 0; i < 14; i++)
            {
                Image card = Box(content, "Synergy_" + i, Card, 0, 0, 458, 68); card.gameObject.AddComponent<LayoutElement>().preferredHeight = 68;
                Image portrait = Box(card.transform, "Portrait", Color.white, 8, 9, 50, 50); portrait.type = Image.Type.Simple; portrait.preserveAspect = true;
                TextMeshProUGUI name = Text(card.transform, "Name", "", 18, Color.white, 70, 0, 280, 38);
                TextMeshProUGUI ingredients = Text(card.transform, "Ingredients", "", 14, Muted, 70, 38, 370, 28); ingredients.richText = true;
                TextMeshProUGUI match = Text(card.transform, "Match", "", 16, Gold, 364, 2, 84, 36); match.alignment = TextAlignmentOptions.TopRight;
                SerializedProperty row = rows.GetArrayElementAtIndex(i); row.FindPropertyRelative("Root").objectReferenceValue = card.gameObject;
                row.FindPropertyRelative("Portrait").objectReferenceValue = portrait; row.FindPropertyRelative("Name").objectReferenceValue = name;
                row.FindPropertyRelative("Ingredients").objectReferenceValue = ingredients; row.FindPropertyRelative("Match").objectReferenceValue = match;
                card.gameObject.SetActive(false);
            }
            TextMeshProUGUI empty = Text(window, "EmptySynergy", "문양을 선택하면 관련 시너지 타워가 표시됩니다.\n재료 3개를 모두 맞추면 스테이지에 등장합니다.", 18, Muted, 1028, 410, 442, 140); empty.alignment = TextAlignmentOptions.Center;
            Text(window, "PriorityHint", "선택 문양 관련 타워 표시 · 3/3이면 등장", 14, Muted, 1006, 751, 488, 38);
            Box(window, "FooterRule", new Color(Gold.r, Gold.g, Gold.b, .2f), 46, 794, 1448, 1);
            Image backSurface = Box(window, "Back", Card, 46, 820, 180, 52); Button back = MakeButton(backSurface);
            Text(back.transform, "Label", "돌아가기", 18, Color.white, 8, 1, 164, 50).alignment = TextAlignmentOptions.Center;
            Image selectSurface = Box(window, "Select", Gold, 1250, 812, 244, 64); Button select = MakeButton(selectSurface);
            TextMeshProUGUI selectLabel = Text(select.transform, "Label", "SELECT", 23, Ink, 8, 0, 228, 64); selectLabel.alignment = TextAlignmentOptions.Center;
            SetArray(so, "img_emblems", icons); SetArray(so, "img_slots", slots); SetArray(so, "m_choices", choices); SetArray(so, "m_slotButtons", slotButtons);
            SetArray(so, "m_borders", borders); SetArray(so, "m_checks", checks); SetArray(so, "m_slotNames", slotNames);
            Set(so, "txt_count", count); Set(so, "m_hint", hint); Set(so, "m_stageTitle", stage); Set(so, "m_synergyCount", synergyCount); Set(so, "m_emptySynergy", empty); Set(so, "m_selectLabel", selectLabel);
            Set(so, "btn_emblemSelect", select); Set(so, "btn_back", back); Set(so, "m_window", window);
            Set(so, "m_synergyCatalog", AssetDatabase.LoadAssetAtPath<SynergyCharacterCatalog>("Assets/Resources/SynergyCharacterCatalog.asset"));
            SerializedProperty counts = so.FindProperty("m_stageSynergyCapacity"); counts.arraySize = capacities.Length;
            for (int i = 0; i < capacities.Length; i++) counts.GetArrayElementAtIndex(i).intValue = capacities[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(SerializedObject so, string key, Object value) => so.FindProperty(key).objectReferenceValue = value;
        private static void SetArray<T>(SerializedObject so, string key, T[] items) where T : Object
        { SerializedProperty list = so.FindProperty(key); list.arraySize = items.Length; for (int i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static Image Box(Transform parent, string name, Color color, float x, float y, float width, float height)
        { Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; if (height > 4) { image.sprite = s_surface; image.type = Image.Type.Sliced; } return image; }
        private static TextMeshProUGUI Text(Transform parent, string name, string value, int size, Color color, float x, float y, float width, float height)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); text.font = s_font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; return text;
        }
        private static Button MakeButton(Image image)
        { image.raycastTarget = true; Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image; ColorBlock colors = button.colors; colors.disabledColor = new Color(.40f, .42f, .48f, 1); button.colors = colors; return button; }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
