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
    public static class InfoPanelLayoutTool
    {
        public const string Output = "ArtPreviews/InfoPanels_20261117";
        private const string PrefabPath = "Assets/2. Prefab/UICanvas.prefab";
        private static readonly Color Paper = new Color32(246, 243, 232, 255);
        private static readonly Color Ink = new Color32(37, 43, 66, 255);
        private static readonly Color Muted = new Color32(106, 108, 118, 255);
        private static readonly Color Gold = new Color32(190, 159, 85, 255);
        private static readonly Color Row = new Color32(234, 231, 218, 255);
        private static TMP_FontAsset s_font;
        private static Sprite s_surface;

        [MenuItem("Tools/SquareTD/정보 패널 레이아웃 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply panels in Edit mode.");
            Directory.CreateDirectory(Output);
            string backup = "ArtBackups/InfoPanels_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string copy = Path.Combine(backup, PrefabPath);
            Directory.CreateDirectory(Path.GetDirectoryName(copy));
            File.Copy(PrefabPath, copy);
            File.Copy(PrefabPath + ".meta", copy + ".meta");
            GameObject canvas = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                s_font = canvas.GetComponentsInChildren<TextMeshProUGUI>(true).First(text => text.font != null).font;
                TMP_FontAsset symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                if (symbols != null && !s_font.fallbackFontAssetTable.Contains(symbols))
                {
                    string fontPath = AssetDatabase.GetAssetPath(s_font);
                    string fontCopy = Path.Combine(backup, fontPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fontCopy));
                    File.Copy(fontPath, fontCopy); File.Copy(fontPath + ".meta", fontCopy + ".meta");
                    s_font.fallbackFontAssetTable.Add(symbols);
                    EditorUtility.SetDirty(s_font);
                }
                s_surface = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 1f;
                }
                SetupTower(canvas.GetComponentInChildren<TowerInfoPanel>(true));
                SetupTile(canvas.GetComponentInChildren<TileinfoPanel>(true));
                SetupEnemy(canvas.GetComponentInChildren<EnemyInfoPanel>(true));
                UIManager manager = canvas.GetComponentInChildren<UIManager>(true);
                if (manager != null)
                {
                    var so = new SerializedObject(manager);
                    so.FindProperty("m_btnChangeTowerInfoPanels").arraySize = 0;
                    so.FindProperty("m_btnChangeTileInfoPanels").arraySize = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
                File.WriteAllText(Output + "/layout.txt", "Shared UICanvas prefab updated.\nPanels: 300 x 760 at top-right, margin 20 / 124.\nName 26px; values 18px; descriptions 18px; labels 16px.\nRemoved obsolete tier-up and manual merge controls.\nBackup: " + backup);
            }
            finally { PrefabUtility.UnloadPrefabContents(canvas); }
            AssetDatabase.SaveAssets();
            Debug.Log("[InfoPanels] Shared tower, tile and enemy panels updated.");
        }

        public static void RunBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception exception) { Directory.CreateDirectory(Output); File.WriteAllText(Output + "/apply-error.txt", exception.ToString()); Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Tools/SquareTD/정보 패널 전신 비율 수정")]
        public static void FixPortraitAspect()
        {
            const string backup = "ArtBackups/InfoPanelTransitions_20261117";
            string copy = Path.Combine(backup, PrefabPath);
            Directory.CreateDirectory(Path.GetDirectoryName(copy));
            if (!File.Exists(copy)) { File.Copy(PrefabPath, copy); File.Copy(PrefabPath + ".meta", copy + ".meta"); }
            GameObject canvas = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var tower = new SerializedObject(canvas.GetComponentInChildren<TowerInfoPanel>(true));
                var enemy = new SerializedObject(canvas.GetComponentInChildren<EnemyInfoPanel>(true));
                foreach (Image image in new[]
                {
                    tower.FindProperty("img_portrait").objectReferenceValue as Image,
                    tower.FindProperty("img_portraitB").objectReferenceValue as Image,
                    enemy.FindProperty("m_imgPortrait").objectReferenceValue as Image,
                }) SetPortraitMode(image);
                foreach (EnemyListItemUI item in canvas.GetComponentsInChildren<EnemyListItemUI>(true))
                    SetPortraitMode(new SerializedObject(item).FindProperty("m_icon").objectReferenceValue as Image);
                PrefabUtility.SaveAsPrefabAsset(canvas, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(canvas); }
            Debug.Log("[InfoPanels] Portraits use Simple images with their original sprite aspect ratios.");
        }

        private static void SetPortraitMode(Image image)
        {
            if (image == null) return;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
        }

        private static RectTransform ResetPanel(Component panel, string heading)
        {
            if (panel == null) throw new InvalidOperationException("A required info panel is missing.");
            var rect = (RectTransform)panel.transform;
            foreach (Transform child in rect.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-20, -124);
            rect.sizeDelta = new Vector2(300, 760);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            Image outer = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
            outer.sprite = s_surface;
            outer.type = UnityEngine.UI.Image.Type.Sliced;
            outer.color = Gold;
            outer.raycastTarget = true;
            Image inner = Image(rect, "Paper", 2, 2, 296, 756, Paper);
            Image(inner.transform, "Header", 0, 0, 296, 38, Ink);
            Text(inner.transform, "Heading", heading, 14, 0, 130, 38, 16, Paper, FontStyles.Bold);
            Button(rect, "Btn_PanelSwitch", "타일 보기", 172, 7, 82, 26, new Color32(60, 67, 92, 255), Paper, 14);
            Button(rect, "Btn_ClosePanel", "X", 260, 7, 30, 26, new Color32(60, 67, 92, 255), Paper, 16);
            return rect;
        }

        private static void SetupTower(TowerInfoPanel panel)
        {
            RectTransform root = ResetPanel(panel, "타워 정보");
            foreach (TowerInfoCardView view in panel.GetComponents<TowerInfoCardView>()) Object.DestroyImmediate(view);
            var so = new SerializedObject(panel);
            Ref(so, "cardView", null);
            Ref(so, "txt_name", Text(root, "TowerName", "타워", 16, 49, 268, 38, 26, Ink, FontStyles.Bold));
            Ref(so, "txt_emblem", Chip(root, "Emblem", 16, 94, 82, out Image emblem));
            Ref(so, "img_emblem", emblem);
            Ref(so, "txt_color", Chip(root, "Role", 104, 94, 92, out Image role));
            Ref(so, "img_color", role);
            Ref(so, "txt_tier", Chip(root, "Tier", 202, 94, 82, out Image tier));
            Ref(so, "img_tier", tier);
            Image portraitArea = Image(root, "PortraitArea", 16, 132, 268, 170, new Color32(231, 229, 219, 255));
            portraitArea.gameObject.AddComponent<RectMask2D>();
            Image shadow = Image(portraitArea.transform, "PortraitShadow", 7, 4, 254, 166, new Color(0, 0, 0, .16f));
            Image portrait = Image(portraitArea.transform, "Portrait", 0, 0, 268, 170, Color.white);
            SetPortraitMode(shadow); SetPortraitMode(portrait);
            Ref(so, "img_portrait", portrait);
            Ref(so, "img_portraitB", shadow);
            Image level = Image(portraitArea.transform, "LevelBadge", 188, 8, 72, 28, Ink);
            Ref(so, "txt_level", Text(level.transform, "Level", "Lv.1", 0, 0, 72, 28, 16, Paper, FontStyles.Bold, TextAlignmentOptions.Center));
            Text(root, "TargetHeading", "공격 대상", 16, 311, 135, 26, 16, Muted);
            Ref(so, "btn_pickAttackCell", Button(root, "PickAttackCell", "칸 지정", 198, 308, 86, 28, Row, Ink, 15));
            Ref(so, "btn_previousTarget", Button(root, "PreviousTarget", "<", 16, 342, 34, 36, Row, Ink, 20));
            Ref(so, "txt_targetPriority", Text(root, "TargetPriority", "가까운 적", 54, 339, 192, 42, 16, Ink, FontStyles.Normal, TextAlignmentOptions.Center));
            Ref(so, "btn_nextTarget", Button(root, "NextTarget", ">", 250, 342, 34, 36, Row, Ink, 20));
            RectTransform front = Rect(root, "Front", 0, 389, 300, 318);
            RectTransform back = Rect(root, "Back", 0, 389, 300, 318);
            Image frontFlip = Button(front, "ShowSkillDetails", "능력치  /  스킬 설명 보기 >", 16, 0, 268, 32, Ink, Paper, 16).GetComponent<Image>();
            Image backFlip = Button(back, "ShowStats", "< 능력치 보기  /  스킬 설명", 16, 0, 268, 32, Ink, Paper, 16).GetComponent<Image>();
            RectTransform stats = Rect(front, "Stats", 16, 44, 268, 270);
            string[] labels = { "공격력", "사거리", "행동 카운트", "공격 횟수", "치명타 확률", "치명타 피해", "스킬 계수", "스킬 카운트", "효과 범위", "차지" };
            string[] fields = { "txt_power", "txt_range", "txt_action", "txt_attackCount", "txt_criticalRate", "txt_criticalDamage", "txt_abilityValue", "txt_duration", "txt_mastery", "txt_stack" };
            for (int i = 0; i < labels.Length; i++) Ref(so, fields[i], Stat(stats, fields[i], labels[i], i * 27, 27));
            RectTransform synergy = Rect(front, "SynergyStats", 16, 44, 268, 270);
            Ref(so, "m_regularStats", stats.gameObject);
            Ref(so, "m_synergyStats", synergy.gameObject);
            Ref(so, "txt_synergyProgress", Text(synergy, "SynergyProgress", "시너지 스택", 6, 4, 256, 262, 18, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft));
            synergy.gameObject.SetActive(false);
            ScrollRect scroll = Scroll(back, "Descriptions", 16, 44, 268, 270, out RectTransform content);
            Vertical(content, 10);
            FlowText(content, "BasicHeading", "기본 동작", 19, Muted, true);
            Ref(so, "txt_default", FlowText(content, "BasicDescription", "", 18, Ink));
            TextMeshProUGUI title = FlowText(content, "SkillTitle", "스킬", 19, Muted, true);
            Array(so, "txt_skillTitles", title);
            Ref(so, "txt_skill", FlowText(content, "SkillDescription", "", 18, Ink));
            Button sale = Button(root, "SellTower", "", 16, 714, 268, 32, Row, Ink, 16);
            Text(sale.transform, "SellLabel", "판매", 12, 0, 68, 32, 16, Ink);
            Ref(so, "txt_sellTowerValue", Text(sale.transform, "SellValue", "0G", 80, 0, 174, 32, 18, Ink, FontStyles.Bold, TextAlignmentOptions.Right));
            Ref(so, "btn_sellTower", sale);
            foreach (string field in new[] { "btn_combineColor", "btn_combineEmblem", "btn_combineExact" }) Ref(so, field, null);
            Ref(so, "m_front", front.gameObject);
            Ref(so, "m_back", back.gameObject);
            Array(so, "m_flipAreas", frontFlip, backFlip);
            back.gameObject.SetActive(false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupTile(TileinfoPanel panel)
        {
            RectTransform root = ResetPanel(panel, "타일 정보");
            var so = new SerializedObject(panel);
            Ref(so, "m_title", Text(root, "TileName", "길 0번 칸", 16, 51, 268, 40, 26, Ink, FontStyles.Bold));
            Text(root, "TileHint", "특수 타일 · 존 · 지속 효과", 16, 97, 268, 26, 16, Muted);
            RectTransform front = Rect(root, "Front", 0, 139, 300, 605);
            Text(front, "ListHeading", "적용 중인 효과", 16, 0, 268, 30, 19, Ink, FontStyles.Bold);
            Text(front, "ListHint", "항목을 눌러 자세히 확인하세요", 16, 34, 268, 24, 15, Muted);
            ScrollRect list = Scroll(front, "EffectList", 16, 72, 268, 533, out RectTransform listContent);
            Button entry = Button(root, "EffectEntryTemplate", "", 0, 0, 268, 92, Row, Ink, 18);
            Text(entry.transform, "Description", "효과 이름\n효과 설명", 12, 9, 244, 74, 18, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            entry.gameObject.SetActive(false);
            RectTransform back = Rect(root, "Back", 0, 139, 300, 605);
            Ref(so, "m_backArea", Button(back, "BackToEffects", "< 효과 목록", 16, 0, 268, 34, Ink, Paper, 16).GetComponent<Image>());
            Ref(so, "m_detailTitle", Text(back, "EffectTitle", "", 16, 52, 268, 82, 22, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft));
            Scroll(back, "EffectDescription", 16, 150, 268, 455, out RectTransform details);
            Vertical(details, 0);
            Ref(so, "m_detailText", FlowText(details, "Body", "", 18, Ink));
            Ref(so, "m_front", front.gameObject); Ref(so, "m_back", back.gameObject);
            Ref(so, "m_list", list); Ref(so, "m_listContent", listContent); Ref(so, "m_entryTemplate", entry);
            so.FindProperty("m_entryHeight").floatValue = 92;
            so.FindProperty("m_entrySpacing").floatValue = 10;
            so.FindProperty("m_buffColor").colorValue = new Color32(232, 235, 221, 255);
            so.FindProperty("m_debuffColor").colorValue = new Color32(231, 224, 235, 255);
            back.gameObject.SetActive(false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupEnemy(EnemyInfoPanel panel)
        {
            RectTransform root = ResetPanel(panel, "적 정보");
            var so = new SerializedObject(panel);
            RectTransform list = Rect(root, "View_EnemyList", 0, 49, 300, 697);
            Ref(so, "m_txtListTitle", Text(list, "Txt_EnemyListTitle", "칸 위의 적", 16, 0, 268, 58, 23, Ink, FontStyles.Bold));
            Text(list, "ListHint", "적을 눌러 능력치와 상태를 확인하세요", 16, 66, 268, 48, 16, Muted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            Ref(so, "m_listScrollRect", Scroll(list, "EnemyList", 16, 122, 268, 575, out RectTransform listContent));
            Ref(so, "m_listContent", listContent);
            Ref(so, "m_txtNoEnemies", Text(list, "Txt_NoEnemies", "이 칸에는 적이 없습니다", 16, 190, 268, 60, 18, Muted, FontStyles.Normal, TextAlignmentOptions.Center));
            Button template = Button(root, "EnemyEntryTemplate", "", 0, 0, 258, 90, Row, Ink, 18);
            Image icon = Image(template.transform, "Img_Icon", 8, 13, 56, 56, Color.white); SetPortraitMode(icon);
            TextMeshProUGUI name = Text(template.transform, "Txt_Name", "적 이름", 76, 5, 172, 46, 18, Ink, FontStyles.Bold);
            name.enableAutoSizing = true; name.fontSizeMin = 16; name.fontSizeMax = 18;
            TextMeshProUGUI hp = Text(template.transform, "Txt_HP", "체력", 76, 51, 172, 28, 16, Muted);
            var item = template.gameObject.AddComponent<EnemyListItemUI>();
            var itemSo = new SerializedObject(item);
            Ref(itemSo, "m_icon", icon); Ref(itemSo, "m_txtName", name); Ref(itemSo, "m_txtHP", hp); Ref(itemSo, "m_button", template);
            itemSo.ApplyModifiedPropertiesWithoutUndo();
            template.gameObject.SetActive(false);
            Ref(so, "m_enemyListItemPrefab", template.gameObject);
            RectTransform detail = Rect(root, "View_EnemyDetail", 0, 49, 300, 697);
            TextMeshProUGUI detailTitle = Text(detail, "Txt_EnemyDetailName", "적 이름", 16, 0, 268, 62, 24, Ink, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            detailTitle.enableAutoSizing = true; detailTitle.fontSizeMin = 20; detailTitle.fontSizeMax = 24;
            Ref(so, "m_txtDetailTitle", detailTitle);
            Ref(so, "m_txtEnemyType", Text(detail, "EnemyKind", "일반 적", 16, 66, 268, 24, 16, Muted));
            Image portraitArea = Image(detail, "PortraitArea", 16, 98, 268, 175, new Color32(231, 229, 219, 255));
            portraitArea.gameObject.AddComponent<RectMask2D>();
            Image portrait = Image(portraitArea.transform, "Portrait", 0, 0, 268, 175, Color.white); SetPortraitMode(portrait);
            Ref(so, "m_imgPortrait", portrait);
            TextMeshProUGUI hpValue = Text(detail, "Txt_EnemyHP", "0 / 0", 16, 281, 268, 31, 20, Ink, FontStyles.Bold, TextAlignmentOptions.Center);
            hpValue.enableAutoSizing = true; hpValue.fontSizeMin = 14; hpValue.fontSizeMax = 20; hpValue.textWrappingMode = TextWrappingModes.NoWrap;
            Ref(so, "m_txtHP", hpValue);
            Image hpTrack = Image(detail, "HealthBar", 16, 317, 268, 8, Row);
            Image hpFill = Image(hpTrack.transform, "Fill", 0, 0, 268, 8, new Color32(96, 139, 115, 255));
            hpFill.rectTransform.anchorMin = Vector2.zero; hpFill.rectTransform.anchorMax = Vector2.one;
            hpFill.rectTransform.offsetMin = hpFill.rectTransform.offsetMax = Vector2.zero;
            Slider slider = hpTrack.gameObject.AddComponent<Slider>(); slider.fillRect = hpFill.rectTransform; slider.interactable = false; slider.transition = Selectable.Transition.None;
            Ref(so, "m_hpSlider", slider);
            Ref(so, "m_btnFlipDetail", Button(detail, "ShowStatuses", "상태 효과 보기 >", 16, 342, 268, 32, Ink, Paper, 16));
            RectTransform front = Rect(detail, "Stats", 16, 386, 268, 236);
            Ref(so, "m_txtDefend", Stat(front, "Defense", "방어력", 0, 40));
            Ref(so, "m_txtAction", Stat(front, "Action", "행동 카운트", 46, 40));
            Ref(so, "m_txtSpeed", Stat(front, "Movement", "이동력", 92, 40));
            Ref(so, "m_txtResistance", Stat(front, "Resistance", "저항력", 138, 40));
            Ref(so, "m_txtDamageTaken", Stat(front, "DamageTaken", "받는 피해 변화", 184, 40));
            RectTransform back = Rect(detail, "Statuses", 16, 386, 268, 236);
            Text(back, "BuffHeading", "강화 효과", 0, 0, 268, 28, 18, Ink, FontStyles.Bold);
            Ref(so, "m_buffScrollRect", Scroll(back, "BuffList", 0, 32, 268, 70, out RectTransform buffs));
            Text(back, "DebuffHeading", "상태 이상", 0, 114, 268, 28, 18, Ink, FontStyles.Bold);
            Ref(so, "m_debuffScrollRect", Scroll(back, "DebuffList", 0, 146, 268, 90, out RectTransform debuffs));
            Ref(so, "m_buffContent", buffs); Ref(so, "m_debuffContent", debuffs);
            Image statusTemplate = Image(root, "StatusEntryTemplate", 0, 0, 258, 70, Row);
            TextMeshProUGUI statusText = Text(statusTemplate.transform, "Description", "", 10, 8, 238, 54, 17, Ink, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            statusTemplate.gameObject.SetActive(false);
            Ref(so, "m_buffEntryPrefab", statusTemplate.gameObject);
            Ref(so, "m_btnBackToList", Button(detail, "BackToList", "< 칸 위의 적 목록", 16, 661, 268, 34, Row, Ink, 16));
            Ref(so, "m_viewEnemyList", list.gameObject); Ref(so, "m_viewEnemyDetail", detail.gameObject);
            Ref(so, "m_detailFront", front.gameObject); Ref(so, "m_detailBack", back.gameObject);
            so.FindProperty("m_unusedValueTexts").arraySize = 0;
            so.FindProperty("m_compactValues").boolValue = true;
            back.gameObject.SetActive(false); detail.gameObject.SetActive(false);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMeshProUGUI Chip(Transform parent, string name, float x, float y, float width, out Image icon)
        {
            Image chip = Image(parent, name + "Chip", x, y, width, 28, Row);
            icon = Image(chip.transform, name + "Icon", 6, 5, 18, 18, Ink); icon.preserveAspect = true;
            return Text(chip.transform, name + "Text", "", 28, 0, width - 30, 28, 15, Ink);
        }

        private static TextMeshProUGUI Stat(Transform parent, string name, string label, float y, float height)
        {
            RectTransform row = Rect(parent, name, 0, y, 268, height);
            Text(row, "Label", label, 6, 0, 114, height, 16, Muted);
            TextMeshProUGUI value = Text(row, "Value", "-", 122, 0, 140, height, 18, Ink, FontStyles.Bold, TextAlignmentOptions.Right);
            value.enableAutoSizing = true; value.fontSizeMin = 16; value.fontSizeMax = 18;
            Image(row, "Rule", 6, height - 1, 256, 1, new Color32(218, 216, 205, 255));
            return value;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image Image(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = s_surface; image.type = UnityEngine.UI.Image.Type.Sliced; image.color = color; image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, float x, float y, float width, float height, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = s_font; text.text = value; text.fontSize = size; text.fontStyle = style; text.color = color;
            text.alignment = alignment; text.raycastTarget = false; text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static Button Button(Transform parent, string name, string caption, float x, float y, float width, float height, Color color, Color textColor, float size)
        {
            Image image = Image(parent, name, x, y, width, height, color); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors; colors.highlightedColor = new Color(.91f, .91f, .94f); colors.pressedColor = new Color(.77f, .77f, .83f); colors.disabledColor = new Color(.7f, .7f, .7f, .5f); button.colors = colors;
            Navigation navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            if (caption.Length != 0) Text(image.transform, "Caption", caption, 4, 0, width - 8, height, size, textColor, FontStyles.Bold, TextAlignmentOptions.Center);
            return button;
        }

        private static ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height, out RectTransform content)
        {
            RectTransform root = Rect(parent, name, x, y, width, height);
            Image area = root.gameObject.AddComponent<Image>(); area.color = Color.clear; area.raycastTarget = true;
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = Rect(root, "Viewport", 0, 0, width, height); viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect(viewport, "Content", 0, 0, width, height); content.anchorMax = new Vector2(1, 1); content.sizeDelta = new Vector2(0, height);
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
            return scroll;
        }

        private static void Vertical(RectTransform content, float spacing)
        {
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = spacing; layout.padding = new RectOffset(4, 4, 4, 8);
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static TextMeshProUGUI FlowText(Transform parent, string name, string value, float size, Color color, bool bold = false)
        {
            TextMeshProUGUI text = Text(parent, name, value, 0, 0, 260, 40, size, color, bold ? FontStyles.Bold : FontStyles.Normal, TextAlignmentOptions.TopLeft);
            return text;
        }

        private static void Ref(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field) ?? throw new InvalidOperationException("Missing field: " + field);
            property.objectReferenceValue = value;
        }

        private static void Array(SerializedObject so, string field, params Object[] values)
        {
            SerializedProperty property = so.FindProperty(field); property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
