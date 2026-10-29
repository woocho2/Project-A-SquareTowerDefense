using System.Collections.Generic;

namespace SquareTD
{
    // 얼음 광역 — 블리자드
    public class IceSplashSkill : AttackSkill
    {
        public IceSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool freezes = enemy != null && EmblemDebuffEffect.StageOn(target, TowerEmblem.Ice) != ZoneStage.None;

            DamageInfo blizzard = hit;
            blizzard.ChangesMovement = freezes;
            ProjectileHitResolver.HitSingle(target, blizzard);

            if (freezes && target.IsAlive)
            {
                enemy.Status.Add(new FreezeEffect());
            }
        }
    }
}
