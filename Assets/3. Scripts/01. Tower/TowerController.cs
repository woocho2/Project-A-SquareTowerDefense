using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    private static int s_nextCreationOrder;

    public int CreationOrder { get; private set; }
    public int SpawnIndex { get; internal set; } = -1;
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TowerStats m_baseStats;
    [SerializeField] private GameObject m_range;
    [SerializeField] private TowerVisual m_towerVisual;
    [SerializeField] private TowerSelectionVisual m_selectionVisual;

    private TowerAttackAction m_attackAction;
    private int m_remainingAction;
    private TargetPriority m_targetPriority = TargetPriority.Closest;

    [Header("Debuff Zone Reference")]
    [SerializeField] private DebuffZone m_debuffZoneChild;

    [Header("Projectile Satellite Attack")]
    [Tooltip("위성 자리 하나를 생성하고 다음 자리를 생성하거나 발사하기까지의 간격입니다.")]
    [SerializeField, Min(0f)] private float m_projectileSatelliteInterval = 0.3f;
    [Tooltip("모든 투사체 위성을 생성한 뒤 첫 번째 묶음을 발사하기 전까지 기다리는 시간입니다.")]
    [SerializeField, Min(0f)] private float m_projectileSatelliteLaunchDelay = 0.5f;
    [Tooltip("타워 중심에서 투사체 위성까지의 거리입니다.")]
    [SerializeField, Min(0f)] private float m_projectileSatelliteRadius = 0.9f;
    [Tooltip("한 위성 자리에 여러 투사체가 생성될 때 서로 겹치지 않게 벌리는 간격입니다.")]
    [SerializeField, Min(0f)] private float m_projectileGroupSpacing = 0.12f;
    [Tooltip("발사 연출이 중단됐을 때 대기 중인 투사체를 자동 회수하기 위한 여유 시간입니다.")]
    [SerializeField, Min(1f)] private float m_projectilePreparationSafetyTime = 5f;

    // 위성을 만들고 쏘는 중이면 true. 턴 진행은 이 값과 남은 투사체 수로 공격이 끝났는지 판단합니다.
    private bool m_isAttackInProgress;
    public bool IsAttackInProgress => m_isAttackInProgress;

    // 버프는 타워에 직접 걸지 않습니다. 버프 타워가 TileManager의 타일에 기록하고, 타워는 자기 타일의 기록을 읽습니다.
    // 스택형 버프(스킬 1 + 스킬 2 틀)의 상태입니다. 타일이 아니라 버프를 받는 타워가 문양별로 하나씩 가집니다.
    private readonly Dictionary<BuffTarget, TowerBuffStatus> m_buffStatuses = new Dictionary<BuffTarget, TowerBuffStatus>();
    // 버프 타워의 한 번 행동을 한 번만 받도록, 시전자별로 마지막에 받은 적용 번호를 기억합니다.
    private readonly Dictionary<int, int> m_appliedBuffVersions = new Dictionary<int, int>();

    // 문양별 스킬입니다. 스킬이 없는 타워는 null입니다.
    private TowerSkill m_skill;

    public TowerStats BaseStats => m_baseStats;

    // 같은 문양의 타워끼리 시너지를 낼 수 있도록, 이 타워가 받고 있는 버프 상태를 문양과 무관한 형태로 알려줍니다.
    /// <summary>해당 버프의 스킬 1(불이면 예열)이 켜져 있는지 여부입니다.</summary>
    public bool IsBuffActive(BuffTarget target) =>
        m_buffStatuses.TryGetValue(target, out TowerBuffStatus status) && status.IsSkill1Active;
    /// <summary>해당 버프의 스킬 2(불이면 과열)가 발동 중인지 여부입니다.</summary>
    public bool IsBuffTriggered(BuffTarget target) =>
        m_buffStatuses.TryGetValue(target, out TowerBuffStatus status) && status.IsSkill2Active;
    /// <summary>해당 버프의 누적 스택 수입니다.</summary>
    public int GetBuffStack(BuffTarget target) =>
        m_buffStatuses.TryGetValue(target, out TowerBuffStatus status) ? status.Stack : 0;

    private void Awake()
    {
        CreationOrder = ++s_nextCreationOrder;
        if (m_towerVisual == null)
        {
            m_towerVisual = GetComponentInChildren<TowerVisual>(true);
        }

        if (m_selectionVisual == null)
        {
            m_selectionVisual = GetComponent<TowerSelectionVisual>();
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
        m_targetPriority = NormalizeTargetPriority(m_towerData.targetPriority);

        // 타워가 생성된 셀의 행동력 타일 효과까지 반영해 첫 행동력부터 맞춥니다.
        m_remainingAction = GetFinalAction();

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
                    debuffAction.BindZone(m_debuffZoneChild, transform);
                }
                m_attackAction = debuffAction;
                break;

            // 시너지 타워는 생성·표시 전용이다. 공격/버프/디버프 행동을 만들지 않는다.
            case AttackType.None:
                m_attackAction = null;
                break;
        }

        m_attackAction?.SetTargetPriority(m_targetPriority);
        m_skill = TowerSkill.Create(this, m_towerData, m_attackAction);
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

        // 버프·디버프 타워는 어떤 버프와 맵 타일 효과도 받지 않고 기본 스탯 그대로 행동합니다.
        if (IsSupportTower())
        {
            if (finalStats.Range < TowerAttackAction.WorldUnitsPerTile)
            {
                finalStats.Range = TowerAttackAction.WorldUnitsPerTile;
            }
            return finalStats;
        }

        // 2. 타일 효과와 버프를 반영합니다.
        finalStats.AttackCount = Mathf.Max(1, finalStats.AttackCount);

        // 타워 스폰 타일 효과는 타워의 현재 월드 위치를 기준으로 매번 계산합니다.
        // 따라서 생성, 이동, 교체(티어/컬러 강화) 후에도 별도 버프 해제 과정 없이 즉시 반영됩니다.
        TowerTileBuffType tileBuff = GetCurrentTowerTileBuff();
        switch (tileBuff)
        {
            case TowerTileBuffType.AttackPowerUp:
                finalStats.AttackPower *= 1.5f;
                break;

            case TowerTileBuffType.AttackCountUp:
                finalStats.AttackCount += 1;
                break;
        }

        ApplyCurrentTileBuffTowerEffects(ref finalStats);
        ApplyStackBuffEffects(ref finalStats);
        m_skill?.ModifyFinalStats(ref finalStats);

        if (finalStats.Range < TowerAttackAction.WorldUnitsPerTile)
        {
            finalStats.Range = TowerAttackAction.WorldUnitsPerTile;
        }
        finalStats.CriticalRate = Mathf.Clamp01(finalStats.CriticalRate);
        finalStats.ArmorPenetrationPercent = Mathf.Clamp(finalStats.ArmorPenetrationPercent, 0f, 100f);

        return finalStats;
    }

    /// <summary>
    /// 에너미 턴에 한 번 호출됩니다. 행동력을 1 소모하고, 0이 된 턴에 행동한 뒤 행동력을 재충전합니다.
    /// 타워는 최종 스탯을 만들어 행동(TowerAttackAction)에 넘기기만 합니다.
    /// 누구를 어떻게 공격하고 피해를 얼마로 계산할지는 행동 스크립트가 정합니다.
    /// 공격 타워는 사거리에 적이 없으면 행동력 0인 채로 대기하다가, 적이 들어온 턴에 바로 공격합니다.
    /// </summary>
    public bool ExecuteTurnAction()
    {
        if (m_attackAction == null || m_towerData == null) return false;

        if (m_remainingAction > 0) m_remainingAction--;
        if (m_remainingAction > 0) return false;

        TowerStats finalStats = GetFinalStats();

        if (m_attackAction.UsesProjectileSatellites)
        {
            if (!m_attackAction.HasTargetInRange(transform, finalStats))
            {
                return false;
            }

            m_isAttackInProgress = true;
            StartCoroutine(RunProjectileAttack(finalStats));
            ConsumeBuffAction();
            m_remainingAction = GetFinalAction();
            return true;
        }

        // 버프·디버프 타워는 한 번 행동할 때 한 번만 실행합니다.
        bool didAct = m_attackAction.ExecuteAction(transform, finalStats);

        m_remainingAction = GetFinalAction();
        return didAct;
    }

    // 투사체를 띄우고 쏘는 과정은 행동 스크립트가 진행합니다. 타워는 끝났는지만 기억합니다.
    private IEnumerator RunProjectileAttack(TowerStats finalStats)
    {
        yield return m_attackAction.SatelliteAttackRoutine(transform, finalStats, new ProjectileSatelliteSettings
        {
            Interval = m_projectileSatelliteInterval,
            LaunchDelay = m_projectileSatelliteLaunchDelay,
            Radius = m_projectileSatelliteRadius,
            GroupSpacing = m_projectileGroupSpacing,
            PreparationSafetyTime = m_projectilePreparationSafetyTime
        });

        m_isAttackInProgress = false;
    }

    private void OnDisable()
    {
        // 비활성화되면 발사 코루틴이 멈추므로, 턴 진행이 끝나지 않는 공격을 기다리지 않게 합니다.
        m_isAttackInProgress = false;
    }

    public TowerData GetTowerData() => m_towerData;
    public void InheritCreationOrder(int creationOrder) => CreationOrder = creationOrder;

    /// <summary>티어 강화로 교체될 때, 이전 타워가 받고 있던 스택형 버프(스킬 1·스킬 2, 스택)를 그대로 이어받습니다.</summary>
    public void InheritBuffStatuses(TowerController previous)
    {
        if (previous == null) return;

        foreach (KeyValuePair<BuffTarget, TowerBuffStatus> pair in previous.m_buffStatuses)
        {
            m_buffStatuses[pair.Key] = pair.Value;
        }
        foreach (KeyValuePair<int, int> pair in previous.m_appliedBuffVersions)
        {
            m_appliedBuffVersions[pair.Key] = pair.Value;
        }
    }
    public DebuffZone GetDebuffZone() => m_towerData != null && m_towerData.attackType == AttackType.Debuff
        ? m_debuffZoneChild : null;

    public bool TryGetDebuffZoneIndex(out int index)
    {
        index = -1;
        return GetDebuffZone() != null && m_debuffZoneChild.TryGetPlacedPathIndex(out index);
    }

    public bool TrySetDebuffZoneIndex(int index)
    {
        return GetDebuffZone() != null && m_debuffZoneChild.SetPlacedPathIndex(index);
    }

    public void SetSelected(bool selected)
    {
        m_selectionVisual?.SetHighlighted(selected);
        ShowRange(selected);
        GetDebuffZone()?.SetHighlighted(selected);
    }
    public int GetRemainingAction() => m_remainingAction;
    public int GetMaxAction() => GetFinalAction();
    public TargetPriority GetTargetPriority() => m_targetPriority;
    public bool CanChangeTargetPriority => m_towerData != null &&
        (m_towerData.attackType == AttackType.Target || m_towerData.attackType == AttackType.Splash);

    public void SetTargetPriority(TargetPriority priority)
    {
        m_targetPriority = NormalizeTargetPriority(priority);
        m_attackAction?.SetTargetPriority(m_targetPriority);
        GameStateVersion.MarkChanged();
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
        // 소유 타워가 이동해도 디버프 장판은 기존 패스 타일에 남습니다.
        // 행동력 감소 타일로 이동한 경우, 이미 충전되어 있던 행동력도 새 최대치 안으로 맞춥니다.
        m_remainingAction = Mathf.Min(m_remainingAction, GetFinalAction());
    }

    private void OnDestroy()
    {
        TileManager.Instance?.RemoveTowerIfMatches(SpawnIndex, this);
        TileManager.Instance?.RemoveAllDynamicEffectsBySource(transform.GetInstanceID());

        // 디버프 존은 타워와 분리되어 있으므로, 타워가 판매·합성·파괴될 때 함께 정리합니다.
        if (m_debuffZoneChild != null && m_debuffZoneChild.transform.parent != transform)
        {
            Destroy(m_debuffZoneChild.gameObject);
        }
    }

    private int GetFinalAction()
    {
        if (m_towerData == null) return 1;
        if (IsSupportTower()) return Mathf.Max(1, m_towerData.action);

        int tileActionReduction = GetCurrentTowerTileBuff() == TowerTileBuffType.ActionCountUp ? 1 : 0;
        tileActionReduction += GetCurrentTileBuffActionReduction();
        return Mathf.Max(1, m_towerData.action - tileActionReduction);
    }

    /// <summary>
    /// 현재 타워가 서 있는 SpawnPoint 셀의 맵 버프를 조회합니다.
    /// TileManager의 Dictionary가 실제 버프 데이터의 기준이며, MapBuff 이펙트는 시각 전용입니다.
    /// </summary>
    private TowerTileBuffType GetCurrentTowerTileBuff()
    {
        if (TileManager.Instance == null || SpawnIndex < 1)
        {
            return TowerTileBuffType.Normal;
        }

        return TileManager.Instance.GetTowerTileBuffAt(SpawnIndex);
    }

    /// <summary>
    /// 현재 스폰 타일에 기록된 버프 중 BuffTarget별 최고 티어 한 개만 적용합니다.
    /// 높은 버프가 사라지면 남아 있는 다음 최고 티어 버프가 자동으로 적용됩니다.
    /// </summary>
    private void ApplyCurrentTileBuffTowerEffects(ref TowerStats finalStats)
    {
        if (TileManager.Instance == null || SpawnIndex < 1) return;

        IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(SpawnIndex);
        Dictionary<BuffTarget, TowerTileBuffEffect> strongestByTarget = GetStrongestTileBuffs(effects);

        foreach (KeyValuePair<BuffTarget, TowerTileBuffEffect> pair in strongestByTarget)
        {
            TowerTileBuffEffect effect = pair.Value;
            int tier = effect.Tier;
            switch (pair.Key)
            {
                case BuffTarget.Sword: finalStats.AttackPower *= 1f + GetTierValue(tier, 10f, 20f, 40f, 100f, 200f) / 100f; break;
                case BuffTarget.Bow:
                    finalStats.Range += TowerAttackAction.WorldUnitsPerTile;
                    finalStats.DistanceDamageBonusPercent += GetBowDistanceDamageBonus(tier);
                    break;
                // Fire is stack-based and is applied below from the target-owned status.
                case BuffTarget.Fire: break;
                case BuffTarget.AttackCount: finalStats.AttackCount += Mathf.RoundToInt(effect.AbilityValue); break;
                case BuffTarget.Spear: finalStats.CriticalRate += GetTierValue(tier, .01f, .025f, .05f, .10f, .20f); break;
                case BuffTarget.Axe: finalStats.CriticalDamage += GetTierValue(tier, .25f, .50f, 1f, 2f, 4f); break;
                case BuffTarget.Hammer: finalStats.ArmorPenetrationPercent += GetTierValue(tier, 2f, 7.5f, 15f, 25f, 50f); break;
                case BuffTarget.Ice:
                    if (m_towerData != null && m_towerData.attackType == AttackType.Splash) finalStats.ProjectileRadius += tier;
                    else if (m_towerData != null && m_towerData.attackType == AttackType.Target) finalStats.IceAdditionalTargetCount += tier;
                    break;
                case BuffTarget.Electricity: finalStats.ExtraHitChance += GetTierValue(tier, 4f, 8f, 12.5f, 25f, 50f); break;
                case BuffTarget.Light: finalStats.ChainCount += tier; break;
                case BuffTarget.Darkness: ApplyTileDarknessBuff(ref finalStats); break;
            }
        }
    }

    /// <summary>
    /// 이 타워의 타일에 기록된 스택형 버프를 읽어, 버프 타워의 행동 한 번마다 한 번씩만 적용합니다.
    /// 모든 버프·디버프 타워가 행동한 뒤에 호출되므로, 보드 순서와 무관하게 같은 턴의 버프를 받습니다.
    /// </summary>
    public void RefreshStackBuffs()
    {
        if (TileManager.Instance == null || SpawnIndex < 1 || IsSupportTower()) return;

        DropBuffStatusesOutOfRange();

        IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(SpawnIndex);
        for (int i = 0; i < effects.Count; i++)
        {
            TowerTileBuffEffect effect = effects[i];
            if (effect.ApplicationVersion <= 0) continue;

            if (m_appliedBuffVersions.TryGetValue(effect.SourceID, out int appliedVersion) &&
                appliedVersion == effect.ApplicationVersion)
            {
                continue;
            }

            if (!m_buffStatuses.TryGetValue(effect.Target, out TowerBuffStatus status))
            {
                status = TowerBuffStatus.Create(effect.Target);
                if (status == null) continue;
                m_buffStatuses.Add(effect.Target, status);
            }

            m_appliedBuffVersions[effect.SourceID] = effect.ApplicationVersion;
            status.OnApplied(effect);
        }
    }

    /// <summary>
    /// 이 타워가 한 번 행동했을 때 호출합니다. 버프와 스킬 버프의 유지는 턴이 아니라 행동 횟수로 세므로,
    /// 이번 행동에 쓴 최종 스탯을 만든 뒤에 남은 횟수를 하나 줄입니다.
    /// </summary>
    private void ConsumeBuffAction()
    {
        foreach (TowerBuffStatus status in m_buffStatuses.Values)
        {
            status.OnOwnerActed();
        }
        m_skill?.OnOwnerActed();
    }

    /// <summary>
    /// 스택형 버프는 그 문양의 버프 타워가 존재하고 이 타워가 사거리 안에 있을 때만 유지됩니다.
    /// 사거리를 벗어나거나 버프 타워가 사라지면 스킬 1·스킬 2와 스택을 모두 지우고, 다시 받을 때 처음부터 쌓습니다.
    /// 유지되는 버프의 세기는 사거리 안의 버프 타워가 지금 가진 스탯에 맞춥니다 (버프 타워를 강화하면 함께 오릅니다).
    /// 적 턴마다 타워가 자기 타일의 버프를 읽는 시점(RefreshStackBuffs)에만 판정합니다.
    /// 플레이어 턴에 타워를 잠깐 사거리 밖으로 옮겼다가 되돌려도 버프가 지워지지 않습니다.
    /// </summary>
    private void DropBuffStatusesOutOfRange()
    {
        if (m_buffStatuses.Count == 0 || TileManager.Instance == null || SpawnIndex < 1) return;

        List<BuffTarget> lostTargets = null;
        foreach (KeyValuePair<BuffTarget, TowerBuffStatus> pair in m_buffStatuses)
        {
            if (TileManager.Instance.TryGetStrongestBuffTowerStatsInRange(SpawnIndex, pair.Key, out TowerStats sourceStats))
            {
                pair.Value.SyncWithSource(sourceStats.AttackPower, sourceStats.AbilityValue);
                continue;
            }

            if (lostTargets == null) lostTargets = new List<BuffTarget>();
            lostTargets.Add(pair.Key);
        }

        if (lostTargets == null) return;
        for (int i = 0; i < lostTargets.Count; i++)
        {
            m_buffStatuses.Remove(lostTargets[i]);
        }
    }

    private void ApplyStackBuffEffects(ref TowerStats finalStats)
    {
        foreach (TowerBuffStatus status in m_buffStatuses.Values)
        {
            status.ModifyFinalStats(ref finalStats);
        }
    }

    /// <summary>적 턴이 시작될 때 한 번 호출됩니다. 스킬이 가진 지속 턴을 줄입니다.</summary>
    public void AdvanceSkillTurn()
    {
        m_skill?.OnEnemyTurnStart();
    }

    /// <summary>
    /// 적 턴마다 공격 타워의 기본 공격 직전에 한 번 호출됩니다. 스킬이 투사체를 발사했으면 true를 반환합니다.
    /// </summary>
    public bool TryUseSkill()
    {
        return m_skill != null && m_skill.OnAttackPhase(GetFinalStats());
    }

    /// <summary>이 타워의 기본 공격 투사체가 노린 대상에 명중했을 때 투사체가 호출합니다.</summary>
    public void NotifyPrimaryTargetHit()
    {
        m_skill?.OnPrimaryTargetHit();
    }

    private bool IsSupportTower()
    {
        return m_towerData != null &&
            (m_towerData.attackType == AttackType.Buff || m_towerData.attackType == AttackType.Debuff);
    }

    private int GetCurrentTileBuffActionReduction()
    {
        if (TileManager.Instance == null || SpawnIndex < 1) return 0;
        Dictionary<BuffTarget, TowerTileBuffEffect> strongestByTarget = GetStrongestTileBuffs(TileManager.Instance.GetTowerBuffEffectsAt(SpawnIndex));
        return strongestByTarget.TryGetValue(BuffTarget.Wind, out TowerTileBuffEffect wind) ? wind.Tier : 0;
    }

    private static Dictionary<BuffTarget, TowerTileBuffEffect> GetStrongestTileBuffs(IReadOnlyList<TowerTileBuffEffect> effects)
    {
        Dictionary<BuffTarget, TowerTileBuffEffect> strongest = new Dictionary<BuffTarget, TowerTileBuffEffect>();
        for (int i = 0; i < effects.Count; i++)
        {
            TowerTileBuffEffect candidate = effects[i];
            if (!strongest.TryGetValue(candidate.Target, out TowerTileBuffEffect current) || candidate.Tier > current.Tier)
            {
                strongest[candidate.Target] = candidate;
            }
        }
        return strongest;
    }

    private static void ApplyTileDarknessBuff(ref TowerStats finalStats)
    {
        float abilityValue = TowerManager.Instance != null ? TowerManager.Instance.GetSharedDarknessAbility() : 0f;
        int milestones = Mathf.FloorToInt(abilityValue / 50f);
        for (int i = 0; i < milestones; i++)
        {
            switch (i % 5)
            {
                case 0: finalStats.AttackPower *= 1.1f; break;
                case 1: finalStats.AttackCount += 1; break;
                case 2: finalStats.Range += TowerAttackAction.WorldUnitsPerTile; break;
                case 3: finalStats.CriticalRate += .1f; break;
                case 4: finalStats.CriticalDamage += .5f; break;
            }
        }
    }

    /// <summary>
    /// 이 타워가 적을 처치했을 때 호출됩니다. 어둠 버프가 기록된 타일 위에 서 있으면 모든 어둠 타워가 공유하는 성장치를 올립니다.
    /// </summary>
    public void NotifyEnemyKilled()
    {
        if (TileManager.Instance == null || SpawnIndex < 1 || TowerManager.Instance == null) return;

        Dictionary<BuffTarget, TowerTileBuffEffect> tileBuffs = GetStrongestTileBuffs(TileManager.Instance.GetTowerBuffEffectsAt(SpawnIndex));
        if (!tileBuffs.TryGetValue(BuffTarget.Darkness, out TowerTileBuffEffect tileDarkness)) return;

        TowerManager.Instance.AddSharedDarknessAbility(tileDarkness.Tier);
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
}
