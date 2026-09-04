using System.Collections;
using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    [SerializeField] private EnemyUIController m_uiController;
    [SerializeField] private GameObject m_hpBar;
    [SerializeField] private EnemyType m_enemyType = EnemyType.Normal;

    private float m_currentHP;
    private float m_maxHP;
    private float m_originalMaxHP;
    private float m_baseDefend;
    private float m_defendMultiplier = 1f;
    private float m_vulnerabilityMultiplier = 1f;
#if false // Synergy system temporarily disabled
    private float m_synergyWeak = 1f;
#endif

    private EnemyMovementController m_movement;
    private EnemyDebuffController m_debuff;
    private EnemyObjectPool2D m_enemyPool;
    private Coroutine m_hpBarFadeRoutine;

    public float CurrentHP => m_currentHP;
    public float MaxHP => m_maxHP;
    public float FinalDefend => Mathf.Clamp(m_baseDefend * m_defendMultiplier, 0.1f, 500f);
    public bool IsBoss => m_enemyType == EnemyType.Boss || m_enemyType == EnemyType.MiddleBoss;

    private void Awake()
    {
        m_movement = GetComponent<EnemyMovementController>();
        m_debuff = GetComponent<EnemyDebuffController>();
        if (m_uiController == null) TryGetComponent(out m_uiController);
    }

    public void SetPool(EnemyObjectPool2D pool) => m_enemyPool = pool;

    public void InitHealth(float maxHP, float defend, EnemyType enemyType = EnemyType.Normal)
    {
        m_enemyType = enemyType;
        m_originalMaxHP = maxHP;
        m_maxHP = maxHP;
        m_currentHP = maxHP;
        m_baseDefend = defend;
        m_defendMultiplier = 1f;
        m_vulnerabilityMultiplier = 1f;


        HideHPBar();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }

    // ==========================================================================================================
    // 타일 버프 관련 체력/방어력 연동 함수
    // ==========================================================================================================

    // 현재 체력 기준 퍼센트 힐 적용 (최대 체력 초과 방지)
    public void HealMaxHealthPercent(float percent)
    {
        if (m_currentHP <= 0f) return;

        float healAmount = m_maxHP * percent;
        m_currentHP = Mathf.Min(m_currentHP + healAmount, m_maxHP);

        ShowHPBar();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }

    // 타일 방어력 버프 초기화
    public void ResetTileDefendBuff()
    {
        m_defendMultiplier = 1f;
    }

    // ==========================================================================================================
    // 대미지 및 사망 처리
    // ==========================================================================================================

    public void ApplyDamage(float rawDamage, bool isCritical = false, float armorPenetrationPercent = 0f, TowerController sourceTower = null)
    {
        if (m_currentHP <= 0f) return;

        if (isCritical) m_debuff?.RegisterCriticalHit();

        ShowHPBar();

        float fixedDamageRatio = m_debuff != null ? m_debuff.GetFixedDamageConversionRatio() : 0f;
        float fixedDamage = rawDamage * fixedDamageRatio;
        float defendedDamage = rawDamage - fixedDamage;
        float defend = FinalDefend * (1f - Mathf.Clamp01(armorPenetrationPercent / 100f));
        float finalWeak = m_vulnerabilityMultiplier;
        float calculatedDamage = Mathf.Clamp((defendedDamage * (1f / (1f + (0.01f * defend))) * finalWeak) + fixedDamage, 0f, 5000f);

        m_currentHP -= calculatedDamage;

        if (calculatedDamage > 0f && DamageTextPool.Instance != null)
        {
            Vector3 spawnPos = transform.position + (Vector3.up * 0.5f) + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0f);
            DamageTextPool.Instance.Spawn(spawnPos, calculatedDamage, isCritical);
        }

        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);

        if (m_currentHP <= 0f)
        {
            m_currentHP = 0f;
            Die(sourceTower);
        }
    }

    public void ExecuteInstantKill()
    {
        m_currentHP = 0f;
        Die();
    }

    private void Die(TowerController sourceTower = null)
    {
        sourceTower?.NotifyEnemyKilled();
        if (IsBoss)
        {
            CurrencyManager.Instance?.AddGold(500);
            CurrencyManager.Instance?.AddGem(5);
        }
        else
        {
            CurrencyManager.Instance?.AddGold(10);
        }

        WaveManager.Instance?.OnEnemyDied(IsBoss);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        StopAllCoroutines();
        HideHPBar();

        if (m_debuff != null)
        {
            m_debuff.ClearAllDebuffs();
        }

        if (m_movement != null)
        {
            m_movement.StopMovement();
        }

        gameObject.SetActive(false);

        if (m_enemyPool != null)
        {
            m_enemyPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetDefendMultiplier(float multiplier) => m_defendMultiplier = multiplier;
    public void SetVulnerability(float multiplier) => m_vulnerabilityMultiplier = multiplier;

    public void SetMaxHealthReductionPercent(float reductionPercent)
    {
        m_maxHP = Mathf.Max(1f, m_originalMaxHP * (1f - Mathf.Clamp01(reductionPercent / 100f)));
        m_currentHP = Mathf.Min(m_currentHP, m_maxHP);
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }
#if false // Synergy system temporarily disabled
    public void SetSynergyWeak(float weak) => m_synergyWeak = weak;
#endif

    private void ShowHPBar()
    {
        if (m_hpBar != null)
        {
            m_hpBar.SetActive(true);
            if (m_hpBarFadeRoutine != null) StopCoroutine(m_hpBarFadeRoutine);
            m_hpBarFadeRoutine = StartCoroutine(DisableHPBarRoutine(3f));
        }
    }

    public void HideHPBar()
    {
        if (m_hpBar != null) m_hpBar.SetActive(false);
        if (m_hpBarFadeRoutine != null)
        {
            StopCoroutine(m_hpBarFadeRoutine);
            m_hpBarFadeRoutine = null;
        }
    }

    private IEnumerator DisableHPBarRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (m_hpBar != null) m_hpBar.SetActive(false);
    }
}
