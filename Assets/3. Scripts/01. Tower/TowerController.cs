using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct TowerStats
{
    public int ID;
    public string Name;
    public int Level;
    public float AttackPower;
    public float Range;
    [FormerlySerializedAs("AttackSpeed")]
    public int AttackCount;
    public float CriticalRate;
    public float CriticalDamage;
    public float Duration;
    public float AbilityValue;
    public float ProjectileSpeed;
    public int ProjectileRadius;
    public int AdditionalHitCount;
}

public class TowerController : MonoBehaviour
{
    [SerializeField] private TowerData m_towerData;
    [SerializeField] private TowerStats m_baseStats;
    [SerializeField] private GameObject m_range;
    [SerializeField] private TowerVisual m_towerVisual;

    private Vector3 m_rangeBaseLocalScale;
    private readonly List<GameObject> m_enemyRangeHighlights = new List<GameObject>();

    private TowerAttackAction m_attackAction;
    private int m_remainingAction;

    [Header("Debuff Zone Reference")]
    [SerializeField] private DebuffZone m_debuffZoneChild;

    // 런타임 보정치
    private float m_bonusAttackPower = 1f;
    private float m_bonusRange = 1f;
    private float m_bonusAttackCount = 1f;
    private float m_bonusCriticalRate = 1f;
    private float m_bonusCriticalDamage = 1f;
    private float m_bonusDuration = 0f;
    private float m_bonusAbilityValue = 1f;

    private Coroutine m_buffRoutine;

    private void Awake()
    {
        if (m_towerVisual == null)
        {
            m_towerVisual = GetComponentInChildren<TowerVisual>(true);
        }

        if (m_range != null)
        {
            m_rangeBaseLocalScale = m_range.transform.localScale;
        }

        if (m_debuffZoneChild == null)
        {
            m_debuffZoneChild = GetComponentInChildren<DebuffZone>(true);
        }
    }

    public void Init(TowerData data, TowerStats initialStats)
    {
        m_towerData = data;
        m_baseStats = initialStats;
        m_remainingAction = Mathf.Max(1, m_towerData.action);

        m_towerVisual?.Apply(m_towerData);

        if (m_debuffZoneChild != null)
        {
            m_debuffZoneChild.gameObject.SetActive(false);
        }

        switch (m_towerData.attackType)
        {
            case AttackType.Splash:
                m_attackAction = new SplashAttackAction(m_towerData);
                break;

            case AttackType.Target:
                m_attackAction = new TargetAttackAction(m_towerData);
                break;

            case AttackType.Buff:
                m_attackAction = new BuffAction(m_towerData);
                break;

            case AttackType.Debuff:
                var debuffAction = new DebuffAction(m_towerData);
                if (m_debuffZoneChild != null)
                {
                    debuffAction.BindZone(m_debuffZoneChild, transform.position, GetFinalStats());
                }
                m_attackAction = debuffAction;
                break;
        }
    }

    public void UpdateBaseStats(TowerStats newStats)
    {
        m_baseStats = newStats;
    }

    /// <summary>
    /// 전역 기본 스탯에 런타임 보정치를 반영한 최종 스탯을 반환합니다.
    /// </summary>
    public TowerStats GetFinalStats()
    {
        // 1. 전역 기본 스탯을 복사합니다.
        TowerStats finalStats = m_baseStats;

        // 2. 버프와 타일 보정치를 반영합니다.
        finalStats.AttackPower *= Mathf.Clamp(m_bonusAttackPower, 0.01f, 10000f);
        finalStats.AttackCount = Mathf.Max(1, Mathf.RoundToInt(finalStats.AttackCount * Mathf.Clamp(m_bonusAttackCount, 0.01f, 10000f)));
        finalStats.Range *= Mathf.Clamp(m_bonusRange, 0.01f, 10000f);
        finalStats.CriticalRate *= Mathf.Clamp(m_bonusCriticalRate, 0.01f, 10000f);
        finalStats.CriticalDamage *= Mathf.Clamp(m_bonusCriticalDamage, 0.01f, 10000f);
        finalStats.Duration += m_bonusDuration;
        finalStats.AbilityValue *= Mathf.Clamp(m_bonusAbilityValue, 0.01f, 10000f);

        if (finalStats.Range <= 1f) finalStats.Range = 1f;
        if (finalStats.CriticalRate >= 1f) finalStats.CriticalRate = 1f;

        return finalStats;
    }

    /// <summary>
    /// 에너미 턴에 한 번 호출됩니다. 행동력을 1 소모하고,
    /// 0이 된 턴에 AttackCount만큼 공격한 뒤 행동력을 재충전합니다.
    /// </summary>
    public bool ExecuteTurnAction()
    {
        if (m_attackAction == null || m_towerData == null) return false;

        m_remainingAction--;
        if (m_remainingAction > 0) return false;

        TowerStats finalStats = GetFinalStats();
        bool didAttack = false;
        for (int i = 0; i < finalStats.AttackCount; i++)
        {
            didAttack |= m_attackAction.ExecuteAction(transform, finalStats);
        }

        m_remainingAction = Mathf.Max(1, m_towerData.action);
        return didAttack;
    }
    public TowerData GetTowerData() => m_towerData;

    private void Update()
    {
        if (m_range == null || !m_range.activeInHierarchy || m_attackAction == null) return;

        RefreshEnemyRangeHighlights();
    }

    public void ShowRange(bool show)
    {
        if (m_range != null)
        {
            m_range.SetActive(show);

            if (show)
            {
                int tileRange = TowerAttackAction.ToTileRange(GetFinalStats().Range);
                float scaleValue = (tileRange * 2f + 1f) / transform.localScale.x;
                m_range.transform.localScale = new Vector3(scaleValue, scaleValue, 1f);
                RefreshEnemyRangeHighlights();
            }
            else
            {
                SetEnemyRangeHighlightsActive(false);
            }
        }
    }

    private void RefreshEnemyRangeHighlights()
    {
        if (m_towerData == null || m_range == null) return;

        Tilemap towerTilemap = TowerManager.Instance?.GetSpawnPointTilemap();
        SpriteRenderer rangeRenderer = m_range.GetComponent<SpriteRenderer>();
        if (towerTilemap == null || rangeRenderer == null)
        {
            SetEnemyRangeHighlightsActive(false);
            return;
        }

        int tileRange = TowerAttackAction.ToTileRange(GetFinalStats().Range);
        Vector3Int towerCell = towerTilemap.WorldToCell(transform.position);
        Vector3 cellSize = towerTilemap.cellSize;
        Vector2 squareSize = new Vector2(
            cellSize.x * (tileRange * 2 + 1),
            cellSize.y * (tileRange * 2 + 1));

        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(transform.position, squareSize, 0f, m_towerData.targetLayer);
        HashSet<Vector3Int> enemyCells = new HashSet<Vector3Int>();

        for (int i = 0; i < hitEnemies.Length; i++)
        {
            EnemyHealthController health = hitEnemies[i].GetComponentInParent<EnemyHealthController>();
            if (health == null || !health.gameObject.activeInHierarchy || health.CurrentHP <= 0f) continue;

            Vector3Int enemyCell = towerTilemap.WorldToCell(hitEnemies[i].transform.position);
            if (Mathf.Abs(enemyCell.x - towerCell.x) <= tileRange && Mathf.Abs(enemyCell.y - towerCell.y) <= tileRange)
            {
                enemyCells.Add(enemyCell);
            }
        }

        int requiredHighlightCount = enemyCells.Count;
        while (m_enemyRangeHighlights.Count < requiredHighlightCount)
        {
            GameObject highlight = Instantiate(m_range, m_range.transform.parent);
            highlight.name = "EnemyRangeHighlight";
            highlight.transform.localScale = m_rangeBaseLocalScale;

            SpriteRenderer highlightRenderer = highlight.GetComponent<SpriteRenderer>();
            highlightRenderer.color = new Color(1f, 0.2f, 0.2f, rangeRenderer.color.a);
            highlightRenderer.sortingLayerID = rangeRenderer.sortingLayerID;
            highlightRenderer.sortingOrder = rangeRenderer.sortingOrder + 1;

            m_enemyRangeHighlights.Add(highlight);
        }

        int highlightIndex = 0;
        foreach (Vector3Int enemyCell in enemyCells)
        {
            GameObject highlight = m_enemyRangeHighlights[highlightIndex++];
            highlight.transform.position = towerTilemap.GetCellCenterWorld(enemyCell);
            highlight.transform.localScale = m_rangeBaseLocalScale;
            highlight.SetActive(true);
        }

        for (int i = highlightIndex; i < m_enemyRangeHighlights.Count; i++)
        {
            m_enemyRangeHighlights[i].SetActive(false);
        }
    }

    private void SetEnemyRangeHighlightsActive(bool isActive)
    {
        for (int i = 0; i < m_enemyRangeHighlights.Count; i++)
        {
            m_enemyRangeHighlights[i].SetActive(isActive);
        }
    }

    public void OnMovedToNewPosition()
    {
        if (m_attackAction is DebuffAction && m_debuffZoneChild != null)
        {
            m_debuffZoneChild.UpdateTowerPosition(transform.position, GetFinalStats().Range);
        }
    }

    public void ApplyBuff(BuffTarget target, float abilityValue, float duration)
    {
        if (m_buffRoutine != null) StopCoroutine(m_buffRoutine);

        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f + (abilityValue / 100f); break;
            case BuffTarget.Bow:
            case BuffTarget.AttackCount: m_bonusAttackCount = 1f + (abilityValue / 100f); break;
            case BuffTarget.Spear: m_bonusRange = 1f + (abilityValue / 100f); break;
            case BuffTarget.Axe: m_bonusCriticalRate = 1f + (abilityValue / 100f); break;
            case BuffTarget.Hammer: m_bonusCriticalDamage = 1f + (abilityValue / 100f); break;
        }

        m_buffRoutine = StartCoroutine(RemoveBuffRoutine(target, duration));
    }

    private System.Collections.IEnumerator RemoveBuffRoutine(BuffTarget target, float duration)
    {
        yield return new WaitForSeconds(duration);

        switch (target)
        {
            case BuffTarget.Sword: m_bonusAttackPower = 1f; break;
            case BuffTarget.Bow:
            case BuffTarget.AttackCount: m_bonusAttackCount = 1f; break;
            case BuffTarget.Spear: m_bonusRange = 1f; break;
            case BuffTarget.Axe: m_bonusCriticalRate = 1f; break;
            case BuffTarget.Hammer: m_bonusCriticalDamage = 1f; break;
        }

        m_buffRoutine = null;
    }
}
