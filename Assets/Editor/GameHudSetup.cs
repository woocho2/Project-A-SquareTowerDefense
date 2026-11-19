using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class GameHudSetup
    {
        public const string Folder = "ArtPreviews/GameHud_20261118", Prefab = "Assets/2. Prefab/UICanvas.prefab";
        private static readonly Color Ink = new Color32(19, 29, 49, 247), Gold = new Color32(226, 194, 120, 255), Paper = new Color32(237, 241, 249, 255), Muted = new Color32(157, 177, 203, 255), Blue = new Color32(112, 192, 228, 255);
        private static TMP_FontAsset s_font;
        private static Sprite s_surface;
        private static double s_next;
        static GameHudSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string path = Folder + "/request.txt"; if (!File.Exists(path)) return;
            string command = File.ReadAllText(path).Trim(); File.Delete(path);
            try
            {
                if (command == "apply") Apply();
                else if (command == "placements") UpdateScenePlacements();
                else if (command == "compile") UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation(UnityEditor.Compilation.RequestScriptCompilationOptions.CleanBuildCache);
                else if (command == "state") File.WriteAllText(Folder + "/editor-state.txt", $"Playing={EditorApplication.isPlayingOrWillChangePlaymode}, Step={SessionState.GetInt("SquareTD.GameHudVerification", 0)}, Paused={EditorApplication.isPaused}, Frame={Time.frameCount}, State={TurnManager.Instance?.Current?.Name}, Background={Application.runInBackground}, CompilationFailed={typeof(EditorUtility).GetProperty("scriptCompilationFailed", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(null)}");
                else if (command == "verify") GameHudVerification.Run();
            }
            catch (Exception exception) { File.WriteAllText(Folder + "/setup-error.txt", exception.ToString()); Debug.LogException(exception); }
        }
        [MenuItem("Tools/SquareTD/게임 HUD 적용")]
        public static void Apply()
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/NotoSansKR-Black SDF.asset");
            s_surface = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                GameInfoPanel info = root.GetComponentInChildren<GameInfoPanel>(true);
                PlayerTurnPanel actions = root.GetComponentInChildren<PlayerTurnPanel>(true);
                Image oldSymbol = actions.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image.name == "Img_CreateTower_Symbol" || image.name == "TowerIcon");
                Sprite castle = oldSymbol != null ? oldSymbol.sprite : null;
                BuildInfo(info); BuildActions(actions, castle);
                ControlsEffectsSetup.Style(root);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); UpdateScenePlacements(); File.WriteAllText(Folder + "/apply-result.txt", "Success");
        }

        public static void UpdateScenePlacements()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 씬을 저장해 주세요.");
            Scene previous = SceneManager.GetActiveScene(); var report = new System.Collections.Generic.List<string>();
            foreach (string path in Directory.GetFiles("Assets/1. Scenes", "*.unity").Where(path => Path.GetFileName(path).Contains("Stage")))
            {
                Scene scene = SceneManager.GetSceneByPath(path.Replace('\\', '/')); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (string source in new[] { path, path + ".meta" })
                    {
                        string backup = "ArtBackups/GameHud_20261118/" + source;
                        if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(source, backup); }
                    }
                    GameInfoPanel info = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameInfoPanel>(true)).FirstOrDefault();
                    PlayerTurnPanel actions = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerTurnPanel>(true)).FirstOrDefault();
                    if (info == null || actions == null) continue;
                    var infoSo = new SerializedObject(info); var actionSo = new SerializedObject(actions);
                    Transform[] controls = { info.transform, actions.transform,
                        ((Button)infoSo.FindProperty("btn_option").objectReferenceValue).transform,
                        ((Button)infoSo.FindProperty("btn_gameSpeed").objectReferenceValue).transform,
                        ((Button)actionSo.FindProperty("btn_createTower").objectReferenceValue).transform };
                    int changed = 0;
                    foreach (Transform control in controls)
                    {
                        if (!PrefabUtility.IsPartOfPrefabInstance(control)) continue;
                        var so = new SerializedObject(control);
                        foreach (string property in new[] { "m_AnchorMin", "m_AnchorMax", "m_AnchoredPosition", "m_SizeDelta", "m_Pivot", "m_LocalScale", "m_LocalRotation" })
                        {
                            SerializedProperty value = so.FindProperty(property);
                            if (value == null || !value.prefabOverride) continue;
                            PrefabUtility.RevertPropertyOverride(value, InteractionMode.AutomatedAction); changed++; so.Update();
                        }
                    }
                    if (changed > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                    report.Add(path + " | HUD placement overrides reverted=" + changed);
                }
                finally { if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            File.WriteAllLines(Folder + "/scene-placements.txt", report);
        }

        private static void BuildInfo(GameInfoPanel info)
        {
            var so = new SerializedObject(info);
            Button option = (Button)so.FindProperty("btn_option").objectReferenceValue, speed = (Button)so.FindProperty("btn_gameSpeed").objectReferenceValue;
            option.transform.SetParent(info.transform, false); speed.transform.SetParent(info.transform, false);
            foreach (Transform child in info.transform.Cast<Transform>().ToArray()) if (child != option.transform && child != speed.transform) Object.DestroyImmediate(child.gameObject);
            RectTransform root = (RectTransform)info.transform; Stretch(root); root.localScale = Vector3.one;
            Image background = info.GetComponent<Image>(); background.color = Color.clear; background.raycastTarget = false;
            RectTransform header = Rect(root, "StatusBar", 0, 0, 1024, 106); header.anchorMin = header.anchorMax = header.pivot = new Vector2(.5f, 1); header.anchoredPosition = new Vector2(0, -14);
            Image waveCard = Card(header, "WaveCard", 0, 250);
            TextMeshProUGUI stage = Text(waveCard.transform, "Stage", "아스가르드", 16, Muted, 16, 10, 218, 24);
            TextMeshProUGUI wave = Text(waveCard.transform, "Wave", "WAVE 1", 29, Paper, 16, 42, 142, 43);
            TextMeshProUGUI boss = Text(waveCard.transform, "BossTurns", "보스 20턴", 15, new Color32(247, 148, 141, 255), 153, 50, 84, 28); boss.gameObject.SetActive(false);
            Image lifeCard = Card(header, "LifeCard", 264, 236); Text(lifeCard.transform, "Label", "적 수 / 한도", 15, Muted, 16, 10, 204, 24);
            TextMeshProUGUI life = Text(lifeCard.transform, "Life", "0 / 20", 28, Paper, 16, 37, 204, 39);
            Image lifeFill = Bar(lifeCard.transform, "LifeProgress", 16, 84, 204, 5, Paper);
            Image goldCard = Card(header, "GoldCard", 514, 180); Text(goldCard.transform, "Label", "보유 골드", 15, Muted, 16, 10, 148, 24);
            TextMeshProUGUI gold = Text(goldCard.transform, "Gold", "500", 29, Gold, 16, 40, 148, 43);
            Image levelCard = Card(header, "PlayerLevelCard", 708, 316); Text(levelCard.transform, "Label", "플레이어 레벨", 15, Muted, 16, 10, 284, 24);
            TextMeshProUGUI level = Text(levelCard.transform, "Level", "Lv. 1", 28, Gold, 16, 38, 92, 40);
            TextMeshProUGUI xp = Text(levelCard.transform, "Experience", "0 / 20 EXP", 17, Paper, 112, 46, 188, 26); xp.alignment = TextAlignmentOptions.MidlineRight;
            Image xpFill = Bar(levelCard.transform, "ExperienceProgress", 16, 84, 284, 6, Blue);
            StyleButton(option, 56, 56); TopRight((RectTransform)option.transform, 16);
            for (int i = 0; i < 3; i++) Box(option.transform, "Line_" + i, Gold, 16, 17 + i * 9, 24, 3);
            StyleButton(speed, 56, 56); TopRight((RectTransform)speed.transform, 82);
            TextMeshProUGUI speedText = Text(speed.transform, "Speed", "×1", 20, Paper, 0, 0, 56, 56); speedText.alignment = TextAlignmentOptions.Center;
            var skipRoot = new GameObject("Group_Skip", typeof(RectTransform), typeof(Image), typeof(Button));
            skipRoot.layer = 5; skipRoot.transform.SetParent(info.transform, false);
            Button skip = skipRoot.GetComponent<Button>();
            StyleButton(skip, 76, 56); TopRight((RectTransform)skip.transform, 148);
            TextMeshProUGUI skipText = Text(skip.transform, "Skip", "스킵", 18, Paper, 0, 0, 76, 56);
            skipText.alignment = TextAlignmentOptions.Center;
            Set(so, "btn_skip", skip); Set(so, "txt_skip", skipText);
            Set(so, "txt_stage", stage); Set(so, "txt_currentWave", wave); Set(so, "txt_currentLimit", life); Set(so, "img_life", lifeFill); Set(so, "txt_currentGold", gold);
            Set(so, "txt_playerLevel", level); Set(so, "txt_playerXp", xp); Set(so, "img_playerXpFill", xpFill); Set(so, "txt_bossTurns", boss); Set(so, "txt_currentGameSpeed", speedText);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void BuildActions(PlayerTurnPanel actions, Sprite castle)
        {
            var so = new SerializedObject(actions);
            Button create = (Button)so.FindProperty("btn_createTower").objectReferenceValue, end = (Button)so.FindProperty("btn_turnEnd").objectReferenceValue;
            TextMeshProUGUI endLabel = end.GetComponentInChildren<TextMeshProUGUI>(true);
            if (endLabel != null) { endLabel.text = "턴 종료"; endLabel.fontSize = 24; }
            Transform keep = actions.transform.Cast<Transform>().First(child => end.transform == child || end.transform.IsChildOf(child));
            create.transform.SetParent(actions.transform, false);
            foreach (Transform child in actions.transform.Cast<Transform>().ToArray()) if (child != keep && child != create.transform) Object.DestroyImmediate(child.gameObject);
            Stretch((RectTransform)actions.transform); actions.GetComponent<Image>().raycastTarget = false;
            StyleButton(create, 220, 124); Bottom((RectTransform)create.transform, false);
            Image symbol = Box(create.transform, "TowerIcon", Gold, 16, 16, 36, 36); symbol.sprite = castle; symbol.type = Image.Type.Simple; symbol.preserveAspect = true;
            if (castle == null) { symbol.enabled = false; Text(create.transform, "Plus", "+", 31, Gold, 16, 14, 36, 40).alignment = TextAlignmentOptions.Center; }
            Text(create.transform, "Label", "타워 생성", 23, Paper, 64, 17, 140, 36);
            Text(create.transform, "GoldLabel", "골드", 15, Muted, 16, 63, 42, 29);
            TextMeshProUGUI buildCost = Text(create.transform, "Cost", TowerManager.FixedBuildCost.ToString(), 26, Gold, 64, 58, 140, 36);
            Text(create.transform, "Hint", "새로운 기물을 소환합니다", 13, Muted, 16, 99, 188, 20);
            var buyRoot = new GameObject("BuyExperience", typeof(RectTransform), typeof(Image), typeof(Button)); buyRoot.layer = 5; buyRoot.transform.SetParent(actions.transform, false);
            Button buy = buyRoot.GetComponent<Button>(); StyleButton(buy, 220, 124); Bottom((RectTransform)buy.transform, true);
            Text(buy.transform, "Label", "경험치 투자", 20, Paper, 16, 17, 136, 34);
            TextMeshProUGUI gain = Text(buy.transform, "Gain", "+1 EXP", 15, Blue, 150, 22, 54, 27); gain.alignment = TextAlignmentOptions.MidlineRight;
            Text(buy.transform, "GoldLabel", "골드", 15, Muted, 16, 63, 42, 29);
            TextMeshProUGUI xpCost = Text(buy.transform, "Cost", BalanceTables.XpPurchaseCost.ToString(), 26, Gold, 64, 58, 140, 36);
            Text(buy.transform, "Hint", "레벨업 · 전체 타워 강화", 13, Muted, 16, 99, 188, 20);
            Set(so, "btn_buyXp", buy); Set(so, "txt_buyXpCost", xpCost); Set(so, "txt_xpGain", gain); Set(so, "txt_createTowerValue", buildCost);
            Set(so, "txt_level", null); Set(so, "txt_xp", null); Set(so, "img_xpFill", null);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Image Card(Transform parent, string name, float x, float width)
        {
            Image card = Box(parent, name, Ink, x, 0, width, 106);
            Box(card.transform, "Accent", new Color(Gold.r, Gold.g, Gold.b, .7f), 12, 0, width - 24, 2);
            return card;
        }
        private static Image Bar(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            Image track = Box(parent, name, new Color32(53, 69, 93, 255), x, y, width, height);
            Image fill = Box(track.transform, "Fill", color, 0, 0, width, height); Stretch(fill.rectTransform); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0; return fill;
        }
        private static void StyleButton(Button button, float width, float height)
        {
            foreach (Transform child in button.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            RectTransform rect = (RectTransform)button.transform; rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            Image image = button.GetComponent<Image>(); if (image == null) image = button.gameObject.AddComponent<Image>(); image.sprite = s_surface; image.type = Image.Type.Sliced; image.color = Ink; image.raycastTarget = true; button.targetGraphic = image;
            ColorBlock colors = ColorBlock.defaultColorBlock; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(.75f, .75f, .75f); colors.disabledColor = new Color(.55f, .55f, .55f); button.colors = colors;
            Box(button.transform, "Accent", Gold, 10, 0, width - 20, 2);
        }
        private static void TopRight(RectTransform rect, float right) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = new Vector2(-right, -14); }
        private static void Bottom(RectTransform rect, bool right) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, 0); rect.anchoredPosition = new Vector2(right ? -16 : 16, 16); }
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static Image Box(Transform parent, string name, Color color, float x, float y, float width, float height)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>(); image.sprite = s_surface; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false; return image;
        }
        private static TextMeshProUGUI Text(Transform parent, string name, string value, int size, Color color, float x, float y, float width, float height)
        {
            TextMeshProUGUI text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); text.font = s_font; text.text = value; text.fontSize = size; text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true; text.fontSizeMin = size - 2; text.fontSizeMax = size; return text;
        }
        private static void Set(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
