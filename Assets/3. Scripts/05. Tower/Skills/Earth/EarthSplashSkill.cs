using System.Collections.Generic;

namespace SquareTD
{
    // 대지 광역 — 대지의 분노
    public class EarthSplashSkill : AttackSkill
    {
        public const string GrowthSource = "대지의 분노 성장";

        public EarthSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            if (EmblemDebuffEffect.StageOn(target, TowerEmblem.Earth) == ZoneStage.None || target.Stats == null) return 1f;
            return 1f + RaisedDefense(target.Stats) * 0.001f;
        }

        public static float RaisedDefense(EnemyStats stats)
        {
            float percent = 0f, flat = 0f;
            foreach (StatModifier modifier in stats.Modifiers.All)
            {
                if (modifier.Stat != StatType.Defense || modifier.Value <= 0f) continue;
                if (modifier.IsPercent) percent += modifier.Value;
                else flat += modifier.Value;
            }
            return stats.GetBase(StatType.Defense) * (1f + percent) + flat;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
        }

        protected override void OnFired(TowerController owner, int hitCount)
        {
            Grow(owner, GrowthSource, StatType.AbilityValue, GrowthPerUse(owner));
        }

        public static float GrowthPerUse(TowerController owner)
        {
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Earth);
            if (stage == ZoneStage.Skill2) return 0.02f;
            if (stage == ZoneStage.Skill1) return 0.015f;
            return 0.01f;
        }
    }
}
