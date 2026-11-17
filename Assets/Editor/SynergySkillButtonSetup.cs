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
    public static class SynergySkillButtonSetup
    {
        public const string ViewPath = "Assets/2. Prefab/SynergySkillButton.prefab";
        private const string TowerPath = "Assets/2. Prefab/1. Tower.prefab";

        [MenuItem("Tools/SquareTD/시너지 스킬 버튼 연결")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Set up the prefab in Edit mode.");
            SynergySkillButtonView view = AssetDatabase.LoadAssetAtPath<SynergySkillButtonView>(ViewPath);
            if (view == null) view = CreateView();
            GameObject viewContents = PrefabUtility.LoadPrefabContents(ViewPath);
            try
            {
                Image fill = viewContents.GetComponentsInChildren<Image>(true).Single(image => image.name == "Charge Fill");
                fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                if (fill.sprite == null) throw new InvalidOperationException("The standard UI sprite is missing.");
                viewContents.GetComponentsInChildren<Image>(true).Single(image => image.name == "Gold Frame").raycastTarget = true;
                PrefabUtility.SaveAsPrefabAsset(viewContents, ViewPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(viewContents); }
            view = AssetDatabase.LoadAssetAtPath<SynergySkillButtonView>(ViewPath);
            GameObject tower = PrefabUtility.LoadPrefabContents(TowerPath);
            try
            {
                SynergySkillButton component = tower.GetComponent<SynergySkillButton>();
                if (component == null) component = tower.AddComponent<SynergySkillButton>();
                SetReference(component, "m_viewPrefab", view);
                PrefabUtility.SaveAsPrefabAsset(tower, TowerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(tower); }
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == "Assets/1. Scenes/3. Stage1.unity")
            {
                Tilemap[] maps = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>(true)).ToArray();
                Tilemap slots = maps.SingleOrDefault(map => map.name == "Synergy");
                Tilemap surface = maps.SingleOrDefault(map => map.name == "SynergyBoard");
                if (slots != null && surface != null)
                {
                    string copy = "ArtBackups/SynergySkillButtons_20261117/" + scene.path;
                    if (!File.Exists(copy))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(copy));
                        File.Copy(scene.path, copy);
                        File.Copy(scene.path + ".meta", copy + ".meta");
                    }
                    Undo.RegisterFullObjectHierarchyUndo(surface.gameObject, "Make room for synergy skill buttons");
                    Rect rect = SupportBoardLayoutTool.SlotBounds(slots, new Vector2(0.75f, 0.75f));
                    rect.yMin -= 0.35f;
                    SupportBoardLayoutTool.Fit(surface, slots, rect);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            Directory.CreateDirectory("Library/SynergySkillButtons");
            File.WriteAllText("Library/SynergySkillButtons/setup-ok.txt", "View prefab created; shared tower prefab linked; synergy shelf fits the buttons.");
            Debug.Log("[SynergySkillButtonSetup] Synergy skill buttons linked to the shared tower prefab.");
        }

        private static SynergySkillButtonView CreateView()
        {
            var root = new GameObject("Synergy Skill Button", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SynergySkillButtonView));
            try
            {
                root.layer = 5;
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(128, 40);
                rect.localPosition = new Vector3(0, -0.72f, -0.02f);
                rect.localScale = Vector3.one * 0.01f;
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = 20;
                canvas.referencePixelsPerUnit = 100;
                Image hit = Image(rect, "Skill", Color.clear);
                Button button = hit.gameObject.AddComponent<Button>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                Image frame = Image(hit.rectTransform, "Gold Frame", new Color(0.85f, 0.68f, 0.22f, 1f));
                frame.raycastTarget = true;
                frame.rectTransform.anchorMin = new Vector2(0, 0.075f);
                frame.rectTransform.anchorMax = new Vector2(1, 0.925f);
                frame.rectTransform.offsetMin = Vector2.zero;
                frame.rectTransform.offsetMax = Vector2.zero;
                Image face = Image(frame.rectTransform, "Background", new Color(0.07f, 0.1f, 0.24f, 1));
                face.rectTransform.offsetMin = new Vector2(2, 2);
                face.rectTransform.offsetMax = new Vector2(-2, -2);
                Image fill = Image(face.rectTransform, "Charge Fill", new Color(0.24f, 0.39f, 0.68f, 1f));
                fill.type = UnityEngine.UI.Image.Type.Filled;
                fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                fill.fillAmount = 0;
                Image glow = Image(face.rectTransform, "Ready Glow", new Color(1f, 0.89f, 0.44f, 0));
                TextMeshProUGUI label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.gameObject.layer = 5;
                label.transform.SetParent(face.transform, false);
                Stretch(label.rectTransform);
                label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/NotoSansKR-Black SDF");
                if (label.font == null) throw new InvalidOperationException("The project UI font is missing.");
                label.fontSize = 22;
                label.enableAutoSizing = true;
                label.fontSizeMin = 15;
                label.fontSizeMax = 22;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.text = "0%";
                button.targetGraphic = frame;
                ColorBlock colors = button.colors;
                colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
                button.colors = colors;
                hit.raycastTarget = true;
                SynergySkillButtonView view = root.GetComponent<SynergySkillButtonView>();
                SetReference(view, "m_button", button);
                SetReference(view, "m_fill", fill);
                SetReference(view, "m_readyGlow", glow);
                SetReference(view, "m_label", label);
                return PrefabUtility.SaveAsPrefabAsset(root, ViewPath).GetComponent<SynergySkillButtonView>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static Image Image(RectTransform parent, string name, Color color)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.gameObject.layer = 5;
            image.transform.SetParent(parent, false);
            Stretch(image.rectTransform);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
