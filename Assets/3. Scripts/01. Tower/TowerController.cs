using System;
using UnityEngine;


[System.Serializable]
public struct TowerStats
{
    public int towerID;
    public string towerName;
    public int level;
    public float damage;                        
    public float range;
    public float attackSpeed;
    public bool isCritical;
    public float criticalRate;
    public float criticalDamage;
    public float duration;
    public float abilityValue;
    public float dotDamage;
    public float dotDuration;
    public float chainCount;
    public float chainDamage;
}

public class TowerController : MonoBehaviour
{
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TowerStats m_towerStats;
    [SerializeField] private GameObject m_range;

    private TowerAttackAction m_attackAction;
    private ProjectileData m_projectileData;

    private float m_bonusDamage = 1f;
    private float m_bonusAttackSpeed = 1f;
    private float m_bonusRange = 1f;
    private float m_bonusCriticalRate = 1f;
    private float m_bonusCriticalDamage = 1f;
    private float m_dotDamage = 0f;
    private float m_dotDuration = 0f;
    private float m_chainCount = 0f;
    private float m_chainDamage = 0f;
    private float m_attackCooldown;

    private Coroutine m_damageBuffRoutine;
    private Coroutine m_attackSpeedBuffRoutine;
    private Coroutine m_rangeBuffRoutine;
    private Coroutine m_criticalRateBuffRoutine;
    private Coroutine m_criticalDamageBuffRoutine;
    private Coroutine m_dotDamageBuffRoutine;
    private Coroutine m_chainBuffRoutine;



    private void Start()
    {
        if (TowerManager.Instance != null)
        {
            if (string.IsNullOrEmpty(m_towerStats.towerName) || m_towerStats.towerID == 0)
            {
                GlobalStats();
            }
            TowerManager.Instance.OnTowerTypeUpgrade += HandleTypeUpgrade;
        }
        else
        {
            Debug.LogError("TowerManager 인스턴스를 찾을 수 없습니다. 타워 스탯 동기화가 실패했습니다.");
        }
    }

    private void OnDestroy()
    {
        if (TowerManager.Instance != null)
        {
            TowerManager.Instance.OnTowerTypeUpgrade -= HandleTypeUpgrade;
        }
    }

    private void HandleTypeUpgrade(int towerID)
    {
        if (m_towerData != null && m_towerData.towerID == towerID)
        {
            GlobalStats();

            if (m_range != null && m_range.activeSelf)
            {
                ShowRange(true);
            }
        }
    }

    public void Init(TowerData data)
    {
        m_towerData = data;

        GlobalStats();

        if (GlobalProjectileManager.Instance != null)
        {
            m_projectileData = GlobalProjectileManager.Instance.GetMatchingProjectile(m_towerData.towerID);
        }

        switch (m_towerData.attackType)
        {
            case AttackType.Target:
                m_attackAction = new TargetAttackAction(m_towerData, m_projectileData);
                break;
            case AttackType.Splash:
                m_attackAction = new SplashAttackAction(m_towerData, m_projectileData);
                break;
            case AttackType.Trap:
                m_attackAction = new TrapAttackAction(m_towerData, m_projectileData);
                break;
            case AttackType.Buff:
                m_attackAction = new BuffAction(m_towerData, m_projectileData);
                break;
            case AttackType.Debuff:
                m_attackAction = new DebuffAction(m_towerData, m_projectileData);
                break;
        }
    }

    private void GlobalStats()
    {
        if (m_towerData != null)
        {
            m_towerStats = TowerManager.Instance.GetGlobalStats(m_towerData.towerID);
        }
    }

    public TowerStats GetFinalStats()
    {
        m_bonusDamage = Mathf.Clamp(m_bonusDamage, 1f, 10000f);
        m_bonusAttackSpeed = Mathf.Clamp(m_bonusAttackSpeed, 1f, 10000f);
        m_bonusRange = Mathf.Clamp(m_bonusRange, 1f, 10000f);
        m_bonusCriticalRate = Mathf.Clamp(m_bonusCriticalRate, 1f, 10000f);
        m_bonusCriticalDamage = Mathf.Clamp(m_bonusCriticalDamage, 1f, 10000f);
        
        TowerStats finalStats = m_towerStats;
        finalStats.damage *= m_bonusDamage;
        finalStats.attackSpeed *= m_bonusAttackSpeed;
        finalStats.range *= m_bonusRange;
        finalStats.criticalRate *= m_bonusCriticalRate;
        finalStats.criticalDamage *= m_bonusCriticalDamage;
        finalStats.dotDamage = m_dotDamage;
        finalStats.dotDuration = m_dotDuration;
        finalStats.chainDamage = m_chainDamage;
        finalStats.chainCount = m_chainCount;

        if (finalStats.range <= 1f) finalStats.range = 1f;
        if (finalStats.criticalRate >= 1f) finalStats.criticalRate = 1f;

        return finalStats;
    }

    private void Update()
    {
        if (m_attackAction == null)
        {
            Debug.Log("해당 타워에게 AttackAction이 장착되어있지 않습니다.");
            return;
        }

        m_attackCooldown -= Time.deltaTime;

        if (m_attackCooldown <= 0f)
        {
            TowerStats FinalStats = GetFinalStats();
            bool didAction = m_attackAction.ExecuteAction(transform, FinalStats);

            if (didAction)
            {
                m_attackCooldown = FinalStats.attackSpeed > 0 ? 1f / FinalStats.attackSpeed : 1f;
            }
        }
    }

    public TowerData GetTowerData()
    {
        return m_towerData;
    }

    public void ShowRange(bool show)
    {
        if (m_range != null)
        {
            m_range.SetActive(show);

            if (show)
            {
                float currentRange = GetFinalStats().range;

                float scaleValue = (currentRange * 2f) / transform.localScale.x;
                m_range.transform.localScale = new Vector3(scaleValue, scaleValue, 1f);
            }
        }
    }

    public void AddBuffStat(BuffTarget buffTarget, float abilityValue, float damageValue, float duration, float dotDuration = 0f)
    {
        switch (buffTarget)
        {
            case BuffTarget.Damage:
                if (m_damageBuffRoutine != null) StopCoroutine(m_damageBuffRoutine);
                m_bonusDamage = abilityValue;
                m_damageBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
            case BuffTarget.AttackSpeed:
                if (m_attackSpeedBuffRoutine != null) StopCoroutine(m_attackSpeedBuffRoutine);
                m_bonusAttackSpeed = abilityValue;
                m_attackSpeedBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
            case BuffTarget.Range:
                if (m_rangeBuffRoutine != null) StopCoroutine(m_rangeBuffRoutine);
                m_bonusRange = abilityValue;
                m_rangeBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
            case BuffTarget.CriticalRate:
                if (m_criticalRateBuffRoutine != null) StopCoroutine(m_criticalRateBuffRoutine);
                m_bonusCriticalRate = abilityValue;
                m_criticalRateBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
            case BuffTarget.CriticalDamage:
                if (m_criticalDamageBuffRoutine != null) StopCoroutine(m_criticalDamageBuffRoutine);
                m_bonusCriticalDamage = abilityValue;
                m_criticalDamageBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
            case BuffTarget.DotDamage:
                if (m_dotDamageBuffRoutine != null) StopCoroutine(m_dotDamageBuffRoutine);
                m_dotDamage = abilityValue * 0.2f;
                m_dotDuration = dotDuration;
                m_dotDamageBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget,duration));
                break;
            case BuffTarget.Chain:
                if (m_chainBuffRoutine != null) StopCoroutine(m_chainBuffRoutine);
                m_chainCount = damageValue;
                m_chainDamage = abilityValue * 0.1f;
                m_chainBuffRoutine = StartCoroutine(RemoveBuffRoutine(buffTarget, duration));
                break;
        }
    }

    private System.Collections.IEnumerator RemoveBuffRoutine(BuffTarget buffTarget, float duration)
    {
        yield return new WaitForSeconds(duration);

        switch (buffTarget) 
        {
            case BuffTarget.Damage:
                m_bonusDamage = 1f; m_damageBuffRoutine = null;
                break;
            case BuffTarget.AttackSpeed:
                m_bonusAttackSpeed = 1f; m_attackSpeedBuffRoutine = null;
                break;
            case BuffTarget.Range:
                m_bonusRange = 1f; m_rangeBuffRoutine = null;
                break;
            case BuffTarget.CriticalRate:
                m_bonusCriticalRate = 1f; m_criticalRateBuffRoutine = null;
                break;
            case BuffTarget.CriticalDamage:
                m_bonusCriticalDamage = 1f; m_criticalDamageBuffRoutine = null;
                break;
            case BuffTarget.DotDamage:
                m_dotDamage = 1f; m_dotDuration = 0f; m_dotDamageBuffRoutine = null;
                break;
            case BuffTarget.Chain:
                m_chainDamage = 0f; m_chainCount = 0f; m_chainBuffRoutine = null;
                break;
        }
    }
}

