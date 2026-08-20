using UnityEngine;

[System.Serializable]

// 투사체의 스탯 정보를 깔끔하게 묶어줄 구조체(Struct)
public struct ProjectileStats
{
    public int projectileID;
    public string projectileName;
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
    public float damage;
    public float speed;
    public bool isCritical;
    public float criticalRate;
    public float criticalDamage;
    public float SplashRadius;
    public float duration;
    public float abilityValue;
    public int hitEffectID;
    public DebuffTarget debuffTarget;
    public float dotDamage;
    public float dotDuration;
    public float chainDamage;
    public float chainCount;
}

public class ProjectileController : MonoBehaviour
{
    public ProjectileStats Stats { get; private set; }

    public void Init(ProjectileStats stats)
    {
        Stats = stats;
    }
}