using System.Collections.Generic;

namespace SquareTD
{
    // 전기 단일 — 과부하
    public class ElectricTargetSkill : AttackSkill
    {
        public ElectricTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        public static int GetChainCount(TowerController owner) => 1 + owner.Stats.ColorLevel * owner.Stats.Mastery;

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget)
        {
            return SkillArea.Chain(m_grid, m_occupancy, mainTarget, GetChainCount(owner), ChargeEffect.Get);
        }

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            float charge = ChargeEffect.Get(target);
            if (EmblemDebuffEffect.StageOn(target, TowerEmblem.Electricity) == ZoneStage.Skill2) charge *= 2f;

            float zone = 1f;
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Electricity);
            if (stage == ZoneStage.Skill2) zone = 2f;
            else if (stage == ZoneStage.Skill1) zone = 1.5f;

            return (1f + charge * 0.01f) * zone;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            ChargeEffect.Clear(target);
        }
    }
}
