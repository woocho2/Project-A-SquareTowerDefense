using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 불 단일 타워의 스킬.
/// 적중을 Duration 회(컬러 강화가 레벨만큼 줄여 둔 값) 채울 때마다 치명타 확률과 치명타 피해가 AbilityValue%p 만큼 오르는 스택을 하나 얻습니다.
/// 스택은 (티어 × 컬러 강화 레벨) 적 턴 동안 유지됩니다. 예: 골드(3) × 레벨 2 = 6턴. 범위는 1~25턴.
/// 불 버프의 예열을 받으면 공격 횟수 +1, 과열이면 +2를 추가로 받습니다.
/// </summary>
public class FireTargetSkill : TowerSkill
{
    private sealed class CriticalStack
    {
        public int RemainingTurns;
        public float Bonus;
    }

    private readonly List<CriticalStack> m_stacks = new List<CriticalStack>();
    private int m_hitCount;

    public FireTargetSkill(TowerController owner, TowerData data) : base(owner, data) { }

    public override void OnEnemyTurnStart()
    {
        for (int i = m_stacks.Count - 1; i >= 0; i--)
        {
            if (--m_stacks[i].RemainingTurns <= 0)
            {
                m_stacks.RemoveAt(i);
            }
        }
    }

    public override void OnPrimaryTargetHit()
    {
        m_hitCount++;
        // 컬러 강화가 Duration을 레벨만큼 나눠 두므로, 레벨이 오를수록 발동에 필요한 적중 횟수가 줄어듭니다 (올림).
        int level = Mathf.Clamp(m_owner.BaseStats.Level, 1, 5);
        int threshold = Mathf.Max(1, Mathf.CeilToInt(m_owner.BaseStats.Duration - 0.0001f));
        if (m_hitCount < threshold) return;

        m_hitCount = 0;

        // 스택은 공격 도중에 생기므로, 얻은 턴은 세지 않고 그 다음 턴부터 (티어 × 레벨) 턴 동안 유지합니다.
        int durationTurns = Mathf.Clamp(m_data.Tier, 1, 5) * level;
        m_stacks.Add(new CriticalStack
        {
            RemainingTurns = durationTurns + 1,
            Bonus = Mathf.Max(0f, m_owner.BaseStats.AbilityValue) / 100f
        });
    }

    public override void ModifyFinalStats(ref TowerStats finalStats)
    {
        float bonus = 0f;
        for (int i = 0; i < m_stacks.Count; i++)
        {
            bonus += m_stacks[i].Bonus;
        }
        finalStats.CriticalRate += bonus;
        finalStats.CriticalDamage += bonus;

        // 불 문양끼리의 시너지: 불 버프 타워가 걸어 준 예열·과열 상태를 읽어 공격 횟수를 늘립니다.
        if (m_owner.IsBuffTriggered(BuffTarget.Fire))
        {
            finalStats.AttackCount += 2;
        }
        else if (m_owner.IsBuffActive(BuffTarget.Fire))
        {
            finalStats.AttackCount += 1;
        }
    }
}
