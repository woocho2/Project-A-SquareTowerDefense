using UnityEngine;

public enum AttackType { Splash, Target, Buff, Debuff }
public enum BuffTarget { None, AttackPower, Range, Barrier, CriticalRate, CriticalDamage, DefensePenetration, Overheat, SplashRadius, ExtraHit, AttackSpeed, TierUpgrade, Chain, Absorption }
public enum DebuffTarget { None, MaxHpDecay, Bleeding, Block, Javelin, Vulnerable, DefenseReduction, DotDamage, Slow, ElectricShock, Push, Root, Disintegrate, Blackhole }
public enum TargetPriority { Default, Closest, First, Last, Strongest, Weakest }

public enum StatType { Armor }

[CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("타워 능력치")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
    public int towerID;
    public string towerName;
    public int towerLevel;
    public float attackPower;
    public float range;
    public float attackSpeed;
    public bool isCritical;
    public float criticalRate;
    public float criticalDamage;
    public float abilityValue;
    public float duration;
    public float projectileSpeed;
    public float splashRadius;
    public int hitEffectID;

    [Header("자동 조립 설정")]
    public AttackType attackType;
    public LayerMask targetLayer;

    [Header("버프/디버프 타워 전용 설정")]
    public BuffTarget buffTarget;
    public DebuffTarget debuffTarget;

    [Header("디버프타워 전용 설정")]
    public LayerMask pathLayer;

    // 장판(Zone) 프리팹을 할당받기 위한 변수 추가
    public DebuffZone debuffZonePrefab;
}