using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class ResultPanelSetup
    {
        public const string Output = "ArtPreviews/ResultPanels_20261118";
        public const string Prefab = "Assets/2. Prefab/UICanvas.prefab";
        private static TMP_FontAsset s_font;
        private static Sprite s_surface;
        private static readonly Color Gold = new Color32(224, 193, 119, 255), Paper = new Color32(237, 241, 249, 255), Muted = new Color32(156, 172, 197, 255);
        private static double s_next;
        static ResultPanelSetup() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string path = Output + "/request.txt"; if (!File.Exists(path)) return;
            string command = File.ReadAllText(path).Trim(); File.Delete(path);
            try
            {
                if (command == "apply") Apply();
                else if (command == "verify") ResultPanelVerification.Run();
            }
            catch (Exception exception) { File.WriteAllText(Output + "/setup-error.txt", exception.ToString()); Debug.LogException(exception); }
        }

        [MenuItem("Tools/SquareTD/승패 결과 패널 적용")]
        public static void Apply()
        {
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/NotoSansKR-Black SDF.asset");
            s_surface = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Transform over = root.GetComponentsInChildren<Transform>(true).First(item => item.name == "GameOverPanel");
                Transform clear = root.GetComponentsInChildren<Transform>(true).First(item => item.name == "GameClearPanel");
                Build(over.gameObject, false); Build(clear.gameObject, true);
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets(); File.WriteAllText(Output + "/apply-result.txt", "Success");
        }

        private static void Build(GameObject root, bool won)
        {
            root.SetActive(false);
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            RectTransform overlay = root.GetComponent<RectTransform>(); Stretch(overlay);
            overlay.localScale = Vector3.one; overlay.localRotation = Quaternion.identity;
            Image shade = root.GetComponent<Image>(); if (shade == null) shade = root.AddComponent<Image>(); shade.color = new Color(.025f, .04f, .075f, .94f); shade.raycastTarget = true;
            CanvasGroup group = root.GetComponent<CanvasGroup>(); if (group == null) group = root.AddComponent<CanvasGroup>(); group.alpha = 1; group.interactable = true; group.blocksRaycasts = true;
            BattleResultPanel view = root.GetComponent<BattleResultPanel>(); if (view == null) view = root.AddComponent<BattleResultPanel>();
            Image window = Box(root.transform, "ResultWindow", new Color32(24, 33, 53, 255), 0, 0, 1480, 920);
            RectTransform windowRect = window.rectTransform; windowRect.anchorMin = windowRect.anchorMax = windowRect.pivot = new Vector2(.5f, .5f); windowRect.anchoredPosition = Vector2.zero;
            Image accent = Box(window.transform, "Accent", Gold, 0, 0, 1480, 4);
            Text(window.transform, "Eyebrow", "전투 결과", 17, Muted, 54, 24, 500, 30);
            TextMeshProUGUI title = Text(window.transform, "Title", won ? "스테이지 클리어" : "도전 종료", 42, Gold, 50, 61, 900, 70);
            TextMeshProUGUI subtitle = Text(window.transform, "Subtitle", "아스가르드 · NORMAL · 10 WAVE", 19, Muted, 54, 132, 1300, 34);
            TextMeshProUGUI count = Summary(window.transform, "Owned", "종료 시점 보유 기물", "0기", 54);
            TextMeshProUGUI total = Summary(window.transform, "Total", "보유 기물 누적 피해", "0", 518);
            TextMeshProUGUI best = Summary(window.transform, "Best", "최고 피해 기물", "—", 982);
            Text(window.transform, "RankHeader", "순위", 15, Muted, 72, 286, 70, 28);
            Text(window.transform, "TowerHeader", "보유 기물 · 배치", 15, Muted, 212, 286, 330, 28);
            Text(window.transform, "TierHeader", "티어", 15, Muted, 580, 286, 120, 28);
            Text(window.transform, "DamageHeader", "실제 누적 피해", 15, Muted, 724, 286, 460, 28);
            Text(window.transform, "ShareHeader", "비중", 15, Muted, 1318, 286, 70, 28);

            RectTransform scrollRoot = Rect(window.transform, "Roster", 54, 326, 1372, 438);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
            RectTransform viewport = Rect(scrollRoot, "Viewport", 0, 0, 1348, 438); viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportHit = viewport.gameObject.AddComponent<Image>(); viewportHit.color = new Color(0, 0, 0, .01f); viewportHit.raycastTarget = true;
            RectTransform content = Rect(viewport, "Content", 0, 0, 1348, 0); content.anchorMax = new Vector2(1, 1); content.sizeDelta = new Vector2(0, 0);
            scroll.viewport = viewport; scroll.content = content;
            Image track = Box(scrollRoot, "Scrollbar", new Color32(38, 50, 73, 255), 1358, 0, 8, 438);
            Image handle = Box(track.transform, "Handle", Muted, 0, 0, 8, 100); Stretch(handle.rectTransform);
            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle; scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            BattleResultRow template = BuildRow(content); template.gameObject.SetActive(false);
            TextMeshProUGUI empty = Text(window.transform, "Empty", "종료 시점에 보유한 기물이 없습니다.", 24, Muted, 54, 470, 1372, 70); empty.alignment = TextAlignmentOptions.Center;
            Text(window.transform, "DamageNote", "적의 남은 체력만큼 실제로 가한 피해입니다. 합성 전 재료 기물의 기록도 포함합니다.", 16, Muted, 54, 780, 1372, 28);
            Text(window.transform, "SupportNote", "버프·디버프의 간접 기여는 피해량으로 환산하지 않습니다.", 15, Muted, 54, 809, 900, 28);
            Button select = Button(window.transform, "StageSelectButton", "스테이지 선택", 806, 848, false);
            Button primary = Button(window.transform, "PrimaryButton", won ? "다음 스테이지" : "다시하기", 1124, 848, true);

            var so = new SerializedObject(view);
            Set(so, "m_title", title); Set(so, "m_subtitle", subtitle); Set(so, "m_count", count); Set(so, "m_total", total); Set(so, "m_best", best); Set(so, "m_empty", empty);
            Set(so, "m_accent", accent); Set(so, "m_window", windowRect); Set(so, "m_content", content); Set(so, "m_group", group); Set(so, "m_scroll", scroll); Set(so, "m_rowTemplate", template);
            Set(so, "m_primary", primary); Set(so, "m_stageSelect", select); Set(so, "m_primaryLabel", primary.GetComponentInChildren<TextMeshProUGUI>());
            StageDefinition[] stages = AssetDatabase.FindAssets("t:StageDefinition").Select(guid => AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid))).OrderBy(stage => stage.Number).ToArray();
            SerializedProperty stageList = so.FindProperty("m_stages"); stageList.arraySize = stages.Length;
            for (int i = 0; i < stages.Length; i++) stageList.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Component wrapper = won ? (Component)root.GetComponent<GameClearPanel>() : root.GetComponent<GameOverPanel>();
            var binding = new SerializedObject(wrapper); Set(binding, "m_result", view); binding.ApplyModifiedPropertiesWithoutUndo();
        }
        private static TextMeshProUGUI Summary(Transform parent, string name, string label, string value, float x)
        {
            Image card = Box(parent, name, new Color32(32, 44, 67, 255), x, 184, 442, 80);
            Text(card.transform, "Label", label, 15, Muted, 20, 10, 402, 24);
            return Text(card.transform, "Value", value, 26, Paper, 20, 36, 402, 38);
        }
        private static BattleResultRow BuildRow(RectTransform parent)
        {
            Image bg = Box(parent, "RowTemplate", new Color32(32, 44, 67, 255), 0, 0, 1348, 80);
            bg.rectTransform.anchorMax = new Vector2(1, 1); bg.rectTransform.sizeDelta = new Vector2(0, 80);
            BattleResultRow row = bg.gameObject.AddComponent<BattleResultRow>(); var so = new SerializedObject(row);
            Set(so, "m_rank", Text(bg.transform, "Rank", "01", 18, Muted, 18, 23, 45, 34));
            Image frame = Box(bg.transform, "FaceFrame", new Color32(58, 73, 99, 255), 76, 8, 64, 64);
            Image face = Box(frame.transform, "Face", Color.white, 4, 4, 56, 56); face.preserveAspect = true; Set(so, "m_portrait", face);
            Set(so, "m_name", Text(bg.transform, "Name", "소드 마스터", 21, Paper, 158, 11, 320, 32));
            Set(so, "m_detail", Text(bg.transform, "Detail", "검 · 광역 / 전장 1", 15, Muted, 158, 45, 320, 24));
            RectTransform badge = Rect(bg.transform, "Badge", 490, 28, 24, 24); Set(so, "m_badge", badge.gameObject.AddComponent<TowerTierBadgeGraphic>());
            Set(so, "m_tier", Text(bg.transform, "Tier", "브론즈", 18, Paper, 526, 23, 120, 34));
            Image track = Box(bg.transform, "DamageTrack", new Color32(48, 61, 85, 255), 670, 34, 420, 12);
            Image fill = Box(track.transform, "DamageFill", Gold, 0, 0, 420, 12); Stretch(fill.rectTransform); fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0; Set(so, "m_damageFill", fill);
            TextMeshProUGUI damage = Text(bg.transform, "Damage", "0", 21, Paper, 1108, 22, 150, 36); damage.alignment = TextAlignmentOptions.MidlineRight; Set(so, "m_damage", damage);
            TextMeshProUGUI share = Text(bg.transform, "Share", "0%", 15, Muted, 1270, 25, 66, 30); share.alignment = TextAlignmentOptions.MidlineRight; Set(so, "m_share", share);
            so.ApplyModifiedPropertiesWithoutUndo(); return row;
        }
        private static Button Button(Transform parent, string name, string label, float x, float y, bool primary)
        {
            Image image = Box(parent, name, primary ? Gold : new Color32(47, 62, 87, 255), x, y, 302, 52); image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f); colors.pressedColor = new Color(.78f, .78f, .78f); colors.disabledColor = new Color(.45f, .45f, .45f); button.colors = colors;
            TextMeshProUGUI text = Text(image.transform, "Label", label, 21, primary ? new Color32(27, 32, 45, 255) : Paper, 0, 0, 302, 52); text.alignment = TextAlignmentOptions.Center;
            return button;
        }
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
            TextMeshProUGUI text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); text.font = s_font; text.text = value; text.fontSize = size; text.color = color; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis; text.enableAutoSizing = true; text.fontSizeMin = size - 2; text.fontSizeMax = size; return text;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Set(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
    }
}
