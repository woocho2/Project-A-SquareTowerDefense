using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 창 광역 — 용맹한 깃창 (20번 4-6장, 2026-10-28 기획 변경: 줄 공격 → 아군 강화)
    //   피해 없이 깃창을 세워 사거리 안의 아군 공격 타워(자기 포함)를 북돋움
    //   공격력 +(Power × AbilityValue)%, 치명 확률 +(Power × AbilityValue)% — Power·AV는 깃창 타워의 값. 스택을 쓰면 둘 다 × (1 + Stack × 0.01 × StackValue)
    //   받은 타워가 (깃창 타워의 컬러 강화 레벨 × 2)번 행동하는 동안 (TimedTowerBuff). 다시 받으면 겹치지 않고 처음부터
    public class SpearSplashSkill : AttackSkill
    {
        public const string Source = "용맹한 깃창";

        private readonly TowerSlots m_slots;

        public SpearSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes, TowerSlots slots)
            : base(grid, occupancy, stackModes)
        {
            m_slots = slots;
        }

        public override bool NeedsLivingTarget => false;

        protected override bool CastInstead(TowerController owner, ITarget mainTarget, bool useStack)
        {
            TimedTowerBuff.Remove(owner.State, Source);     // 먼저 떼야 지난 깃창으로 오른 공격력이 이번 식에 끼지 않음
            TowerFinalStats stats = owner.Stats;

            float percent = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);     // 브론즈 10 × 2 = 20(%)
            if (useStack) percent = SkillFormula.WithStack(percent, owner.State.Stack.Max, stats.StackValue);

            foreach (TowerController ally in GetAlliesInRange(owner, stats.Range))
            {
                TimedTowerBuff.Apply(ally.State, Source, stats.ColorLevel * 2,
                    StatModifier.Percent(Source, StatType.Power, percent / 100f),
                    StatModifier.Flat(Source, StatType.CritRate, percent / 100f));     // 치명 확률은 0 ~ 1, 100%를 넘으면 계산기가 1로 자름
            }
            return true;
        }

        // 이 타워의 스폰 칸에서 타일 거리 range 이내에 선 공격 타워 (자기 포함). 판이 없으면(테스트) 자기만
        private List<TowerController> GetAlliesInRange(TowerController owner, int range)
        {
            var allies = new List<TowerController>();
            if (m_slots != null)
            {
                Vector3Int center = m_grid.GetSpawnCell(owner.SlotIndex);
                for (int i = 0; i < m_slots.Count; i++)
                {
                    TowerController tower = m_slots.Get(i);
                    if (tower == null || !tower.IsAttackTower) continue;            // 버프·디버프 타워는 공격 스탯이 없음
                    if (BoardGrid.GetTileDistance(center, m_grid.GetSpawnCell(i)) <= range) allies.Add(tower);
                }
            }
            if (!allies.Contains(owner)) allies.Add(owner);
            return allies;
        }

        // 피해가 없는 스킬 (CastInstead가 true라 Fire가 여기까지 오지 않음)
        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => new List<ITarget>();
        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit) { }
    }
}
