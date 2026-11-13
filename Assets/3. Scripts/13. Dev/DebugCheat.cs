using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SquareTD
{
    // 개발용 단축키와 키 목록
    public class DebugCheat : MonoBehaviour
    {
        private const float DummyHp = 50000000f;

        private bool m_autoEndTurn;

        private void Start()
        {
            EnemyManager.Instance.CountChanged += OnEnemyCountChanged;
            CurrencyManager.Instance.Changed += OnCurrencyChanged;
        }

        private void OnDestroy()
        {
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= OnEnemyCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= OnCurrencyChanged;
            if (m_speedIndex != 0) Time.timeScale = 1f;
            TowerStatCalculator.ShowcaseOverride = false;
        }

        // ── 테스트 모드 (T로 켜고 끔) ──
        private const int TestGold = 100000;
        private const int TestGem = 100000;
        private const float SkillCountGap = 2f;
        private bool m_testMode;

        private void ToggleTestMode()
        {
            m_testMode = !m_testMode;
            Debug.LogWarning($"[DebugCheat] 테스트 모드 {(m_testMode ? "켬" : "끔")}");
        }

        private void HandleTestModeKeys(Keyboard keyboard)
        {
            if (keyboard.f1Key.wasPressedThisFrame)
            {
                m_typingId = true;
                m_idBuffer = "";
            }
            if (keyboard.f2Key.wasPressedThisFrame) CurrencyManager.Instance.AddGold(TestGold);
            if (keyboard.f3Key.wasPressedThisFrame) CurrencyManager.Instance.AddGem(TestGem);
            if (keyboard.f4Key.wasPressedThisFrame) ToggleShowcase();
            if (keyboard.f5Key.wasPressedThisFrame) FillSkillCounts();
        }

        private static List<TowerController> AllTowers()
        {
            var towers = new List<TowerController>();
            TowerSlots slots = BoardManager.Instance.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null) towers.Add(tower);
            }
            return towers;
        }

        private static void ToggleShowcase()
        {
            TowerStatCalculator.ShowcaseOverride = !TowerStatCalculator.ShowcaseOverride;
            List<TowerController> towers = AllTowers();
            foreach (TowerController tower in towers) tower.RecalculateStats();
            Debug.LogWarning(TowerStatCalculator.ShowcaseOverride
                ? $"[DebugCheat] 공격 연출 모드 켬 — 타워 {towers.Count}기 MaxAction {TowerStatCalculator.ShowcaseAction} · Range {TowerStatCalculator.ShowcaseRange} 고정"
                : $"[DebugCheat] 공격 연출 모드 끔 — 타워 {towers.Count}기 원래 스탯");
        }

        private static void FillSkillCounts()
        {
            List<TowerController> towers = AllTowers();
            foreach (TowerController tower in towers)
            {
                float target = Mathf.Max(0f, tower.Stats.SkillCount - SkillCountGap);
                tower.State.SkillCount = Mathf.Max(tower.State.SkillCount, target);
            }
            Debug.Log($"[DebugCheat] 타워 {towers.Count}기 스킬 카운트 → 최대 −{SkillCountGap:0}");
        }

        private static void SpawnDummy()
        {
            EnemyController enemy = EnemyManager.Instance.SpawnForDebug(DummyHp);
            if (enemy != null) Debug.Log($"[DebugCheat] 체력 {DummyHp:N0} 허수아비 → {enemy.PathIndex}번 칸");
        }

        private static readonly float[] s_speeds = { 1f, 2f, 3f };
        private int m_speedIndex;

        private void NextGameSpeed()
        {
            if (Time.timeScale == 0f)
            {
                Debug.Log("[DebugCheat] 일시정지 중에는 배속을 바꾸지 않음");
                return;
            }

            m_speedIndex = (m_speedIndex + 1) % s_speeds.Length;
            Time.timeScale = s_speeds[m_speedIndex];
            Debug.LogWarning($"[DebugCheat] 배속 x{s_speeds[m_speedIndex]:0}");
        }

        private static readonly Key[] s_digitKeys =
            { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] s_numpadKeys =
            { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
        private bool m_typingId;
        private string m_idBuffer = "";

        private void HandleIdInput(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                m_typingId = false;
                Debug.Log("[DebugCheat] 타워 ID 입력 취소");
                return;
            }
            if (keyboard.backspaceKey.wasPressedThisFrame && m_idBuffer.Length > 0)
                m_idBuffer = m_idBuffer.Substring(0, m_idBuffer.Length - 1);

            for (int digit = 0; digit <= 9; digit++)
            {
                if (keyboard[s_digitKeys[digit]].wasPressedThisFrame || keyboard[s_numpadKeys[digit]].wasPressedThisFrame)
                    m_idBuffer += digit;
            }

            bool confirm = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (m_idBuffer.Length >= 4 || (confirm && m_idBuffer.Length > 0))
            {
                m_typingId = false;
                SpawnById(int.Parse(m_idBuffer));
            }
        }

        private static void SpawnById(int id)
        {
            if (!TowerManager.Instance.Database.TryGet(id, out TowerDefinition definition))
            {
                Debug.LogWarning($"[DebugCheat] {id}번 타워가 CSV에 없음 (티어 1~5 · 컬러 1~4 · 문양 01~14, 예: 1101)");
                return;
            }

            TowerController tower = TowerManager.Instance.SpawnForDebug(definition);
            if (tower != null)
                Debug.Log($"[DebugCheat] {tower.name} ({EmblemNames.GetTierName(definition.Tier)}) → {tower.SlotIndex}번 칸");
        }

        private TowerEmblem m_spawnGroupFirst;
        private int m_spawnNext;

        private void SpawnAllTier1(TowerEmblem first, TowerEmblem last)
        {
            if (m_spawnGroupFirst != first)
            {
                m_spawnGroupFirst = first;
                m_spawnNext = 0;
            }

            int total = (last - first + 1) * 4;
            int start = m_spawnNext;
            for (; m_spawnNext < total; m_spawnNext++)
            {
                TowerEmblem emblem = first + m_spawnNext / 4;
                TowerColor color = TowerColor.Red + m_spawnNext % 4;
                if (!TowerManager.Instance.Database.TryGet(TowerId.Make(1, color, emblem), out TowerDefinition definition)) continue;
                if (TowerManager.Instance.SpawnForDebug(definition) == null)
                {
                    Debug.LogWarning($"[DebugCheat] 1티어 {first} ~ {last}: {start + 1} ~ {m_spawnNext}번째 세움 / {total}종 — 빈 칸이 모자람. 0으로 판 뒤 다시 누르면 이어서");
                    return;
                }
            }
            Debug.Log($"[DebugCheat] 1티어 {first} ~ {last}: {start + 1} ~ {total}번째 세움 / {total}종 — 끝. 다시 누르면 처음부터");
            m_spawnNext = 0;
        }

        private static void SellAll()
        {
            TowerSlots slots = BoardManager.Instance.Slots;
            int sold = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                TowerController tower = slots.Get(i);
                if (tower != null && TowerManager.Instance.TrySell(tower)) sold++;
            }
            Debug.Log($"[DebugCheat] 타워 {sold}개 판매");
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (m_typingId)
            {
                HandleIdInput(keyboard);
                return;
            }
            if (keyboard.tKey.wasPressedThisFrame) ToggleTestMode();
            if (m_testMode)
            {
                HandleTestModeKeys(keyboard);
                if (m_typingId) return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                TurnManager.Instance.OnClickEndTurn();
            }

            if (keyboard.aKey.wasPressedThisFrame)
            {
                m_autoEndTurn = !m_autoEndTurn;
                Debug.Log($"[DebugCheat] 자동 진행 {(m_autoEndTurn ? "켬" : "끔")}");
            }

            if (m_autoEndTurn && TurnManager.Instance.Current.CanPlayerAct && AutoPlanner.PlayStep())
            {
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

            if (keyboard.xKey.wasPressedThisFrame)
            {
                TowerManager.Instance.TrySell(TowerManager.Instance.Selected);
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                StartPickAttackCell();
            }

            if (keyboard.pKey.wasPressedThisFrame) NextTargetPriority();
            if (keyboard.f12Key.wasPressedThisFrame) m_showKeys = !m_showKeys;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SpawnDummy();
            if (keyboard.zKey.wasPressedThisFrame) StartPlaceZone();
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) NextGameSpeed();
            if (keyboard.digit8Key.wasPressedThisFrame) SpawnAllTier1(TowerEmblem.Sword, TowerEmblem.Hammer);
            if (keyboard.digit9Key.wasPressedThisFrame) SpawnAllTier1(TowerEmblem.Fire, TowerEmblem.Darkness);
            if (keyboard.digit0Key.wasPressedThisFrame) SellAll();
        }

        private bool m_showKeys = true;

        private static readonly string[] s_keyLines =
        {
            "F12 키 목록 숨기기   T 테스트 모드 켜고 끄기",
            "Space 턴 종료   A 자동 진행   R 다시 시작",
            "5 배속 1 → 2 → 3 → 1",
            "B 생성",
            "1 체력 5천만 허수아비 소환 (피해 확인용)",
            "── 고른 타워 ──",
            "X 판매   C 공격 칸 고르기   P 우선순위 다음",
            "Z 존 자리 고르기 (버프·디버프 타워)",
            "── 타워 세우기 ──",
            "8 무기 1티어 24종   9 원소 1티어 32종   0 전부 판매",
            "  (칸이 모자라면 0으로 판 뒤 같은 키 → 이어서)",
            "Tools > Click Debug 창",
        };

        private void OnGUI()
        {
            if (m_typingId)
            {
                var box = new Rect(Screen.width * 0.5f - 170f, 12f, 340f, 30f);
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x + 10f, box.y + 6f, box.width - 20f, 20f),
                    $"타워 ID: {m_idBuffer}_   (숫자 4자리 · Enter 확인 · Esc 취소)");
            }

            const float lineHeight = 18f;
            float y = 8f;
            if (m_showKeys)
            {
                var area = new Rect(8f, y, 330f, lineHeight * s_keyLines.Length + 10f);
                DrawLines(area, s_keyLines, lineHeight);
                y = area.yMax + 6f;
            }

            if (m_testMode)
            {
                string[] lines =
                {
                    "■ 테스트 모드 (T로 끔)",
                    "F1 타워 ID로 세우기 (예: 1101 = 브론즈 소드 마스터)",
                    $"F2 골드 +{TestGold:N0}   F3 젬 +{TestGem:N0}",
                    $"F4 공격 연출: 행동 {TowerStatCalculator.ShowcaseAction} · 사거리 {TowerStatCalculator.ShowcaseRange} [{(TowerStatCalculator.ShowcaseOverride ? "켬" : "끔")}]",
                    $"F5 모든 타워 스킬 카운트 → 최대 −{SkillCountGap:0}",
                };
                DrawLines(new Rect(8f, y, 330f, lineHeight * lines.Length + 10f), lines, lineHeight);
            }
        }

        private static void DrawLines(Rect area, string[] lines, float lineHeight)
        {
            GUI.Box(area, GUIContent.none);
            for (int i = 0; i < lines.Length; i++)
                GUI.Label(new Rect(area.x + 6f, area.y + 4f + i * lineHeight, area.width - 12f, lineHeight), lines[i]);
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

        private void StartPlaceZone()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || tower.IsAttackTower || !TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[DebugCheat] 플레이어 턴에 버프·디버프 타워를 먼저 클릭하세요");
                return;
            }

            BoardInputController input = FindAnyObjectByType<BoardInputController>();
            if (input != null) input.ChangeMode(new PlaceZoneMode(input, tower, BoardManager.Instance.Zones));
        }

        private void StartPickAttackCell()
        {
            TowerController tower = TowerManager.Instance.Selected;
            if (tower == null || !tower.IsAttackTower || !TurnManager.Instance.Current.CanPlayerAct)
            {
                Debug.Log("[DebugCheat] 플레이어 턴에 공격 타워를 먼저 클릭하세요");
                return;
            }

            BoardInputController input = FindAnyObjectByType<BoardInputController>();
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
