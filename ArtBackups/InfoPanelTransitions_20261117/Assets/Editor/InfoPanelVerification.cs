using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SquareTD.EditorTools
{
    [InitializeOnLoad]
    public static class InfoPanelVerification
    {
        private const string Key = "SquareTD.InfoPanelVerification";
        private const string Folder = InfoPanelLayoutTool.Output;
        private static double s_next;
        private static TowerInfoPanel s_towerPanel;
        private static TileinfoPanel s_tilePanel;
        private static EnemyInfoPanel s_enemyPanel;
        private static TowerController s_tower, s_support;
        private static EnemyController s_enemy;
        private static EnemyController s_boss;
        private static readonly List<string> s_checks = new List<string>();
        private static readonly List<string> s_errors = new List<string>();
        private static readonly List<string> s_existingErrors = new List<string>();
        private static readonly List<string> s_overflows = new List<string>();
        private static TargetPriority s_priority;

        static InfoPanelVerification() { EditorApplication.update += Tick; Application.logMessageReceived += Log; }

        [MenuItem("Tools/SquareTD/정보 패널 플레이 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start in Edit mode.");
            SimulationSettings settings = SimulationSettings.Load();
            if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("A balance simulation is scheduled. Finish it before running this UI probe.");
            Directory.CreateDirectory(Folder);
            foreach (string file in new[] { "verification-error.txt", "raycast-debug.txt" }) if (File.Exists(Folder + "/" + file)) File.Delete(Folder + "/" + file);
            AuditSceneBindings();
            SessionState.SetInt(Key, 1);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180f);
            EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        public static void RunBatch() { SessionState.SetBool(Key + ".batch", true); try { Run(); } catch (Exception e) { Fail(e); } }
        public static void ApplyAndRunBatch() { SessionState.SetBool(Key + ".batch", true); try { InfoPanelLayoutTool.Apply(); Run(); } catch (Exception e) { Fail(e); } }

        private static void AuditSceneBindings()
        {
            var lines = new List<string>();
            foreach (string path in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/1. Scenes" }).Select(AssetDatabase.GUIDToAssetPath).Where(path => Path.GetFileName(path).Contains("Stage")))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                TowerInfoPanel tower = Find<TowerInfoPanel>(scene);
                TileinfoPanel tile = Find<TileinfoPanel>(scene);
                EnemyInfoPanel enemy = Find<EnemyInfoPanel>(scene);
                if (tower == null && tile == null && enemy == null) { lines.Add(path + ": no shared info panels"); continue; }
                if (tower == null || tile == null || enemy == null) throw new InvalidOperationException("Incomplete panels in " + path);
                foreach (Component panel in new Component[] { tower, tile, enemy })
                {
                    if (panel.transform.Find("Btn_PanelSwitch") == null) throw new InvalidOperationException("Old panel layout in " + path);
                    if (panel.GetComponentsInChildren<Transform>(true).Any(child => child.name.Contains("TierUpTower"))) throw new InvalidOperationException("Tier-up UI remains in " + path);
                }
                if (Field<Object>(tower, "txt_name") == null || Field<Object>(tower, "btn_sellTower") == null || Field<Object>(tile, "m_entryTemplate") == null || Field<Object>(enemy, "m_txtHP") == null)
                    throw new InvalidOperationException("Broken panel references in " + path);
                lines.Add(path + ": shared layout and serialized references OK");
            }
            File.WriteAllLines(Folder + "/scene-bindings.txt", lines);
        }

        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
        private static T Field<T>(Object owner, string field) where T : Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
        private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); s_checks.Add(message); }
        private static void Log(string message, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (message == "Cannot instantiate objects with a parent which is persistent. New object will be created without a parent." && stack.Contains("TowerFactory")) s_existingErrors.Add(message);
            else s_errors.Add(message);
        }
        private static void Next(int phase, double delay = .75) { SessionState.SetInt(Key, phase); s_next = EditorApplication.timeSinceStartup + delay; }

        private static void Tick()
        {
            int phase = SessionState.GetInt(Key, 0);
            if (phase == 0 || EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (phase == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0);
                    if (SessionState.GetBool(Key + ".batch", false)) EditorApplication.Exit(0);
                    return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("Info panel probe timed out at phase " + phase);
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                if (SimulationRunner.IsOn) throw new InvalidOperationException("Balance simulation detected; UI probe aborted without changing settings.");
                switch (phase)
                {
                    case 1:
                        if (TowerManager.Instance?.Database == null || EnemyManager.Instance == null || UIManager.Instance == null || TurnManager.Instance?.Current == null) return;
                        s_towerPanel = Find<TowerInfoPanel>(SceneManager.GetActiveScene());
                        s_tilePanel = Find<TileinfoPanel>(SceneManager.GetActiveScene());
                        s_enemyPanel = Find<EnemyInfoPanel>(SceneManager.GetActiveScene());
                        if (s_towerPanel == null || s_tilePanel == null || s_enemyPanel == null) throw new InvalidOperationException("Panels missing in Play mode.");
                        Next(2, 2); break;
                    case 2:
                        TowerManager manager = TowerManager.Instance;
                        Assert(manager.Database.TryGet(TowerId.Make(1, TowerColor.Red, TowerEmblem.Sword), out TowerDefinition sword), "Tower definition exists");
                        s_tower = manager.SpawnForDebug(sword);
                        manager.Database.TryGet(TowerId.Make(1, TowerColor.Yellow, TowerEmblem.Sword), out TowerDefinition support);
                        s_support = manager.SpawnForDebug(support);
                        EnemyManager.Instance.SpawnAdditionalNormals(1, 2);
                        s_enemy = EnemyManager.Instance.Registry.Alive.First();
                        UIManager.Instance.ShowTowerPanel(s_tower);
                        Next(3, 2); break;
                    case 3:
                        Time.timeScale = 0;
                        Assert(Field<TextMeshProUGUI>(s_towerPanel, "txt_name").text == EmblemNames.GetConceptName(s_tower.Definition.Id), "Tower name follows selection");
                        Assert(Field<Image>(s_towerPanel, "img_portrait").sprite != null, "Tower Full portrait is assigned");
                        Assert(Field<Image>(s_towerPanel, "img_portraitB").sprite == Field<Image>(s_towerPanel, "img_portrait").sprite, "Portrait shadow follows Full sprite");
                        Capture("tower-stats.png");
                        s_priority = s_tower.State.TargetPriority;
                        Click(Field<Button>(s_towerPanel, "btn_nextTarget"));
                        Assert(s_tower.State.TargetPriority != s_priority, "Target priority button changes the target mode");
                        Click(s_towerPanel.transform.Find("Front/ShowSkillDetails").GetComponent<Button>());
                        Next(4); break;
                    case 4:
                        Assert(Field<GameObject>(s_towerPanel, "m_back").activeSelf, "Tower skill page opens");
                        ScrollRect desc = s_towerPanel.transform.Find("Back/Descriptions").GetComponent<ScrollRect>();
                        Canvas.ForceUpdateCanvases();
                        TextMeshProUGUI description = Field<TextMeshProUGUI>(s_towerPanel, "txt_skill");
                        string original = description.text;
                        description.text += string.Concat(Enumerable.Repeat("\n스크롤 길이 검증용 문장입니다.", 12));
                        LayoutRebuilder.ForceRebuildLayoutImmediate(desc.content);
                        Canvas.ForceUpdateCanvases();
                        Assert(desc.content.rect.height > desc.viewport.rect.height, "Long tower descriptions have scrollable content");
                        desc.verticalNormalizedPosition = 0;
                        Assert(desc.content.anchoredPosition.y > 0, "Tower description scroll reaches the lower text");
                        description.text = original;
                        LayoutRebuilder.ForceRebuildLayoutImmediate(desc.content);
                        desc.verticalNormalizedPosition = 1;
                        Capture("tower-skills.png");
                        Click(s_towerPanel.transform.Find("Back/ShowStats").GetComponent<Button>());
                        Next(5); break;
                    case 5:
                        Assert(Field<GameObject>(s_towerPanel, "m_front").activeSelf, "Tower stat page returns");
                        UIManager.Instance.ShowTowerPanel(s_support);
                        Next(6); break;
                    case 6:
                        Assert(Field<TextMeshProUGUI>(s_towerPanel, "txt_name").text == EmblemNames.GetConceptName(s_support.Definition.Id), "Support tower name updates");
                        Assert(!Field<Button>(s_towerPanel, "btn_nextTarget").interactable, "Support tower target controls are disabled");
                        Capture("tower-support.png");
                        UIManager.Instance.ShowTowerPanel(TowerManager.Instance.Synergy.Entries.First().Tower);
                        Next(7); break;
                    case 7:
                        Assert(Field<GameObject>(s_towerPanel, "m_synergyStats").activeSelf && !Field<GameObject>(s_towerPanel, "m_regularStats").activeSelf, "Synergy panel shows stack and recipe instead of ordinary stats");
                        Assert(!Field<TextMeshProUGUI>(s_towerPanel, "txt_skill").text.Contains("미정"), "Synergy description matches an implemented skill");
                        Capture("tower-synergy.png");
                        int path = Enumerable.Range(0, BoardManager.Instance.Grid.PathCount).First(index => BoardManager.Instance.Tiles.GetPathTile(index) != PathTileType.None);
                        UIManager.Instance.ShowTileInfoPanel(path, -1);
                        Next(8); break;
                    case 8:
                        Assert(s_tilePanel.gameObject.activeSelf, "Tile panel opens");
                        Capture("tile-list.png");
                        Button effect = Field<RectTransform>(s_tilePanel, "m_listContent").GetComponentsInChildren<Button>().First();
                        Click(effect); Next(9); break;
                    case 9:
                        Assert(Field<GameObject>(s_tilePanel, "m_back").activeSelf, "Tile effect detail opens");
                        Assert(Field<TextMeshProUGUI>(s_tilePanel, "m_detailText").text.Length > 0, "Tile effect description is populated");
                        Capture("tile-detail.png");
                        Click(Field<Graphic>(s_tilePanel, "m_backArea").GetComponent<Button>()); Next(10); break;
                    case 10:
                        Assert(Field<GameObject>(s_tilePanel, "m_front").activeSelf, "Tile effect list returns");
                        UIManager.Instance.ShowTileInfoPanel(-1, s_tower.SlotIndex);
                        Next(11); break;
                    case 11:
                        Click(s_tilePanel.transform.Find("Btn_PanelSwitch").GetComponent<Button>()); Next(12); break;
                    case 12:
                        Assert(s_towerPanel.gameObject.activeSelf, "Tile-to-tower navigation works");
                        UIManager.Instance.ShowEnemyInfoPanel(s_enemy.PathIndex); Next(13); break;
                    case 13:
                        Assert(s_enemyPanel.gameObject.activeSelf, "Enemy list opens");
                        EnemyListItemUI enemyItem = Field<RectTransform>(s_enemyPanel, "m_listContent").GetComponentsInChildren<EnemyListItemUI>().First();
                        Assert(Field<Image>(enemyItem, "m_icon").sprite == enemyItem.Target.Body.Definition.Appearance.FaceSprite, "Enemy list uses Face sprites");
                        Capture("enemy-list.png");
                        Click(enemyItem.GetComponent<Button>()); Next(14); break;
                    case 14:
                        Assert(s_enemyPanel.SelectedTarget != null, "Enemy list button opens selected enemy");
                        Assert(Field<Image>(s_enemyPanel, "m_imgPortrait").sprite == s_enemyPanel.SelectedTarget.Body.Definition.Appearance.FullSprite, "Enemy detail uses Full sprite");
                        Capture("enemy-stats.png");
                        Click(Field<Button>(s_enemyPanel, "m_btnFlipDetail")); Next(15); break;
                    case 15:
                        Assert(Field<GameObject>(s_enemyPanel, "m_detailBack").activeSelf, "Enemy effects page opens");
                        Capture("enemy-effects.png");
                        Click(Field<Button>(s_enemyPanel, "m_btnBackToList")); Next(16); break;
                    case 16:
                        Assert(s_enemyPanel.SelectedTarget == null && Field<GameObject>(s_enemyPanel, "m_viewEnemyList").activeSelf, "Enemy detail returns to the list");
                        s_boss = EnemyManager.Instance.SpawnWave(10).First(enemy => enemy.Definition.Kind == EnemyKind.FinalBoss);
                        UIManager.Instance.ShowEnemyInfoPanel(s_boss.PathIndex);
                        Next(17); break;
                    case 17:
                        EnemyListItemUI bossItem = Field<RectTransform>(s_enemyPanel, "m_listContent").GetComponentsInChildren<EnemyListItemUI>().First(item => ReferenceEquals(item.Target, s_boss));
                        Click(bossItem.GetComponent<Button>());
                        Next(18); break;
                    case 18:
                        Assert(Field<TextMeshProUGUI>(s_enemyPanel, "m_txtEnemyType").text.Contains("최종 보스") && Field<TextMeshProUGUI>(s_enemyPanel, "m_txtEnemyType").text.Contains("20턴"), "Boss type and remaining defeat turns are displayed separately");
                        Capture("enemy-boss.png");
                        Click(s_enemyPanel.transform.Find("Btn_ClosePanel").GetComponent<Button>());
                        Assert(!s_enemyPanel.gameObject.activeSelf, "Close button hides the panel");
                        UIManager.Instance.ShowTowerPanel(s_tower);
                        Next(19); break;
                    case 19:
                        int gold = CurrencyManager.Instance.Gold;
                        Click(Field<Button>(s_towerPanel, "btn_sellTower"));
                        Assert(CurrencyManager.Instance.Gold == gold + TowerManager.GetSellGold(s_tower.Definition.Tier), "Sale button pays current gold value");
                        Assert(!s_towerPanel.gameObject.activeSelf, "Sale closes the removed tower's panel");
                        Assert(s_errors.Count == 0, "No new console errors during the info panel probe (existing TowerFactory parent error reported separately)");
                        File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Result { checks = s_checks.ToArray(), errors = s_errors.ToArray(), existingErrors = s_existingErrors.ToArray(), textOverflowWarnings = s_overflows.Distinct().ToArray(), playModeVerified = true }, true));
                        Time.timeScale = 1;
                        Next(100, .2);
                        EditorApplication.isPlaying = false;
                        break;
                }
            }
            catch (Exception exception) { Fail(exception); }
        }

        private static void Click(Button button)
        {
            Assert(button != null && button.IsActive() && button.interactable, "Button enabled: " + (button != null ? button.name : "missing"));
            foreach (CanvasScaler scaler in Object.FindObjectsByType<CanvasScaler>()) scaler.SendMessage("Update", SendMessageOptions.DontRequireReceiver);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            if (!hits.Any(hit => hit.gameObject == button.gameObject || hit.gameObject.transform.IsChildOf(button.transform)))
                File.WriteAllText(Folder + "/raycast-debug.txt", $"Button {button.name} at {data.position}; screen {Screen.width} x {Screen.height}; canvas {button.GetComponentInParent<Canvas>().renderMode}; canvas rect {((RectTransform)button.GetComponentInParent<Canvas>().transform).rect}; hits: " + string.Join(", ", hits.Select(hit => hit.gameObject.name)));
            Assert(hits.Any(hit => hit.gameObject == button.gameObject || hit.gameObject.transform.IsChildOf(button.transform)), "UI raycast reaches: " + button.name);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }

        private static void Capture(string file)
        {
            Camera camera = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            RenderTexture oldTarget = camera.targetTexture;
            float aspect = camera.aspect;
            var target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            RenderTexture oldActive = RenderTexture.active;
            Texture2D pixels = null;
            try
            {
                camera.targetTexture = target; camera.aspect = 16f / 9;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
                Canvas.ForceUpdateCanvases();
                foreach (TextMeshProUGUI text in Object.FindObjectsByType<TextMeshProUGUI>())
                {
                    if (!text.transform.IsChildOf(s_towerPanel.transform) && !text.transform.IsChildOf(s_enemyPanel.transform) && !text.transform.IsChildOf(s_tilePanel.transform)) continue;
                    text.ForceMeshUpdate();
                    if (text.isTextOverflowing) s_overflows.Add(file + ": " + text.name + " = " + text.text);
                }
                camera.Render(); RenderTexture.active = target;
                pixels = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
                File.WriteAllBytes(Folder + "/" + file, pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive; camera.targetTexture = oldTarget; camera.aspect = aspect;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; }
                if (pixels != null) Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
                Canvas.ForceUpdateCanvases();
            }
        }

        private static void Fail(Exception error)
        {
            Directory.CreateDirectory(Folder); File.WriteAllText(Folder + "/verification-error.txt", error.ToString());
            File.WriteAllLines(Folder + "/collected-errors.txt", s_errors.Concat(s_existingErrors));
            SessionState.SetInt(Key, 0); Time.timeScale = 1;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            Debug.LogException(error);
            if (SessionState.GetBool(Key + ".batch", false)) EditorApplication.Exit(1);
        }

        [Serializable] private class Result { public bool playModeVerified; public string[] checks, errors, existingErrors, textOverflowWarnings; }
    }
}
