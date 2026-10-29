using TMPro;
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
        [SerializeField] Image img_shield;
        [SerializeField] TextMeshProUGUI txt_currentGold;
        [SerializeField] TextMeshProUGUI txt_currentGem;
        [SerializeField] TextMeshProUGUI txt_currentGameSpeed;
        [SerializeField] TextMeshProUGUI txt_bossTurns;

        [SerializeField] Button btn_option;
        [SerializeField] Button btn_gameSpeed;

        private void Start()
        {
            if (txt_stage != null) txt_stage.text = SceneManager.GetActiveScene().name;

            WaveManager.Instance.WaveChanged += UpdateWaveText;
            GameSession.Instance.LimitChanged += HandleLimitChanged;
            EnemyManager.Instance.CountChanged += HandleCountChanged;
            CurrencyManager.Instance.Changed += UpdateCurrencyText;
            TurnManager.Instance.EnemyTurnEnded += HandleEnemyTurnEnded;

            if (btn_option != null) btn_option.onClick.AddListener(HandleOptionClick);
            if (btn_gameSpeed != null) btn_gameSpeed.onClick.AddListener(HandleGameSpeedClick);

            UpdateWaveText(WaveManager.Instance.CurrentWave);
            UpdateLimitText();
            UpdateCurrencyText(CurrencyManager.Instance.Gold, CurrencyManager.Instance.Gem);
            UpdateGameSpeedText(1f);
            UpdateBossTurnsText();
            if (img_shield != null) img_shield.enabled = false;
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null) WaveManager.Instance.WaveChanged -= UpdateWaveText;
            if (GameSession.Instance != null) GameSession.Instance.LimitChanged -= HandleLimitChanged;
            if (EnemyManager.Instance != null) EnemyManager.Instance.CountChanged -= HandleCountChanged;
            if (CurrencyManager.Instance != null) CurrencyManager.Instance.Changed -= UpdateCurrencyText;
            if (TurnManager.Instance != null) TurnManager.Instance.EnemyTurnEnded -= HandleEnemyTurnEnded;

            if (btn_option != null) btn_option.onClick.RemoveListener(HandleOptionClick);
            if (btn_gameSpeed != null) btn_gameSpeed.onClick.RemoveListener(HandleGameSpeedClick);
        }

        private void UpdateWaveText(int wave)
        {
            if (txt_currentWave != null) txt_currentWave.text = $"Wave {wave}";
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
            txt_currentLimit.text = $"{EnemyManager.Instance.Registry.Count}/{GameSession.Instance.CurrentLimit}";
        }

        private void UpdateCurrencyText(int gold, int gem)
        {
            if (txt_currentGold != null) txt_currentGold.text = $"{gold}";
            if (txt_currentGem != null) txt_currentGem.text = $"{gem}";
        }

        private void UpdateGameSpeedText(float speed)
        {
            if (txt_currentGameSpeed != null) txt_currentGameSpeed.text = $"{speed}";
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

        private void HandleGameSpeedClick()
        {
            Debug.Log("[GameInfoPanel] 게임 속도는 수업 43에서 연결");
        }
    }
}
