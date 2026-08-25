using UnityEngine;

// 행동 부품 및 버프 관련 열거형
public enum AttackType { Splash, Target, Buff, Debuff }
public enum BuffTarget { None, AttackPower, Range, DefensePenetration, CriticalRate, CriticalDamage, DotDamage, ProjectileSpeed, Chain, AttackSpeed }
public enum DebuffTarget { None, MaxHpDecay, PiercingShards, Stun, CriticalRate, CriticalDamage, DotDamage, Slow, Weak, Push }
public enum TargetPriority { Default, Closest, First, Last, Strongest, Weakest }


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

    [Header("디버프타워 전용 설정)")]
    public LayerMask pathLayer;
}