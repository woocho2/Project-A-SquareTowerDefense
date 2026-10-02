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

    private int m_currentGold;
    private int m_currentGem;

    public int CurrentGold => m_currentGold;
    public int CurrentGem => m_currentGem;

    public event Action<int> CurrentGoldChanged;
    public event Action<int> CurrentGemChanged;

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
        m_currentGold = startGold;
        m_currentGem = startGem;
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
        return m_currentGold;
    }

    public int GetCurrentGem()
    {
        return m_currentGem;
    }

    public bool HasEnoughGold(int amount)
    {
        return m_currentGold >= amount;
    }

    public bool HasEnoughGem(float amount)
    {
        return m_currentGem >= amount;
    }

    #endregion

    #region Currency Changes

    public void AddGold(int amount)
    {
        m_currentGold += amount;
        GameStateVersion.MarkChanged();
        CurrentGoldChanged?.Invoke(m_currentGold);
    }

    public void AddGem(int amount)
    {
        m_currentGem += amount;
        GameStateVersion.MarkChanged();
        CurrentGemChanged?.Invoke(m_currentGem);
    }

    public void SpendGold(int amount)
    {
        if (HasEnoughGold(amount))
        {
            m_currentGold -= amount;
            GameStateVersion.MarkChanged();
            CurrentGoldChanged?.Invoke(m_currentGold);
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
            m_currentGem -= amount;
            GameStateVersion.MarkChanged();
            CurrentGemChanged?.Invoke(m_currentGem);
        }
        else
        {
            Debug.LogWarning("보석이 부족합니다! 필요한 보석: " + amount);
        }
    }

    #endregion
}
