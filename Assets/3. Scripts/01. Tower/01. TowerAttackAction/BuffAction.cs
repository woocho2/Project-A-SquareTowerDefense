using UnityEngine;

public class BuffAction : TowerAttackAction
{
    public BuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
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
                // TowerController의 변경된 ApplyBuff 메서드 호출
                targetTower.ApplyBuff(m_data.buffTarget, currentStats.AbilityValue, currentStats.Duration);
                appliedAny = true;
            }
        }

        return appliedAny;
    }
}