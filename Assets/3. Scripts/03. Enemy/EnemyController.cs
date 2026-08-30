using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    public string EnemyID;
    public float currentHP;
    public float MaxHP;
    public float Defend;
    public float Speed;
    public float Resistance;
}

public class EnemyController : MonoBehaviour
{
    [SerializeField] public EnemyData EnemyData;
    [SerializeField] protected LayerMask m_projetileLayer;
    [SerializeField] protected LayerMask m_endLayer;

    [SerializeField] protected EnemyUIController m_uiController;
    [SerializeField] protected GameObject HPBar;
    [SerializeField] protected DamageText m_damageTextPrefab;

    private EnemyObjectPool2D enemyPool;
    private Collider2D m_col;

    private Transform target;
    private int wavePointIndex = 0;
    private bool IsReturning = false;

    private protected EnemyStats m_enemyStats;
    public float currentHP => m_enemyStats.currentHP;
    public float MaxHP => m_enemyStats.MaxHP;
    public bool IsAlive => m_enemyStats.currentHP > 0;

    private protected Coroutine DamageAfter3s;
    protected bool m_isBoss = false;

    private float m_debuffSpeed = 1f;
    private float m_debuffDefend = 1f;
    private float m_debuffDotDamage = 1f;
    private float m_debuffWeak = 1f;
    private bool m_isStunned = false;
    private bool m_isPushed = false;
    private float m_synergySlow = 1f;
    private float m_synergyWeak = 1f;

    // 디버프 스택 관리를 위한 딕셔너리 추가
    private Dictionary<DebuffType, int> m_debuffStacks = new Dictionary<DebuffType, int>();

    private Coroutine m_speedDebuffRoutine;
    private Coroutine m_defendDebuffRoutine;
    private Coroutine m_stunDebuffRoutine;
    private Coroutine m_pushDebuffRoutine;
    private Coroutine m_dotDamageDebuffRoutine;
    private Coroutine m_weakDebuffRoutine;
    private Coroutine m_heatwaveDebuffRoutine;

    protected virtual void Awake()
    {
        m_col = GetComponent<Collider2D>();

        if (m_uiController == null)
        {
            if (TryGetComponent<EnemyUIController>(out EnemyUIController uIController))
                m_uiController = uIController;
            else
                Debug.LogWarning("EnemyUIController 컴포넌트를 찾을 수 없습니다.");
        }
    }

    protected virtual void Start()
    {
        if (EnemyData != null)
        {
            m_enemyStats.EnemyID = EnemyData.EnemyID;
            m_enemyStats.MaxHP = EnemyData.MaxHP;
            m_enemyStats.currentHP = m_enemyStats.MaxHP;
            m_enemyStats.Defend = EnemyData.Defend;
            m_enemyStats.Speed = EnemyData.Speed;
        }

        if (WayPointController.points == null || WayPointController.points.Length == 0)
        {
            Debug.LogError("웨이포인트가 설정되지 않았습니다!");
            return;
        }

        target = WayPointController.points[0];
        if (HPBar != null) HPBar.SetActive(false);
    }

    protected virtual void Update()
    {
        if (target == null || IsReturning || m_isStunned || m_isPushed) return;

        if (TowerManager.Instance != null)
        {
            if (TowerManager.Instance.IsHeatWaveActive && m_heatwaveDebuffRoutine == null)
            {
                m_heatwaveDebuffRoutine = StartCoroutine(HeatWaveRoutine());
            }
            else if (!TowerManager.Instance.IsHeatWaveActive && m_heatwaveDebuffRoutine != null)
            {
                StopCoroutine(m_heatwaveDebuffRoutine);
                m_heatwaveDebuffRoutine = null;
            }
        }

        Vector3 dir = target.position - transform.position;
        dir.z = 0f;
        float distanceToTarget = dir.magnitude;

        float currentSpeed = GetFinalStats().Speed;
        float moveDistanceThisFrame = currentSpeed * Time.deltaTime;

        if (moveDistanceThisFrame >= distanceToTarget)
        {
            transform.position = target.position;
            GetNextWaypoint();
        }
        else
        {
            transform.Translate(dir.normalized * moveDistanceThisFrame, Space.World);
        }
    }

    void GetNextWaypoint()
    {
        if (wavePointIndex >= WayPointController.points.Length - 1)
        {
            ReturnToPool();
            return;
        }
        wavePointIndex++;
        target = WayPointController.points[wavePointIndex];
    }

    public void ResetEnemy(Vector3 spawnPosition, float hpMultiplier, float defendMultiplier)
    {
        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        IsReturning = false;
        ClearAllDebuff();

        if (m_col != null) m_col.enabled = true;

        if (EnemyData != null)
        {
            m_enemyStats.EnemyID = EnemyData.EnemyID;
            m_enemyStats.MaxHP = EnemyData.MaxHP * hpMultiplier;
            m_enemyStats.currentHP = m_enemyStats.MaxHP;
            m_enemyStats.Defend = EnemyData.Defend * defendMultiplier;
            m_enemyStats.Speed = EnemyData.Speed;
        }

        wavePointIndex = 0;
        if (WayPointController.points != null && WayPointController.points.Length > 0)
            target = WayPointController.points[0];

        if (HPBar != null) HPBar.SetActive(false);
        if (m_uiController != null) m_uiController.SetHPBar(m_enemyStats.currentHP, m_enemyStats.MaxHP);
    }

    public EnemyStats GetFinalStats()
    {
        EnemyStats finalStats = m_enemyStats;
        m_debuffSpeed = Mathf.Clamp(m_debuffSpeed, 0.01f, 1f);
        m_debuffDefend = Mathf.Clamp(m_debuffDefend, 0.01f, 1f);
        m_synergySlow = Mathf.Clamp(m_synergySlow, 0.01f, 1f);

        float finalSlow = m_debuffSpeed * m_synergySlow;
        finalStats.Speed *= finalSlow;
        finalStats.Defend *= m_debuffDefend;

        if (finalStats.Speed <= 0.01f) finalStats.Speed = 0.01f;
        if (finalStats.Defend <= 0.01f) finalStats.Defend = 0.1f;

        return finalStats;
    }

    public void SetPool(EnemyObjectPool2D pool)
    {
        enemyPool = pool;
    }

    protected void ReturnToPool()
    {
        if (IsReturning) return;
        IsReturning = true;

        if (DamageAfter3s != null)
        {
            StopCoroutine(DamageAfter3s);
            DamageAfter3s = null;
        }
        if (HPBar != null) HPBar.SetActive(false);

        gameObject.SetActive(false);
        if (enemyPool != null) enemyPool.Release(this);
        else Destroy(gameObject);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (CommonUtil.ContainsLayer(m_endLayer, collision.gameObject.layer))
        {
            if (UIManager.Instance != null) UIManager.Instance.OnPlayerHit(m_isBoss);
            ReturnToPool();
        }
    }

    public virtual void TakeDamage(float damage, bool isCritical = false)
    {
        if (IsAlive == false) return;

        if (HPBar != null)
        {
            HPBar.SetActive(true);
            if (DamageAfter3s != null) StopCoroutine(DamageAfter3s);
            DamageAfter3s = StartCoroutine(DisableUIAfterDelayRoutine(3f));
        }

        float defend = Mathf.Clamp(GetFinalStats().Defend, 0f, 500f);
        float finalWeak = m_debuffWeak * m_synergyWeak;
        float finalDamage = Mathf.Clamp((damage * (1 / (1 + (0.01f * defend))) * finalWeak), 0f, 5000f);

        m_enemyStats.currentHP -= finalDamage;

        if (finalDamage > 0 && DamageTextPool.Instance != null)
        {
            Vector2 moveDir = Vector2.zero;
            if (target != null) moveDir = target.position - transform.position;

            Vector3 spawnOffset;
            if (Mathf.Abs(moveDir.x) > Mathf.Abs(moveDir.y)) spawnOffset = Vector3.up * 0.5f;
            else spawnOffset = Vector3.right * 0.5f;

            spawnOffset.x += UnityEngine.Random.Range(-0.1f, 0.1f);
            spawnOffset.y += UnityEngine.Random.Range(-0.1f, 0.1f);
            Vector3 spawnPos = transform.position + spawnOffset;

            DamageTextPool.Instance.Spawn(spawnPos, finalDamage, isCritical);
        }

        if (m_uiController != null) m_uiController.SetHPBar(m_enemyStats.currentHP, m_enemyStats.MaxHP);

        if (m_enemyStats.currentHP <= 0f)
        {
            m_enemyStats.currentHP = 0f;
            Die(m_isBoss);
        }
    }

    public void Die(bool IsBoss)
    {
        if (IsBoss == false) CurrencyManager.Instance.AddGold(10);
        else
        {
            CurrencyManager.Instance.AddGold(500);
            CurrencyManager.Instance.AddGem(5);
        }
        if (HPBar != null) HPBar.SetActive(false);
        WaveManager.Instance.OnEnemyDied(IsBoss);
        ReturnToPool();
    }

    public void ExecuteDeath()
    {
        m_enemyStats.currentHP = 0f;
        Die(m_isBoss);
    }

    // 장판(DebuffAction)에서 호출되는 스택 및 상태 추가 시스템
    public void ApplyStack(DebuffType type, int amount)
    {
        if (!m_debuffStacks.ContainsKey(type)) m_debuffStacks[type] = 0;

        m_debuffStacks[type] += amount;

        // 빙결(Ice) 변환 로직 예시 - 5스택 초과 시 1초 스턴(빙결) 후 리셋
        if (type == DebuffType.Slow && m_debuffStacks[type] >= 5)
        {
            ApplyDebuff(DebuffTarget.Stun, 0f, 1f);
            m_debuffStacks[type] = 0;
        }
    }

    public void ApplyStatModifier(StatType type, float amount)
    {
        if (type == StatType.Armor) m_debuffDefend += (amount / 100f);
    }

    public void RemoveStatModifier(StatType type, float amount)
    {
        if (type == StatType.Armor) m_debuffDefend -= (amount / 100f);
    }

    public void PullToPosition(Vector3 targetPos, float duration)
    {
        StartCoroutine(PullRoutine(targetPos, duration));
    }

    private IEnumerator PullRoutine(Vector3 targetPos, float duration)
    {
        float timer = 0f;
        while (timer < duration && IsAlive)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * (m_enemyStats.Speed * 2f));
            timer += Time.deltaTime;
            yield return null;
        }
    }

    public void ApplyDebuff(DebuffTarget target, float value, float duration)
    {
        if (!gameObject.activeInHierarchy) return;

        switch (target)
        {
            case DebuffTarget.Slow:
                if (m_speedDebuffRoutine != null) StopCoroutine(m_speedDebuffRoutine);
                m_debuffSpeed = 100 / (100 + value);
                m_speedDebuffRoutine = StartCoroutine(RemoveDebuffRoutine(target, duration));
                break;
            case DebuffTarget.Defense:
                if (m_defendDebuffRoutine != null) StopCoroutine(m_defendDebuffRoutine);
                m_debuffDefend = 100 / (100 + value);
                m_defendDebuffRoutine = StartCoroutine(RemoveDebuffRoutine(target, duration));
                break;
            case DebuffTarget.Stun:
                if (m_stunDebuffRoutine != null) StopCoroutine(m_stunDebuffRoutine);
                m_isStunned = true;
                m_stunDebuffRoutine = StartCoroutine(RemoveDebuffRoutine(target, duration));
                break;
            case DebuffTarget.DotDamage:
                if (m_dotDamageDebuffRoutine != null) StopCoroutine(m_dotDamageDebuffRoutine);
                m_debuffDotDamage = 0.05f * value;
                m_dotDamageDebuffRoutine = StartCoroutine(DotDamageDebuffRoutine(m_debuffDotDamage, duration));
                break;
            case DebuffTarget.Weak:
                if (m_weakDebuffRoutine != null) StopCoroutine(m_weakDebuffRoutine);
                m_debuffWeak = 1 + (0.01f * value);
                m_weakDebuffRoutine = StartCoroutine(RemoveDebuffRoutine(target, duration));
                break;
            case DebuffTarget.Push:
                if (m_pushDebuffRoutine != null) StopCoroutine(m_pushDebuffRoutine);
                m_pushDebuffRoutine = StartCoroutine(PushDebuffRoutine(value, duration)); // 고정값 대신 인자 전달
                break;
        }
    }

    private IEnumerator RemoveDebuffRoutine(DebuffTarget target, float duration)
    {
        yield return new WaitForSeconds(duration);

        switch (target)
        {
            case DebuffTarget.Slow: m_debuffSpeed = 1f; m_speedDebuffRoutine = null; break;
            case DebuffTarget.Defense: m_debuffDefend = 1f; m_defendDebuffRoutine = null; break;
            case DebuffTarget.Stun: m_isStunned = false; m_stunDebuffRoutine = null; break;
            case DebuffTarget.DotDamage: m_debuffDotDamage = 0f; m_dotDamageDebuffRoutine = null; break;
            case DebuffTarget.Weak: m_debuffWeak = 1f; m_weakDebuffRoutine = null; break;
        }
    }

    private void ClearAllDebuff()
    {
        if (m_speedDebuffRoutine != null) StopCoroutine(m_speedDebuffRoutine);
        if (m_defendDebuffRoutine != null) StopCoroutine(m_defendDebuffRoutine);
        if (m_stunDebuffRoutine != null) StopCoroutine(m_stunDebuffRoutine);
        if (m_weakDebuffRoutine != null) StopCoroutine(m_weakDebuffRoutine);
        if (m_pushDebuffRoutine != null) StopCoroutine(m_pushDebuffRoutine);
        if (m_heatwaveDebuffRoutine != null) StopCoroutine(m_heatwaveDebuffRoutine);

        m_speedDebuffRoutine = null; m_defendDebuffRoutine = null; m_stunDebuffRoutine = null;
        m_weakDebuffRoutine = null; m_pushDebuffRoutine = null; m_heatwaveDebuffRoutine = null;

        m_debuffStacks.Clear(); // 스택 딕셔너리 초기화

        m_debuffSpeed = 1f; m_debuffDefend = 1f; m_debuffWeak = 1f;
        m_isStunned = false; m_isPushed = false;
    }

    private System.Collections.IEnumerator DotDamageDebuffRoutine(float value, float duration)
    {
        float timer = 0f;
        float interval = 0.25f;

        while (timer < duration)
        {
            yield return new WaitForSeconds(interval);
            TakeDamage(value);
            timer += interval;
        }
        m_debuffDotDamage = 0f;
        m_dotDamageDebuffRoutine = null;
    }

    private IEnumerator PushDebuffRoutine(float pushSpeed, float fixedDuration)
    {
        m_isPushed = true;
        float timer = 0f;

        while (timer < fixedDuration)
        {
            Vector3 backwardTarget;
            if (wavePointIndex > 0) backwardTarget = WayPointController.points[wavePointIndex - 1].position;
            else backwardTarget = WayPointController.points[0].position;

            float distanceToBackward = Vector3.Distance(transform.position, backwardTarget);
            float moveStep = 0.05f * EnemyData.Speed * pushSpeed * Time.deltaTime;

            if (distanceToBackward <= moveStep)
            {
                transform.position = backwardTarget;
                if (wavePointIndex > 0)
                {
                    wavePointIndex--;
                    target = WayPointController.points[wavePointIndex];
                }
            }
            else
            {
                Vector3 pushDirection = (backwardTarget - transform.position).normalized;
                transform.position += pushDirection * moveStep;
            }

            timer += Time.deltaTime;
            yield return null;
        }
        m_isPushed = false;
        m_pushDebuffRoutine = null;
    }

    private IEnumerator HeatWaveRoutine()
    {
        m_synergySlow = 0.5f;
        m_synergyWeak = 1.5f;

        while (TowerManager.Instance != null && TowerManager.Instance.IsHeatWaveActive)
        {
            TakeDamage(m_enemyStats.MaxHP * 0.01f);
            yield return new WaitForSeconds(1f);
        }
        m_synergySlow = 1f;
        m_synergyWeak = 1f;
        m_heatwaveDebuffRoutine = null;
    }

    private protected IEnumerator DisableUIAfterDelayRoutine(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        if (HPBar != null) HPBar.SetActive(false);
    }

    public void HideHPBar()
    {
        if (HPBar != null && HPBar.activeSelf) HPBar.SetActive(false);
        if (DamageAfter3s != null)
        {
            StopCoroutine(DamageAfter3s);
            DamageAfter3s = null;
        }
    }
}