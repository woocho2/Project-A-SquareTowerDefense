using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    public class DebugCheat : MonoBehaviour
    {
        private bool m_autoEndTurn;

        [Header("화면 분할 참격 미리보기 (8)")]
        [Tooltip("화면 너비에 대한 이동 비율입니다. 일곱 선으로 나뉜 조각이 번갈아 좌우로 이동합니다.")]
        [SerializeField, Range(0f, 0.1f)] private float m_areaSlashSlide = 0.025f;
        [Tooltip("노란 선 일곱 개를 무작위 순서로 모두 그은 뒤 1초 대기하고, 이 시간 동안 화면이 어긋났다 이어집니다.")]
        [SerializeField, Min(0.1f)] private float m_areaSlashDuration = 1.35f;
        [SerializeField, Range(-80f, 80f)] private float m_areaSlashAngle = -20f;
        private AreaSlashEffect m_areaSlashPreview;

        // ── 로그 필터 (K) ── 일반 로그(Log) 중 tag가 들어간 것만 콘솔로 보냄. 경고·에러·예외는 그대로
        private sealed class TagLogFilter : ILogHandler
        {
            public readonly ILogHandler Inner;
            private readonly string m_tag;

            public TagLogFilter(ILogHandler inner, string tag)
            {
                Inner = inner;
                m_tag = tag;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Log)
                {
                    string message = args != null && args.Length > 0 ? string.Format(format, args) : format;
                    if (!message.Contains(m_tag)) return;
                }
                Inner.LogFormat(logType, context, format, args);
            }

            public void LogException(System.Exception exception, Object context) => Inner.LogException(exception, context);
        }

        private TagLogFilter m_logFilter;

        private void SetLogFilter(string tag)
        {
            if (m_logFilter != null)                                // 켜져 있던 것을 먼저 풂 (원래 처리기로)
            {
                Debug.unityLogger.logHandler = m_logFilter.Inner;
                m_logFilter = null;
            }
            if (tag == null) return;

            m_logFilter = new TagLogFilter(Debug.unityLogger.logHandler, tag);
            Debug.unityLogger.logHandler = m_logFilter;
        }

        private void Start()
        {
            EnemyManager.Instance.CountChanged += OnEnemyCountChanged;
            CurrencyManager.Instance.Changed += OnCurrencyChanged;
            TowerManager.Instance.TowerAdded += OnTowerAdded;
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= OnEnemyCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= OnCurrencyChanged;
            if (TowerManager.Instance != null) TowerManager.Instance.TowerAdded -= OnTowerAdded;
            SetLogFilter(null);                                     // 씬을 나가면 로그를 원래대로
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.kKey.wasPressedThisFrame)                  // K: [Skill] 로그만 보기 (켜고 끄기). 경고·에러는 늘 보임
            {
                bool turnOn = m_logFilter == null;
                SetLogFilter(turnOn ? "[Skill]" : null);
                Debug.LogWarning($"[DebugCheat] 로그 필터 {(turnOn ? "켬: [Skill]만" : "끔: 전부")}");
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TurnManager.Instance.OnClickEndTurn();                // 턴 종료
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                m_autoEndTurn = !m_autoEndTurn;                        // 뒤집기
                Debug.Log($"[DebugCheat] 자동 진행 {(m_autoEndTurn ? "켬" : "끔")}");
            }

            if (m_autoEndTurn && TurnManager.Instance.Current.CanPlayerAct)   // 플레이어가 조작할 수 있는 상태일 때만
            {
                AutoPlayerTurn();
                TurnManager.Instance.OnClickEndTurn();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            if (keyboard.bKey.wasPressedThisFrame)
            {
                TowerManager.Instance.TryBuild();
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                CurrencyManager.Instance.AddGold(1000);
            }

            if (keyboard.xKey.wasPressedThisFrame)
            {
                TowerManager.Instance.TrySell(TowerManager.Instance.Selected);
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                StartPickAttackCell();
            }

            if (keyboard.digit1Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Exact);
            if (keyboard.digit2Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Color);
            if (keyboard.digit3Key.wasPressedThisFrame) TowerManager.Instance.TryMerge(TowerManager.Instance.Selected, MergeMode.Emblem);
            if (keyboard.uKey.wasPressedThisFrame) TowerManager.Instance.TryTierUpgrade(TowerManager.Instance.Selected);
            if (keyboard.hKey.wasPressedThisFrame) CurrencyManager.Instance.AddGem(100);

            if (keyboard.lKey.wasPressedThisFrame)                  // L: 고른 타워의 컬러 강화 (바뀐 값은 Tools > Click Debug 창에서)
            {
                TowerController selected = TowerManager.Instance.Selected;
                if (selected == null) Debug.Log("[DebugCheat] 컬러 강화할 타워를 먼저 클릭하세요");
                else TowerManager.Instance.TryUpgradeColor(selected.Definition.Color);
            }

            if (keyboard.tKey.wasPressedThisFrame) NextTargetPriority();    // T: 고른 공격 타워의 우선순위 다음 것으로
            if (keyboard.vKey.wasPressedThisFrame) RequestStackConsume();   // V: 고른 타워의 수동 스택 소모 (적 턴에도)
            if (keyboard.f1Key.wasPressedThisFrame) m_showKeys = !m_showKeys;  // F1: 키 목록 보이기 / 숨기기
            if (keyboard.zKey.wasPressedThisFrame) StartPlaceZone();            // Z: 고른 버프·디버프 타워의 존 자리 고르기
            if (keyboard.jKey.wasPressedThisFrame) StunAllEnemies();         // J: 살아 있는 적 전부에게 기절 (수업 25 확인용, 저항으로 막힐 수 있음)
            if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame)
                PreviewAreaSlash();
        }

        private void PreviewAreaSlash()
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            if (m_areaSlashPreview == null)
            {
                var preview = new GameObject("Debug Screen Slash");
                preview.transform.SetParent(transform, false);
                m_areaSlashPreview = preview.AddComponent<AreaSlashEffect>();
            }

            m_areaSlashPreview.Play(camera, m_areaSlashSlide, m_areaSlashDuration, m_areaSlashAngle);
        }

        private void OnDisable()
        {
            if (m_areaSlashPreview != null) m_areaSlashPreview.Stop();
        }

        private static void StunAllEnemies()                                // ── 수업 25 확인용 임시 코드 ── 기절 규칙(수업 32 방패)이 생기면 지움
        {
            int stunned = 0, blocked = 0;
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                if (enemy.Status.Add(new ImmobilizeEffect("기절 (J 키)"))) stunned++;
                else blocked++;
            }
            Debug.Log($"[DebugCheat] 기절 {stunned}마리, 막힘(저항·이미 걸림) {blocked}마리");
        }

        // ── 키 목록 (F1) ── 화면 왼쪽 위에 작게. 키를 더하면 여기에도 한 줄 더함
        private bool m_showKeys = true;

        private static readonly string[] s_keyLines =
        {
            "F1  키 목록 숨기기",
            "Space 턴 종료   A 자동 진행   R 다시 시작",
            "B 생성   G 골드 +1000   H 젬 +100",
            "── 고른 타워 ──",
            "1 · 2 · 3 확정 · 컬러 · 문양 합성",
            "U 티어 강화   L 컬러 강화   X 판매",
            "C 공격 칸 고르기   T 우선순위 다음",
            "Z 존 자리 고르기 (버프·디버프 타워)",
            "V 수동 스택 소모 (적 턴에도)",
            "── 적 ──",
            "J 살아 있는 적 전부 기절 (수업 25 확인용)",
            "── 보기 ──",
            "8 게임 화면 전체를 가르는 참격",
            "K [Skill] 로그만   Tools > Click Debug 창",
        };

        private void OnGUI()
        {
            if (!m_showKeys) return;

            const float lineHeight = 18f;
            var area = new Rect(8f, 8f, 330f, lineHeight * s_keyLines.Length + 10f);
            GUI.Box(area, GUIContent.none);
            for (int i = 0; i < s_keyLines.Length; i++)
            {
                GUI.Label(new Rect(area.x + 6f, area.y + 4f + i * lineHeight, area.width - 12f, lineHeight), s_keyLines[i]);
            }
        }

        private static readonly TargetPriority[] s_priorityOrder =
        {
            TargetPriority.Closest, TargetPriority.Farthest, TargetPriority.MostHP,
            TargetPriority.LeastHP, TargetPriority.MostCrowded, TargetPriority.Boss,
        };

        private static void NextTargetPriority()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || !tower.IsAttackTower) return;

            int index = System.Array.IndexOf(s_priorityOrder, tower.State.TargetPriority);
            tower.State.TargetPriority = s_priorityOrder[(index + 1) % s_priorityOrder.Length];
            Debug.Log($"[DebugCheat] {tower.name} 우선순위 → {tower.State.TargetPriority}");
        }

        private static void RequestStackConsume()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || !tower.IsAttackTower) return;
            if (!TurnManager.Instance.Current.CanConsumeStack) return;

            tower.State.Stack.RequestManualConsume();

            Debug.Log($"[DebugCheat] {tower.name} 스택 소모 요청 {(tower.State.Stack.ManualRequested ? "됨" : "안 됨 (아직 안 참)")}");
        }

        private static readonly MergeMode[] s_autoMergeOrder = { MergeMode.Exact, MergeMode.Color, MergeMode.Emblem };

        private void AutoPlayerTurn()
        {
            for (int round = 0; round < 50; round++)                  // 멈추지 않게 횟수 제한
            {
                AutoBuildAll();                                        // 골드가 있는 만큼 생성
                if (BoardManager.Instance.Slots.TryGetFirstEmpty(out _)) break;   // 칸이 남았으면 합성하지 않고 다음 턴까지 기다림
                if (!AutoMergeAll()) break;                            // 꽉 찼으면 합성 → 빈 칸이 생기면 다시 생성부터
            }

            AutoArrangeAttackTowers();
            AutoPlaceZones();                                          // 자리 잡기 뒤에 (버프존이 공격 타워가 선 칸을 고르게)
        }

        private const int AutoLongRange = 3;                           // Range가 이 이상이면 긴 사거리, 아래면 짧은 사거리

        // 공격 타워 자리 잡기: 높은 티어부터 남은 칸 중 가장 좋은 칸으로 (그 칸의 타워와 교환)
        //   긴 사거리  사거리 안에 들어오는 길 칸이 가장 많은 곳 (같으면 길에 가까운 곳)
        //   짧은 사거리 길에 가장 가까운 곳 (같으면 사거리 안의 길 칸이 많은 곳)
        private static void AutoArrangeAttackTowers()
        {
            BoardGrid grid = BoardManager.Instance.Grid;
            TowerSlots slots = BoardManager.Instance.Slots;

            var attackers = new List<TowerController>();
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && tower.IsAttackTower) attackers.Add(tower);
            }
            attackers.Sort((a, b) => a.Definition.Tier != b.Definition.Tier
                ? b.Definition.Tier.CompareTo(a.Definition.Tier)
                : b.Stats.Range.CompareTo(a.Stats.Range));

            var placed = new bool[slots.Count];                        // 이미 자리를 잡은 타워가 있는 칸 (다시 빼앗지 않음)
            foreach (TowerController tower in attackers)
            {
                int range = tower.Stats.Range;
                int best = tower.SlotIndex;                            // 같은 점수면 지금 자리에 둠 (쓸데없이 옮기지 않게)
                (int, int) bestScore = GetSlotScore(grid, best, range);

                for (int slot = 0; slot < slots.Count; slot++)
                {
                    if (placed[slot]) continue;
                    (int, int) score = GetSlotScore(grid, slot, range);
                    if (score.CompareTo(bestScore) > 0)
                    {
                        best = slot;
                        bestScore = score;
                    }
                }

                if (best != tower.SlotIndex) TowerManager.Instance.TryMoveOrSwap(tower, best);
                placed[tower.SlotIndex] = true;
            }
        }

        private static (int, int) GetSlotScore(BoardGrid grid, int slot, int range)    // 클수록 좋음 (앞 값 먼저 비교)
        {
            Vector3Int cell = grid.GetSpawnCell(slot);
            int coverage = grid.GetPathIndicesInSquare(cell, range).Count;
            int distance = GetDistanceToPath(grid, cell);

            return range >= AutoLongRange ? (coverage, -distance) : (-distance, coverage);
        }

        private static int GetDistanceToPath(BoardGrid grid, Vector3Int cell)
        {
            int nearest = int.MaxValue;
            for (int i = 0; i < grid.PathCount; i++)
            {
                nearest = Mathf.Min(nearest, BoardGrid.GetTileDistance(grid.GetPathCell(i), cell));
            }
            return nearest;
        }

        private void OnTowerAdded(TowerController tower)                // 자동 진행 중 3티어 타워가 생기면(합성) 그 컬러를 한 번 컬러 강화
        {
            if (!m_autoEndTurn || tower.Definition.Tier != 3) return;

            TowerColor color = tower.Definition.Color;
            ColorUpgradeState levels = TowerManager.Instance.ColorLevels;
            if (!levels.CanUpgrade(color) || CurrencyManager.Instance.Gem < levels.GetCost(color)) return;   // 안 되는 건 로그 없이 넘어감

            TowerManager.Instance.TryUpgradeColor(color);
        }

        private static bool AutoBuildAll()
        {
            TowerManager towers = TowerManager.Instance;
            bool builtAny = false;
            while (CurrencyManager.Instance.Gold >= towers.BuildCost
                   && BoardManager.Instance.Slots.TryGetFirstEmpty(out _)
                   && towers.TryBuild())
            {
                builtAny = true;
            }
            return builtAny;
        }

        private static bool AutoMergeAll()
        {
            bool mergedAny = false;
            for (int count = 0; count < 100; count++)
            {
                if (!AutoMergeOnce()) break;
                mergedAny = true;
            }
            return mergedAny;
        }

        private static bool AutoMergeOnce()                            // 확정 → 컬러 → 문양 순서로 하나라도 되면 true (되면 다시 확정부터)
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            var materials = new List<TowerController>();

            foreach (MergeMode mode in s_autoMergeOrder)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    TowerController tower = slots.Get(i);
                    if (tower == null) continue;
                    if (!TowerProgression.TryFindMaterials(tower, mode, slots, materials)) continue;   // 재료가 없으면 로그 없이 건너뜀
                    if (TowerManager.Instance.TryMerge(tower, mode)) return true;
                }
            }
            return false;
        }

        private void StartPlaceZone()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || tower.IsAttackTower || !TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[DebugCheat] 플레이어 턴에 버프·디버프 타워를 먼저 클릭하세요");
                return;
            }

            BoardInputController input = FindFirstObjectByType<BoardInputController>();
            if (input != null) input.ChangeMode(new PlaceZoneMode(input, tower, BoardManager.Instance.Zones));
        }

        // 자동 진행: 안 고른 존을 자동으로 놓음 (안 하면 턴 종료가 막힘). 규칙은 임시 — 사용자가 봇 규칙을 정하면 바꿈
        //   버프존: 공격 타워가 선 칸 중 티어가 가장 높은 곳, 없으면 놓을 수 있는 첫 칸 / 디버프존: 적이 가장 많이 선 길 칸, 같으면 앞 번호
        private static void AutoPlaceZones()
        {
            BoardManager board = BoardManager.Instance;
            ZoneRegistry zones = board.Zones;

            foreach (Zone zone in new List<Zone>(zones.All))
            {
                if (zone.IsPlaced || !zones.HasPlaceableCell(zone)) continue;

                int best = -1;
                int bestScore = int.MinValue;
                int cellCount = zone.IsBuff ? board.Grid.SpawnCount : board.Grid.PathCount;
                for (int cell = 0; cell < cellCount; cell++)
                {
                    if (!zones.CanPlace(zone, cell, out _)) continue;

                    int score;
                    if (zone.IsBuff)
                    {
                        TowerController tower = board.Slots.Get(cell);
                        score = tower != null && tower.IsAttackTower ? tower.Definition.Tier : 0;
                    }
                    else score = board.Occupancy.GetAt(cell).Count;

                    if (score > bestScore)
                    {
                        best = cell;
                        bestScore = score;
                    }
                }
                if (best >= 0) zones.TryPlace(zone, best);
            }
        }

        private void StartPickAttackCell()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || !tower.IsAttackTower || !TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[DebugCheat] 플레이어 턴에 공격 타워를 먼저 클릭하세요");
                return;
            }

            BoardInputController input = FindFirstObjectByType<BoardInputController>();
            if (input != null) input.ChangeMode(new PickAttackCellMode(input, tower));
        }

        private void OnEnemyCountChanged(int count)
        {
            Debug.Log($"[DebugCheat] 적 {count} / 한도 {GameSession.Instance.CurrentLimit}");
        }

        private void OnCurrencyChanged(int gold, int gem)
        {
            Debug.Log($"[DebugCheat] 골드 {gold} / 젬 {gem}");
        }
    }
}
