using System.Collections.Generic;

namespace SquareTD
{
    // 물 광역 — 쓰나미
    public class WaterSplashSkill : AttackSkill
    {
        public const string Key = "쓰나미";
        public const int SpeedCut = 2;

        public WaterSplashSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.WholeMap(m_grid, m_occupancy);

        protected override float ModifyOutgoingDamage(TowerController owner, ITarget target)
        {
            ZoneStage stage = BuffRule.ReceivedStage(owner, TowerEmblem.Water);
            if (stage == ZoneStage.Skill2) return 1.5f;
            if (stage == ZoneStage.Skill1) return 1.25f;
            return 1f;
        }

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            EnemyController enemy = target.Body;
            bool slows = enemy != null && enemy.IsAlive && Chance.Roll(enemy.Resistance.ResistWeak(1f));

            DamageInfo wave = hit;
            wave.ChangesMovement = slows;
            ProjectileHitResolver.HitSingle(target, wave);

            if (slows && enemy.IsAlive)
            {
                enemy.Status.Add(new TimedModifierEffect(Key, 1, SpeedCut, $"다음 행동 이동력 −{SpeedCut}",
                    StatModifier.Flat(Key, StatType.SpeedMin, -SpeedCut),
                    StatModifier.Flat(Key, StatType.SpeedMax, -SpeedCut)));
            }
        }
    }
}
