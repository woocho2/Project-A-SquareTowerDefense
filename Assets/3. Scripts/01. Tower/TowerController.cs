using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct TowerStats
{
    public int ID;
    public string Name;
    public int Level;
    public float AttackPower;
    public float Range;
    [FormerlySerializedAs("AttackSpeed")]
    public int AttackCount;
    public float CriticalRate;
    public float CriticalDamage;
    public float Duration;
    public float AbilityValue;
    public float ProjectileSpeed;
    public int ProjectileRadius;
    public int AdditionalHitCount;
    public float DistanceDamageBonusPercent;
    public float ArmorPenetrationPercent;
    public int IceAdditionalTargetCount;
    public int ChainCount;
    public float ExtraHitChance;
}

public class TowerController : MonoBehaviour
{
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TowerStats m_baseStats;
    [SerializeField] private GameObject m_range;
    [SerializeField] private TowerVisual m_towerVisual;

    private TowerAttackAction m_attackAction;
    private int m_remainingAction;
    private TargetPriority m_targetPriority = TargetPriority.Closest;

    [Header("Debuff Zone Reference")]
    [SerializeField] private DebuffZone m_debuffZoneChild;

    // 런타임 보정치
    private float m_bonusAttackPower = 1f;
    private float m_bonusRange;
    private int m_bonusAttackCount;
    private int m_bonusActionReduction;
    private float m_bonusDistanceDamagePercent;
    private float m_bonusCriticalRate;
    private float m_bonusCriticalDamage;
    private float m_bonusDuration = 0f;
    private float m_bonusAbilityValue = 1f;
    private float m_bonusArmorPenetrationPercent;
    private int m_bonusProjectileRadius;
    private int m_bonusIceAdditionalTargetCount;
    private int m_bonusChainCount;
    private float m_bonusExtraHitChance;

    private float m_darknessBonusAttackPower;
    private int m_darknessBonusAttackCount;
    private float m_darknessBonusRange;
    private float m_darknessBonusCriticalRate;
    private float m_darknessBonusCriticalDamage;

    private sealed class ActiveBuff
    {
        public int SourceID;
        public TowerController SourceTower;
        public int Tier;
        public float AbilityValue;
        public int RemainingTurns;
    }

    // 버프 종류별로 시전자 효과를 따로 저장합니다. 같은 종류는 가장 높은 수치만 반영합니다.
    private readonly Dictionary<BuffTarget, Dictionary<int, ActiveBuff>> m_activeBuffs =
        new Dictionary<BuffTarget, Dictionary<int, ActiveBuff>>();

    private void Awake()
    {
        if (m_towerVisual == null)
        {
            m_towerVisual = GetComponentInChildren<TowerVisual>(true);
        }

        if (m_debuffZoneChild == null)
        {
            m_debuffZoneChild = GetComponentInChildren<DebuffZone>(true);
        }
    }

    public void Init(TowerData data, TowerStats initialStats)
    {
        m_towerData = data;
        m_baseStats = initialStats;
        m_remainingAction = Mathf.Max(1, m_towerData.action);
        m_targetPriority = NormalizeTargetPriority(m_towerData.targetPriority);

        m_towerVisual?.Apply(m_towerData);

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
                    debuffAction.BindZone(m_debuffZoneChild, transform, GetFinalStats());
                }
                m_attackAction = debuffAction;
                break;
        }

        m_attackAction?.SetTargetPriority(m_targetPriority);
    }

    public void UpdateBaseStats(TowerStats newStats)
    {
        m_baseStats = newStats;
    }

    /// <summary>
    /// 전역 기본 스탯에 런타임 보정치를 반영한 최종 스탯을 반환합니다.
    /// </summary>
    public TowerStats GetFinalStats()
    {
        // 1. 전역 기본 스탯을 복사합니다.
        TowerStats finalStats = m_baseStats;

        // 2. 버프와 타일 보정치를 반영합니다.
        finalStats.AttackPower *= Mathf.Clamp(m_bonusAttackPower * (1f + m_darknessBonusAttackPower), 0.01f, 10000f);
        finalStats.AttackCount = Mathf.Max(1, finalStats.AttackCount + m_bonusAttackCount + m_darknessBonusAttackCount);
        finalStats.Range += m_bonusRange + m_darknessBonusRange;
        finalStats.CriticalRate += m_bonusCriticalRate + m_darknessBonusCriticalRate;
        finalStats.CriticalDamage += m_bonusCriticalDamage + m_darknessBonusCriticalDamage;
        finalStats.Duration += m_bonusDuration;
        finalStats.AbilityValue *= Mathf.Clamp(m_bonusAbilityValue, 0.01f, 10000f);
        finalStats.DistanceDamageBonusPercent += m_bonusDistanceDamagePercent;
        finalStats.ArmorPenetrationPercent += m_bonusArmorPenetrationPercent;
        finalStats.ProjectileRadius += m_bonusProjectileRadius;
        finalStats.IceAdditionalTargetCount += m_bonusIceAdditionalTargetCount;
        finalStats.ChainCount += m_bonusChainCount;
        finalStats.ExtraHitChance += m_bonusExtraHitChance;

        if (finalStats.Range < TowerAttackAction.WorldUnitsPerTile)
        {
            finalStats.Range = TowerAttackAction.WorldUnitsPerTile;
        }
        finalStats.CriticalRate = Mathf.Clamp01(finalStats.CriticalRate);
        finalStats.ArmorPenetrationPercent = Mathf.Clamp(finalStats.ArmorPenetrationPercent, 0f, 100f);

        return finalStats;
    }

    /// <summary>
    /// 에너미 턴에 한 번 호출됩니다. 행동력을 1 소모하고,
    /// 0이 된 턴에 AttackCount만큼 공격한 뒤 행동력을 재충전합니다.
    /// </summary>
    public bool ExecuteTurnAction()
    {
        if (m_attackAction == null || m_towerData == null) return false;

        m_remainingAction--;
        if (m_remainingAction > 0) return false;

        TowerStats finalStats = GetFinalStats();
        bool didAttack = false;
        for (int i = 0; i < finalStats.AttackCount; i++)
        {
            didAttack |= m_attackAction.ExecuteAction(transform, finalStats);
        }

        m_remainingAction = GetFinalAction();
        return didAttack;
    }
    public TowerData GetTowerData() => m_towerData;
    public int GetRemainingAction() => m_remainingAction;
    public int GetMaxAction() => GetFinalAction();
    public TargetPriority GetTargetPriority() => m_targetPriority;
    public bool CanChangeTargetPriority => m_towerData != null &&
        (m_towerData.attackType == AttackType.Target || m_towerData.attackType == AttackType.Splash);

    public void SetTargetPriority(TargetPriority priority)
    {
        m_targetPriority = NormalizeTargetPriority(priority);
        m_attackAction?.SetTargetPriority(m_targetPriority);
    }

    private static TargetPriority NormalizeTargetPriority(TargetPriority priority)
    {
        return priority == TargetPriority.Default ? TargetPriority.Closest : priority;
    }

    public void ShowRange(bool show)
    {
        // 디버프 타워는 고정 장판이 범위를 대신 보여주므로 사거리 원형은 표시하지 않습니다.
        if (m_towerData != null && m_towerData.attackType == AttackType.Debuff)
        {
            if (m_range != null) m_range.SetActive(false);
            return;
        }

        if (m_range != null)
        {
            m_range.SetActive(show);

            if (show)
            {
                int tileRange = TowerAttackAction.ToTileRange(GetFinalStats().Range);
                float scaleValue = (tileRange * 2f + 1f) * TowerAttackAction.WorldUnitsPerTile / transform.localScale.x;
                m_range.transform.localScale = new Vector3(scaleValue, scaleValue, 1f);
            }
        }
    }

    public void OnMovedToNewPosition()
    {
        // 디버프 존은 최초로 배치된 길목에 남습니다.
    }

    private void OnDestroy()
    {
        // 디버프 존은 타워와 분리되어 있으므로, 타워가 판매·합성·파괴될 때 함께 정리합니다.
        if (m_debuffZoneChild != null && m_debuffZoneChild.transform.parent != transform)
        {
            Destroy(m_debuffZoneChild.gameObject);
        }
    }

    private int GetFinalAction()
    {
        return Mathf.Max(1, m_towerData.action - m_bonusActionReduction);
    }

    public void ApplyBuff(BuffTarget target, TowerController sourceTower, int sourceID, int sourceTier, float abilityValue, float duration)
    {
        if (target == BuffTarget.None) return;

        if (!m_activeBuffs.TryGetValue(target, out Dictionary<int, ActiveBuff> buffsBySource))
        {
            buffsBySource = new Dictionary<int, ActiveBuff>();
            m_activeBuffs.Add(target, buffsBySource);
        }

        buffsBySource[sourceID] = new ActiveBuff
        {
            SourceID = sourceID,
            SourceTower = sourceTower,
            Tier = Mathf.Clamp(sourceTier, 1, 5),
            AbilityValue = abilityValue,
            RemainingTurns = Mathf.Max(1, Mathf.RoundToInt(duration))
        };

        RefreshBuff(target);
    }

    /// <summary>
    /// 에너미 턴 시작 시 한 번 호출됩니다. 각 버프의 남은 턴을 줄이고 최고 수치를 다시 선택합니다.
    /// </summary>
    public void AdvanceBuffTurn()
    {
        List<BuffTarget> targetsToRefresh = new List<BuffTarget>(m_activeBuffs.Keys);

        foreach (BuffTarget target in targetsToRefresh)
        {
            Dictionary<int, ActiveBuff> buffsBySource = m_activeBuffs[target];
            List<int> expiredSources = new List<int>();

            foreach (KeyValuePair<int, ActiveBuff> pair in buffsBySource)
            {
                pair.Value.RemainingTurns--;
                if (pair.Value.RemainingTurns <= 0)
                {
                    expiredSources.Add(pair.Key);
                }
            }

            foreach (int sourceID in expiredSources)
            {
                buffsBySource.Remove(sourceID);
            }

            if (buffsBySource.Count == 0)
            {
                m_activeBuffs.Remove(target);
            }

            RefreshBuff(target);
        }
    }

    /// <summary>
    /// 이 타워가 적을 처치했을 때, 적용 중인 최상위 Darkness 버프의 성장치를 누적합니다.
    /// </summary>
    public void NotifyEnemyKilled()
    {
        if (!m_activeBuffs.TryGetValue(BuffTarget.Darkness, out Dictionary<int, ActiveBuff> darknessBuffs) || darknessBuffs.Count == 0)
        {
            return;
        }

        ActiveBuff strongestDarkness = null;
        foreach (ActiveBuff buff in darknessBuffs.Values)
        {
            if (strongestDarkness == null || GetBuffStrength(BuffTarget.Darkness, buff) > GetBuffStrength(BuffTarget.Darkness, strongestDarkness))
            {
                strongestDarkness = buff;
            }
        }

        if (strongestDarkness != null)
        {
            TowerManager.Instance?.AddSharedDarknessAbility(strongestDarkness.Tier);
        }
    }

    public void RefreshExternalBuff(BuffTarget target)
    {
        RefreshBuff(target);
    }

    private void RefreshBuff(BuffTarget target)
    {
        if (!m_activeBuffs.TryGetValue(target, out Dictionary<int, ActiveBuff> buffsBySource) || buffsBySource.Count == 0)
        {
            ResetBuff(target);
            return;
        }

        ActiveBuff strongestBuff = null;
        foreach (ActiveBuff buff in buffsBySource.Values)
        {
            if (strongestBuff == null || GetBuffStrength(target, buff) > GetBuffStrength(target, strongestBuff))
            {
                strongestBuff = buff;
            }
        }

        ApplyStrongestBuff(target, strongestBuff);
    }

    private static float GetBuffStrength(BuffTarget target, ActiveBuff buff)
    {
        switch (target)
        {
            case BuffTarget.Darkness:
                return TowerManager.Instance != null ? TowerManager.Instance.GetSharedDarknessAbility() : 0f;
            default:
                return buff.Tier;
        }
    }

    private void ApplyStrongestBuff(BuffTarget target, ActiveBuff buff)
    {
        int tierValue = buff.Tier;

        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f + (GetTierValue(tierValue, 10f, 20f, 40f, 100f, 200f) / 100f); break;
            case BuffTarget.Bow:
                m_bonusRange = TowerAttackAction.WorldUnitsPerTile;
                m_bonusDistanceDamagePercent = GetBowDistanceDamageBonus(tierValue);
                break;
            case BuffTarget.Fire: m_bonusAttackCount = tierValue; break;
            case BuffTarget.Wind: m_bonusActionReduction = tierValue; break;
            case BuffTarget.AttackCount: m_bonusAttackCount = Mathf.RoundToInt(buff.AbilityValue); break;
            case BuffTarget.Spear: m_bonusCriticalRate = GetTierValue(tierValue, .01f, .025f, .05f, .10f, .20f); break;
            case BuffTarget.Axe: m_bonusCriticalDamage = GetTierValue(tierValue, .25f, .50f, 1f, 2f, 4f); break;
            case BuffTarget.Hammer: m_bonusArmorPenetrationPercent = GetTierValue(tierValue, 2f, 7.5f, 15f, 25f, 50f); break;
            case BuffTarget.Ice:
                ApplyIceBuff(tierValue);
                break;
            case BuffTarget.Electricity: m_bonusExtraHitChance = GetTierValue(tierValue, 4f, 8f, 12.5f, 25f, 50f); break;
            case BuffTarget.Light: m_bonusChainCount = tierValue; break;
            case BuffTarget.Darkness: ApplyDarknessBuff(buff); break;
        }

        // Wind가 새로 적용된 경우, 이미 충전 중인 행동력도 새 최대치 안으로 맞춥니다.
        if (target == BuffTarget.Wind)
        {
            m_remainingAction = Mathf.Min(m_remainingAction, GetFinalAction());
        }

    }

    private static float GetBowDistanceDamageBonus(int tier)
    {
        return tier switch
        {
            1 => 10f,
            2 => 25f,
            3 => 50f,
            4 => 100f,
            _ => 200f
        };
    }

    private static float GetTierValue(int tier, float tier1, float tier2, float tier3, float tier4, float tier5)
    {
        switch (Mathf.Clamp(tier, 1, 5))
        {
            case 1: return tier1;
            case 2: return tier2;
            case 3: return tier3;
            case 4: return tier4;
            default: return tier5;
        }
    }

    private void ApplyIceBuff(int tier)
    {
        if (m_towerData == null) return;

        if (m_towerData.attackType == AttackType.Splash)
        {
            m_bonusProjectileRadius = tier;
        }
        else if (m_towerData.attackType == AttackType.Target)
        {
            m_bonusIceAdditionalTargetCount = tier;
        }
    }

    private void ApplyDarknessBuff(ActiveBuff buff)
    {
        float abilityValue = TowerManager.Instance != null ? TowerManager.Instance.GetSharedDarknessAbility() : 0f;
        int milestones = Mathf.FloorToInt(abilityValue / 50f);

        m_darknessBonusAttackPower = 0f;
        m_darknessBonusAttackCount = 0;
        m_darknessBonusRange = 0f;
        m_darknessBonusCriticalRate = 0f;
        m_darknessBonusCriticalDamage = 0f;

        for (int i = 0; i < milestones; i++)
        {
            switch (i % 5)
            {
                case 0: m_darknessBonusAttackPower += .10f; break;
                case 1: m_darknessBonusAttackCount += 1; break;
                case 2: m_darknessBonusRange += TowerAttackAction.WorldUnitsPerTile; break;
                case 3: m_darknessBonusCriticalRate += .10f; break;
                case 4: m_darknessBonusCriticalDamage += .50f; break;
            }
        }
    }

    private void ResetBuff(BuffTarget target)
    {
        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f; break;
            case BuffTarget.Bow:
                m_bonusRange = 0f;
                m_bonusDistanceDamagePercent = 0f;
                break;
            case BuffTarget.Fire:
            case BuffTarget.AttackCount: m_bonusAttackCount = 0; break;
            case BuffTarget.Wind: m_bonusActionReduction = 0; break;
            case BuffTarget.Spear: m_bonusCriticalRate = 0f; break;
            case BuffTarget.Axe: m_bonusCriticalDamage = 0f; break;
            case BuffTarget.Hammer: m_bonusArmorPenetrationPercent = 0f; break;
            case BuffTarget.Ice:
                m_bonusProjectileRadius = 0;
                m_bonusIceAdditionalTargetCount = 0;
                break;
            case BuffTarget.Electricity: m_bonusExtraHitChance = 0f; break;
            case BuffTarget.Light: m_bonusChainCount = 0; break;
            case BuffTarget.Darkness:
                m_darknessBonusAttackPower = 0f;
                m_darknessBonusAttackCount = 0;
                m_darknessBonusRange = 0f;
                m_darknessBonusCriticalRate = 0f;
                m_darknessBonusCriticalDamage = 0f;
                break;
        }
    }
}
