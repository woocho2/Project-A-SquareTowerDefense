using UnityEngine;

public enum AttackType { Splash, Target, Buff, Debuff, None }
public enum BuffTarget { None, Sword, Bow, Shield, Spear, Axe, Hammer, Fire, Ice, Electricity, Wind, Earth, Light, Darkness, AttackCount }
public enum DebuffTarget { None, Sword, Bow, Shield, Spear, Axe, Hammer, Fire, Ice, Electricity, Wind, Earth, Light, Darkness }
public enum TargetPriority { Default, Closest, First, Last, Strongest, Weakest }
public enum StatType { Armor }

public static class TowerEmblem
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
    [Header("기본 타워 정보")]
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
    public int towerID;
    [Tooltip("0이면 towerID를 그대로 외형 ID로 사용합니다. 시너지 타워처럼 논리 ID와 외형 조합이 다른 경우에만 지정합니다.")]
    public int visualTowerID;
    public string towerName;
    public float power;
    public float range;
    [Min(1)] public int action = 1;
    [Min(1)] public int attackCount = 1;
    public float criticalRate;
    public float criticalDamage;
    public float abilityValue;
    public float duration;
    public float projectileSpeed;
    [Min(0)] public int splashRadius;
    [Min(0)] public int additionalHitCount;
    public int hitEffectID;

    [Header("공격 및 타겟 설정")]
    public AttackType attackType;
    public TargetPriority targetPriority = TargetPriority.Closest;

    [Header("버프/디버프 타워 전용 설정")]
    public BuffTarget buffTarget;
    public DebuffTarget debuffTarget;

    [Header("디버프 영역 전용 설정")]
    public LayerMask pathLayer;

    public DebuffZone debuffZonePrefab;

    // CSV를 읽거나 시너지 데이터를 만들 때 한 번 계산합니다.
    // 타워 인스턴스는 이 TowerData를 공유하므로 ID를 다시 분해할 필요가 없습니다.
    public int Tier { get; private set; }
    public int ColorId { get; private set; }
    public int EmblemId { get; private set; }
    public int NextTierTowerID { get; private set; }
    public int VisualTier { get; private set; }
    public int VisualColorId { get; private set; }
    public int VisualEmblemId { get; private set; }

    public int VisualTowerID => visualTowerID > 0 ? visualTowerID : towerID;

    public void InitializeIdentity()
    {
        Tier = towerID / 1000;
        ColorId = (towerID % 1000) / 100;
        EmblemId = towerID % 100;
        NextTierTowerID = towerID + 1000;

        int visualId = VisualTowerID;
        VisualTier = visualId / 1000;
        VisualColorId = (visualId % 1000) / 100;
        VisualEmblemId = visualId % 100;
    }

    private void OnEnable() => InitializeIdentity();

#if UNITY_EDITOR
    private void OnValidate() => InitializeIdentity();
#endif

    /// <summary>
    /// 기본 타워 데이터를 런타임에서 사용하는 TowerStats 구조체로 변환합니다.
    /// </summary>
    public TowerStats ToTowerStats()
    {
        return new TowerStats
        {
            ID = this.towerID,
            Name = this.towerName,
            Level = 1,
            AttackPower = this.power,
            Range = TowerAttackAction.ToWorldRange(this.range),
            AttackCount = this.attackCount,
            CriticalRate = this.criticalRate,
            CriticalDamage = this.criticalDamage,
            Duration = this.duration,
            AbilityValue = this.abilityValue,
            ProjectileSpeed = this.projectileSpeed,
            ProjectileRadius = this.splashRadius,
            AdditionalHitCount = this.additionalHitCount
        };
    }
}
