using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD
{
    // 승패 공용 결과 화면과 다시하기·다음 스테이지·스테이지 선택 연결
    public sealed class BattleResultPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_title, m_subtitle, m_count, m_total, m_best, m_primaryLabel, m_empty;
        [SerializeField] private Image m_accent;
        [SerializeField] private RectTransform m_window, m_content;
        [SerializeField] private CanvasGroup m_group;
        [SerializeField] private ScrollRect m_scroll;
        [SerializeField] private BattleResultRow m_rowTemplate;
        [SerializeField] private Button m_primary, m_stageSelect;
        [SerializeField] private StageDefinition[] m_stages;
        private readonly List<BattleResultRow> m_rows = new List<BattleResultRow>();
        private Sequence m_animation;
        private StageDefinition m_nextStage;
        private bool m_leaving;
        public BattleResults.Snapshot Data { get; private set; }
        public IReadOnlyList<BattleResultRow> Rows => m_rows;
        public Button PrimaryButton => m_primary;
        public Button StageSelectButton => m_stageSelect;
        public static string FormatDamage(double value) => value >= 100 ? value.ToString("N0") : value.ToString("0.##");

        private void Awake() { m_primary.onClick.AddListener(Primary); m_stageSelect.onClick.AddListener(StageSelect); }
        public void Open(bool won)
        {
            if (SimulationRunner.IsOn || GameSession.Instance == null || !GameSession.Instance.IsOver) return;
            Display(GameSession.Instance.Results.Final ?? GameSession.Instance.Results.Capture(won));
        }
        public void Display(BattleResults.Snapshot data)
        {
            Data = data; m_leaving = false;
            transform.SetAsLastSibling();
            m_title.text = data.Won ? "스테이지 클리어" : "도전 종료";
            m_title.color = data.Won ? new Color32(245, 214, 139, 255) : new Color32(244, 150, 145, 255);
            m_accent.color = m_title.color;
            m_subtitle.text = data.StageName + "  ·  " + data.Mode + "  ·  " + data.Wave + " WAVE";
            m_count.text = data.Rows.Count + "기"; m_total.text = FormatDamage(data.TotalDamage);
            m_best.text = data.Rows.Count > 0 && data.Rows[0].Damage > 0 ? data.Rows[0].Name : "—";
            m_empty.gameObject.SetActive(data.Rows.Count == 0);
            foreach (BattleResultRow row in m_rows) { row.gameObject.SetActive(false); Destroy(row.gameObject); }
            m_rows.Clear();
            double maximum = data.Rows.Count > 0 ? data.Rows[0].Damage : 0;
            for (int i = 0; i < data.Rows.Count; i++)
            {
                BattleResultRow row = Instantiate(m_rowTemplate, m_content); row.name = "Result_" + i;
                ((RectTransform)row.transform).anchoredPosition = new Vector2(0, -i * 88);
                row.Bind(data.Rows[i], i + 1, maximum, data.TotalDamage); row.gameObject.SetActive(true); m_rows.Add(row);
            }
            m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, data.Rows.Count * 88);
            m_scroll.verticalNormalizedPosition = 1;
            m_nextStage = FindNextStage();
            m_primaryLabel.text = data.Won ? (m_nextStage != null ? "다음 스테이지" : "마지막 스테이지") : "다시하기";
            m_primary.interactable = !data.Won || m_nextStage != null;
            m_stageSelect.interactable = true;
            if (!Application.isPlaying) return;
            m_animation?.Kill(); m_group.alpha = 0; m_window.anchoredPosition = new Vector2(0, -16);
            m_animation = DOTween.Sequence().SetUpdate(true).Append(m_group.DOFade(1, .24f))
                .Join(m_window.DOAnchorPos(Vector2.zero, .28f).SetEase(Ease.OutCubic));
        }
        private StageDefinition FindNextStage()
        {
            StageDefinition current = GameSession.Instance != null ? GameSession.Instance.Stage : null;
            if (current == null || m_stages == null) return null;
            foreach (StageDefinition stage in m_stages)
                if (stage != null && stage.Number == current.Number + 1 && Application.CanStreamedLevelBeLoaded(stage.SceneName)) return stage;
            return null;
        }
        private void Primary()
        {
            if (m_leaving || Data == null || SceneFlow.PendingScene != null) return;
            if (Data.Won && m_nextStage == null) return;
            GameSession session = GameSession.Instance;
            StageDefinition target = Data.Won ? m_nextStage : session.Stage;
            string destination = target != null ? target.SceneName : SceneManager.GetActiveScene().name;
            if (!Application.CanStreamedLevelBeLoaded(destination)) return;
            if (target != null) StartSettings.Set(session.Mode, target, TowerManager.Instance.SelectedEmblems);
            Leave(); SceneFlow.Load(destination);
        }
        private void StageSelect()
        {
            if (m_leaving || SceneFlow.PendingScene != null || !Application.CanStreamedLevelBeLoaded("2. Menu")) return;
            Leave(); SceneFlow.Load("2. Menu");
        }
        private void Leave() { m_leaving = true; m_primary.interactable = m_stageSelect.interactable = false; }
        private void OnDisable() => m_animation?.Kill();
        private void OnDestroy() { m_animation?.Kill(); m_primary.onClick.RemoveListener(Primary); m_stageSelect.onClick.RemoveListener(StageSelect); }
    }
}
