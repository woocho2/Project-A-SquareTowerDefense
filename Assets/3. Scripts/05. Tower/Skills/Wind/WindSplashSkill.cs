using System.Collections.Generic;

namespace SquareTD
{
    // 바람 광역 — 칼바람 (20번 4-12장)
    //   기본 공격과 같은 착탄 범위 안의 적에게, 반드시 치명. 총 피해 = Power × AbilityValue × 치명 배율 (스택을 쓰면 ×)
    //   총 피해를 범위 안의 적 수만큼 나눠 줌 → 적이 적을수록 한 마리가 세게 맞음
    //   역풍인 적은 그 2배, 난기류인 적은 3배
    //   바람 타워끼리: 역풍인 적을 기본 공격으로 맞히면 다음 칼바람까지의 적중 수를 1 당기고, 난기류면 2 당김 (맞힌 적마다 전부 더함)
    public class WindSplashSkill : AttackSkill
    {
        private int m_areaCount = 1;                // 이번 칼바람의 범위 안 적 수 (나눌 값)

        public WindSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            List<ITarget> area = SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);

            m_areaCount = 0;
            foreach (ITarget target in area)
            {
                if (target.IsAlive) m_areaCount++;
            }
            if (m_areaCount < 1) m_areaCount = 1;
            return area;
        }

        protected override bool RollCritical(TowerController owner, TowerFinalStats stats, ITarget mainTarget) => true;     // 반드시 치명

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            return WindMultiplier(target) / m_areaCount;
        }

        private static float WindMultiplier(ITarget target)
        {
            ZoneStage wind = EmblemDebuffEffect.StageOn(target, TowerEmblem.Wind);
            if (wind == ZoneStage.Skill2) return 3f;
            if (wind == ZoneStage.Skill1) return 2f;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            // [B] 피해 주기 — SwordSplashSkill.OnHitTarget과 똑같은 한 줄
        }

        // 기본 공격이 역풍·난기류인 적에게 맞으면 칼바람을 당김 (맞힌 적마다)
        public override void OnBasicHit(TowerController owner, ITarget target)
        {
            ZoneStage wind = EmblemDebuffEffect.StageOn(target, TowerEmblem.Wind);
            if (wind == ZoneStage.None) return;
            owner.AdvanceSkillCount(wind == ZoneStage.Skill2 ? 2 : 1, target);
        }
    }
}
