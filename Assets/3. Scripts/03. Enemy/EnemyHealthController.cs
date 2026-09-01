using System.Collections;
using UnityEngine;

public class EnemyHealthController : MonoBehaviour
{
    [SerializeField] private EnemyUIController m_uiController;
    [SerializeField] private GameObject m_hpBar;
    [SerializeField] private EnemyType m_enemyType = EnemyType.Normal;

    private float m_currentHP;
    private float m_maxHP;
    private float m_baseDefend;
    private float m_defendMultiplier = 1f;
    private float m_vulnerabilityMultiplier = 1f;
    private float m_synergyWeak = 1f;

    private EnemyMovementController m_movement;
    private EnemyDebuffController m_debuff;
    private EnemyObjectPool2D m_enemyPool;
    private Coroutine m_hpBarFadeRoutine;

    public float CurrentHP => m_currentHP;
    public float MaxHP => m_maxHP;
    public float FinalDefend => Mathf.Clamp(m_baseDefend * m_defendMultiplier, 0.1f, 500f);
    public bool IsBoss => m_enemyType == EnemyType.Boss || m_enemyType == EnemyType.SpecialBoss;

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
        m_maxHP = maxHP;
        m_currentHP = maxHP;
        m_baseDefend = defend;
        m_defendMultiplier = 1f;
        m_vulnerabilityMultiplier = 1f;
        m_synergyWeak = 1f;

        HideHPBar();
        if (m_uiController != null) m_uiController.SetHPBar(m_currentHP, m_maxHP);
    }

    public void ApplyDamage(float rawDamage, bool isCritical = false)
    {
        if (m_currentHP <= 0f) return;

        ShowHPBar();

        float defend = FinalDefend;
        float finalWeak = m_vulnerabilityMultiplier * m_synergyWeak;
        float calculatedDamage = Mathf.Clamp((rawDamage * (1f / (1f + (0.01f * defend))) * finalWeak), 0f, 5000f);

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
            Die();
        }
    }

    public void ExecuteInstantKill()
    {
        m_currentHP = 0f;
        Die();
    }

    private void Die()
    {
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
        // 1. 실행 중인 코루틴 전체 정지
        StopAllCoroutines();
        HideHPBar();

        // 2. 디버프 및 지속 효과 초기화
        if (m_debuff != null)
        {
            m_debuff.ClearAllDebuffs();
        }

        // 3. 이동 관련 코루틴 및 플래그 정지
        if (m_movement != null)
        {
            m_movement.StopMovement();
        }

        gameObject.SetActive(false);

        // 4. 풀 반환
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
    public void SetSynergyWeak(float weak) => m_synergyWeak = weak;

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