using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquareTD
{
    // 웨이브·적 수·한도 등 게임 정보 패널
    public class GameInfoPanel : MonoBehaviour
    {
        [Header("정보 표시")]
        [SerializeField] TextMeshProUGUI txt_stage;
        [SerializeField] TextMeshProUGUI txt_currentWave;
        [SerializeField] TextMeshProUGUI txt_currentLimit;
        [SerializeField] Image img_life;
        [SerializeField] TextMeshProUGUI txt_currentGold;
        [SerializeField] TextMeshProUGUI txt_playerLevel, txt_playerXp;
        [SerializeField] Image img_playerXpFill;
        private Tween m_xpTween;
        private int m_shownLevel;
        private float m_shownSpeed = -1;
        [SerializeField] TextMeshProUGUI txt_currentGameSpeed;
        [SerializeField] TextMeshProUGUI txt_bossTurns;

        [SerializeField] Button btn_option;
        [SerializeField] Button btn_gameSpeed;
        [SerializeField] Button btn_skip;
        [SerializeField] TextMeshProUGUI txt_skip;

        private void Start()
        {
            if (txt_stage != null) txt_stage.text = GameSession.Instance.Stage != null ? GameSession.Instance.Stage.DisplayName : SceneManager.GetActiveScene().name;

            WaveManager.Instance.WaveChanged += UpdateWaveText;
            GameSession.Instance.LimitChanged += HandleLimitChanged;
            EnemyManager.Instance.CountChanged += HandleCountChanged;
            CurrencyManager.Instance.Changed += UpdateCurrencyText;
            TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;
            TowerManager.Instance.ColorLevels.XpChanged += UpdatePlayerLevel;

            if (btn_option != null) btn_option.onClick.AddListener(HandleOptionClick);
            if (btn_gameSpeed != null) btn_gameSpeed.onClick.AddListener(HandleGameSpeedClick);
            if (btn_skip != null) btn_skip.onClick.AddListener(HandleSkipClick);

            UpdateWaveText(WaveManager.Instance.CurrentWave);
            UpdateLimitText();
            UpdateCurrencyText(CurrencyManager.Instance.Gold);
            UpdateGameSpeedText(Time.timeScale);
            UpdateBossTurnsText();
            UpdatePlayerLevel();
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null) WaveManager.Instance.WaveChanged -= UpdateWaveText;
            if (GameSession.Instance != null) GameSession.Instance.LimitChanged -= HandleLimitChanged;
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= HandleCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= UpdateCurrencyText;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;
            if (TowerManager.Instance != null) TowerManager.Instance.ColorLevels.XpChanged -= UpdatePlayerLevel;
            m_xpTween?.Kill();
            if (txt_playerLevel != null) txt_playerLevel.rectTransform.DOKill();

            if (btn_option != null) btn_option.onClick.RemoveListener(HandleOptionClick);
            if (btn_gameSpeed != null) btn_gameSpeed.onClick.RemoveListener(HandleGameSpeedClick);
            if (btn_skip != null) btn_skip.onClick.RemoveListener(HandleSkipClick);
        }

        private void UpdateWaveText(int wave)
        {
            if (txt_currentWave != null) txt_currentWave.text = $"WAVE {wave}";
            UpdateBossTurnsText();
        }

        private void HandleLimitChanged(int limit)
        {
            UpdateLimitText();
        }

        private void HandleCountChanged(int count)
        {
            UpdateLimitText();
            UpdateBossTurnsText();
        }

        private void HandleEnemyTurnEnded(int turn)
        {
            UpdateBossTurnsText();
        }

        private void UpdateLimitText()
        {
            if (txt_currentLimit == null) return;
            int count = EnemyManager.Instance.Registry.Count, limit = GameSession.Instance.CurrentLimit;
            txt_currentLimit.text = $"{count} / {limit}";
            Color color = count >= limit ? new Color32(246, 123, 119, 255) : count >= limit * .8f ? new Color32(246, 190, 106, 255) : new Color32(237, 241, 249, 255);
            txt_currentLimit.color = color;
            if (img_life != null) { img_life.fillAmount = limit > 0 ? Mathf.Clamp01((float)count / limit) : 0; img_life.color = color; }
        }

        private void UpdateCurrencyText(int gold)
        {
            if (txt_currentGold != null) txt_currentGold.text = gold.ToString("N0");
        }

        private void UpdateGameSpeedText(float speed)
        {
            if (txt_currentGameSpeed != null) txt_currentGameSpeed.text = $"×{speed:0.#}";
        }

        private void Update()
        {
            TurnManager turns = TurnManager.Instance;
            if (btn_skip != null) btn_skip.interactable = turns != null && turns.CanSkip && Time.timeScale > 0f;
            if (txt_skip != null) txt_skip.text = turns != null && turns.IsSkipping ? "스킵 중" : "스킵";
            if (btn_gameSpeed != null) btn_gameSpeed.interactable = !TurnManager.Skipping
                && Time.timeScale > 0f && (UIManager.Instance == null || !UIManager.Instance.IsPaused);
            float speed = turns != null ? turns.PlaybackSpeed : Time.timeScale;
            if (speed <= 0f || Mathf.Approximately(m_shownSpeed, speed)) return;
            m_shownSpeed = speed;
            UpdateGameSpeedText(speed);
        }

        private void UpdatePlayerLevel()
        {
            ColorUpgradeState level = TowerManager.Instance.ColorLevels;
            if (txt_playerLevel != null)
            {
                txt_playerLevel.text = $"Lv. {level.Level}";
                if (m_shownLevel > 0 && m_shownLevel != level.Level)
                {
                    txt_playerLevel.rectTransform.DOKill(true); txt_playerLevel.rectTransform.localScale = Vector3.one;
                    txt_playerLevel.rectTransform.DOPunchScale(Vector3.one * .10f, .3f, 3, .5f).SetUpdate(true);
                }
            }
            if (txt_playerXp != null) txt_playerXp.text = level.IsMax ? "최대 레벨" : $"{level.Xp} / {level.XpToNext} EXP";
            if (img_playerXpFill != null)
            {
                float target = level.IsMax ? 1 : (float)level.Xp / Mathf.Max(1, level.XpToNext);
                m_xpTween?.Kill();
                if (m_shownLevel == 0) img_playerXpFill.fillAmount = target;
                else m_xpTween = img_playerXpFill.DOFillAmount(target, .25f).SetUpdate(true);
            }
            m_shownLevel = level.Level;
        }

        private void UpdateBossTurnsText()
        {
            if (txt_bossTurns == null) return;

            int fewest = int.MaxValue;
            foreach (EnemyController enemy in EnemyManager.Instance.Registry.Alive)
            {
                if (!enemy.IsBoss) continue;
                int remaining = EnemyManager.Instance.BossTimer.GetRemaining(enemy);
                if (remaining < fewest) fewest = remaining;
            }

            bool hasBoss = fewest != int.MaxValue;
            txt_bossTurns.gameObject.SetActive(hasBoss);
            if (hasBoss) txt_bossTurns.text = $"보스 {fewest}턴";
        }

        private void HandleOptionClick()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[GameInfoPanel] UIManager가 없어 옵션 패널을 열 수 없음");
                return;
            }

            UIManager.Instance.ShowOptionPanel();
        }

        private void HandleSkipClick() => TurnManager.Instance?.SkipToNextPlayerTurn();

        private void HandleGameSpeedClick()
        {
            if (Time.timeScale <= 0 || TurnManager.Skipping || (UIManager.Instance != null && UIManager.Instance.IsPaused)) return;
            Time.timeScale = Time.timeScale < 1.5f ? 2f : Time.timeScale < 2.5f ? 3f : 1f;
            UpdateGameSpeedText(Time.timeScale);
        }
    }
}
