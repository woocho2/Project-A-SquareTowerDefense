using System;
using UnityEngine;

[System.Serializable]
public struct TowerStats
{
    public int ID;
    public string Name;
    public int Level;
    public float AttackPower;
    public float Range;
    public float AttackSpeed;
    public float CriticalRate;
    public float CriticalDamage;
    public float Duration;
    public float AbilityValue;
    public float ProjectileSpeed;
    public float ProjectileRadius;
}

public class TowerController : MonoBehaviour
{
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TowerStats m_baseStats;
    [SerializeField] private GameObject m_range;

    private TowerAttackAction m_attackAction;
    private float m_attackCooldown;

    [Header("Debuff Zone Reference")]
    [SerializeField] private DebuffZone m_debuffZoneChild;

    // 버프 배율 변수들
    private float m_bonusAttackPower = 1f;
    private float m_bonusRange = 1f;
    private float m_bonusAttackSpeed = 1f;
    private float m_bonusCriticalRate = 1f;
    private float m_bonusCriticalDamage = 1f;
    private float m_bonusDuration = 0f;
    private float m_bonusAbilityValue = 1f;

    private Coroutine m_buffRoutine;

    private void Awake()
    {
        if (m_debuffZoneChild == null)
        {
            m_debuffZoneChild = GetComponentInChildren<DebuffZone>(true);
        }
    }

    public void Init(TowerData data, TowerStats initialStats)
    {
        m_towerData = data;
        m_baseStats = initialStats;

        if (m_debuffZoneChild != null)
        {
            m_debuffZoneChild.gameObject.SetActive(false);
        }

        switch (m_towerData.attackType)
        {
            case AttackType.Splash:
                m_attackAction = new SplashAttackAction(m_towerData);
                break;

            case AttackType.Target:
                m_attackAction = new TargetAttackAction(m_towerData);
                break;

            case AttackType.Buff:
                m_attackAction = new BuffAction(m_towerData);
                break;

            case AttackType.Debuff:
                var debuffAction = new DebuffAction(m_towerData);
                if (m_debuffZoneChild != null)
                {
                    debuffAction.BindZone(m_debuffZoneChild, transform.position, GetFinalStats());
                }
                m_attackAction = debuffAction;
                break;
        }
    }

    public void UpdateBaseStats(TowerStats newStats)
    {
        m_baseStats = newStats;
    }

    /// <summary>
    /// 업그레이드된 전역 스탯(GlobalStats)을 직접 가져와 현재 버프 배율만 즉시 연산하여 반환합니다.
    /// </summary>
    public TowerStats GetFinalStats()
    {
        // 1. 매니저의 최신 업그레이드 스탯을 직접 조회
        TowerStats finalStats = m_baseStats;

        // 2. 개별 버프 배율 연산
        finalStats.AttackPower *= Mathf.Clamp(m_bonusAttackPower, 0.01f, 10000f);
        finalStats.AttackSpeed *= Mathf.Clamp(m_bonusAttackSpeed, 0.01f, 10000f);
        finalStats.Range *= Mathf.Clamp(m_bonusRange, 0.01f, 10000f);
        finalStats.CriticalRate *= Mathf.Clamp(m_bonusCriticalRate, 0.01f, 10000f);
        finalStats.CriticalDamage *= Mathf.Clamp(m_bonusCriticalDamage, 0.01f, 10000f);
        finalStats.Duration += m_bonusDuration;
        finalStats.AbilityValue *= Mathf.Clamp(m_bonusAbilityValue, 0.01f, 10000f);

        if (finalStats.Range <= 1f) finalStats.Range = 1f;
        if (finalStats.CriticalRate >= 1f) finalStats.CriticalRate = 1f;

        return finalStats;
    }

    private void Update()
    {
        if (m_attackAction == null) return;

        m_attackCooldown -= Time.deltaTime;

        if (m_attackCooldown <= 0f)
        {
            TowerStats finalStats = GetFinalStats();
            bool didAction = m_attackAction.ExecuteAction(transform, finalStats);

            if (didAction)
            {
                m_attackCooldown = finalStats.AttackSpeed > 0f ? 1f / finalStats.AttackSpeed : 1f;
            }
        }
    }

    public TowerData GetTowerData() => m_towerData;

    public void ShowRange(bool show)
    {
        if (m_range != null)
        {
            m_range.SetActive(show);

            if (show)
            {
                float currentRange = GetFinalStats().Range;
                float scaleValue = (currentRange * 2f) / transform.localScale.x;
                m_range.transform.localScale = new Vector3(scaleValue, scaleValue, 1f);
            }
        }
    }

    public void OnMovedToNewPosition()
    {
        if (m_attackAction is DebuffAction && m_debuffZoneChild != null)
        {
            m_debuffZoneChild.UpdateTowerPosition(transform.position, GetFinalStats().Range);
        }
    }

    public void ApplyBuff(BuffTarget target, float abilityValue, float duration)
    {
        if (m_buffRoutine != null) StopCoroutine(m_buffRoutine);

        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f + (abilityValue / 100f); break;
            case BuffTarget.Bow: m_bonusAttackSpeed = 1f + (abilityValue / 100f); break;
            case BuffTarget.Spear: m_bonusRange = 1f + (abilityValue / 100f); break;
            case BuffTarget.Axe: m_bonusCriticalRate = 1f + (abilityValue / 100f); break;
            case BuffTarget.Hammer: m_bonusCriticalDamage = 1f + (abilityValue / 100f); break;
        }

        m_buffRoutine = StartCoroutine(RemoveBuffRoutine(target, duration));
    }

    private System.Collections.IEnumerator RemoveBuffRoutine(BuffTarget target, float duration)
    {
        yield return new WaitForSeconds(duration);

        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f; break;
            case BuffTarget.Bow: m_bonusAttackSpeed = 1f; break;
            case BuffTarget.Spear: m_bonusRange = 1f; break;
            case BuffTarget.Axe: m_bonusCriticalRate = 1f; break;
            case BuffTarget.Hammer: m_bonusCriticalDamage = 1f; break;
        }

        m_buffRoutine = null;
    }
}