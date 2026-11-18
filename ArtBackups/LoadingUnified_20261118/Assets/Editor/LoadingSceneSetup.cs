using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class LoadingSceneSetup
    {
        public const string Output = "ArtPreviews/LoadingScene_20261118";
        public const string Prefab = "Assets/2. Prefab/LoadingScreen.prefab";
        public const string First = "Assets/1. Scenes/LoadingScene1.unity";
        public const string Second = "Assets/1. Scenes/LoadingScene2.unity";
        private static TMP_FontAsset s_font;
        private static Sprite s_surface;
        private static readonly Color Ink = new Color32(17, 23, 43, 255);
        private static readonly Color Paper = new Color32(245, 243, 231, 255);
        private static readonly Color Gold = new Color32(218, 191, 119, 255);
        private static readonly Color Muted = new Color32(166, 178, 198, 255);
        private static double s_next;

        static LoadingSceneSetup()
        {
            EditorApplication.update += Poll;
            EditorApplication.delayCall += WriteState;
        }

        private static void WriteState()
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/editor-state.txt", "Playing=" + EditorApplication.isPlayingOrWillChangePlaymode + "\n" +
                string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i)).Select(scene => scene.path + " | dirty=" + scene.isDirty)));
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            s_next = EditorApplication.timeSinceStartup + .5;
            string request = Output + "/request.txt";
            if (!File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim();
            if (command != "state" && EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try
            {
                if (command == "state") WriteState();
                else if (command == "refresh") AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                else if (command == "apply") { Apply(); File.WriteAllText(Output + "/apply-result.txt", "Success"); }
                else if (command == "verify") LoadingSceneVerification.Run();
                else if (command == "capture") LoadingSceneVerification.AuditAndCapture();
            }
            catch (Exception exception) { File.WriteAllText(Output + "/setup-error.txt", exception.ToString()); Debug.LogException(exception); }
        }

        [MenuItem("Tools/SquareTD/로딩 화면 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 종료 후 적용해 주세요.");
            foreach (string path in new[] { First, Second })
            {
                Scene loaded = SceneManager.GetSceneByPath(path);
                if (loaded.IsValid() && loaded.isDirty) throw new InvalidOperationException("저장하지 않은 로딩 씬 수정이 있습니다: " + path);
                Backup(path);
            }
            if (File.Exists(Prefab)) Backup(Prefab);
            s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/NotoSansKR-Black SDF.asset");
            s_surface = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Scene previous = SceneManager.GetActiveScene();
            Scene buildScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(buildScene);
                GameObject root = Build();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(buildScene, true);
            }
            ConfigureScene(First, "2. Menu");
            ConfigureScene(Second, "3. Stage1");
            AssetDatabase.SaveAssets();
            WriteState();
            Debug.Log("[LoadingScene] 공용 로딩 화면과 두 로딩 씬 연결 완료.");
        }

        private static void Backup(string path)
        {
            foreach (string source in new[] { path, path + ".meta" })
            {
                if (!File.Exists(source)) continue;
                string destination = "ArtBackups/LoadingScene_20261118/" + source;
                if (File.Exists(destination)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination);
            }
        }

        private static void ConfigureScene(string path, string fallback)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Ink;
                camera.orthographic = true;
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                GameObject canvas = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), scene);
                var so = new SerializedObject(canvas.GetComponent<LoadingSceneController>());
                so.FindProperty("m_fallbackScene").stringValue = fallback;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject Build()
        {
            var root = new GameObject("LoadingScreen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(LoadingSceneController));
            root.layer = 5;
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            Image backdrop = Box(root.transform, "Backdrop", Ink, 0, 0, 1920, 1080);
            Stretch(backdrop.rectTransform);
            var background = new GameObject("Yggdrasil", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            background.transform.SetParent(root.transform, false);
            Stretch((RectTransform)background.transform);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/4. Asset/1. BackGround/BG_Title_Ragnarok 1.png");
            RawImage raw = background.GetComponent<RawImage>();
            raw.texture = texture; raw.color = new Color(.40f, .46f, .64f, 1); raw.raycastTarget = false;
            AspectRatioFitter fit = background.GetComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = texture != null ? (float)texture.width / texture.height : 16f / 9f;
            Image shade = Box(root.transform, "Shade", new Color(0.035f, .055f, .10f, .76f), 0, 0, 1920, 1080);
            Stretch(shade.rectTransform);
            RectTransform safe = Rect(root.transform, "SafeArea", 0, 0, 1920, 1080); Stretch(safe);
            RectTransform body = Rect(safe, "TowerSpotlight", 0, 0, 1400, 730);
            body.anchorMin = body.anchorMax = body.pivot = new Vector2(.5f, .5f);
            body.anchoredPosition = new Vector2(0, 85);
            Text(body, "Collection", "타워 도감", 20, Gold, 0, 2, 400, 40);
            Text(body, "Brand", "네모네모 디펜스 : 라그나로크", 19, Muted, 820, 2, 580, 40).alignment = TextAlignmentOptions.MidlineRight;
            Box(body, "TopRule", new Color(Gold.r, Gold.g, Gold.b, .35f), 0, 50, 1400, 1);
            Image portrait = Box(body, "Portrait", Color.white, 5, 48, 700, 690);
            portrait.sprite = AssetDatabase.LoadAssetAtPath<TowerCharacterCatalog>("Assets/Resources/TowerCharacterCatalog.asset").GetCharacter(1, 1);
            portrait.type = Image.Type.Simple; portrait.preserveAspect = true;
            RectTransform info = Rect(body, "TowerInformation", 760, 90, 640, 640);
            TextMeshProUGUI category = Text(info, "Category", "검  /  광역", 20, Gold, 0, 0, 640, 42);
            TextMeshProUGUI name = Text(info, "Name", "소드 마스터", 44, Paper, 0, 44, 640, 90);
            TextMeshProUGUI introduction = Text(info, "Introduction", "여러 적을 함께 공격하며 전장의 흐름을 바꾸는 광역 타워입니다.", 22, Muted, 0, 132, 640, 83);
            Box(info, "Rule", new Color(Gold.r, Gold.g, Gold.b, .3f), 0, 234, 640, 1);
            TextMeshProUGUI basicTitle = Text(info, "BasicTitle", "기본 공격", 20, Gold, 0, 260, 640, 42);
            TextMeshProUGUI basic = Text(info, "Basic", "", 23, Paper, 0, 304, 640, 112);
            TextMeshProUGUI skillTitle = Text(info, "SkillTitle", "보이드 슬래시 / 스킬", 20, Gold, 0, 448, 640, 42);
            TextMeshProUGUI skill = Text(info, "Skill", "", 23, Paper, 0, 492, 640, 148);
            foreach (TextMeshProUGUI text in new[] { basicTitle, skillTitle, basic, skill }) { text.enableAutoSizing = true; text.fontSizeMin = 18; text.fontSizeMax = text.fontSize; }
            RectTransform footer = Rect(safe, "ProgressArea", 0, 0, 1400, 140);
            footer.anchorMin = footer.anchorMax = footer.pivot = new Vector2(.5f, 0);
            footer.anchoredPosition = new Vector2(0, 52);
            string caption = "Loading...";
            var letters = new RectTransform[caption.Length];
            TMP_FontAsset latin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/8. TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            float letterX = 0;
            for (int i = 0; i < caption.Length; i++)
            {
                TextMeshProUGUI letter = Text(footer, "Letter_" + i, caption[i].ToString(), 30, Paper, letterX, 0, 42, 44);
                letter.font = latin; letter.fontStyle = FontStyles.Bold;
                letterX += letter.GetPreferredValues(caption[i].ToString()).x;
                letters[i] = letter.rectTransform;
            }
            TextMeshProUGUI percent = Text(footer, "Percent", "0%", 25, Gold, 1260, 0, 140, 44); percent.alignment = TextAlignmentOptions.MidlineRight;
            Image border = Box(footer, "Progress", new Color(Gold.r, Gold.g, Gold.b, .5f), 0, 62, 1400, 16);
            Image track = Box(border.transform, "Track", new Color32(24, 33, 51, 255), 2, 2, 1396, 12);
            Image fill = Box(track.transform, "Fill", Gold, 0, 0, 1396, 12);
            Stretch(fill.rectTransform);
            Slider slider = border.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform; slider.handleRect = null; slider.targetGraphic = track;
            slider.direction = Slider.Direction.LeftToRight; slider.minValue = 0; slider.maxValue = 1; slider.value = 0; slider.interactable = false;
            slider.transition = Selectable.Transition.None; slider.navigation = new Navigation { mode = Navigation.Mode.None };
            Text(footer, "Baseline", "타워 정보 · 브론즈 / 컬러 Lv.1 기준", 17, Muted, 0, 99, 740, 32);
            TextMeshProUGUI status = Text(footer, "Status", "다음 화면을 준비하고 있습니다", 17, Muted, 740, 99, 660, 32); status.alignment = TextAlignmentOptions.MidlineRight;
            LoadingSceneController controller = root.GetComponent<LoadingSceneController>();
            var so = new SerializedObject(controller);
            Set(so, "m_towerCsv", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TowerData.csv"));
            Set(so, "m_catalog", AssetDatabase.LoadAssetAtPath<TowerCharacterCatalog>("Assets/Resources/TowerCharacterCatalog.asset"));
            Set(so, "m_fadeGroup", root.GetComponent<CanvasGroup>()); Set(so, "m_safeArea", safe);
            Set(so, "m_progress", slider); Set(so, "m_percent", percent); Set(so, "m_status", status);
            Set(so, "m_name", name); Set(so, "m_category", category); Set(so, "m_introduction", introduction);
            Set(so, "m_basicTitle", basicTitle); Set(so, "m_basic", basic); Set(so, "m_skillTitle", skillTitle); Set(so, "m_skill", skill); Set(so, "m_portrait", portrait);
            SerializedProperty characters = so.FindProperty("m_letters"); characters.arraySize = letters.Length;
            for (int i = 0; i < letters.Length; i++) characters.GetArrayElementAtIndex(i).objectReferenceValue = letters[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            controller.ShowTower(1101);
            return root;
        }

        private static void Set(SerializedObject so, string key, Object value) => so.FindProperty(key).objectReferenceValue = value;
        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static Image Box(Transform parent, string name, Color color, float x, float y, float width, float height)
        {
            Image image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            if (height > 4) { image.sprite = s_surface; image.type = Image.Type.Sliced; }
            return image;
        }
        private static TextMeshProUGUI Text(Transform parent, string name, string value, int size, Color color, float x, float y, float width, float height)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = s_font; text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft; text.raycastTarget = false; text.richText = false;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.lineSpacing = 4;
            return text;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
