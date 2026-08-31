using UnityEngine;

public enum AttackType { Splash, Target, Buff, Debuff }
public enum BuffTarget { None, Sword, Bow, Shield, Spear, Axe, Hammer, Fire, Ice, Electricity, Wind, Earth, Light, Darkness }
public enum DebuffTarget { None, Sword, Bow, Shield, Spear, Axe, Hammer, Fire, Ice, Electricity, Wind, Earth, Light, Darkness }
public enum TargetPriority { Default, Closest, First, Last, Strongest, Weakest }
public enum StatType { Armor }

public static class TowerPattern
{
    public const int SWORD = 1;
    public const int BOW = 2;
    public const int SHIELD = 3;
    public const int SPEAR = 4;
    public const int AXE = 5;
    public const int HAMMER = 6;
    public const int FIRE = 7;
    public const int ICE = 8;
    public const int ELECTRICITY = 9;
    public const int WIND = 10;
    public const int EARTH = 11;
    public const int LIGHT = 12;
    public const int DARKNESS = 13;
}

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

    public DebuffZone debuffZonePrefab;

    /// <summary>
    /// TowerData의 기본 스탯을 기반으로 런타임용 TowerStats 구조체를 생성하여 반환합니다.
    /// </summary>
    public TowerStats ToTowerStats()
    {
        return new TowerStats
        {
            ID = this.towerID,
            Name = this.towerName,
            Level = 1,
            AttackPower = this.attackPower,
            Range = this.range,
            AttackSpeed = this.attackSpeed,
            CriticalRate = this.criticalRate,
            CriticalDamage = this.criticalDamage,
            Duration = this.duration,
            AbilityValue = this.abilityValue,
            ProjectileSpeed = this.projectileSpeed,
            ProjectileRadius = this.splashRadius
        };
    }
}