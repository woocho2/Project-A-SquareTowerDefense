using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    [Header("Economy Settings")]
    [SerializeField] private int startGold =300;
    [SerializeField] private int startGem = 0;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI Txt_gold;
    [SerializeField] private TextMeshProUGUI Txt_gem;

    private int currentGold;
    private float currentGem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentGold = startGold;
        currentGem = startGem;
        UpdateGoldUI();
        UpdateGemUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetCurrentGold()
    {
        return currentGold;
    }

    public float GetCurrentGem()
    {
        return currentGem;
    }

    public bool HasEnoughMoney(int amount)
    {
        return currentGold >= amount;
    }

    public bool HasEnoughGem(float amount)
    {
        return currentGem >= amount;
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }

    public void AddGem(float amount)
    {
        currentGem += amount;
        UpdateGemUI();
    }

    public void SpendMoney(int amount)
    {
        if (HasEnoughMoney(amount))
        {
            currentGold -= amount;
            UpdateGoldUI();
        }
        else
        {
            Debug.LogWarning("골드가 부족합니다! 필요한 골드: " + amount);
        }
    }

    public void SpendGem(float amount)
    {
        if (HasEnoughGem(amount))
        {
            currentGem -= amount;
            UpdateGemUI();
        }
        else
        {
            Debug.LogWarning("보석이 부족합니다! 필요한 보석: " + amount);
        }
    }

    private void UpdateGoldUI()
    {
        if (Txt_gold != null)
        {
            Txt_gold.text = $"{currentGold.ToString()}";
        }
    }

    private void UpdateGemUI()
    {
        if (Txt_gem != null)
        {
            Txt_gem.text = $"{currentGem.ToString()}";
        }
    }
}
