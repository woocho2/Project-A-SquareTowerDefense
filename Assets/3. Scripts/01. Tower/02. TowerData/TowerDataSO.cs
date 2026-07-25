using UnityEngine;

// 행동 부품 및 버프 관련 열거형
public enum AttackType { Splash, Target, Trap, Buff, Debuff }
public enum BuffTarget { None, Damage, AttackSpeed, Range, CriticalRate, CriticalDamage, DotDamage, Chain }
public enum DebuffTarget { None, Slow, Defense, Stun, DotDamage, Weak, Push}


[CreateAssetMenu(fileName = "NewTowerData", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    [Header("타워 능력치")]
    public GameObject towerPrefab;
    public int towerID;
    public string towerName;
    public int towerLevel;
    public float damage;
    public float range;
    public float attackSpeed;
    public bool isCritical;
    public float criticalRate;
    public float criticalDamage;
    public float duration;
    public float abilityValue;

    [Header("자동 조립 설정")]
    public AttackType attackType;
    public LayerMask targetLayer;

    [Header("버프/디버프 타워 전용 설정")]
    public BuffTarget buffTarget;
    public DebuffTarget debuffTarget;

    [Header("트랩 타워 전용 설정 (AttackType이 Trap일 때만 사용)")]
    public float trapLifeTime = 60f;
    public LayerMask pathLayer;
    public int maxPlacementAttempts = 10;
}
