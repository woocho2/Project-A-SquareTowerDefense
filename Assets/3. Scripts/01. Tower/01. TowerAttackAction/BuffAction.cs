using UnityEngine;
using System.Collections.Generic;

public class BuffAction : TowerAttackAction
{
    public BuffAction(TowerData data) : base(data) { }

    public override bool ExecuteAction(Transform towerTransform, TowerStats currentStats)
    {
        int sourceTier = Mathf.Clamp(m_data.Tier, 1, 5);
        if (m_data.buffTarget == BuffTarget.Shield)
        {
            GameManager.Instance?.AddShield(1);
            return true;
        }

        // Earth는 웨이브가 끝날 때 TowerManager가 따로 처리하므로(대지 타워의 티어 강화), 주변 타워에 일반 버프를 적용하지 않습니다.
        if (m_data.buffTarget == BuffTarget.Earth) return true;

        if (TileManager.Instance == null || !towerTransform.TryGetComponent(out TowerController sourceTower) ||
            sourceTower.SpawnIndex < 1) return false;

        // 버프의 대상은 콜라이더나 현재 설치된 타워가 아니라, 사거리 안의 모든 타워 스폰 타일입니다.
        // 타워가 나중에 그 타일로 이동/소환되어도 그 즉시 같은 효과를 읽습니다.
        int sourceID = towerTransform.GetInstanceID();
        TileManager.Instance.RemoveTowerBuffEffectsBySource(sourceID);
        bool appliedAny = false;
        List<int> affectedIndices = TileManager.Instance.GetTowerSpawnIndicesInRange(
            sourceTower.SpawnIndex, ToTileRange(currentStats.Range));

        for (int i = 0; i < affectedIndices.Count; i++)
        {
            // 새 틀(스킬 1 + 스킬 2)로 옮긴 문양은 아래에서 스택형 버프로 따로 기록합니다.
            if (m_data.buffTarget == BuffTarget.Fire) continue;

            TileManager.Instance.AddTowerBuffEffect(
                affectedIndices[i], sourceID, m_data.buffTarget, sourceTier,
                currentStats.AttackPower, currentStats.Duration);
            appliedAny = true;
        }

        if (m_data.buffTarget == BuffTarget.Fire && affectedIndices.Count > 0)
        {
            // 스킬 1(예열): Duration 턴 동안 Power 만큼의 버프, 한 번 행동에 AttackCount 만큼 스택.
            // 스킬 2(과열): 스택이 60 ÷ 컬러 강화 레벨에 닿으면 Duration 턴 동안 Power × AbilityValue.
            TileManager.Instance.SetFirePreheatEffects(
                affectedIndices,
                sourceID,
                sourceTier,
                currentStats.AbilityValue,
                GetStackSkillThreshold(currentStats.Level),
                Mathf.RoundToInt(currentStats.Duration),
                currentStats.AttackPower,
                Mathf.Max(1, currentStats.AttackCount));
            appliedAny = true;
        }

        return appliedAny;
    }
}
