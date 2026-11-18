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
    public static class TowerDescriptionSetup
    {
        private const string Prefab = "Assets/2. Prefab/UICanvas.prefab";
        private static readonly Color Paper = new Color32(246, 243, 232, 255);
        private static readonly Color Ink = new Color32(37, 43, 66, 255);
        private static readonly Color Muted = new Color32(106, 108, 118, 255);

        [MenuItem("Tools/SquareTD/타워 효과 설명·수치 계산 연결")]
        public static void Apply()
        {
            GameObject canvas = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                Configure(canvas);
                PrefabUtility.SaveAsPrefabAsset(canvas, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(canvas); }
            AssetDatabase.SaveAssets();
            Debug.Log("[TowerDescription] Concise effect descriptions and clickable values connected.");
        }

        public static void Configure(GameObject canvas)
        {
            TowerInfoPanel panel = canvas.GetComponentInChildren<TowerInfoPanel>(true);
            var panelSo = new SerializedObject(panel);
            TextMeshProUGUI basic = Ref<TextMeshProUGUI>(panelSo, "txt_default");
            TextMeshProUGUI skill = Ref<TextMeshProUGUI>(panelSo, "txt_skill");
            TMP_FontAsset font = basic.font;
            RectTransform content = (RectTransform)basic.transform.parent;
            ScrollRect descriptions = content.GetComponentInParent<ScrollRect>(true);
            TextMeshProUGUI basicTitle = content.Find("BasicHeading").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI skillTitle = content.Find("SkillTitle").GetComponent<TextMeshProUGUI>();
            SkillDescriptionView view = panel.GetComponent<SkillDescriptionView>();
            if (view == null) view = panel.gameObject.AddComponent<SkillDescriptionView>();
            foreach (string name in new[] { "Activation", "ValueHint" })
            {
                Transform previous = content.Find(name);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
            }
            Transform oldDetails = descriptions.transform.parent.Find("CalculationDetails");
            if (oldDetails != null) Object.DestroyImmediate(oldDetails.gameObject);
            foreach (TextMeshProUGUI text in new[] { basic, skill })
            {
                text.fontSize = 18; text.enableAutoSizing = false;
                text.lineSpacing = 3; text.paragraphSpacing = 6;
                text.raycastTarget = true;
                text.richText = true;
                SkillValueLink link = text.GetComponent<SkillValueLink>();
                if (link == null) link = text.gameObject.AddComponent<SkillValueLink>();
                var linkSo = new SerializedObject(link);
                Set(linkSo, "m_text", text); Set(linkSo, "m_view", view); linkSo.ApplyModifiedPropertiesWithoutUndo();
            }
            basicTitle.fontSize = skillTitle.fontSize = 19;
            TextMeshProUGUI activation = Text(content, "Activation", "", font, 15, Muted);
            TextMeshProUGUI hint = Text(content, "ValueHint", "밑줄 친 숫자를 누르면 계산을 볼 수 있습니다.", font, 14, Muted);
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12; layout.padding = new RectOffset(4, 4, 4, 12);

            RectTransform details = Rect(descriptions.transform.parent, "CalculationDetails");
            RectTransform source = (RectTransform)descriptions.transform;
            details.anchorMin = source.anchorMin; details.anchorMax = source.anchorMax; details.pivot = source.pivot;
            details.anchoredPosition = source.anchoredPosition; details.sizeDelta = source.sizeDelta;
            Image background = details.gameObject.AddComponent<Image>(); background.color = Paper; background.raycastTarget = true;
            Button close = Button(details, "BackToDescription", "< 효과 설명", font);
            At((RectTransform)close.transform, 0, 0, 268, 34);
            TextMeshProUGUI valueTitle = Text(details, "ValueTitle", "피해량 계산", font, 20, Ink);
            At(valueTitle.rectTransform, 4, 46, 260, 56);
            valueTitle.fontStyle = FontStyles.Bold;
            RectTransform scrollRoot = Rect(details, "CalculationScroll"); At(scrollRoot, 0, 112, 268, source.sizeDelta.y - 112);
            Image raycast = scrollRoot.gameObject.AddComponent<Image>(); raycast.color = Color.clear; raycast.raycastTarget = true;
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
            RectTransform viewport = Rect(scrollRoot, "Viewport"); Stretch(viewport); viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform calculationContent = Rect(viewport, "Content");
            calculationContent.anchorMin = new Vector2(0, 1); calculationContent.anchorMax = Vector2.one;
            calculationContent.pivot = new Vector2(.5f, 1); calculationContent.sizeDelta = Vector2.zero;
            VerticalLayoutGroup valueLayout = calculationContent.gameObject.AddComponent<VerticalLayoutGroup>();
            valueLayout.padding = new RectOffset(4, 4, 4, 12); valueLayout.childControlWidth = valueLayout.childControlHeight = true;
            valueLayout.childForceExpandWidth = true; valueLayout.childForceExpandHeight = false;
            calculationContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI valueText = Text(calculationContent, "ValueExplanation", "", font, 17, Ink); valueText.lineSpacing = 3; valueText.paragraphSpacing = 6;
            scroll.viewport = viewport; scroll.content = calculationContent;
            var viewSo = new SerializedObject(view);
            Set(viewSo, "m_basicTitle", basicTitle); Set(viewSo, "m_basicText", basic); Set(viewSo, "m_skillTitle", skillTitle); Set(viewSo, "m_skillText", skill);
            Set(viewSo, "m_activation", activation); Set(viewSo, "m_hint", hint); Set(viewSo, "m_details", details.gameObject);
            Set(viewSo, "m_valueTitle", valueTitle); Set(viewSo, "m_valueText", valueText); Set(viewSo, "m_valueScroll", scroll); Set(viewSo, "m_close", close);
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            Set(panelSo, "descriptionView", view); panelSo.ApplyModifiedPropertiesWithoutUndo();
            details.gameObject.SetActive(false);
        }

        private static T Ref<T>(SerializedObject so, string field) where T : Object => so.FindProperty(field).objectReferenceValue as T;
        private static void Set(SerializedObject so, string field, Object value) => so.FindProperty(field).objectReferenceValue = value;
        private static RectTransform Rect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            At(rect, 0, 0, 260, 40); return rect;
        }
        private static void At(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static TextMeshProUGUI Text(Transform parent, string name, string value, TMP_FontAsset font, float size, Color color)
        {
            TextMeshProUGUI text = Rect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = size; text.color = color; text.text = value; text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false; text.enableAutoSizing = false; text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, TMP_FontAsset font)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>(); image.color = Ink; image.raycastTarget = true;
            Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            TextMeshProUGUI caption = Text(rect, "Caption", label, font, 16, Paper); Stretch(caption.rectTransform); caption.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
