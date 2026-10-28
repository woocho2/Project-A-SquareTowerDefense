using System.Collections.Generic;

namespace SquareTD
{
    // 방패 광역 — 배쉬 (20번 4-7장)
    //   착탄 범위(Mastery) 안의 적에게 Power × AbilityValue (스택을 쓰면 ×), 맞은 적을 20% 확률로 1행동 기절
    //   서프레스·락다운인 적은 기절 확률이 더 오름 (+5% / +10%, 적 쪽에서 더함 — EnemyController.TryStun)
    public class ShieldSplashSkill : AttackSkill
    {
        public const float StunChance = 0.2f;

        public ShieldSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            if (mainTarget == null) return new List<ITarget>();
            return SkillArea.Impact(m_grid, m_occupancy, mainTarget.PathIndex, owner.Stats.Mastery);
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            DamageInfo bash = hit;
            bash.StunChance = StunChance;
            ProjectileHitResolver.HitSingle(target, bash);
        }
    }
}
