using UnityEngine;
using UnityEngine.EventSystems;

namespace SquareTD
{
    // 시너지 타워에만 스킬 버튼을 만들고 기존 시너지 스킬에 연결함.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TowerController))]
    public sealed class SynergySkillButton : MonoBehaviour
    {
        [SerializeField] private SynergySkillButtonView m_viewPrefab;

        private TowerController m_tower;
        private SynergyBoard m_board;
        private SynergyBoard.Entry m_entry;
        private SynergySkillButtonView m_view;
        private BoardInputController m_input;
        private PickSynergyTargetMode m_targeting;

        public SynergySkillButtonView View => m_view;

        private void Awake() => m_tower = GetComponent<TowerController>();

        private void Update()
        {
            if (m_tower.Definition == null) return;
            if (m_tower.Place != TowerPlace.Synergy || !TowerId.IsSynergy(m_tower.Definition.Id) || SimulationRunner.IsOn)
            {
                enabled = false;
                return;
            }
            if (m_entry == null)
            {
                m_board = TowerManager.Instance != null ? TowerManager.Instance.Synergy : null;
                m_entry = m_board?.Find(m_tower);
                if (m_entry == null) return;
                if (m_viewPrefab == null)
                {
                    Debug.LogError("[SynergySkillButton] 스킬 버튼 프리팹을 연결해 주세요.", this);
                    enabled = false;
                    return;
                }
                m_view = Instantiate(m_viewPrefab, transform, false);
                m_view.GetComponent<Canvas>().worldCamera = Camera.main;
                m_view.ActionButton.onClick.AddListener(HandleClick);
                m_board.Used += HandleUsed;
            }
            Refresh();
        }

        public static bool NeedsPathSelection(int id) => id == SynergyEffects.LokiId || id == SynergyEffects.NjordId;

        public static bool CanActivate(SynergySkill skill, ITurnState turn, bool paused)
        {
            if (skill == null || !skill.IsReady || paused || !(turn is PlayerTurnState || turn is EnemyTurnState)) return false;
            return !NeedsPathSelection(skill.Id) || turn.CanPlayerAct;
        }

        private void Refresh()
        {
            if (m_view == null || m_entry?.Skill == null) return;
            ITurnState turn = TurnManager.Instance != null ? TurnManager.Instance.Current : null;
            bool canActivate = CanActivate(m_entry.Skill, turn, Time.timeScale <= 0f || UIManager.Instance != null && UIManager.Instance.IsPaused);
            bool picking = m_targeting != null && m_targeting.IsActive;
            string label = picking ? (m_targeting.FirstPathIndex < 0 ? "길 선택" : "다음 칸") :
                m_entry.Skill.IsReady ? "스킬 사용" : PercentLabel(m_entry.Skill.Stack.Ratio);
            if (!picking && m_entry.Skill is BaldrSkill baldr && baldr.Charges > 0) label = $"스킬 {baldr.Charges}회";
            if (!picking && m_entry.Skill.IsReady && NeedsPathSelection(m_entry.Skill.Id) && turn is EnemyTurnState) label = "내 턴에 사용";
            m_view.Render(m_entry.Skill.Stack.Ratio, m_entry.Skill.IsReady, canActivate, picking, label);
        }

        private static readonly string[] s_percents = new string[100];

        private static string PercentLabel(float ratio)
        {
            int percent = Mathf.Clamp(Mathf.FloorToInt(ratio * 100f), 0, 99);
            return s_percents[percent] ??= $"{percent}%";
        }

        private void HandleClick()
        {
            TryActivate();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void HandleUsed(SynergyBoard.Entry entry)
        {
            if (entry != m_entry) return;
            if (isActiveAndEnabled) m_view?.PlayUsed();
            Refresh();
        }

        public bool TryActivate()
        {
            if (m_targeting != null && m_targeting.IsActive)
            {
                m_targeting.OnCancel();
                Refresh();
                return false;
            }
            ITurnState turn = TurnManager.Instance != null ? TurnManager.Instance.Current : null;
            if (m_entry == null || !CanActivate(m_entry.Skill, turn, Time.timeScale <= 0f || UIManager.Instance != null && UIManager.Instance.IsPaused)) return false;
            if (NeedsPathSelection(m_entry.Skill.Id))
            {
                if (m_input == null) m_input = FindAnyObjectByType<BoardInputController>();
                if (m_input == null) return false;
                m_targeting = new PickSynergyTargetMode(m_input, m_board, m_entry, Refresh);
                m_input.ChangeMode(m_targeting);
                return true;
            }
            bool used = m_board.TryUse(m_entry);
            Refresh();
            return used;
        }

        private void OnDisable()
        {
            if (m_targeting != null && m_targeting.IsActive) m_targeting.OnCancel();
        }

        private void OnDestroy()
        {
            if (m_view != null) m_view.ActionButton.onClick.RemoveListener(HandleClick);
            if (m_board != null) m_board.Used -= HandleUsed;
        }
    }
}
