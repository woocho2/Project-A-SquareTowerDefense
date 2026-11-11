using System.Collections.Generic;

namespace SquareTD
{
    // 해머 광역 — 저지먼트 (착탄 범위의 적에게 Power × AbilityValue, 맞은 적을 착탄 칸의 맞은편 칸으로 날려 보내 그 칸의 타일 효과를 받게 함)
    public class HammerSplashSkill : AttackSkill
    {
        private int m_landingIndex;

        public HammerSplashSkill(BoardGrid grid, PathOccupancy occupancy)
            : base(grid, occupancy) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            m_landingIndex = m_grid.GetBifrostPartner(mainTarget.PathIndex);
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool knocks = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(1f));

            DamageInfo judgment = hit;
            judgment.ChangesMovement = knocks;
            ProjectileHitResolver.HitSingle(target, judgment);
            if (!knocks || !enemy.IsAlive || enemy.Movement == null) return;

            enemy.Movement.TeleportTo(m_landingIndex);
            EnemyTileEffectApplier.ApplyForcedStop(enemy);
        }
    }
}
