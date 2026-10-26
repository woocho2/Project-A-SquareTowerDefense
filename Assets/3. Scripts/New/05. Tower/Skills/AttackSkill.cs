using System.Collections.Generic;
using UnityEngine;

namespace SquareTD
{
    // 공격 타워의 스킬 2 베이스 (20번 4-1장, 23번 4-4) — 템플릿 메서드
    //   Fire가 순서를 쥠: 스택을 쓸지 → 피해(공통 식) → 치명 1번 → 범위 → 대상마다 → 스택 비우기
    //   문양 스킬(수업 27부터)은 빈칸 둘(abstract)을 꼭 채우고, 훅(virtual)은 필요한 것만 덮어씀
    public abstract class AttackSkill
    {
        protected readonly BoardGrid m_grid;
        protected readonly PathOccupancy m_occupancy;
        private readonly StackModeSettings m_stackModes;

        protected AttackSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
        {
            m_grid = grid;
            m_occupancy = occupancy;
            m_stackModes = stackModes;
        }

        // ── 템플릿: 순서는 여기서만 정함. 문양 스킬은 이 함수를 덮어쓰지 않음 ──
        public int Fire(TowerController owner, ITarget mainTarget)     // 💬 void → int: 맞힌 대상 수를 돌려줌 (4b에서 바꿈)
        {
            TowerFinalStats stats = owner.Stats;
            TowerStack stack = owner.State.Stack;

            bool useStack = m_stackModes != null && m_stackModes.ShouldUseStack(owner.Definition, stack);

            float damage = SkillFormula.AttackSkill(stats.Power, stats.AbilityValue);
            if (useStack) damage = SkillFormula.WithStack(damage, stack.Max, stats.StackValue);

            bool isCritical = RollCritical(owner, stats);
            List<ITarget> area = GetArea(owner, mainTarget);
            int hitCount = 0;                               // 💬 몇 명을 맞혔나 셈

            foreach (ITarget target in area)
            {
                if (!target.IsAlive) continue;

                DamageInfo hit = MakeSkillHit(owner, stats, target, damage * ModifyOutgoingDamage(owner, target), isCritical);

                OnHitTarget(owner, target, hit);
                hitCount++;                                 // 💬 한 명 맞힐 때마다 +1
            }
            if (useStack && hitCount > 0) stack.Consume();  // 💬 아무도 못 맞혔으면 다 찬 스택을 버리지 않고 남김
            return hitCount;                                // 💬 0이면 광역 타워가 "발동 안 한 것"으로 보고 다음 턴에 다시 함
        }

        protected abstract List<ITarget> GetArea(TowerController owner, ITarget mainTarget);           // 맵 전체 / 착탄 / 줄 / 하나 / 연쇄
        protected abstract void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit);     // 피해 + 부가 효과(기절, 화상 …)

        protected virtual bool RollCritical(TowerController owner, TowerFinalStats stats) => CriticalRoll.Roll(stats.CriticalRate);   // 도끼 강타·바람 칼바람: 반드시 치명
        protected virtual float ModifyOutgoingDamage(TowerController owner, ITarget target) => 1f;    // 불·바람·대지·전기·빛·어둠: 대상 상태에 따른 배율
        public virtual void OnBasicHit(TowerController owner, ITarget target) { }                     // 바람 광역(당기기), 전기 광역(전하), 빛 단일(도깨비불)
        public virtual void OnOwnerActed(TowerController owner) { }                                   // 도끼·바람 단일: 자기 버프 유지 감소
        public virtual void OnEnemyTurnStart(TowerController owner) { }                               // ⓪
        // 수업 31: ModifyOwnerStats — 자기 강화(쪼개기, 폭풍, 성장)를 최종 스탯에

        protected DamageInfo MakeSkillHit(TowerController owner, TowerFinalStats stats, ITarget target, float damage, bool isCritical)
        {
            float critMultiplier = isCritical ? CriticalRoll.Multiplier(stats.CriticalDamage) : 1f;
            Vector3Int from = m_grid.GetSpawnCell(owner.SlotIndex);
            Vector3Int to = m_grid.GetPathCell(target.PathIndex);

            return new DamageInfo
            {
                Attacker = owner,
                Source = DamageSource.Skill,
                Emblem = owner.Definition.Emblem,
                Attribute = owner.Definition.Attribute,
                BaseDamage = DamageCalculator.GetOutgoing(damage, 1f, critMultiplier),
                IsCritical = isCritical,
                CritMultiplier = critMultiplier,
                Distance = BoardGrid.GetTileDistance(from, to),
            };
        }
    }
}
