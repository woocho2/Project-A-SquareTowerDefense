using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public static class GameplayFeedbackVerification
    {
        public const string Folder = "ArtPreviews/GameplayFeedback_20261119";
        private const string Key = "SquareTD.GameplayFeedbackVerification";
        private static readonly List<string> Checks = new List<string>(), Errors = new List<string>(), Existing = new List<string>();
        private static double s_next, s_poll;
        private static TowerController s_attack, s_buff, s_debuff;
        private static PlayerTurnPanel s_actions;
        [Serializable] private class Scenes { public SceneSetup[] values; }
        [Serializable] private class Report { public bool verified; public string[] checks, errors, existingErrors; }
        static GameplayFeedbackVerification() { EditorApplication.update += Poll; EditorApplication.update += Tick; Application.logMessageReceived += Log; }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_poll || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            s_poll = EditorApplication.timeSinceStartup + .5;
            string request = Folder + "/request.txt"; if (!File.Exists(request)) return;
            string command = File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if (command == "verify") Run();
                if (command == "state") File.WriteAllText(Folder + "/editor-state.txt", $"Playing={EditorApplication.isPlayingOrWillChangePlaymode}, Step={SessionState.GetInt(Key, 0)}, Frame={Time.frameCount}, CompilationFailed={typeof(EditorUtility).GetProperty("scriptCompilationFailed", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null)}\n" + string.Join("\n", Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).path + " dirty=" + SceneManager.GetSceneAt(i).isDirty)));
            }
            catch (Exception e) { File.WriteAllText(Folder + "/setup-error.txt", e.ToString()); }
        }
        [MenuItem("Tools/SquareTD/적 초상화·사거리·배치 안내 검증")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("플레이를 종료하고 씬을 저장해 주세요.");
            SimulationSettings settings = SimulationSettings.Load(); if (settings != null && settings.Active && !settings.Done) throw new InvalidOperationException("밸런스 시뮬레이션이 진행 중입니다.");
            Directory.CreateDirectory(Folder); Checks.Clear(); Errors.Clear(); Existing.Clear();
            SessionState.SetString(Key + ".scenes", JsonUtility.ToJson(new Scenes { values = EditorSceneManager.GetSceneManagerSetup() }));
            SessionState.SetBool(Key + ".background", Application.runInBackground); SessionState.SetBool(Key + ".listenerCaptured", false);
            SessionState.SetFloat(Key + ".deadline", (float)EditorApplication.timeSinceStartup + 180);
            SessionState.SetInt(Key, 1); EditorSceneManager.OpenScene("Assets/1. Scenes/3. Stage1.unity", OpenSceneMode.Single);
            Application.runInBackground = true; EditorApplication.isPaused = false; EditorApplication.isPlaying = true;
        }
        private static T Field<T>(object source, string key) => (T)source.GetType().GetField(key, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
        private static void Check(bool valid, string label) { if (!valid) throw new InvalidOperationException(label); Checks.Add(label); }
        private static void Next(int step, double delay = .4) { SessionState.SetInt(Key, step); s_next = EditorApplication.timeSinceStartup + delay; }
        private static void Log(string text, string stack, LogType type)
        {
            if (SessionState.GetInt(Key, 0) <= 0 || (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)) return;
            if (text.StartsWith("Cannot instantiate objects with a parent which is persistent.") && stack.Contains("TowerFactory")) Existing.Add(text); else Errors.Add(text);
        }
        private static void Tick()
        {
            int step = SessionState.GetInt(Key, 0); if (step == 0 || EditorApplication.isCompiling) return;
            EditorApplication.QueuePlayerLoopUpdate(); if (EditorApplication.timeSinceStartup < s_next) return;
            try
            {
                if (step == 100)
                {
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                    SessionState.SetInt(Key, 0); Scenes scenes = JsonUtility.FromJson<Scenes>(SessionState.GetString(Key + ".scenes", ""));
                    if (scenes?.values != null) EditorSceneManager.RestoreSceneManagerSetup(scenes.values); return;
                }
                if (EditorApplication.timeSinceStartup > SessionState.GetFloat(Key + ".deadline", 0)) throw new TimeoutException("검증 시간 초과: " + step);
                if (!EditorApplication.isPlaying || !(TurnManager.Instance?.Current is PlayerTurnState)) return;
                Application.runInBackground = true; EditorApplication.isPaused = false;
                if (step == 1)
                {
                    SessionState.SetFloat(Key + ".listener", AudioListener.volume); SessionState.SetBool(Key + ".listenerCaptured", true); AudioListener.volume = 0;
                    Shader shader = Resources.Load<Shader>("BoardOverlay"); Check(shader != null && shader.isSupported, "보드 표시 셰이더 지원");
                    Check(!ShaderUtil.GetShaderMessages(shader).Any(message => message.severity.ToString() == "Error"), "셰이더 컴파일 오류 없음");
                    VerifyEnemies();
                    s_actions = Object.FindAnyObjectByType<PlayerTurnPanel>();
                    TowerManager.Instance.Database.TryGet(1101, out TowerDefinition attack); TowerManager.Instance.Database.TryGet(4301, out TowerDefinition buff); TowerManager.Instance.Database.TryGet(4402, out TowerDefinition debuff);
                    s_attack = TowerManager.Instance.SpawnForDebug(attack); s_buff = TowerManager.Instance.SpawnForDebug(buff); s_debuff = TowerManager.Instance.SpawnForDebug(debuff);
                    Check(s_attack != null && s_buff != null && s_debuff != null, "공격·버프·디버프 타워 실제 소환");
                    UIManager.Instance.ShowTowerPanel(s_attack); Next(2); return;
                }
                if (step == 2)
                {
                    SpriteRenderer range = Field<SpriteRenderer>(s_attack.GetComponent<TowerSelectionVisual>(), "m_range");
                    var block = new MaterialPropertyBlock(); range.GetPropertyBlock(block);
                    Check(range.gameObject.activeInHierarchy && range.sharedMaterial.shader.name == "SquareTD/BoardOverlay", "선택한 공격 타워의 새 격자 표시");
                    Check(Mathf.Abs(range.bounds.size.x - (s_attack.Stats.Range * 2 + 1) * EffectSpace.CellWorldSize()) < .01f && block.GetFloat("_Cells") == s_attack.Stats.Range * 2 + 1, "사거리 실제 정사각 판정·칸 수 일치");
                    Check(Marker(s_buff) && Marker(s_debuff), "미배치 버프·디버프 타워 붉은 경고 동시 표시");
                    SpriteRenderer marker = s_buff.transform.Find("ZonePlacementRequired").GetComponent<SpriteRenderer>();
                    Check(marker.sortingLayerName == "Tower" && marker.sortingOrder > Field<SpriteRenderer>(s_buff.GetComponent<TowerVisual>(), "m_decorationRenderer").sortingOrder, "붉은 경고가 타워 바탕 뒤에 가려지지 않음");
                    File.WriteAllText(Folder + "/render-state.txt", $"CameraMask={Camera.main.cullingMask}, Layer={marker.gameObject.layer}, Enabled={marker.enabled}, Mask={marker.maskInteraction}, Bounds={marker.bounds}, Color={marker.color}, Warning={marker.sharedMaterial.GetFloat("_Warning")}, Shader={marker.sharedMaterial.shader.name}, UV={string.Join(";", marker.sprite.uv.Select(v => v.ToString()))}, sorting={marker.sortingLayerName}/{marker.sortingOrder}, range={range.bounds}");
                    Capture("range-and-unplaced-zones.png", 1920, 1080);
                    CurrencyManager.Instance.TrySpendGold(CurrencyManager.Instance.Gold); Next(3); return;
                }
                if (step == 3)
                {
                    Click(Field<Button>(s_actions, "btn_createTower")); Check(GameNotice.CurrentMessage.Contains("골드가 부족") && GameNotice.CurrentMessage.Contains(TowerManager.Instance.BuildCost.ToString()), "비활성 타워 생성 버튼 클릭 시 필요·보유 골드 안내");
                    Click(Field<Button>(s_actions, "btn_buyXp")); Check(GameNotice.CurrentMessage.Contains("골드가 부족") && GameNotice.CurrentMessage.Contains(BalanceTables.XpPurchaseCost.ToString()), "비활성 EXP 버튼 클릭 시 골드 안내");
                    Time.timeScale = 0; GameNotice.Warn("골드가 부족합니다. 필요 250 / 보유 0"); Next(4); return;
                }
                if (step == 4)
                {
                    Check(Object.FindAnyObjectByType<GameNotice>().GetComponentInChildren<CanvasGroup>().alpha > .99f, "일시정지 중에도 안내 애니메이션 표시");
                    Capture("insufficient-gold.png", 1920, 1080); Time.timeScale = 1;
                    TurnManager.Instance.OnClickEndTurn(); Check(GameNotice.CurrentMessage.Contains("존") && GameNotice.ActionButton.gameObject.activeInHierarchy, "미배치 존 턴 종료 차단·배치 버튼 안내"); Next(5); return;
                }
                if (step == 5)
                {
                    Check(TurnManager.Instance.Current is PlayerTurnState, "미배치 존이 있으면 플레이어 턴 유지");
                    GameNotice notice = Object.FindAnyObjectByType<GameNotice>(); Check(Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).All(canvas => canvas.sortingOrder <= notice.GetComponent<Canvas>().sortingOrder), "안내가 게임 UI 위에 표시됨");
                    var corners = new Vector3[4]; notice.GetComponentInChildren<CanvasGroup>().GetComponent<RectTransform>().GetWorldCorners(corners);
                    var endCorners = new Vector3[4]; Field<Button>(s_actions, "btn_turnEnd").GetComponent<RectTransform>().GetWorldCorners(endCorners);
                    Check(corners[0].y > endCorners[2].y, "안내와 턴 종료 버튼이 겹치지 않음");
                    Capture("zone-placement-required.png", 1920, 1080); Next(6); return;
                }
                if (step == 6)
                {
                    Click(GameNotice.ActionButton); BoardInputController input = Object.FindAnyObjectByType<BoardInputController>();
                    Check(input.Current is PlaceZoneMode, "안내의 존 배치 버튼으로 배치 모드 진입");
                    TowerController owner = UIManager.Instance.SelectedTower; Check(owner != null, "미배치 타워 자동 선택");
                    Zone zone = BoardManager.Instance.Zones.GetByOwner(owner).First(z => !z.IsPlaced);
                    input.Current.OnPointerDown(new BoardPoint(Vector3.zero, zone.IsBuff ? 0 : -1, zone.IsBuff ? -1 : 0));
                    Check(GameNotice.CurrentMessage.Contains("배치할 수 없습니다"), "잘못된 종류의 칸을 누르면 배치 실패 이유 안내");
                    PlaceAll(owner, input); Next(7); return;
                }
                if (step == 7)
                {
                    TowerController owner = UIManager.Instance.SelectedTower;
                    Check(!Marker(owner) && !BoardManager.Instance.Zones.NeedsPlacement(owner), "존 배치가 끝난 타워만 붉은 경고 해제");
                    TowerController other = owner == s_buff ? s_debuff : s_buff;
                    Check(Marker(other), "다른 미배치 타워의 붉은 경고 유지");
                    BoardInputController input = Object.FindAnyObjectByType<BoardInputController>(); input.ChangeMode(new PlaceZoneMode(input, other, BoardManager.Instance.Zones)); PlaceAll(other, input);
                    Check(!BoardManager.Instance.Zones.HasUnplaced(), "버프·디버프 존의 기존 배치 규칙 유지");
                    UIManager.Instance.ShowTowerPanel(s_buff); Next(8); return;
                }
                if (step == 8)
                {
                    Check(!Field<SpriteRenderer>(s_buff.GetComponent<TowerSelectionVisual>(), "m_range").gameObject.activeSelf, "지원 타워에 공격 사거리 오표시 없음");
                    Check(TowerManager.Instance.TryMoveTo(s_attack, TowerPlace.Bench, 0), "공격 타워 대기석 이동"); UIManager.Instance.ShowTowerPanel(s_attack); Next(9); return;
                }
                if (step == 9)
                {
                    Check(!Field<SpriteRenderer>(s_attack.GetComponent<TowerSelectionVisual>(), "m_range").gameObject.activeSelf, "대기석 타워 사거리 숨김");
                    Check(TowerManager.Instance.TryMoveTo(s_attack, TowerPlace.Board, 0), "공격 타워 전장 복귀");
                    BoardManager.Instance.Zones.EnsureZones(s_buff, BoardManager.Instance.Zones.GetByOwner(s_buff).Count + 1, true); Next(10); return;
                }
                if (step == 10)
                {
                    Check(Marker(s_buff), "추가 존 생성 시 붉은 경고 재표시");
                    Check(TowerManager.Instance.TryMoveTo(s_buff, TowerPlace.Bench, 0), "미배치 타워 대기석 이동"); Next(11); return;
                }
                if (step == 11)
                {
                    Check(!Marker(s_buff), "대기석 미배치 경고 숨김");
                    Check(TowerManager.Instance.TryMoveTo(s_buff, TowerPlace.Board, 1), "지원 타워 전장 복귀"); Next(12); return;
                }
                if (step == 12)
                {
                    Check(Marker(s_buff), "지원 타워 전장 복귀 시 경고 표시");
                    UIManager.Instance.ShowTowerPanel(s_attack); GameNotice.ZoneRequired(s_buff); Next(13); return;
                }
                if (step == 13)
                {
                    Capture("feedback-16x10.png", 1728, 1080);
                    Check(Errors.Count == 0, "이번 변경의 새 런타임 오류 없음");
                    File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = true, checks = Checks.ToArray(), errors = Errors.ToArray(), existingErrors = Existing.ToArray() }, true)); Finish();
                }
            }
            catch (Exception e) { File.WriteAllText(Folder + "/verification-error.txt", e.ToString()); File.WriteAllText(Folder + "/playmode-result.json", JsonUtility.ToJson(new Report { verified = false, checks = Checks.ToArray(), errors = Errors.Concat(new[] { e.Message }).ToArray(), existingErrors = Existing.ToArray() }, true)); Finish(); }
        }
        private static bool Marker(TowerController tower) => tower.transform.Find("ZonePlacementRequired") != null && tower.transform.Find("ZonePlacementRequired").gameObject.activeSelf;
        private static void PlaceAll(TowerController tower, BoardInputController input)
        {
            ZoneRegistry zones = BoardManager.Instance.Zones;
            for (int guard = 0; guard < 50 && zones.NeedsPlacement(tower); guard++)
            {
                Zone zone = zones.GetByOwner(tower).First(z => !z.IsPlaced && zones.HasPlaceableCell(z));
                int count = zone.IsBuff ? BoardManager.Instance.Grid.SpawnCount : BoardManager.Instance.Grid.PathCount;
                int index = Enumerable.Range(0, count).First(i => zones.CanPlace(zone, i, out _));
                input.Current.OnPointerDown(new BoardPoint(Vector3.zero, zone.IsBuff ? -1 : index, zone.IsBuff ? index : -1));
            }
        }
        private static void VerifyEnemies()
        {
            EnemyManager manager = EnemyManager.Instance; BoardManager board = BoardManager.Instance;
            EnemySpawnEntry normal = Field<EnemySpawnEntry>(manager, "m_normal"), middle = Field<EnemySpawnEntry>(manager, "m_middleBoss"), final = Field<EnemySpawnEntry>(manager, "m_finalBoss");
            EnemyInfoPanel panel = Field<EnemyInfoPanel>(UIManager.Instance, "m_enemyInfoPanelUI");
            StageDefinition[] stages = AssetDatabase.FindAssets("t:StageDefinition").Select(guid => AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid))).Where(stage => stage.Number >= 1 && stage.Number <= 10).OrderBy(stage => stage.Number).ToArray();
            Check(stages.Select(stage => stage.Number).Distinct().Count() == 10, "스테이지 10종 데이터 확인");
            foreach (StageDefinition stage in stages)
            {
                int cycles = stage.RagnarokCycles != null && stage.RagnarokCycles.Length > 0 ? stage.RagnarokCycles.Length : 1;
                var spawner = new EnemySpawner(board, manager.Registry, normal, middle, final, manager.BossTimer, stage);
                for (int cycle = 1; cycle <= cycles; cycle++)
                foreach (EnemyKind kind in new[] { EnemyKind.Normal, EnemyKind.MiddleBoss, EnemyKind.FinalBoss })
                {
                    int wave = (cycle - 1) * 10 + (kind == EnemyKind.Normal ? 1 : kind == EnemyKind.MiddleBoss ? 5 : 10);
                    EnemyController enemy = kind == EnemyKind.Normal ? spawner.SpawnNormalWithHp(wave, 1000) : spawner.SpawnWave(wave).Single();
                    StageEnemyLook look = stage.GetLook(kind, cycle); Check(look?.Appearance != null && look.Appearance.FullSprite != null && look.Appearance.FaceSprite != null, $"{stage.Number}/{cycle}/{kind}: 원본 Full·Face 연결");
                    Object.FindAnyObjectByType<BoardInputController>().Current.OnPointerDown(new BoardPoint(enemy.transform.position, enemy.PathIndex, -1));
                    EnemyListItemUI item = panel.GetComponentsInChildren<EnemyListItemUI>(true).Single(row => row.gameObject.activeSelf && ReferenceEquals(row.Target, enemy));
                    Check(enemy.Appearance == look.Appearance && enemy.DisplayName == look.Name && Field<Image>(item, "m_icon").sprite == look.Appearance.FaceSprite, $"{stage.Number}/{cycle}/{kind}: 실소환·목록 Face·이름 일치");
                    Field<Button>(item, "m_button").onClick.Invoke();
                    Check(ReferenceEquals(panel.SelectedTarget, enemy) &&Field<Image>(panel, "m_imgPortrait").sprite == look.Appearance.FullSprite, $"{stage.Number}/{cycle}/{kind}: 목록 클릭 후 상세 Full 일치");
                    if (stage.Number == 1 && kind == EnemyKind.FinalBoss) Capture("asgard-enemy-detail.png", 1920, 1080);
                    if (stage.Number == 8 && kind == EnemyKind.FinalBoss) Capture("muspelheim-enemy-detail.png", 1920, 1080);
                    board.Occupancy.Remove(enemy, enemy.PathIndex); manager.Registry.Unregister(enemy); if (enemy.IsBoss) manager.BossTimer.Remove(enemy); enemy.Pool.Release(enemy);
                }
            }
        }
        private static void Click(Button button)
        {
            Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)button.transform; Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, rect.TransformPoint(rect.rect.center)), button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            GameObject target = hits.Select(hit => ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject)).FirstOrDefault(go => go != null);
            Check(target == button.gameObject, "실제 클릭 위치: " + button.name); ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
        private static void Capture(string name, int width, int height)
        {
            Camera camera = Camera.main;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>().Where(canvas => canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            Camera[] cameras = canvases.Select(canvas => canvas.worldCamera).ToArray(); float[] distances = canvases.Select(canvas => canvas.planeDistance).ToArray();
            int[] layers = canvases.Select(canvas => canvas.sortingLayerID).ToArray();
            RenderTexture previous = camera.targetTexture, active = RenderTexture.active; var target = new RenderTexture(width, height, 24); target.Create();
            try
            {
                camera.targetTexture = target;
                foreach (Canvas canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.sortingLayerName = "UI"; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply(); File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG());
                try
                {
                    if (name == "range-and-unplaced-zones.png")
                    foreach (TowerController tower in new[] { s_buff, s_debuff })
                    {
                        Bounds bounds = tower.transform.Find("ZonePlacementRequired").GetComponent<SpriteRenderer>().bounds;
                        Vector3 a = camera.WorldToScreenPoint(bounds.min), b = camera.WorldToScreenPoint(bounds.max);
                        int red = 0;
                        for (int y = Mathf.Max(0, (int)a.y); y < Mathf.Min(height, (int)b.y); y++)
                        for (int x = Mathf.Max(0, (int)a.x); x < Mathf.Min(width, (int)b.x); x++) { Color pixel = image.GetPixel(x, y); if (pixel.r > .55f && pixel.r > pixel.g * 1.8f && pixel.r > pixel.b * 1.5f) red++; }
                        Check(red > 50, "실제 렌더 이미지에서 붉은 배치 경고 확인: " + tower.name + "/" + red);
                    }
                }
                finally { Object.Destroy(image); }
                GameNotice notice = Object.FindAnyObjectByType<GameNotice>();
                if (notice != null)
                foreach (TextMeshProUGUI text in notice.GetComponentsInChildren<TextMeshProUGUI>()) { text.ForceMeshUpdate(); Check(!text.isTextOverflowing, "안내 텍스트 넘침 없음: " + name + "/" + text.name); }
            }
            finally
            {
                for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = RenderMode.ScreenSpaceOverlay; canvases[i].worldCamera = cameras[i]; canvases[i].planeDistance = distances[i]; canvases[i].sortingLayerID = layers[i]; }
                camera.targetTexture = previous; RenderTexture.active = active; target.Release(); Object.Destroy(target); Canvas.ForceUpdateCanvases();
            }
        }
        private static void Finish()
        {
            if (SessionState.GetBool(Key + ".listenerCaptured", false)) AudioListener.volume = SessionState.GetFloat(Key + ".listener", 1);
            Application.runInBackground = SessionState.GetBool(Key + ".background", false); Time.timeScale = 1; SessionState.SetInt(Key, 100); EditorApplication.isPlaying = false;
        }
    }
}
