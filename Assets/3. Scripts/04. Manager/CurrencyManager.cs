using System;
using UnityEngine;

/// <summary>골드와 젬의 보유량을 관리하고, 값이 바뀌면 구독자에게 알립니다.</summary>
public class CurrencyManager : MonoBehaviour
{
    #region Singleton and Inspector

    public static CurrencyManager Instance;

    [Header("Economy Settings")]
    [SerializeField] private int startGold = 300;
    [SerializeField] private int startGem = 0;

    #endregion

    #region Runtime State

    private int currentGold;
    private int currentGem;

    /// <summary>골드 또는 젬이 변경되었을 때 발생합니다.</summary>
    public event Action CurrencyChanged;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentGold = startGold;
        currentGem = startGem;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Queries

    public int GetCurrentGold()
    {
        return currentGold;
    }

    public int GetCurrentGem()
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

    #endregion

    #region Currency Changes

    public void AddGold(int amount)
    {
        currentGold += amount;
        NotifyCurrencyChanged();
    }

    public void AddGem(int amount)
    {
        currentGem += amount;
        NotifyCurrencyChanged();
    }

    public void SpendMoney(int amount)
    {
        if (HasEnoughMoney(amount))
        {
            currentGold -= amount;
            NotifyCurrencyChanged();
        }
        else
        {
            Debug.LogWarning("골드가 부족합니다! 필요한 골드: " + amount);
        }
    }

    public void SpendGem(int amount)
    {
        if (HasEnoughGem(amount))
        {
            currentGem -= amount;
            NotifyCurrencyChanged();
        }
        else
        {
            Debug.LogWarning("보석이 부족합니다! 필요한 보석: " + amount);
        }
    }

    #endregion

    #region Change Notification

    private void NotifyCurrencyChanged()
    {
        CurrencyChanged?.Invoke();
    }

    #endregion
}
