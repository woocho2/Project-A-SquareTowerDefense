using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerTurnPanel : MonoBehaviour
{
    [SerializeField] Button btn_createTower;
    [SerializeField] TextMeshProUGUI txt_createTowerValue;
    [SerializeField] Button btn_colorUpgrade;
    [SerializeField] TextMeshProUGUI txt_colorUpgradeValue;
    [SerializeField] Button btn_tierUpgrade;
    [SerializeField] TextMeshProUGUI txt_tierUpgradeValue;
    [SerializeField] Button btn_summonMiddleBoss;
    [SerializeField] Button btn_turnEnd;

    private void Start()
    {
        if (GameManager.Instance == null && TowerManager.Instance == null && WaveManager.Instance == null)
        {
            Debug.LogError("PlayerTurnPanel : GameManager/TowerManager/WaveManager가 없습니다.");
            return;
        }

        //UpdateCreateTowerText(TowerManager.Instance.CreateTowerGold);
        //UpdateColorUpgradeText(TowerManager.Instance.ColorUpgradeGold);
        //UpdateTierUpgradeText(TowerManager.Instance.TierUpgradeGold);

        //TowerManager.Instance.CreateTowerGoldChanged += UpdateCreateTowerText;
        //TowerManager.Instance.ColorUpgradeGoldChanged += UpdateColorUpgradeText;
        //TowerManager.Instance.TierUpgradeGoldChanged += UpdateTierUpgradeText;

        btn_createTower.onClick.AddListener(HandleCreateTowerClick);
        btn_colorUpgrade.onClick.AddListener(HandleColorUpgradeClick);
        btn_tierUpgrade.onClick.AddListener(HandleTierUpgradeClick);
        btn_turnEnd.onClick.AddListener(HandleTurnEndClick);
    }

    private void OnDestroy()
    {
        if (btn_createTower != null)
        {
            btn_createTower.onClick.RemoveListener(HandleCreateTowerClick);
        }
        if (btn_colorUpgrade != null)
        {
            btn_colorUpgrade.onClick.RemoveListener(HandleColorUpgradeClick);
        }
        if (btn_tierUpgrade != null)
        {
            btn_tierUpgrade.onClick.RemoveListener(HandleTierUpgradeClick);
        }
        if (btn_turnEnd != null)
        {
            btn_turnEnd.onClick.RemoveListener(HandleTurnEndClick);
        }
    }

    private void UpdateCreateTowerText(int amount)
    {
        txt_createTowerValue.text = $"{amount}";
    }

    private void UpdateColorUpgradeText(int amount)
    {
        txt_colorUpgradeValue.text = $"{amount}";
    }

    private void UpdateTierUpgradeText(int amount)
    {
        txt_tierUpgradeValue.text = $"{amount}";
    }

    private void HandleCreateTowerClick()
    {

    }

    private void HandleColorUpgradeClick()
    {

    }

    private void HandleTierUpgradeClick()
    {

    }

    private void HandleTurnEndClick()
    {
        GameManager.Instance?.OnClickSkipPlayerTurn();
    }
}
