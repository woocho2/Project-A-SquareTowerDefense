using UnityEngine;

public class BuffAction : TowerAttackAction
{
    public BuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        int sourceTier = Mathf.Clamp(m_data.towerID / 1000, 1, 5);
        TowerController sourceTower = towerTransform.GetComponent<TowerController>();

        if (m_data.buffTarget == BuffTarget.Shield)
        {
            // 쉴드 타워는 행동할 때마다 자신의 티어를 최대치로 하여 쉴드 1을 충전합니다.
            GameManager.Instance?.AddShield(
                towerTransform.GetInstanceID(),
                sourceTier);
            return true;
        }

        // Earth는 TowerController의 100초 타이머에서 전역 강화하므로, 주변 타워에 일반 버프를 적용하지 않습니다.
        if (m_data.buffTarget == BuffTarget.Earth) return true;

        // 버프 범위 내의 아군 타워 콜라이더 탐색
        Collider2D[] hitTowers = Physics2D.OverlapCircleAll(towerTransform.position, currentStats.Range, m_data.targetLayer);

        if (hitTowers.Length == 0) return false;

        bool appliedAny = false;

        for (int i = 0; i < hitTowers.Length; i++)
        {
            Collider2D hit = hitTowers[i];

            // 자기 자신 타워는 제외
            if (hit.transform == towerTransform) continue;

            if (hit.TryGetComponent<TowerController>(out var targetTower))
            {
                // 버프 타워 자신의 티어를 기준으로 특수 버프 수치를 계산합니다.
                targetTower.ApplyBuff(
                    m_data.buffTarget,
                    sourceTower,
                    towerTransform.GetInstanceID(),
                    sourceTier,
                    currentStats.AbilityValue,
                    currentStats.Duration);
                appliedAny = true;
            }
        }

        return appliedAny;
    }
}
