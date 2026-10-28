using System.Collections.Generic;

namespace SquareTD
{
    // 물 광역 — 쓰나미 (20번 4-10장)
    //   맵 전체의 적에게 Power × AbilityValue (스택을 쓰면 ×). 맞은 적의 최소·최대 이동력 −2, 그 적의 다음 행동 한 번만
    //   이동력 감소는 (100 − 저항력)% 확률로 걸림. 피해는 저항과 무관. 실제로 걸린 타격만 "이동력을 바꾼 공격" (젖음이 전부 받음)
    //   물 타워끼리: 물결 존에 서 있으면 쓰나미 피해 ×1.25, 해류 존에서는 ×1.5 (스택 배율과 따로 곱함)
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
