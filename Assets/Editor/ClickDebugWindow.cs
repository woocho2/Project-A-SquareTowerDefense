using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace SquareTD
{
    public class ClickDebugWindow : EditorWindow
    {
        private readonly List<GameObject> m_uiHits = new List<GameObject>();

        private bool m_hasClick;
        private bool m_wasPressed;
        private int m_pathIndex = -1;
        private int m_spawnIndex = -1;
        private Vector2 m_scroll;

        [MenuItem("Tools/Click Debug")]
        private static void Open() => GetWindow<ClickDebugWindow>("Click Debug");

        private void OnEnable() => InputSystem.onAfterUpdate += PollClick;
        private void OnDisable() => InputSystem.onAfterUpdate -= PollClick;

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && m_hasClick) Repaint();
        }

        private void PollClick()
        {
            if (InputState.currentUpdateType == InputUpdateType.Editor) return;

            if (!Application.isPlaying || Mouse.current == null)
            {
                m_wasPressed = false;
                return;
            }

            bool isPressed = Mouse.current.leftButton.isPressed;
            bool pressedNow = isPressed && !m_wasPressed;
            m_wasPressed = isPressed;
            if (!pressedNow) return;

            Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (camera == null || BoardManager.Instance == null) return;

            RecordClick(camera, Mouse.current.position.ReadValue());
            Repaint();
        }

        private void RecordClick(Camera camera, Vector2 screenPosition)
        {
            m_hasClick = true;
            m_uiHits.Clear();
            CollectUIHits(screenPosition);

            Vector3 world = camera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            BoardManager.Instance.TryGetPoint(world, out BoardPoint point);
            m_pathIndex = point.PathIndex;
            m_spawnIndex = point.SpawnIndex;
        }

        private void CollectUIHits(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, results);
            foreach (RaycastResult result in results)
            {
                if (result.gameObject != null) m_uiHits.Add(result.gameObject);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서 게임 화면을 클릭하면 표시됩니다.", MessageType.Info);
                return;
            }
            if (!m_hasClick || BoardManager.Instance == null)
            {
                EditorGUILayout.HelpBox("아직 클릭한 대상이 없습니다.", MessageType.Info);
                return;
            }

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

            if (m_uiHits.Count > 0)
            {
                Header("UI (보드 입력 차단됨)");
                foreach (GameObject hit in m_uiHits)
                {
                    if (hit != null && GUILayout.Button(hit.name, EditorStyles.linkLabel)) EditorGUIUtility.PingObject(hit);
                }
            }

            if (m_spawnIndex >= 0) DrawSpawnCell(m_spawnIndex);
            else if (m_pathIndex >= 0) DrawPathCell(m_pathIndex);
            else if (m_uiHits.Count == 0) EditorGUILayout.LabelField("판 밖");

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpawnCell(int spawnIndex)
        {
            BoardManager board = BoardManager.Instance;
            Header($"스폰 칸 {spawnIndex}");
            Row("특수 타일", board.Tiles.GetSpawnTile(spawnIndex).ToString());
            DrawZones("버프존", board.Zones.GetAtSpawn(spawnIndex));

            TowerController tower = board.Slots.Get(spawnIndex);
            if (tower == null)
            {
                Row("타워", "없음");
                return;
            }
            DrawTower(tower);
        }

        private void DrawTower(TowerController tower)
        {
            TowerDefinition definition = tower.Definition;
            TowerFinalStats stats = tower.Stats;
            TowerState state = tower.State;
            bool support = !tower.IsAttackTower;

            Header($"{tower.name}");
            Row("ID", $"{definition.Id}  ({EmblemNames.GetTierName(definition.Tier)} · {EmblemNames.GetColorName(definition.Color)} · {EmblemNames.GetEmblemName(definition.Emblem)})");
            Row("컬러 레벨", $"{stats.ColorLevel}");

            Header("12칸 (타워 카드와 같은 뜻)");
            Row("위력", $"{stats.Power:0.##}");
            Row("사거리", support ? "-" : $"{stats.Range}칸");
            Row("행동력", $"{state.RemainingAction} / {stats.Action}  (매 턴 −{1 + Mathf.RoundToInt(stats.Get(StatType.ActionGainPerTurn))})");
            Row("공격 횟수", support ? $"{stats.AttackCount}스택" : $"{stats.AttackCount}회");
            Row("치명 확률", support ? "-" : $"{stats.CriticalRate * 100f:0.##}%");
            Row("치명 피해", support ? "-" : $"{CriticalRoll.Multiplier(stats.CriticalDamage) * 100f:0.##}%");
            Row("최종 피해", support ? "-" : $"×{stats.FinalDamageMultiplier:0.##}");
            Row("스킬 계수", $"×{stats.AbilityValue:0.##}");
            Row("Duration", GetDurationText(definition.Color, stats));
            Row("Mastery", $"{stats.Mastery}");
            Row("스택", support ? $"최대 {stats.Stack}" : $"{state.Stack.Current:0.#} / {state.Stack.Max}  (StackValue ×{stats.StackValue:0.##})");

            if (!support)
            {
                bool manual = TowerManager.Instance != null && TowerManager.Instance.StackModes.IsManual(definition);
                Row("스택 소모", manual ? (state.Stack.ManualRequested ? "수동 · 눌림 (다음 스킬 2에 씀)" : "수동 · 안 눌림") : "자동");
                Row("스킬 2", tower.Skill == null ? "없음" : $"{tower.Skill.GetType().Name} · 적중 {state.SkillHitCount} / {stats.Duration}회");
                Row("우선순위", $"{state.TargetPriority}{(state.AttackCellIndex >= 0 ? $" · 공격 칸 {state.AttackCellIndex}" : "")}");

                var received = new List<string>();
                foreach (Zone zone in state.ReceivedZoneBuffs.Keys) received.Add($"{zone.Owner.name} ({zone.Stage})");
                Row("받는 존 버프", received.Count > 0 ? string.Join(", ", received) : "없음");

                var timed = new List<string>();
                foreach (KeyValuePair<string, int> pair in state.TimedBuffs) timed.Add($"{pair.Key} {pair.Value}회 행동 남음");
                Row("시간 버프", timed.Count > 0 ? string.Join(", ", timed) : "없음");
            }
            else
            {
                DrawZones("내 존", BoardManager.Instance.Zones.GetByOwner(tower));
            }

            DrawTowerSources(stats);
        }

        // 존 목록: "검 버프존 · 스폰 2번 · 12 / 60 · Skill1"
        private static void DrawZones(string title, List<Zone> zones)
        {
            if (zones.Count == 0)
            {
                Row(title, "없음");
                return;
            }
            foreach (Zone zone in zones)
            {
                string kind = zone.IsBuff ? "버프존" : "디버프존";
                string cell = !zone.IsPlaced ? "자리 안 고름" : zone.IsBuff ? $"스폰 {zone.CellIndex}번" : $"길 {zone.CellIndex}번";
                Row($"  {title}", $"{EmblemNames.GetEmblemName(zone.Emblem)} {kind} · {cell} · {zone.Stack:0.#} / {zone.Owner.Stats.Stack} · {zone.Stage}");
            }
        }

        private static string GetDurationText(TowerColor color, TowerFinalStats stats)
        {
            switch (color)
            {
                case TowerColor.Red:
                case TowerColor.Blue: return $"스킬 2까지 적중 {stats.Duration}회";
                case TowerColor.Purple: return $"디버프 유지 {stats.Duration}회 행동";
                default: return "-";
            }
        }

        private static void DrawTowerSources(TowerFinalStats stats)
        {
            var byStat = new Dictionary<StatType, List<StatSource>>();
            foreach (StatSource source in stats.Sources)
            {
                if (!byStat.TryGetValue(source.Stat, out List<StatSource> list)) byStat[source.Stat] = list = new List<StatSource>();
                list.Add(source);
            }

            Header("출처 (수정자)");
            if (byStat.Count == 0)
            {
                EditorGUILayout.LabelField("없음 (티어 표 · 컬러 표 값 그대로)");
                return;
            }

            foreach (KeyValuePair<StatType, List<StatSource>> pair in byStat)
            {
                string line = $"기본 {stats.GetBase(pair.Key):0.##}";
                foreach (StatSource source in pair.Value) line += $" + {source.Source} {source.Amount:+0.##;-0.##}";
                Row(pair.Key.ToString(), $"{line} = {stats.Get(pair.Key):0.##}");
            }
        }

        private void DrawPathCell(int pathIndex)
        {
            BoardManager board = BoardManager.Instance;
            Header($"길 칸 {pathIndex}");
            PathTileType tile = board.Tiles.GetPathTile(pathIndex);
            Row("특수 타일", tile == PathTileType.Bifrost ? $"Bifrost (맞은편 {board.Grid.GetBifrostPartner(pathIndex)})" : tile.ToString());
            DrawZones("디버프존", board.Zones.GetAtPath(pathIndex));
            List<GroundEffect> grounds = board.Grounds.GetAt(pathIndex);
            if (grounds.Count == 0) Row("장판", "없음");
            foreach (GroundEffect ground in grounds) Row("  장판", ground.Describe());

            IReadOnlyList<ITarget> targets = board.Occupancy.GetAt(pathIndex);
            Row("서 있는 대상", $"{targets.Count}");
            foreach (ITarget target in targets)
            {
                if (target.Body != null) DrawEnemy(target.Body);
                else Row(target.Transform != null ? target.Transform.name : "대상", $"체력 {target.CurrentHp:0} / {target.MaxHp:0}");
            }
        }

        private void DrawEnemy(EnemyController enemy)
        {
            EnemyStats stats = enemy.Stats;
            MoveRange range = stats.GetMoveRange();

            Header($"{enemy.name}  ({enemy.Definition.Kind})");
            float spawnMaxHp = enemy.Definition.MaxHP * WaveTable.GetHpMultiplier(enemy.SpawnWave, enemy.Definition.Kind);
            string maxHpChange = Mathf.Approximately(spawnMaxHp, enemy.MaxHp) ? "" : $"  (처음 최대 {spawnMaxHp:0.#} → {enemy.MaxHp / spawnMaxHp * 100f - 100f:+0.#;-0.#}%)";
            Row("체력", $"{enemy.CurrentHp:0.#} / {enemy.MaxHp:0.#}{maxHpChange}");
            Row("방어력", $"{stats.Get(StatType.Defense):0.#}  (기본 {stats.GetBase(StatType.Defense):0.#})");
            Row("받는 피해", $"×{stats.Get(StatType.DamageTaken):0.##}");
            Row("이동력", $"{range.Min} ~ {range.Max}  (기본 {enemy.Definition.SpeedMin} ~ {enemy.Definition.SpeedMax})");
            Row("행동", $"{range.Action}턴 주기 · 다음 행동까지 {Mathf.Max(0, enemy.Movement.ActionCounter)}");
            Row("저항력", $"지금 {enemy.Resistance.Current:0.#}%  (기본 {enemy.Resistance.Base:0.#}%)");

            List<string> effects = StatusTextFormatter.DescribeAll(enemy.Status);
            if (effects.Count == 0) Row("상태 효과", "없음");
            foreach (string line in effects) Row("  효과", line);

            if (stats.Modifiers.All.Count == 0)
            {
                Row("수정자", "없음");
                return;
            }
            foreach (StatModifier modifier in stats.Modifiers.All)
            {
                string value = modifier.IsPercent ? $"{modifier.Value * 100f:+0.##;-0.##}%" : $"{modifier.Value:+0.##;-0.##}";
                Row($"  {modifier.Source}", $"{modifier.Stat} {value}");
            }
        }

        // ── 한 줄 그리기 ──

        private static void Header(string text)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        private static void Row(string label, string value) => EditorGUILayout.LabelField(label, value);
    }
}
