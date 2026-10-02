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
    // 디버프(해머 등)가 정하는 방어력 배율입니다. 타일 방어 버프와는 합연산합니다.
    private float m_defendMultiplier = 1f;
    private float m_tileDefendBonus;
    private float m_vulnerabilityMultiplier = 1f;
    private float m_healReceivedMultiplier = 1f;

    private EnemyMoveController m_movement;
    private EnemyDebuffController m_debuff;
    private EnemyObjectPool2D m_enemyPool;
    private Coroutine m_hpBarFadeRoutine;
    private bool m_hasDied;
    // 처치 보상과 최종 보스 판정은 죽은 시점이 아니라 소환된 웨이브를 기준으로 합니다.
    private int m_spawnWave = 1;
    // 이미 발사되어 날아오는 탄이 줄 것으로 예상되는 피해의 합입니다. 탄 사전 분배에만 씁니다.
    private float m_reservedDamage;

    public int SpawnWave => m_spawnWave;
    /// <summary>날아오는 탄만으로 이미 죽을 예정이면 true. 다음 탄은 다른 적에게 배정합니다.</summary>
    public bool HasLethalDamageReserved => m_reservedDamage >= m_currentHP;
    public float CurrentHP => m_currentHP;
    public float MaxHP => m_maxHP;
    public float FinalDefend => Mathf.Clamp(m_baseDefend * DefendMultiplier, 0.1f, 500f);
    public float BaseDefend => m_baseDefend;
    // 예: 방어 타일 +20%와 해머 디버프 -15%가 겹치면 1 + 0.2 - 0.15 = 1.05배입니다.
    public float DefendMultiplier => Mathf.Max(0f, m_defendMultiplier + m_tileDefendBonus);
    public float VulnerabilityMultiplier => m_vulnerabilityMultiplier;
    public bool IsBoss => m_enemyType == EnemyType.Boss || m_enemyType == EnemyType.MiddleBoss;
    public EnemyType EnemyType => m_enemyType;
    public EnemyData EnemyData { get; private set; }
    public EnemyMoveController Movement => m_movement;

    private void Awake()
    {
        m_movement = GetComponent<EnemyMoveController>();
        m_debuff = GetComponent<EnemyDebuffController>();
        if (m_uiController == null) TryGetComponent(out m_uiController);
    }

    public void SetPool(EnemyObjectPool2D pool) => m_enemyPool = pool;

    public void InitHealth(EnemyData data, float maxHP, float defend, EnemyType enemyType = EnemyType.Normal)
    {
        EnemyData = data;
        InitHealth(maxHP, defend, enemyType);
    }

    public void InitHealth(float maxHP, float defend, EnemyType enemyType = EnemyType.Normal)
    {
        m_hasDied = false;
        m_enemyType = enemyType;
        m_spawnWave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 1;
        m_originalMaxHP = maxHP;
        m_maxHP = maxHP;
        m_currentHP = maxHP;
        m_baseDefend = defend;
        m_defendMultiplier = 1f;
        m_tileDefendBonus = 0f;
        m_vulnerabilityMultiplier = 1f;
        m_healReceivedMultiplier = 1f;
        m_reservedDamage = 0f;

        HideHPBar();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }

    // ==========================================================================================================
    // 타일 버프 관련 체력/방어력 연동 함수
    // ==========================================================================================================

    // 최대 체력 기준 퍼센트 힐 적용 (최대 체력 초과 방지)
    public void HealMaxHealthPercent(float percent)
    {
        if (m_currentHP <= 0f) return;

        float healAmount = m_maxHP * percent * m_healReceivedMultiplier;
        m_currentHP = Mathf.Min(m_currentHP + healAmount, m_maxHP);
        GameStateVersion.MarkChanged();

        ShowHPBar();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }

    // 타일 방어력 버프 설정 (0.2 = 방어력 +20%)
    public void SetTileDefendBonus(float bonus)
    {
        m_tileDefendBonus = bonus;
    }

    // 타일 방어력 버프 초기화
    public void ResetTileDefendBuff()
    {
        m_tileDefendBonus = 0f;
    }

    // ==========================================================================================================
    // 대미지 및 사망 처리
    // ==========================================================================================================

    public void ApplyDamage(float rawDamage, bool isCritical = false, float armorPenetrationPercent = 0f, TowerController sourceTower = null)
    {
        if (m_hasDied || m_currentHP <= 0f || !gameObject.activeInHierarchy) return;

        if (isCritical) m_debuff?.RegisterCriticalHit();
        // 치명타에 반응한 처형이 적을 즉시 풀로 반환했으면 일반 피해를 이어서 적용하지 않는다.
        if (m_hasDied || m_currentHP <= 0f || !gameObject.activeInHierarchy) return;

        ShowHPBar();

        float calculatedDamage = EstimateDamage(rawDamage, armorPenetrationPercent);

        m_currentHP -= calculatedDamage;
        GameStateVersion.MarkChanged();

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

    /// <summary>
    /// 지금 상태에서 이 피해를 받으면 실제로 깎일 체력입니다. 방어력·방어 관통·취약·고정 피해 전환을 반영합니다.
    /// 체력을 바꾸지 않으므로 발사 전에 탄을 분배할 때도 씁니다.
    /// </summary>
    public float EstimateDamage(float rawDamage, float armorPenetrationPercent = 0f)
    {
        float fixedDamageRatio = m_debuff != null ? m_debuff.GetFixedDamageConversionRatio() : 0f;
        float fixedDamage = rawDamage * fixedDamageRatio;
        float defendedDamage = rawDamage - fixedDamage;
        float defend = FinalDefend * (1f - Mathf.Clamp01(armorPenetrationPercent / 100f));
        float finalWeak = m_vulnerabilityMultiplier;
        return Mathf.Clamp((defendedDamage * (1f / (1f + (0.01f * defend))) * finalWeak) + fixedDamage, 0f, 50000f);
    }

    public void ReserveDamage(float expectedDamage)
    {
        m_reservedDamage += Mathf.Max(0f, expectedDamage);
    }

    public void ReleaseReservedDamage(float expectedDamage)
    {
        m_reservedDamage = Mathf.Max(0f, m_reservedDamage - Mathf.Max(0f, expectedDamage));
    }

    public void ExecuteInstantKill()
    {
        if (m_hasDied || m_currentHP <= 0f || !gameObject.activeInHierarchy) return;
        m_currentHP = 0f;
        Die();
    }

    private void Die(TowerController sourceTower = null)
    {
        if (m_hasDied) return;
        m_hasDied = true;

        sourceTower?.NotifyEnemyKilled();

        WaveManager.GetKillReward(m_enemyType, m_spawnWave, out int gold, out int gem);
        if (gold > 0) CurrencyManager.Instance?.AddGold(gold);
        if (gem > 0) CurrencyManager.Instance?.AddGem(gem);

        WaveManager.Instance?.OnEnemyDied(m_enemyType, m_spawnWave);
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (!gameObject.activeInHierarchy) return;
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
    public void SetHealReceivedMultiplier(float multiplier) => m_healReceivedMultiplier = Mathf.Clamp01(multiplier);

    public void SetMaxHealthReductionPercent(float reductionPercent)
    {
        m_maxHP = Mathf.Max(1f, m_originalMaxHP * (1f - Mathf.Clamp01(reductionPercent / 100f)));
        m_currentHP = Mathf.Min(m_currentHP, m_maxHP);
        GameStateVersion.MarkChanged();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }
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
