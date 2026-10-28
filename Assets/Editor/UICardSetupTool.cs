using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SquareTD.EditorTools
{
    // 정보 카드 세 장(타워·적·타일)을 카드형으로 정리하는 한 번만 쓰는 도구 (2026-10-28)
    //   메뉴: Tools > UI 카드 정리 (한 번만) / CLI: -executeMethod SquareTD.EditorTools.UICardSetupTool.RunBatch
    //   하는 일
    //     타워: 머리(이름·그림·컬러·문양·Mastery·공격 대상)와 합성·강화 버튼을 앞·뒤 공통으로 꺼냄
    //           앞면 = 스탯 칸(공격력·사거리·행동력·공격 횟수·치명 확률·치명 피해) + 스킬 칸(스킬 계수·Duration·스택)
    //           뒷면 = 같은 자리에 기본 공격 설명 + 스킬 설명. 뒷면에 있던 머리 사본은 지움
    //     적:   앞면 구분선을 스탯 칸 안으로 (뒤집을 때 같이 사라지게)
    //     타일: 스크립트를 붙이고 앞면(목록) · 뒷면(설명)을 만듦
    //     세 장 모두 왼쪽 세로 탭을 끔 (오른쪽 위 접힌 귀퉁이가 대신함 — UIManager가 실행 중에 만듦)
    //   실행 전에 98. _Recovery에 프리팹 백업. 이미 정리했으면 아무것도 안 함
    public static class UICardSetupTool
    {
        private const string PrefabPath = "Assets/2. Prefab/UICanvas.prefab";
        private const string BackupPath = "Assets/98. _Recovery/UICanvas (카드 정리 전 백업).prefab";
        private const string CardIconAtlas = "Assets/4. Asset/5. UI/Tower_Card_Icon_Atlas_v1.png";
        private const string ActionAtlas = "Assets/4. Asset/5. UI/UI_Action_Atlas.png";
        private const string AttackTypeAtlas = "Assets/4. Asset/5. UI/Tower_AttackType_Icon_Atlas.png";

        private static readonly List<string> s_log = new List<string>();

        [MenuItem("Tools/UI 카드 정리 (한 번만)")]
        public static void Run()
        {
            s_log.Clear();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError($"[UI 카드 정리] {PrefabPath}가 없음");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BackupPath) == null)
                AssetDatabase.CopyAsset(PrefabPath, BackupPath);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform tower = Find(root.transform, "TowerInfoPanel");
                if (tower != null && tower.Find("Front/Group_SkillStats") != null)
                {
                    Debug.Log("[UI 카드 정리] 이미 정리됨 — 아무것도 안 함");
                    return;
                }

                SetupTower(tower);
                SetupEnemy(Find(root.transform, "EnemyInfoPanel"));
                GameObject tileTemplateSource = tower != null ? tower.Find("Front/Group_BasicStats/Img_A_Stats_BG")?.gameObject : null;
                SetupTile(Find(root.transform, "TileInfoPanel"), tileTemplateSource);
                WireUIManager(root);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[UI 카드 정리] 끝\n" + string.Join("\n", s_log));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // CLI용 (Unity를 끈 채 -batchmode -executeMethod … -quit)
        public static void RunBatch() => Run();

        // ── 타워 카드 ──

        private static void SetupTower(Transform panel)
        {
            if (panel == null) { s_log.Add("TowerInfoPanel 없음"); return; }
            Transform front = panel.Find("Front"), back = panel.Find("Back");
            if (front == null || back == null) { s_log.Add("타워: Front/Back 없음"); return; }

            // 머리·버튼을 앞·뒤 공통으로 (앞면 것을 패널 바로 밑으로, 뒷면 사본은 지움)
            string[] shared = { "Group_BackGround", "Group_Tower", "Group_Color", "Group_Emblem", "Group_Target",
                                "Group_CombineColor", "Group_CombineExact", "Group_CombineEmblem", "Group_TierUpTower" };
            foreach (string name in shared)
            {
                Transform part = front.Find(name);
                if (part != null) part.SetParent(panel, false);
                Transform copy = back.Find(name);
                if (copy != null) Object.DestroyImmediate(copy.gameObject);
            }
            Transform background = panel.Find("Group_BackGround");
            if (background != null) background.SetAsFirstSibling();
            front.SetSiblingIndex(background != null ? background.GetSiblingIndex() + 1 : 0);
            back.SetSiblingIndex(front.GetSiblingIndex() + 1);

            // 컬러 · 문양 · Mastery 칩 세 개 (공통)
            Transform color = panel.Find("Group_Color"), emblem = panel.Find("Group_Emblem");
            Transform mastery = null;
            if (emblem != null)
            {
                mastery = Object.Instantiate(emblem.gameObject, panel).transform;
                mastery.name = "Group_Mastery";
                mastery.SetSiblingIndex(emblem.GetSiblingIndex() + 1);
                Rename(mastery, "Img_A_Emblem_BG", "Img_A_Mastery_BG");
                Rename(mastery, "Img_Emblem_Frame", "Img_Mastery_Frame");
                Rename(mastery, "Img_Emblem_Symbol", "Img_Mastery_Symbol");
                Rename(mastery, "Txt_C_Emblem", "Txt_C_Mastery");
                SetSprite(mastery.Find("Img_Mastery_Symbol"), ActionAtlas, "Symbol_TierUpgrade");
            }
            LayoutChip(color, -88f);
            LayoutChip(emblem, 0f);
            LayoutChip(mastery, 88f);

            // 공격 대상 줄: 오른쪽 위 귀퉁이 자리를 비움
            Transform target = panel.Find("Group_Target");
            if (target != null)
            {
                Place(target, new Vector2(-22f, 300f), new Vector2(206f, 30f));
                foreach (string child in new[] { "Img_A_Target_BG", "Img_Target_Frame", "Txt_A_Target", "Btn_Target" })
                    Place(target.Find(child), Vector2.zero, new Vector2(206f, 30f));
                Place(target.Find("Btn_Target/Img_Target"), new Vector2(88f, 0f), new Vector2(15f, 15f));
            }

            // 앞면: 스탯 칸 + 스킬 칸
            Transform basic = front.Find("Group_Stats");
            Transform frontSkill = null;
            var skillValues = new Dictionary<string, TextMeshProUGUI>();
            if (basic != null)
            {
                basic.name = "Group_BasicStats";
                Place(basic, new Vector2(0f, -95f), new Vector2(250f, 150f));
                Place(basic.Find("Img_A_Stats_BG"), Vector2.zero, new Vector2(250f, 150f));
                RemoveChildren(basic, "Devide");
                Header(basic.Find("Img_AttackType_Symbol"), "Txt_A_AttackType", "기본 공격", 57f);
                Hide(basic.Find("Img_AttackType_Symbol/Txt_A_AttackTypeValue"));

                string[] cells = { "Power", "Range", "Action", "AttackCount", "CriticalRate", "CriticalDamage" };
                for (int i = 0; i < cells.Length; i++)
                    LayoutCell(basic.Find($"Img_{cells[i]}_Symbol"), new Vector2(i % 2 == 0 ? -100f : 22f, 25f - 40f * (i / 2)), 34f, 80f);

                frontSkill = MakeBox(front, "Group_SkillStats", basic.Find("Img_A_Stats_BG"), new Vector2(0f, -225f), new Vector2(250f, 95f));
                Transform skillHeader = basic.Find("Img_Skill_Symbol");
                if (skillHeader != null)
                {
                    skillHeader.SetParent(frontSkill, false);
                    Header(skillHeader, "Txt_A_Skill", "스킬", 32f, 150f);
                    Hide(skillHeader.Find("Txt_A_SkillValue"));
                }

                Transform cellSource = basic.Find("Img_Action_Symbol");
                (string id, string label, string atlas, string sprite)[] skillCells =
                {
                    ("AbilityValue", "스킬 계수", AttackTypeAtlas, "AttackType_Buff"),
                    ("Duration", "Duration", CardIconAtlas, "CardStat_Action"),
                    ("Stack", "스택", ActionAtlas, "Symbol_UpgradePlus"),
                };
                for (int i = 0; i < skillCells.Length && cellSource != null; i++)
                {
                    var cell = skillCells[i];
                    Transform copy = Object.Instantiate(cellSource.gameObject, frontSkill).transform;
                    copy.name = $"Img_{cell.id}_Symbol";
                    Rename(copy, "Txt_A_Action", $"Txt_A_{cell.id}");
                    Rename(copy, "Txt_A_ActionValue", $"Txt_A_{cell.id}Value");
                    SetText(copy.Find($"Txt_A_{cell.id}"), cell.label);
                    SetSprite(copy, cell.atlas, cell.sprite);
                    LayoutCell(copy, new Vector2(-108f + 82f * i, -13f), 30f, 56f);
                    skillValues[cell.id] = copy.Find($"Txt_A_{cell.id}Value")?.GetComponent<TextMeshProUGUI>();
                }
            }

            // 뒷면: 같은 자리에 기본 공격 설명 + 스킬 설명
            Transform backBasic = back.Find("Group_Stats");
            Transform backSkill = null;
            TextMeshProUGUI basicDescription = null, skillDescription = null;
            if (backBasic != null)
            {
                backBasic.name = "Group_BasicDesc";
                Place(backBasic, new Vector2(0f, -95f), new Vector2(250f, 150f));
                Place(backBasic.Find("Img_A_Stats_BG"), Vector2.zero, new Vector2(250f, 150f));
                RemoveChildren(backBasic, "Devide");
                Transform attackHeader = backBasic.Find("Img_AttackType_Symbol");
                Header(attackHeader, "Txt_A_AttackType", "기본 공격", 57f);
                basicDescription = Description(attackHeader?.Find("Txt_A_AttackTypeValue"), backBasic, new Vector2(0f, -12f), new Vector2(232f, 112f));

                backSkill = MakeBox(back, "Group_SkillDesc", backBasic.Find("Img_A_Stats_BG"), new Vector2(0f, -225f), new Vector2(250f, 95f));
                Transform skillHeader = backBasic.Find("Img_Skill_Symbol");
                if (skillHeader != null)
                {
                    skillHeader.SetParent(backSkill, false);
                    Header(skillHeader, "Txt_A_Skill", "스킬", 32f, 150f);
                    skillDescription = Description(skillHeader.Find("Txt_A_SkillValue"), backSkill, new Vector2(0f, -12f), new Vector2(232f, 62f));
                }
            }
            HideTabs(panel);

            // 스크립트 칸 연결
            var so = new SerializedObject(panel.GetComponent<TowerInfoPanel>());
            SetRef(so, "m_front", front.gameObject);
            SetRef(so, "m_back", back.gameObject);
            SetRef(so, "img_portrait", panel.Find("Group_Tower/Img_Tower")?.GetComponent<Image>());
            SetRef(so, "txt_mastery", mastery?.Find("Txt_C_Mastery")?.GetComponent<TextMeshProUGUI>());
            SetRef(so, "txt_abilityValue", skillValues.GetValueOrDefault("AbilityValue"));
            SetRef(so, "txt_duration", skillValues.GetValueOrDefault("Duration"));
            SetRef(so, "txt_stack", skillValues.GetValueOrDefault("Stack"));
            SetRef(so, "txt_default", basicDescription);
            SetRef(so, "txt_skill", skillDescription);
            SetArray(so, "txt_skillTitles", new Object[]
            {
                frontSkill?.Find("Img_Skill_Symbol/Txt_A_Skill")?.GetComponent<TextMeshProUGUI>(),
                backSkill?.Find("Img_Skill_Symbol/Txt_A_Skill")?.GetComponent<TextMeshProUGUI>(),
            });
            SetArray(so, "m_flipAreas", new Object[]
            {
                basic?.Find("Img_A_Stats_BG")?.GetComponent<Graphic>(),
                frontSkill?.Find("Img_A_Stats_BG")?.GetComponent<Graphic>(),
                backBasic?.Find("Img_A_Stats_BG")?.GetComponent<Graphic>(),
                backSkill?.Find("Img_A_Stats_BG")?.GetComponent<Graphic>(),
            });
            so.ApplyModifiedPropertiesWithoutUndo();
            s_log.Add("타워 카드: 머리 공통 · 앞면 스탯/스킬 칸 · 뒷면 설명 칸 · Mastery 칩");
        }

        // 칩 하나 (80 × 35): 바탕·테두리 꽉, 아이콘 왼쪽, 글자 오른쪽
        private static void LayoutChip(Transform chip, float x)
        {
            if (chip == null) return;
            Place(chip, new Vector2(x, 0.5f), new Vector2(80f, 35f));
            foreach (Transform child in chip)
            {
                if (child.name.EndsWith("_BG") || child.name.EndsWith("_Frame")) Place(child, Vector2.zero, new Vector2(80f, 35f));
                else if (child.name.EndsWith("_Symbol")) Place(child, new Vector2(-26f, 0f), new Vector2(22f, 22f));
                else if (child.GetComponent<TextMeshProUGUI>() != null) Place(child, new Vector2(10f, 0f), new Vector2(52f, 35f));
            }
        }

        // 스탯 한 칸: 아이콘 + 오른쪽에 이름(위) · 값(아래)
        private static void LayoutCell(Transform symbol, Vector2 position, float iconSize, float textWidth)
        {
            if (symbol == null) return;
            Place(symbol, position, new Vector2(iconSize, iconSize));
            float textX = iconSize * 0.5f + 6f + textWidth * 0.5f;
            foreach (Transform child in symbol)
            {
                if (child.GetComponent<TextMeshProUGUI>() == null) continue;
                bool isValue = child.name.EndsWith("Value") && child.name != "Txt_A_AbilityValue";   // "스킬 계수" 이름 칸도 Value로 끝남
                Place(child, new Vector2(textX, isValue ? -9f : 8f), new Vector2(textWidth, 18f));
            }
        }

        // 칸 제목 줄: 작은 아이콘 + 글자 (칸 위쪽)
        private static void Header(Transform symbol, string labelName, string label, float y, float labelWidth = 120f)
        {
            if (symbol == null) return;
            Place(symbol, new Vector2(-108f, y), new Vector2(24f, 24f));
            Transform text = symbol.Find(labelName);
            Place(text, new Vector2(16f + labelWidth * 0.5f, 0f), new Vector2(labelWidth, 24f));
            if (text != null && text.TryGetComponent(out TextMeshProUGUI tmp))
            {
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.fontStyle = FontStyles.Bold;
            }
        }

        // 설명 글: 칸 안 왼쪽 위부터 줄바꿈, 넘치면 글자가 작아짐
        private static TextMeshProUGUI Description(Transform text, Transform box, Vector2 position, Vector2 size)
        {
            if (text == null) return null;
            text.SetParent(box, false);
            Place(text, position, size);
            TextMeshProUGUI tmp = text.GetComponent<TextMeshProUGUI>();
            if (tmp == null) return null;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = Mathf.Max(12f, tmp.fontSize);
            tmp.fontSizeMin = 9f;
            return tmp;
        }

        // 새 칸 상자 (바탕은 기존 칸 바탕 그림을 복사)
        private static Transform MakeBox(Transform parent, string name, Transform backgroundSource, Vector2 position, Vector2 size)
        {
            var box = new GameObject(name, typeof(RectTransform)).transform;
            box.SetParent(parent, false);
            Place(box, position, size);
            if (backgroundSource != null)
            {
                Transform background = Object.Instantiate(backgroundSource.gameObject, box).transform;
                background.name = "Img_A_Stats_BG";
                Place(background, Vector2.zero, size);
            }
            return box;
        }

        // ── 적 카드 ──

        private static void SetupEnemy(Transform panel)
        {
            if (panel == null) { s_log.Add("EnemyInfoPanel 없음"); return; }
            Transform single = panel.Find("Enemy_Single");
            Transform stats = single != null ? single.Find("Group_Stats") : null;
            if (stats != null)
            {
                foreach (Transform divider in single.Cast<Transform>().Where(t => t.name == "Devide").ToList())
                    divider.SetParent(stats, true);                 // 앞면(스탯 칸)과 같이 뒤집혀 사라지게
            }
            HideTabs(panel);
            s_log.Add("적 카드: 구분선을 스탯 칸 안으로, 왼쪽 탭 끔");
        }

        // ── 타일 카드 ──

        private static void SetupTile(Transform panel, GameObject entryBackground)
        {
            if (panel == null) { s_log.Add("TileInfoPanel 없음"); return; }
            HideTabs(panel);

            Transform front = panel.Find("Tile_Multi");
            if (front == null) { s_log.Add("타일: Tile_Multi 없음"); return; }
            front.name = "Front";

            // 칸 이름 (앞·뒤 공통 머리)
            Transform title = front.Find("Txt_A_EnemyList");
            if (title != null)
            {
                title.name = "Txt_A_TileTitle";
                title.SetParent(panel, false);
                Place(title, new Vector2(0f, 262f), new Vector2(250f, 50f));
            }

            // 목록: 카드 안쪽에 맞춤 (Viewport·Content가 어긋나 있던 것도 바로잡음)
            Transform scroll = front.Find("Scroll View");
            RectTransform content = null;
            if (scroll != null)
            {
                var scrollRect = (RectTransform)scroll;
                scrollRect.anchorMin = scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
                Place(scroll, new Vector2(0f, -40f), new Vector2(250f, 520f));
                var viewport = (RectTransform)scroll.Find("Viewport");
                if (viewport != null)
                {
                    viewport.anchorMin = Vector2.zero;
                    viewport.anchorMax = Vector2.one;
                    viewport.pivot = new Vector2(0f, 1f);
                    viewport.sizeDelta = new Vector2(-17f, 0f);
                    viewport.anchoredPosition = Vector2.zero;
                    content = (RectTransform)viewport.Find("Content");
                    if (content != null)
                    {
                        content.anchorMin = new Vector2(0f, 1f);
                        content.anchorMax = new Vector2(1f, 1f);
                        content.pivot = new Vector2(0.5f, 1f);
                        content.anchoredPosition = Vector2.zero;
                        content.sizeDelta = new Vector2(0f, 520f);
                    }
                }
            }

            // 목록 한 줄의 틀 (꺼 둠)
            Button template = null;
            TextMeshProUGUI sampleText = title != null ? title.GetComponent<TextMeshProUGUI>() : null;
            if (content != null)
            {
                var entry = new GameObject("Entry_Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                var rect = (RectTransform)entry.transform;
                rect.SetParent(content, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(-12f, 58f);
                Image image = entry.GetComponent<Image>();
                Image source = entryBackground != null ? entryBackground.GetComponent<Image>() : null;
                if (source != null)
                {
                    image.sprite = source.sprite;
                    image.type = source.type;
                }
                template = entry.GetComponent<Button>();
                template.targetGraphic = image;
                TextMeshProUGUI text = MakeText(rect, "Txt_Entry", sampleText, 14f, TextAlignmentOptions.MidlineLeft);
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(12f, 4f);
                text.rectTransform.offsetMax = new Vector2(-8f, -4f);
                text.color = new Color(0.15f, 0.12f, 0.2f);
                entry.SetActive(false);
            }

            // 뒷면: 고른 효과의 제목 + 설명
            var back = new GameObject("Back", typeof(RectTransform)).transform;
            back.SetParent(panel, false);
            Place(back, new Vector2(0f, -40f), new Vector2(250f, 520f));
            Image backArea = null;
            if (entryBackground != null)
            {
                Transform background = Object.Instantiate(entryBackground, back).transform;
                background.name = "Img_A_Stats_BG";
                Place(background, Vector2.zero, new Vector2(250f, 520f));
                backArea = background.GetComponent<Image>();
            }
            TextMeshProUGUI detailTitle = MakeText((RectTransform)back, "Txt_A_DetailTitle", sampleText, 18f, TextAlignmentOptions.Top);
            Place(detailTitle.transform, new Vector2(0f, 230f), new Vector2(230f, 40f));
            TextMeshProUGUI detailText = MakeText((RectTransform)back, "Txt_A_DetailValue", sampleText, 14f, TextAlignmentOptions.TopLeft);
            Place(detailText.transform, new Vector2(0f, -25f), new Vector2(230f, 450f));
            back.gameObject.SetActive(false);

            Transform background0 = panel.Find("Group_BackGround");
            if (background0 != null) background0.SetAsFirstSibling();

            TileinfoPanel component = panel.GetComponent<TileinfoPanel>();
            if (component == null) component = panel.gameObject.AddComponent<TileinfoPanel>();
            var so = new SerializedObject(component);
            SetRef(so, "m_title", title != null ? title.GetComponent<TextMeshProUGUI>() : null);
            SetRef(so, "m_front", front.gameObject);
            SetRef(so, "m_list", scroll != null ? scroll.GetComponent<ScrollRect>() : null);
            SetRef(so, "m_listContent", content);
            SetRef(so, "m_entryTemplate", template);
            SetRef(so, "m_back", back.gameObject);
            SetRef(so, "m_detailTitle", detailTitle);
            SetRef(so, "m_detailText", detailText);
            SetRef(so, "m_backArea", backArea);
            so.ApplyModifiedPropertiesWithoutUndo();
            s_log.Add("타일 카드: 스크립트 · 앞면 목록 · 뒷면 설명");
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, TextMeshProUGUI sample, float size, TextAlignmentOptions alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.rectTransform.SetParent(parent, false);
            if (sample != null)
            {
                text.font = sample.font;
                text.fontSharedMaterial = sample.fontSharedMaterial;
                text.color = sample.color;
            }
            text.fontSize = size;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // ── UIManager ──

        private static void WireUIManager(GameObject root)
        {
            UIManager manager = root.GetComponentInChildren<UIManager>(true);
            Transform tile = Find(root.transform, "TileInfoPanel");
            if (manager == null || tile == null) return;
            var so = new SerializedObject(manager);
            SetRef(so, "m_tileInfoPanel", tile.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            s_log.Add("UIManager: 타일 카드 연결");
        }

        // ── 도우미 ──

        private static void HideTabs(Transform panel)
        {
            foreach (string name in new[] { "Group_ChangeTilePanel", "Group_ChangeTowerPanel" })
            {
                Transform tab = panel.Find(name);
                if (tab != null) tab.gameObject.SetActive(false);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child;
            }
            s_log.Add($"{name} 없음");
            return null;
        }

        private static void Place(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect)) return;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Rename(Transform parent, string from, string to)
        {
            Transform child = parent.Find(from);
            if (child != null) child.name = to;
        }

        private static void Hide(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        private static void RemoveChildren(Transform parent, string name)
        {
            foreach (Transform child in parent.Cast<Transform>().Where(t => t.name == name).ToList())
                Object.DestroyImmediate(child.gameObject);
        }

        private static void SetText(Transform target, string text)
        {
            if (target != null && target.TryGetComponent(out TextMeshProUGUI tmp)) tmp.text = text;
        }

        private static void SetSprite(Transform target, string atlasPath, string spriteName)
        {
            if (target == null || !target.TryGetComponent(out Image image)) return;
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(atlasPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
            if (sprite != null) image.sprite = sprite;
            else s_log.Add($"그림 {spriteName} 없음 ({atlasPath})");
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { s_log.Add($"{so.targetObject.GetType().Name}.{field} 칸 없음"); return; }
            property.objectReferenceValue = value;
            if (value == null) s_log.Add($"{so.targetObject.GetType().Name}.{field} 비어 있음");
        }

        private static void SetArray(SerializedObject so, string field, Object[] values)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null) { s_log.Add($"{so.targetObject.GetType().Name}.{field} 칸 없음"); return; }
            values = values.Where(v => v != null).ToArray();
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
