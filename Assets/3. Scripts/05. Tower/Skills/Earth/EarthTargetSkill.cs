using System.Collections.Generic;

namespace SquareTD
{
    // 대지 단일 — 씨뿌리기 (20번 4-11장)
    //   그 적에게 Power × AbilityValue (스택을 쓰면 ×). 쓸 때마다 이 타워의 Power +1% (복리가 아님, 판 위에 있는 동안 계속)
    //   대지 타워끼리: 성장 존에 서 있으면 한 번에 +1.5%, 만개 존에서는 +2% (광역과 같은 값)
    //     진흙인 적을 공격하면 최소 이동력 −1, 석화면 −2 (기본 공격과 씨뿌리기 모두). 정수형이라 저항할 수 없음
    //     그 적이 (레벨 × 2)번 행동하는 동안. 겹치지 않음 (다시 맞으면 유지만 처음부터)
    public class EarthTargetSkill : AttackSkill
    {
        public const string GrowthSource = "씨뿌리기 성장";
        public const string BuryKey = "파묻힘";

        public EarthTargetSkill(BoardGrid grid, PathOccupancy occupancy, StackModeSettings stackModes)
            : base(grid, occupancy, stackModes) { }

        protected override List<ITarget> GetArea(TowerController owner, ITarget mainTarget) => SkillArea.Single(mainTarget);

        protected override void OnHitTarget(TowerController owner, ITarget target, DamageInfo hit)
        {
            ProjectileHitResolver.HitSingle(target, hit);
            // [B] 씨뿌리기도 진흙·석화인 적을 파묻음 — 바로 아래 OnBasicHit이 부르는 함수를 같은 인자로
        }

        public override void OnBasicHit(TowerController owner, ITarget target) => Bury(owner, target);

        // 진흙 −1, 석화 −2 (최소 이동력)
        public static void Bury(TowerController owner, ITarget target)
        {
            EnemyController enemy = target?.Body;
            if (enemy == null || !enemy.IsAlive) return;

            ZoneStage mud = EmblemDebuffEffect.StageOn(target, TowerEmblem.Earth);
            if (mud == ZoneStage.None) return;

            int amount = mud == ZoneStage.Skill2 ? 2 : 1;
            enemy.Status.Add(new TimedModifierEffect(BuryKey, owner.Stats.ColorLevel * 2, amount, $"최소 이동력 −{amount}",
                StatModifier.Flat(BuryKey, StatType.SpeedMin, -amount)));
        }

        protected override void OnFired(TowerController owner, int hitCount)
        {
            Grow(owner, GrowthSource, StatType.Power, EarthSplashSkill.GrowthPerUse(owner));
        }
    }
}
