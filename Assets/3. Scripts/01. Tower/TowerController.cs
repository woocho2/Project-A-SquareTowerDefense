using System;
using System.Collections;
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

    // Fire support is owned by the target tower, not by the tile.  This lets a
    // synergy tower use exactly the same Preheat/Overheat rules as a normal tower.
    private sealed class FirePreheatStack
    {
        public int RemainingTurns;
        public float AbilityValue;
    }

    private readonly Dictionary<int, int> m_appliedFirePreheatVersions = new Dictionary<int, int>();
    private readonly List<FirePreheatStack> m_firePreheatStacks = new List<FirePreheatStack>();
    private int m_fireOverheatRemainingTurns;
    private float m_fireOverheatAbilityValue;
    private int m_fireTargetHitCount;
    private float m_fireTargetCriticalRateBonus;
    private float m_fireTargetCriticalDamageBonus;
    private int m_fireSplashTurnCounter;

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
        m_targetPriority = NormalizeTargetPriority(m_towerData.targetPriority);

        // 타워가 생성된 셀의 행동력 타일 효과까지 반영해 첫 행동력부터 맞춥니다.
        m_remainingAction = GetFinalAction();

        m_towerVisual?.Apply(m_towerData.VisualTowerID);

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

            // 시너지 타워는 생성·표시 전용이다. 공격/버프/디버프 행동을 만들지 않는다.
            case AttackType.None:
                m_attackAction = null;
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
        ApplyFireStatusEffects(ref finalStats);

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

        if (m_attackAction.UsesProjectileSatellites)
        {
            if (!m_attackAction.HasTargetInRange(transform, finalStats))
            {
                m_remainingAction = GetFinalAction();
                return false;
            }

            StartCoroutine(ExecuteProjectileSatelliteAttack(finalStats));
            m_remainingAction = GetFinalAction();
            return true;
        }

        bool didAttack = false;
        for (int i = 0; i < finalStats.AttackCount; i++)
        {
            didAttack |= m_attackAction.ExecuteAction(transform, finalStats);
        }

        m_remainingAction = GetFinalAction();
        return didAttack;
    }

    private IEnumerator ExecuteProjectileSatelliteAttack(TowerStats finalStats)
    {
        int satelliteCount = Mathf.Max(1, finalStats.AttackCount);
        int projectilesPerSatellite = Mathf.Max(1, m_attackAction.GetProjectilesPerSatellite(finalStats));
        float sequenceDuration =
            m_projectileSatelliteInterval * Mathf.Max(0, (satelliteCount - 1) * 2) +
            m_projectileSatelliteLaunchDelay;
        float preparationLifetime = sequenceDuration + m_projectilePreparationSafetyTime;

        List<List<ProjectileHit2D>> satelliteGroups = new List<List<ProjectileHit2D>>(satelliteCount);

        // 12시 → 6시 → 9시 → 3시 → 1시 → 7시 → 11시 → 5시 순으로 생성합니다.
        for (int satelliteIndex = 0; satelliteIndex < satelliteCount; satelliteIndex++)
        {
            Vector3 satelliteOffset = GetSatelliteOffset(satelliteIndex);
            Vector3 satelliteCenter = transform.position + satelliteOffset;
            Vector3 tangentDirection = new Vector3(-satelliteOffset.y, satelliteOffset.x, 0f).normalized;
            List<ProjectileHit2D> group = new List<ProjectileHit2D>(projectilesPerSatellite);

            for (int projectileIndex = 0; projectileIndex < projectilesPerSatellite; projectileIndex++)
            {
                float centeredIndex = projectileIndex - (projectilesPerSatellite - 1) * 0.5f;
                Vector3 spawnPosition = satelliteCenter + tangentDirection * (centeredIndex * m_projectileGroupSpacing);
                ProjectileHit2D projectile = m_attackAction.PrepareSatelliteProjectile(spawnPosition, preparationLifetime);
                if (projectile != null) group.Add(projectile);
            }

            satelliteGroups.Add(group);

            if (satelliteIndex < satelliteCount - 1 && m_projectileSatelliteInterval > 0f)
            {
                yield return new WaitForSeconds(m_projectileSatelliteInterval);
            }
        }

        // 완성된 위성 배치를 잠시 보여준 뒤 발사를 시작해 전투 흐름을 읽기 쉽게 합니다.
        if (m_projectileSatelliteLaunchDelay > 0f)
        {
            yield return new WaitForSeconds(m_projectileSatelliteLaunchDelay);
        }

        // 생성된 자리 순서대로 같은 위치의 투사체를 한 묶음씩 발사합니다.
        for (int satelliteIndex = 0; satelliteIndex < satelliteGroups.Count; satelliteIndex++)
        {
            m_attackAction.LaunchSatelliteProjectiles(transform, finalStats, satelliteGroups[satelliteIndex]);

            if (satelliteIndex < satelliteGroups.Count - 1 && m_projectileSatelliteInterval > 0f)
            {
                yield return new WaitForSeconds(m_projectileSatelliteInterval);
            }
        }
    }

    private Vector3 GetSatelliteOffset(int satelliteIndex)
    {
        const float diagonal = 0.70710678f;
        Vector2[] orderedDirections =
        {
            Vector2.up,                         // 12시
            Vector2.down,                       // 6시
            Vector2.left,                       // 9시
            Vector2.right,                      // 3시
            new Vector2(diagonal, diagonal),    // 1시
            new Vector2(-diagonal, -diagonal),  // 7시
            new Vector2(-diagonal, diagonal),   // 11시
            new Vector2(diagonal, -diagonal)    // 5시
        };

        // 8개를 넘는 공격횟수는 같은 방향의 다음 바깥 고리에 배치합니다.
        int ring = satelliteIndex / orderedDirections.Length;
        float radius = m_projectileSatelliteRadius * (1f + ring * 0.5f);
        return orderedDirections[satelliteIndex % orderedDirections.Length] * radius;
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

        // 행동력 감소 타일로 이동한 경우, 이미 충전되어 있던 행동력도 새 최대치 안으로 맞춥니다.
        m_remainingAction = Mathf.Min(m_remainingAction, GetFinalAction());
    }

    private void OnDestroy()
    {
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

        int tileActionReduction = GetCurrentTowerTileBuff() == TowerTileBuffType.ActionCountUp ? 1 : 0;
        tileActionReduction += GetCurrentTileBuffActionReduction();
        return Mathf.Max(1, m_towerData.action - m_bonusActionReduction - tileActionReduction);
    }

    /// <summary>
    /// 현재 타워가 서 있는 SpawnPoint 셀의 맵 버프를 조회합니다.
    /// TileManager의 Dictionary가 실제 버프 데이터의 기준이며, MapBuff 이펙트는 시각 전용입니다.
    /// </summary>
    private TowerTileBuffType GetCurrentTowerTileBuff()
    {
        if (TileManager.Instance == null || TowerManager.Instance == null)
        {
            return TowerTileBuffType.Normal;
        }

        Vector3Int currentCell = TowerManager.Instance.WorldToCell(transform.position);
        return TileManager.Instance.GetTowerTileBuffAt(currentCell);
    }

    /// <summary>
    /// 현재 스폰 타일에 기록된 버프 중 BuffTarget별 최고 티어 한 개만 적용합니다.
    /// 높은 버프가 사라지면 남아 있는 다음 최고 티어 버프가 자동으로 적용됩니다.
    /// </summary>
    private void ApplyCurrentTileBuffTowerEffects(ref TowerStats finalStats)
    {
        if (TileManager.Instance == null || TowerManager.Instance == null) return;

        Vector3Int cell = TowerManager.Instance.WorldToCell(transform.position);
        IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(cell);
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
    /// Reads the Fire field on this tower's cell and applies each source action once.
    /// It is called after all support towers act, so board order never changes which
    /// attack tower receives the current turn's Preheat.
    /// </summary>
    public void RefreshFireTileStatus()
    {
        if (TileManager.Instance == null || TowerManager.Instance == null) return;

        Vector3Int cell = TowerManager.Instance.WorldToCell(transform.position);
        IReadOnlyList<TowerTileBuffEffect> effects = TileManager.Instance.GetTowerBuffEffectsAt(cell);
        for (int i = 0; i < effects.Count; i++)
        {
            TowerTileBuffEffect effect = effects[i];
            if (effect.Target != BuffTarget.Fire || effect.ApplicationVersion <= 0) continue;

            if (m_appliedFirePreheatVersions.TryGetValue(effect.SourceID, out int appliedVersion) &&
                appliedVersion == effect.ApplicationVersion)
            {
                continue;
            }

            m_appliedFirePreheatVersions[effect.SourceID] = effect.ApplicationVersion;
            m_firePreheatStacks.Add(new FirePreheatStack
            {
                AbilityValue = Mathf.Max(0f, effect.AbilityValue),
                RemainingTurns = Mathf.Max(1, effect.StackLifetime)
            });

            if (m_firePreheatStacks.Count >= Mathf.Max(1, effect.StackThreshold))
            {
                m_firePreheatStacks.Clear();
                m_fireOverheatAbilityValue = Mathf.Max(m_fireOverheatAbilityValue, effect.AbilityValue * 2f);
                m_fireOverheatRemainingTurns = Mathf.Max(
                    m_fireOverheatRemainingTurns,
                    Mathf.Max(1, effect.StackLifetime * 2));
            }
        }
    }

    /// <summary>Advances target-owned Fire state once at the start of every enemy turn.</summary>
    public void AdvanceFireStatusTurn()
    {
        for (int i = m_firePreheatStacks.Count - 1; i >= 0; i--)
        {
            if (--m_firePreheatStacks[i].RemainingTurns <= 0)
            {
                m_firePreheatStacks.RemoveAt(i);
            }
        }

        if (m_fireOverheatRemainingTurns > 0 && --m_fireOverheatRemainingTurns <= 0)
        {
            m_fireOverheatAbilityValue = 0f;
        }
    }

    private void ApplyFireStatusEffects(ref TowerStats finalStats)
    {
        float preheatPercent = 0f;
        for (int i = 0; i < m_firePreheatStacks.Count; i++)
        {
            preheatPercent += m_firePreheatStacks[i].AbilityValue;
        }
        finalStats.AttackPower *= 1f + preheatPercent / 100f;

        if (m_fireOverheatRemainingTurns > 0)
        {
            finalStats.AttackPower *= 1f + m_fireOverheatAbilityValue / 100f;
            finalStats.AttackCount += 1;
        }

        if (!IsFireTargetTower()) return;

        finalStats.CriticalRate += m_fireTargetCriticalRateBonus;
        finalStats.CriticalDamage += m_fireTargetCriticalDamageBonus;

        if (m_fireOverheatRemainingTurns > 0)
        {
            finalStats.AttackCount += 2;
        }
        else if (m_firePreheatStacks.Count > 0)
        {
            finalStats.AttackCount += 1;
        }
    }

    public void NotifyFireTargetHit()
    {
        if (!IsFireTargetTower()) return;

        m_fireTargetHitCount++;
        int threshold = Mathf.Max(1, Mathf.RoundToInt(m_baseStats.Duration));
        if (m_fireTargetHitCount < threshold) return;

        m_fireTargetHitCount = 0;
        float bonus = Mathf.Max(0f, m_baseStats.AbilityValue) / 100f;
        m_fireTargetCriticalRateBonus += bonus;
        m_fireTargetCriticalDamageBonus += bonus;
    }

    public bool TryTriggerFireMeteor()
    {
        if (!IsFireSplashTower() || !(m_attackAction is SplashAttackAction splashAction)) return false;

        m_fireSplashTurnCounter++;
        int threshold = Mathf.Max(1, Mathf.RoundToInt(GetFinalStats().Duration));
        if (m_fireSplashTurnCounter < threshold) return false;

        m_fireSplashTurnCounter = 0;
        TowerStats finalStats = GetFinalStats();
        return splashAction.LaunchFireMeteor(transform, finalStats);
    }

    private bool IsFireTargetTower()
    {
        return m_towerData != null && m_towerData.attackType == AttackType.Target && m_towerData.towerID % 100 == TowerPattern.FIRE;
    }

    private bool IsFireSplashTower()
    {
        return m_towerData != null && m_towerData.attackType == AttackType.Splash && m_towerData.towerID % 100 == TowerPattern.FIRE;
    }

    private int GetCurrentTileBuffActionReduction()
    {
        if (TileManager.Instance == null || TowerManager.Instance == null) return 0;
        Vector3Int cell = TowerManager.Instance.WorldToCell(transform.position);
        Dictionary<BuffTarget, TowerTileBuffEffect> strongestByTarget = GetStrongestTileBuffs(TileManager.Instance.GetTowerBuffEffectsAt(cell));
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
            // 버프 타일 방식에서는 현재 타워가 선 타일에서 Darkness를 읽어 같은 성장치를 공유합니다.
            if (TileManager.Instance == null || TowerManager.Instance == null) return;
            Vector3Int cell = TowerManager.Instance.WorldToCell(transform.position);
            Dictionary<BuffTarget, TowerTileBuffEffect> tileBuffs = GetStrongestTileBuffs(TileManager.Instance.GetTowerBuffEffectsAt(cell));
            if (!tileBuffs.TryGetValue(BuffTarget.Darkness, out TowerTileBuffEffect tileDarkness)) return;

            TowerManager.Instance?.AddSharedDarknessAbility(tileDarkness.Tier);
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
